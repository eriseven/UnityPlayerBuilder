using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PlayerBuilder.Eidtor;
using UnityEditor;
using UnityEngine;

namespace ProjectBuilder.Editor
{
    public class PlayerBuilder
    {
        // [MenuItem("ProjectBuilder/Build Player")]
        public static void StartBuild<T>() where T : BuildConfig
        {
            OnStart<T>();
        }
        
        [Serializable]
        public class BuildContext
        {
            public string[] args;
            public BuildConfig buildConfig = new BuildConfig();
            public int currentStepIndex = -1;
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
        }

        static void ClearLastBuildSession()
        {
            EditorPrefs.DeleteKey($"BuildSession[{Application.dataPath}]");
            buildSession = null;
        }

        static void StoreBuildSession(BuildContext context)
        {
            EditorPrefs.SetString($"BuildSession[{Application.dataPath}]",
                JsonConvert.SerializeObject(context,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
        }

        static BuildContext RestoreBuildSession()
        {
            if (EditorPrefs.HasKey($"BuildSession[{Application.dataPath}]"))
            {
                var jsonStr = EditorPrefs.GetString($"BuildSession[{Application.dataPath}]", "");
                return JsonConvert.DeserializeObject<BuildContext>(jsonStr,  new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            }

            return null;
        }

        private static BuildContext buildSession = null;

        static void OnStart<T>() where T : BuildConfig
        {
            Debug.Log("BuildSession Started");
            ClearLastBuildSession();
            var commandLineArgs = Environment.GetCommandLineArgs();
            var buildConfig = ParseCommandLineArgs<T>(commandLineArgs);

            buildSession = new BuildContext()
            {
                args = commandLineArgs,
                buildConfig = buildConfig,
            };
            StoreBuildSession(buildSession);

            Debug.Log($"BuildSession \n {buildSession}");

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            RunSteps().Forget();
        }

        [MenuItem("ProjectBuilder/Cancel Building")]
        static void OnEnd()
        {
            Debug.Log($"BuildSession[{Application.dataPath}] build end.");
            EditorApplication.update -= OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            ClearLastBuildSession();
        }

        static void OnUpdate()
        {
            if (EditorApplication.isUpdating)
            {
                Debug.Log($"BuildSession[{Application.dataPath}] is updating ...");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.Log($"BuildSession[{Application.dataPath}] is compiling ...");
                return;
            }
        }

        static void OnBeforeAssemblyReload()
        {
            Debug.Log($"BuildSession[{Application.dataPath}] is reloading ...");
            StoreBuildSession(buildSession);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnAfterAssemblyReload()
        {
            Debug.Log($"BuildSession[{Application.dataPath}] is reloaded.");
            buildSession = RestoreBuildSession();
            Debug.Log($"BuildSession \n {buildSession}");
            if (buildSession == null)
            {
                OnEnd();
                return;
            }

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            RunSteps().Forget();
        }


        static BuildConfig ParseCommandLineArgs<T>(string[] args) where T : BuildConfig
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);

            string buildConfigPath = "";
            var optionSet = new Mono.Options.OptionSet()
                .Add("build-config", "Build config", x => buildConfigPath = x.ToLower());


            optionSet.Parse(args);

            BuildConfig buildConfig = null;
            if (string.IsNullOrEmpty(buildConfigPath))
            {
                buildConfigPath = "BuildConfigs/International.json";
            }
            
            if (projectPath != null && File.Exists(Path.Combine(projectPath, buildConfigPath)))
            {
                buildConfig =
                    JsonConvert.DeserializeObject<T>(
                        Encoding.UTF8.GetString(File.ReadAllBytes(buildConfigPath)));
            }
            else
            {
                buildConfig = new BuildConfig();
            }

            buildConfig.ParseCommandLineArgs(args);

            return buildConfig;
        }

        static async UniTask<int> RunStep(PlayerBuilderProcessor processor)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                return await processor.Process(buildSession.buildConfig);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
        
        static async UniTask RunSteps()
        {
            CollectProcessors();
            buildSession.currentStepIndex++;

            // for (int i = buildSession.currentStepIndex; i < buildSteps.Count; i++)
            for (; buildSession.currentStepIndex < buildSteps.Count; buildSession.currentStepIndex++)
            {
                int result = 0;
                try
                {
                    result = await RunStep(buildSteps[buildSession.currentStepIndex]);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    OnEnd();
                    throw;
                }

                await UniTask.NextFrame();
                // await UniTask.WaitUntil(() => !EditorApplication.isPlaying);

                StoreBuildSession(buildSession);
            }

            OnEnd();
        }

        private static List<PlayerBuilderProcessor> buildSteps = new();

        static void CollectProcessors()
        {
            buildSteps = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(PlayerBuilderProcessor))).Select(type =>
                // .Where(type => !type.IsInterface && typeof(IPlayerBuilderProcessor).IsAssignableFrom(type)).Select(type =>
                    Activator.CreateInstance(type))
                .Cast<PlayerBuilderProcessor>().OrderBy(o => o.callbackOrder).ToList();
        }

        static void ProcessScriptDefineSymbols()
        {
            // HashSet<string> defineSymbols = new HashSet<string>()
            var defaultSymbols = new HashSet<string>(PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';'));
            var configSymbols = new HashSet<string>(buildSession.buildConfig.scriptSymbols.Split(';'));
            var toRemove = new HashSet<string>(buildSession.buildConfig.removeScriptSymbols.Split(';'));
            defaultSymbols.UnionWith(configSymbols);
            defaultSymbols.ExceptWith(toRemove);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defaultSymbols));
        }

        static void PrePostprocessBuild()
        {
        }

        static void BuildPlayer()
        {
        }
    }
}