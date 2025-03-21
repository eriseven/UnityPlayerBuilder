using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using ProjectBuilder.Editor;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

#if UNITY_ANDROID
using UnityEditor.Android;
#endif

#if UNITY_IOS
using UnityEditor.iOS;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

namespace PlayerBuilder.Eidtor
{
    public class ApplyPlayerSettingsPreset : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = -1;

        public override async UniTask<int> Process(BuildConfig buildConfig)
        {
            if (!string.IsNullOrEmpty(buildConfig.playerSettings))
            {
                Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(buildConfig.playerSettings);
                if (preset != null)
                {
                    try
                    {
                        Debug.Log($"ResetPlayerSettings: {preset.name}");
                        // AssetDatabase.StartAssetEditing();
                        var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
                        preset.ApplyTo(playerSettings);
                        AssetDatabase.Refresh();
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(playerSettings));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        // AssetDatabase.StopAssetEditing();
                    }
                }
            }

            return 0;
        }
    }

    public class ScriptDefineSymbolsProcessor : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 5001;

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

            PlayerSettings.bundleVersion = config.SpecificVersion.ToString();

            int buildNumber = 0;
            if (config.buildNumber < 0)
            {
                buildNumber = EditorPrefs.GetInt($"{Application.dataPath}_{PlayerSettings.bundleVersion}_buildNumber", 0);
                EditorPrefs.SetInt($"{Application.dataPath}_{PlayerSettings.bundleVersion}_buildNumber", buildNumber++);
            }
            
#if UNITY_ANDROID
            PlayerSettings.Android.renderOutsideSafeArea = true;
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

            var ver = config.SpecificVersion;
            
            PlayerSettings.Android.bundleVersionCode = buildNumber + ver.Build * 100 + ver.Minor * 100 * 1000 + ver.Major * 100 * 1000 * 1000;
#endif

#if UNITY_IOS
            PlayerSettings.iOS.appleEnableAutomaticSigning = !string.IsNullOrEmpty(config.appleDeveloperTeamID);
            if (PlayerSettings.iOS.appleEnableAutomaticSigning)
            {
                PlayerSettings.iOS.appleDeveloperTeamID = config.appleDeveloperTeamID;
            }

            PlayerSettings.iOS.buildNumber = $"{PlayerSettings.bundleVersion}.{buildNumber}";
#endif

            return 0;
        }
    }
}