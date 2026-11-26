using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OrbitToss.EditorTools
{
    /// <summary>
    /// Builds the Quest APK to Builds/Android/OrbitToss.apk.
    /// Menu: Orbit Toss > Build Quest APK
    /// Batch: -executeMethod OrbitToss.EditorTools.OrbitTossBuild.BuildApkBatch
    /// </summary>
    public static class OrbitTossBuild
    {
        public const string ApkPath = "Builds/Android/OrbitToss.apk";

        [MenuItem("Orbit Toss/Build Quest APK")]
        public static void BuildFromMenu() => BuildApk();

        public static void BuildApkBatch()
        {
            bool ok = BuildApk();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool BuildApk()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/OrbitToss/Scenes/OrbitToss.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[OrbitToss] Build result: {s.result}, size {s.totalSize / (1024f * 1024f):F1} MB, errors {s.totalErrors}, time {s.totalTime}, output {Path.GetFullPath(ApkPath)}");
            return s.result == BuildResult.Succeeded;
        }
    }
}
