using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectBuilder.Editor
{
    [Serializable]
    public class BuildConfig
    {
        #region Common

        public string configName = "";
        public bool development = false;
        public string packageName = "";

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
        
        public string customConfig = "{}";
    }
}