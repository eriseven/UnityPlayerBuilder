using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
#endif
            return 0;
        }
    }

    public class BuildPlayer : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 5010;

        public override async UniTask<int> Process(BuildConfig config)
        {
            Debug.Log($"BuildPlayer");
            return 0;
        }
    }
}