using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayerBuilder.Eidtor;
using UnityEngine;
using ProjectBuilder.Editor;
using UnityEditor;
using Builder = ProjectBuilder.Editor.PlayerBuilder;

#if UNITY_EDITOR

public static class DemoBuilder
{
    [MenuItem("Demo/Build Player")]
    public static void Build()
    {
        Debug.Log("DemoBuilder.Build");
        Builder.StartBuild<DemoBuildConfig>();
    }

    public class DemoPreProcessor : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 100;

        public override async UniTask<int> Process(BuildConfig config)
        {
            DemoBuildConfig buildConfig = config as DemoBuildConfig;
            Debug.Assert(buildConfig != null);

            Debug.Log($"DemoPreProcessor({callbackOrder}): \n buildConfig.channel: {buildConfig.channel}");

            return 0;
        }
    }

    public class DemoPostProcessor : PlayerBuilderProcessor
    {
        public override int callbackOrder { get; } = 10000;

        public override async UniTask<int> Process(BuildConfig config)
        {
            DemoBuildConfig buildConfig = config as DemoBuildConfig;
            Debug.Assert(buildConfig != null);

            Debug.Log($"DemoPostProcessor({callbackOrder}): \n buildConfig.channel: {buildConfig.channel}");

            return 0;
        }
    }
}

#endif