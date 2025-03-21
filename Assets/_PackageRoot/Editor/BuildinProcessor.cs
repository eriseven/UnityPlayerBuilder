using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using ProjectBuilder.Editor;
using UnityEditor;
using UnityEngine;

namespace PlayerBuilder.Eidtor
{
    public class ScriptDefineSymbolsProcessor : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 5000;

        public override async UniTask<int> Process(BuildConfig config)
        {
            Debug.Log($"ScriptDefineSymbolsProcessor");
            var defaultSymbols = new HashSet<string>(PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';'));
            var configSymbols = new HashSet<string>(config.scriptSymbols.Split(';'));
            var toRemove = new HashSet<string>(config.removeScriptSymbols.Split(';'));
            defaultSymbols.UnionWith(configSymbols);
            defaultSymbols.ExceptWith(toRemove);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defaultSymbols));
            return 0;
        }
    }

    public class PrePostprocessor : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 5001;

        public override async UniTask<int> Process(BuildConfig config)
        {
            Debug.Log($"PrePostprocessor");

            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;

            PlayerSettings.gcIncremental = true;

            var buildConfig = config;


            if (!string.IsNullOrEmpty(buildConfig.packageName))
            {
                PlayerSettings.applicationIdentifier = buildConfig.packageName;
            }

#if UNITY_ANDROID
            if (Enum.IsDefined(typeof(AndroidSdkVersions), buildConfig.minAndroidSdkVersion))
            {
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)buildConfig.minAndroidSdkVersion;
            }

            if (Enum.IsDefined(typeof(AndroidSdkVersions), buildConfig.targetAndroidSdkVersion))
            {
                PlayerSettings.Android.targetSdkVersion =
                    (AndroidSdkVersions)buildConfig.targetAndroidSdkVersion;
            }

            PlayerSettings.Android.useCustomKeystore = !string.IsNullOrEmpty(config.keystoreName);

            if (PlayerSettings.Android.useCustomKeystore)
            {
                PlayerSettings.Android.keystoreName = config.keystoreName;
                PlayerSettings.Android.keystorePass = config.keystorePass;

                PlayerSettings.Android.keyaliasName = config.keyaliasName;
                PlayerSettings.Android.keyaliasPass = config.keystorePass;
            }

            EditorUserBuildSettings.buildAppBundle = config.forceBuildAppBundle;
            PlayerSettings.Android.useAPKExpansionFiles = config.forceBuildAppBundle;

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
#endif
            return 0;
        }
    }

    public class BuildPlayer : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 7000;

        static string[] GetBuildScenes(BuildConfig config)
        {
            return EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        }

        static BuildOptions GetBuildOptions(BuildConfig config)
        {
            BuildOptions buidlOptions = BuildOptions.None;
            if (config.development)
            {
                buidlOptions |= BuildOptions.Development;
                buidlOptions |= BuildOptions.EnableDeepProfilingSupport;
                buidlOptions |= BuildOptions.AllowDebugging;
                var linkerFlagsWlStubGroupSize = "--linker-flags=-Wl,--stub-group-size=11534360";
                PlayerSettings.SetAdditionalIl2CppArgs(linkerFlagsWlStubGroupSize);
            }

            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                buidlOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
            }

            return buidlOptions;
        }

        public override async UniTask<int> Process(BuildConfig config)
        {
            Debug.Log($"BuildPlayer");

            string targetPath = config.buildTargetPath;
            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "buildOutput");
            }

#if UNITY_ANDROID
            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
            }
#endif

            BuildPipeline.BuildPlayer(GetBuildScenes(config), targetPath, BuildTarget.Android, GetBuildOptions(config));

            return 0;
        }
    }
}