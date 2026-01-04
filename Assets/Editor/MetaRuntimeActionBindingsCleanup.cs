#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class MetaRuntimeActionBindingsCleanup : IPostprocessBuildWithReport
{

    // this script removes the RuntimeActionBindings.json for MacOS builds,
    // since that is causing export issues on MacOS when overwriting builds

    // Earlier is safer.
    public int callbackOrder => -10000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneOSX)
        {
            return;
        }

        var appPath = report.summary.outputPath;
        var exportDir = Path.GetDirectoryName(appPath);
        if (string.IsNullOrEmpty(exportDir))
        {
            return;
        }

        var filePath = Path.Combine(exportDir, "RuntimeActionBindings.json");

        TryDelete(filePath);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal); // avoid read-only edge cases
            File.Delete(path);
        }
    }

}
#endif
