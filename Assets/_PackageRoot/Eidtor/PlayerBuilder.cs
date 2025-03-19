using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ProjectBuilder.Editor
{
    public class PlayerBuilder
    {
        [MenuItem("ProjectBuilder/Build Player")]
        static void StartBuild()
        {
            OnStart();
        }

        [MenuItem("ProjectBuilder/Test/StartTask")]
        static void StartTask()
        {
            AssetDatabase.StartAssetEditing();
        }

        [MenuItem("ProjectBuilder/Test/EndTask")]
        static void EndTask()
        {
            AssetDatabase.StopAssetEditing();
        }
        
        [Serializable]
        public class BuildContext
        {
            public string[] args;
            public BuildConfig buildConfig = new BuildConfig();
        }

        static void ClearLastBuildSession()
        {
            EditorPrefs.DeleteKey($"BuildSession[{Application.dataPath}]");
            buildSession = null;
        }

        static void StoreBuildSession(BuildContext context)
        {
            EditorPrefs.SetString($"BuildSession[{Application.dataPath}]",  JsonConvert.SerializeObject(context));
        }

        static BuildContext RestoreBuildSession()
        {
            if (EditorPrefs.HasKey($"BuildSession[{Application.dataPath}]"))
            {
                var jsonStr = EditorPrefs.GetString($"BuildSession[{Application.dataPath}]", "");
                return JsonConvert.DeserializeObject<BuildContext>(jsonStr);
            }
            return null;
        }

        private static BuildContext buildSession = null;

        static void OnStart()
        {
            Debug.Log("BuildSession Started");
            ClearLastBuildSession();
            var commandLineArgs = Environment.GetCommandLineArgs();
            var buildConfig = ParseCommandLineArgs(commandLineArgs);

            buildSession = new BuildContext()
            {
                args = commandLineArgs,
                buildConfig = buildConfig,
            };
            StoreBuildSession(buildSession);

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
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
            if (buildSession == null)
            {
                // OnEnd();
                return;
            }

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }


        static BuildConfig ParseCommandLineArgs(string[] args)
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);

            string buildConfigPath = "";
            var optionSet = new Mono.Options.OptionSet()
                .Add("build-config", "Build config", x => buildConfigPath = x.ToLower());


            optionSet.Parse(args);

            BuildConfig buildConfig = null;

            if (projectPath != null && File.Exists(Path.Combine(projectPath, buildConfigPath)))
            {
                buildConfig =
                    JsonConvert.DeserializeObject<BuildConfig>(
                        Encoding.UTF8.GetString(File.ReadAllBytes(buildConfigPath)));
            }
            else
            {
                buildConfig = new BuildConfig();
            }

            buildConfig.ParseCommandLineArgs(args);

            return buildConfig;
        }

        static void PrePostprocessBuild()
        {
        }

        static void BuildPlayer()
        {
        }
    }
}