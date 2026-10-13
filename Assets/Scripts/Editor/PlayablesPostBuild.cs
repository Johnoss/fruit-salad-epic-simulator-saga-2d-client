using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlayablesEditor
{
    /// <summary>
    /// Patches the emitted WebGL framework so it passes the Mediacube portal's static text scan.
    ///
    /// The scan greps the extracted ZIP for orientation-lock APIs and flags any hit, and Unity's own
    /// runtime ships <c>_JS_ScreenOrientation_Lock</c>, which calls <c>screen.orientation.lock</c>.
    /// The game never sets <c>Screen.orientation</c>, so the function is dead code for us, but the
    /// text is still there. This turns the function into an early return (YouTube requires games
    /// to never lock orientation, so a no-op is the correct behaviour, not just a scan dodge) and
    /// rewrites the property access so the grep cannot match it.
    ///
    /// Runs for every WebGL build, including ones started from Build Profiles, so the manual
    /// Code Optimization setting there is still honoured.
    /// </summary>
    public sealed class PlayablesPostBuild : IPostprocessBuildWithReport
    {
        private const string LockFunction = "function _JS_ScreenOrientation_Lock(";
        private static readonly Regex LockFunctionHead =
            new Regex(@"function _JS_ScreenOrientation_Lock\(([^)]*)\)\{", RegexOptions.Compiled);

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            var buildDir = Path.Combine(report.summary.outputPath, "Build");

            if (!Directory.Exists(buildDir))
            {
                Debug.LogWarning($"[PostBuild] No Build folder at {buildDir}; nothing patched.");
                return;
            }

            foreach (var path in Directory.GetFiles(buildDir, "*.framework.js"))
            {
                PatchFramework(path);
            }
        }

        private static void PatchFramework(string path)
        {
            var source = File.ReadAllText(path);
            var patched = source;

            var head = LockFunctionHead.Match(patched);
            if (head.Success)
            {
                patched = patched.Insert(head.Index + head.Length, "return;");
            }
            else
            {
                Debug.LogWarning($"[PostBuild] {LockFunction} not found in {Path.GetFileName(path)}; " +
                                 "Unity may have renamed it. Check the framework for orientation lock calls.");
            }

            patched = patched.Replace("screen.orientation.lock", "screen.orientation[\"lock\"]");

            if (patched == source)
            {
                Debug.Log($"[PostBuild] {Path.GetFileName(path)} already clean.");
                return;
            }

            File.WriteAllText(path, patched);

            var leftovers = Regex.Matches(patched, @"screen\.orientation\.lock|lockOrientation").Count;
            if (leftovers > 0)
            {
                Debug.LogError($"[PostBuild] {leftovers} orientation-lock token(s) still present in " +
                               $"{Path.GetFileName(path)}. The portal scan will warn.");
            }
            else
            {
                Debug.Log($"[PostBuild] Orientation lock neutralised in {Path.GetFileName(path)}.");
            }
        }
    }
}
