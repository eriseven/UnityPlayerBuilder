using UnityEditor.Android;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Eflatun.AndroidManifestHook
{
    public abstract class AndroidManifestHooker : IPostGenerateGradleAndroidProject
    {
        public const string launcherProjectName = "launcher";
        public const string unityLibraryProjectName = "unityLibrary";
        public readonly string manifestSubPath = Path.Combine("src", "main", "AndroidManifest.xml");
        
        protected string unityLibraryPath => Path.Combine(ProjectPath, unityLibraryProjectName, manifestSubPath);
        protected string launcherPath => Path.Combine(ProjectPath, launcherProjectName, manifestSubPath);
 
       
        public abstract int callbackOrder { get; }
        protected string ProjectPath;

        static string ReadRawUTF8(string path)
        {
            return Encoding.UTF8.GetString(File.ReadAllBytes(path));
        }
        
        static void WriteRawUTF8(string path, string content)
        {
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(content));
        }
        
        
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            ProjectPath = Path.GetDirectoryName(path);
            
            var className = this.GetType().UnderlyingSystemType.Name;

            var unityLibraryManifest = new AndroidManifest(unityLibraryPath);
            
            Debug.Log($"AndroidManifestHooker ({className}): Modifying {unityLibraryPath} file.");
            
            unityLibraryManifest = Modify(unityLibraryManifest);
            unityLibraryManifest.Save();
            
            var launcherManifest = new AndroidManifest(launcherPath);
            
            Debug.Log($"AndroidManifestHooker ({className}): Modifying {launcherPath} file.");
            
            launcherManifest = ModifyLauncherManifest(launcherManifest);
            launcherManifest.Save();

            if (File.Exists(baseGradlePath))
            {
                var processedGradleContent = ProcessBaseGradleProject(ReadRawUTF8(baseGradlePath));
                if (!string.IsNullOrEmpty(processedGradleContent))
                {
                    WriteRawUTF8(baseGradlePath, processedGradleContent);
                }
            }

            if (File.Exists(mainGradlePath))
            {
                var processedGradleContent = ProcessMainGradleProject(ReadRawUTF8(mainGradlePath));
                if (!string.IsNullOrEmpty(processedGradleContent))
                {
                    WriteRawUTF8(mainGradlePath, processedGradleContent);
                }
            }

            if (File.Exists(launcherGradlePath))
            {
                var processedGradleContent = ProcessLauncherGradleProject(ReadRawUTF8(launcherGradlePath));
                if (!string.IsNullOrEmpty(processedGradleContent))
                {
                    WriteRawUTF8(launcherGradlePath, processedGradleContent);
                }               
            }

            OnPostprocess();
        }

        protected abstract AndroidManifest Modify(AndroidManifest androidManifest);

        protected virtual AndroidManifest ModifyLauncherManifest(AndroidManifest androidManifest)
        {
            return androidManifest;
        }
        
        protected string baseGradlePath => Path.Combine(ProjectPath, "build.gradle");
        protected string mainGradlePath => Path.Combine(ProjectPath, unityLibraryProjectName, "build.gradle");
        protected string launcherGradlePath => Path.Combine(ProjectPath, launcherProjectName, "build.gradle");
 
        protected virtual string dependenciesPlaceHolder { get; } = @"// **ADDTIONAL-DEPENDENCIES**";
        protected virtual string classpathsPlaceHolder { get; } = @"// **ADDTIONAL-CLASSPATHS**";
        protected virtual string repositoriesPlaceHolder { get; } = @"// **ADDTIONAL-REPOSITORIES**";
        protected virtual string applyPluginsPlaceHolder { get; } = @"// **ADDTIONAL-APPLYPLUGINS**";

        protected virtual List<string> AdditionalDependencies { get; } = new List<string>();
        protected virtual List<string> AdditionalClasspaths { get; } = new List<string>();
        protected virtual List<string> AdditionalRepositories { get; } = new List<string>();
        protected virtual List<string> AdditionalApplyPlugins { get; } = new List<string>();
        

        
        static StringBuilder sb = new StringBuilder();
        protected virtual string ProcessBaseGradleProject(string content)
        {
            var modified = false;
            try
            {
                if (AdditionalRepositories != null && AdditionalRepositories.Count > 0 && content.IndexOf(repositoriesPlaceHolder) != -1)
                {
                    sb.Clear();
                    foreach (var repository in AdditionalRepositories)
                    {
                        sb.AppendLine($"maven {{ url '{repository}' }}");
                    }

                    sb.Append(repositoriesPlaceHolder);
                    content = content.Replace(repositoriesPlaceHolder, sb.ToString());
                    modified = true;
                }

                if (AdditionalClasspaths != null && AdditionalClasspaths.Count > 0 && content.IndexOf(classpathsPlaceHolder) != -1)
                {
                    sb.Clear();
                    foreach (var classpath in AdditionalClasspaths)
                    {
                        sb.AppendLine($"classpath '{classpath}'");
                    }

                    sb.Append(classpathsPlaceHolder);
                    content = content.Replace(classpathsPlaceHolder, sb.ToString());
                    modified = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                content = null;
            }

            return modified ? content : null;
        }

        protected virtual string ProcessMainGradleProject(string content)
        {
            var modified = false;
            try
            {
                if (AdditionalApplyPlugins != null && AdditionalApplyPlugins.Count > 0 && content.IndexOf(applyPluginsPlaceHolder) != -1)
                {
                    sb.Clear();
                    foreach (var plugin in AdditionalApplyPlugins)
                    {
                        sb.AppendLine($"apply plugin: '{plugin}'");
                    }

                    sb.Append(applyPluginsPlaceHolder);
                    content = content.Replace(applyPluginsPlaceHolder, sb.ToString());
                    modified = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                content = null;
            }
            
            return modified ? content : null;
        }

        protected virtual string ProcessLauncherGradleProject(string content)
        {
            var modified = false;
            try
            {
                if (AdditionalApplyPlugins != null && AdditionalApplyPlugins.Count > 0 && content.IndexOf(applyPluginsPlaceHolder) != -1)
                {
                    sb.Clear();
                    foreach (var plugin in AdditionalApplyPlugins)
                    {
                        sb.AppendLine($"apply plugin: '{plugin}'");
                    }

                    sb.Append(applyPluginsPlaceHolder);
                    content = content.Replace(applyPluginsPlaceHolder, sb.ToString());
                    modified = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                content = null;
            }
            
            return modified ? content : null;           
        }

        protected virtual void OnPostprocess()
        {
            
        }
    }
}
