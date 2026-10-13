using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlayablesEditor
{
    /// <summary>
    /// Builds the WebGL player and reports the payload against the 30 MB Playables budget.
    /// The portal enforces that limit per extracted file (the wasm is the one that fails), so the
    /// pass/fail is on the largest file; the total is reported for context only.
    ///
    /// Deliberately does not change any player settings: the point of the baseline is to measure
    /// the project as it actually stands, not as it would be after the size pass.
    /// </summary>
    public static class WebGLBuildScript
    {
        private const long BudgetBytes = 30L * 1024 * 1024;
        private const string OutputPath = "Builds/WebGL";

        [MenuItem("Tools/Fruit Salad/Build WebGL + Size Report")]
        public static void BuildFromMenu()
        {
            Build(false);
        }

        /// Entry point for -executeMethod. Exits with 1 on build failure or budget overrun.
        public static void BuildWebGL()
        {
            Build(true);
        }

        private static void Build(bool exitWhenDone)
        {
            var scenePaths = GetScenePaths();

            if (scenePaths.Length == 0)
            {
                Fail("No scenes found to build.", exitWhenDone);
                return;
            }

            Debug.Log($"[Build] Scenes:\n  {string.Join("\n  ", scenePaths)}");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[Build] Switching active build target to WebGL...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail($"Build {report.summary.result} with {report.summary.totalErrors} error(s).", exitWhenDone);
                return;
            }

            var withinBudget = ReportPayload();

            Debug.Log($"[Build] Finished in {report.summary.totalTime}.");

            if (exitWhenDone)
            {
                EditorApplication.Exit(withinBudget ? 0 : 1);
            }
        }

        [MenuItem("Tools/Fruit Salad/Build Android (APK)")]
        public static void BuildAndroidFromMenu()
        {
            BuildAndroid(false);
        }

        /// Entry point for -executeMethod.
        public static void BuildAndroidPlayer()
        {
            BuildAndroid(true);
        }

        private static void BuildAndroid(bool exitWhenDone)
        {
            var scenePaths = GetScenePaths();

            if (scenePaths.Length == 0)
            {
                Fail("No scenes found to build.", exitWhenDone);
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[Build] Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // An APK rather than an app bundle: this is for sideloading onto a device, not for Play.
            EditorUserBuildSettings.buildAppBundle = false;

            const string apkPath = "Builds/Android/FruitSalad.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(apkPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail($"Android build {report.summary.result} with {report.summary.totalErrors} error(s).",
                    exitWhenDone);
                return;
            }

            var apk = new FileInfo(apkPath);
            Debug.Log($"[Build] APK {Format(apk.Length)} at {apk.FullName} " +
                      $"(orientation {PlayerSettings.defaultInterfaceOrientation}, " +
                      $"id {Application.identifier}) in {report.summary.totalTime}.");

            if (exitWhenDone)
            {
                EditorApplication.Exit(0);
            }
        }

        private static string[] GetScenePaths()
        {
            var configured = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (configured.Length > 0)
            {
                return configured;
            }

            // Build settings are empty in this project, so fall back to whatever scene exists.
            return AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .ToArray();
        }

        private static bool ReportPayload()
        {
            var root = new DirectoryInfo(OutputPath);

            if (!root.Exists)
            {
                Debug.LogError($"[Build] Output missing at {root.FullName}");
                return false;
            }

            var files = root.GetFiles("*", SearchOption.AllDirectories)
                .OrderByDescending(file => file.Length)
                .ToArray();

            var total = files.Sum(file => file.Length);

            var report = new StringBuilder();
            report.AppendLine("===== PLAYABLES PAYLOAD REPORT =====");
            report.AppendLine($"Output: {root.FullName}");
            report.AppendLine();

            foreach (var file in files)
            {
                report.AppendLine($"{Format(file.Length),12}  {GetRelativePath(root, file)}");
            }

            report.AppendLine();
            report.AppendLine($"{Format(total),12}  TOTAL");
            report.AppendLine($"{Format(BudgetBytes),12}  BUDGET");

            var largest = files.Length > 0 ? files[0] : null;
            var largestBytes = largest?.Length ?? 0;
            var withinBudget = largestBytes <= BudgetBytes;
            var difference = Math.Abs(BudgetBytes - largestBytes);

            report.AppendLine($"{Format(largestBytes),12}  LARGEST FILE ({(largest != null ? GetRelativePath(root, largest) : "none")})");
            report.AppendLine(withinBudget
                ? $"{Format(difference),12}  UNDER PER-FILE BUDGET"
                : $"{Format(difference),12}  OVER PER-FILE BUDGET");

            report.AppendLine("====================================");

            if (withinBudget)
            {
                Debug.Log(report.ToString());
            }
            else
            {
                Debug.LogError(report.ToString());
            }

            return withinBudget;
        }

        private static string GetRelativePath(DirectoryInfo root, FileInfo file)
        {
            return file.FullName
                .Substring(root.FullName.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string Format(long bytes)
        {
            return $"{bytes / 1024f / 1024f:F2} MB";
        }

        private static void Fail(string message, bool exitWhenDone)
        {
            Debug.LogError($"[Build] {message}");

            if (exitWhenDone)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
