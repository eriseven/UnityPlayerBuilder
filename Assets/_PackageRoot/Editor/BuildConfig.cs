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
        public string specificVersionString = "0.0.1";

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

        protected virtual Option[] AdditionalOptions => Array.Empty<Option>();


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
                p.Add(op);
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