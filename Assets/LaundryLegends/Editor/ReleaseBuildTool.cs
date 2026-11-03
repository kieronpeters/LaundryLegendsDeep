using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Phase 17 release candidate: stamps the version, runs the normal build, then
    /// writes a manifest and copies the release documents next to the executable.
    /// </summary>
    public static class ReleaseBuildTool
    {
        public const string Version = "1.0.0";

        [MenuItem("Laundry Legends/Phase 17/Build Release Candidate")]
        public static void BuildRelease()
        {
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.productName = "Laundry Legends";
            PlayerSettings.companyName = "LaundryLegends";

            BuildGameTool.BuildDirect();

            string project = Directory.GetCurrentDirectory();
            string exe = Path.Combine(project, "Build", "LaundryLegends.exe");
            string data = Path.Combine(project, "Build", "LaundryLegends_Data");
            if (!File.Exists(exe) || !Directory.Exists(data))
            {
                throw new InvalidOperationException("Release build output is incomplete.");
            }

            string commit = Git("rev-parse HEAD");
            string branch = Git("rev-parse --abbrev-ref HEAD");
            long size = new FileInfo(exe).Length;
            string manifest =
                "{\n" +
                $"  \"version\": \"{Version}\",\n" +
                $"  \"commit\": \"{commit}\",\n" +
                $"  \"branch\": \"{branch}\",\n" +
                $"  \"builtAt\": \"{DateTime.UtcNow:O}\",\n" +
                $"  \"exe\": \"LaundryLegends.exe\",\n" +
                $"  \"exeBytes\": {size},\n" +
                $"  \"unity\": \"{Application.unityVersion}\",\n" +
                "  \"scenes\": [\"Bootstrap\", \"MainMenu\", \"Laundromat\"]\n" +
                "}\n";
            File.WriteAllText(Path.Combine(project, "Build", "release-manifest.json"), manifest, Encoding.UTF8);

            foreach (string doc in new[] { "RELEASE_NOTES.md", "KNOWN_ISSUES.md", "CREDITS.md", "RELEASE_CHECKLIST.md" })
            {
                string source = Path.Combine(project, doc);
                if (File.Exists(source))
                {
                    File.Copy(source, Path.Combine(project, "Build", doc), true);
                }
            }

            UnityEngine.Debug.Log(
                $"[RELEASE] {Version} built from {branch}@{commit.Substring(0, 7)}; " +
                $"exe {size / (1024 * 1024)} MB; manifest and docs copied to Build.");
        }

        private static string Git(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(info))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(10000);
                    return output;
                }
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
