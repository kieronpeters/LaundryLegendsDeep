using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Builds the playable Windows player from the enabled build settings, so the
    /// player always follows Bootstrap -> MainMenu -> Laundromat. Editor build
    /// settings are left untouched; this tool only reads them.
    /// <para>
    /// Display settings are set here for fullscreen owner playtests and are
    /// committed deliberately as project settings.
    /// </para>
    /// </summary>
    public static class BuildGameTool
    {
        private const string OutputPath = "Build/LaundryLegends.exe";

        [MenuItem("Laundry Legends/Build Playable")]
        public static void BuildDirect()
        {
            Directory.CreateDirectory("Build");

            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BUILD] No enabled scenes in build settings; refusing to build a menu-less player.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BUILD] Succeeded: {OutputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, scenes: {string.Join(", ", SceneNames(scenes))})");
            }
            else
            {
                Debug.LogError($"[BUILD] {summary.result} with {summary.totalErrors} errors");
            }
        }

        /// <summary>Paths of every enabled scene, in build order.</summary>
        public static string[] EnabledScenes()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes.ToArray();
        }

        private static IEnumerable<string> SceneNames(string[] paths)
        {
            foreach (string path in paths)
            {
                yield return Path.GetFileNameWithoutExtension(path);
            }
        }
    }
}
