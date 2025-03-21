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
using UnityEditor.Build.Reporting;
using UnityEditor.Presets;
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
                return JsonConvert.DeserializeObject<BuildContext>(jsonStr,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            }

            return null;
        }

        private static BuildContext buildSession = null;

        static void ResetPlayerSettings(BuildConfig buildConfig)
        {
            if (!string.IsNullOrEmpty(buildConfig.playerSettings))
            {
                Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(buildConfig.playerSettings);
                if (preset != null)
                {
                    try
                    {
                        Debug.Log($"ResetPlayerSettings: {preset.name}");
                        AssetDatabase.StartAssetEditing();
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
                        AssetDatabase.StopAssetEditing();
                    }
                }
            }
        }

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

            // ResetPlayerSettings(buildConfig);

            RunSteps().Forget();
        }

        // [MenuItem("ProjectBuilder/Cancel Building")]
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
            bool assetEdit = processor.AssetEdit;
            try
            {
                if (assetEdit) AssetDatabase.StartAssetEditing();
                return await processor.Process(buildSession.buildConfig);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                if (assetEdit) AssetDatabase.StopAssetEditing();
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
                    // throw;
                }

                // Debug.Log($"WaitNextFame");
                // await UniTask.NextFrame();
                Debug.Log($"Wait for building player");
                await UniTask.WaitUntil(() => BuildPipeline.isBuildingPlayer == false);

                Debug.Log($"Wait for editor updating");
                await UniTask.WaitUntil(() => EditorApplication.isUpdating == false);

                Debug.Log($"Wait for editor compiling");
                await UniTask.WaitUntil(() => EditorApplication.isCompiling == false);


                StoreBuildSession(buildSession);
            }

            BuildPlayer(buildSession.buildConfig);
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


        static string[] GetBuildScenes(BuildConfig config)
        {
            return EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        }

        static string LogBuildReportSteps(BuildReport buildReport)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Build steps: {buildReport.steps.Length}");
            int maxWidth = buildReport.steps.Max(s => s.name.Length + s.depth) + 3;
            foreach (var step in buildReport.steps)
            {
                string rawStepOutput = new string('-', step.depth + 1) + ' ' + step.name;
                sb.AppendLine($"{rawStepOutput.PadRight(maxWidth)}: {step.duration:g}");
            }

            return sb.ToString();
        }

        static string LogBuildMessages(BuildReport buildReport)
        {
            var sb = new StringBuilder();
            foreach (var step in buildReport.steps)
            {
                foreach (var message in step.messages)
                    // If desired, this logic could ignore any Info or Warning messages to focus on more serious messages
                    sb.AppendLine($"[{message.type}] {message.content}");
            }

            string messages = sb.ToString();
            if (messages.Length > 0)
                return "Messages logged during Build:\n" + messages;
            else
                return "";
        }

        static bool BuildPlayer(BuildConfig config)
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

            var buildReport = UnityEditor.BuildPipeline.BuildPlayer(GetBuildScenes(config), targetPath,
                BuildTarget.Android,
                GetBuildOptions(config));
#endif

#if UNITY_IOS
            var buildReport = UnityEditor.BuildPipeline.BuildPlayer(GetBuildScenes(config), targetPath, BuildTarget.iOS,
                GetBuildOptions(config));
#endif
            var report = buildReport;
            var sb = new StringBuilder();
            sb.AppendLine("Build result   : " + report.summary.result);
            sb.AppendLine("Build size     : " + report.summary.totalSize + " bytes");
            sb.AppendLine("Build time     : " + report.summary.totalTime);
            // sb.AppendLine("Error summary  : " + report.SummarizeErrors());
            sb.Append(LogBuildReportSteps(report));
            sb.AppendLine(LogBuildMessages(report));
            Debug.Log(sb.ToString());
            
            return buildReport.summary.result == BuildResult.Succeeded;
        }
    }
}