    #if UNITY_EDITOR
     
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build;
    using System.IO;
    using ICSharpCode.SharpZipLib.Core;
    using ICSharpCode.SharpZipLib.Zip;
     
     
    class CustomBuildPreProcess : IPreprocessBuild
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildTarget target, string path) {
     
            string dataDirectory = Application.dataPath + "/Plugins/Android/assets/Examples/";
            string fileToCreate = Application.streamingAssetsPath + "/Examples.tgz";
     
            Utility_SharpZipCommands.CreateTarGZ_FromDirectory (fileToCreate, dataDirectory);

            dataDirectory = Application.dataPath + "/Plugins/Android/assets/Samples/";
            fileToCreate = Application.streamingAssetsPath + "/Samples.tgz";
     
            Utility_SharpZipCommands.CreateTarGZ_FromDirectory (fileToCreate, dataDirectory);
     
        }
    }
     
    #endif
