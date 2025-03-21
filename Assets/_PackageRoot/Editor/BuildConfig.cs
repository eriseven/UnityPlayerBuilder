using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Options;
using Newtonsoft.Json;
using UnityEngine;


namespace ProjectBuilder.Editor
{
    [Serializable]
    public class BuildConfig
    {
        #region Common

        public string configName = "";
        public string playerSettings = "";
        public bool development = false;
        public string packageName = "";
        public string scriptSymbols = "";
        public string removeScriptSymbols = "";
        public string buildTargetPath = "";
        public string specificVersionString = "0.0.0";
        public int buildNumber = -1;

        #endregion

        #region Android

        public bool forceBuildAppBundle = false;
        public bool arm64 = true;
        public int minAndroidSdkVersion = -1;
        public int targetAndroidSdkVersion = -1;

        public bool useCustomKeystore = false;
        public string keystoreName = "";
        public string keystorePass = "";
        public string keyaliasName = "";
        public string keyaliasPass = "";

        #endregion

        #region iOS

        public string appleDeveloperTeamID = "";

        #endregion

        public Version SpecificVersion => Version.Parse(specificVersionString); 
        
        // protected virtual Tuple<string, string, Action<string>>[] AdditionalOptions => new Tuple<string, string, Action<string>>[0];
        protected virtual (string, string, Action<string>)[] AdditionalOptions => Array.Empty<(string, string, Action<string>)>();


        public virtual void ParseCommandLineArgs(string[] args)
        {
            var symbols = new HashSet<string>();
            var p = new Mono.Options.OptionSet()
            {
                {
                    "d|def=", "", v =>
                    {
                        if (!string.IsNullOrEmpty(v))
                        {
                            // Debug.Log($"defineSymbol:{v}");
                            symbols.Add(v);
                        }
                    }
                },
                {
                    "dev", "", v => { this.development = v != null; }
                },
                {
                    "buildAAB", "", v => { this.forceBuildAppBundle = v != null; }
                },
                {
                    "ver=", "", v =>
                    {
                        if (Version.TryParse(v, out var ver))
                        {
                            this.specificVersionString = ver.ToString();
                        }
                    }
                },
            };

            foreach (var op in AdditionalOptions)
            {
                try
                {
                    p.Add(op.Item1, op.Item2, op.Item3);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            p.Parse(args);
            
            this.scriptSymbols = string.Join(';',  this.scriptSymbols, string.Join(';', symbols));
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}