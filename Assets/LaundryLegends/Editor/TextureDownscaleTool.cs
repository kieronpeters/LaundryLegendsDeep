using System.Text;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Caps every texture in the project at 2048 px so no 4K maps ship. 4K costs
    /// roughly 42 MB of VRAM per PBR set on a 4 GB card, and the target is 1080p.
    /// </summary>
    public static class TextureDownscaleTool
    {
        private const int MaxSize = 2048;

        [MenuItem("Laundry Legends/Phase 8/Downscale Textures To 2K")]
        public static void Downscale()
        {
            var report = new StringBuilder();
            report.AppendLine("[TEXTURE] capping at 2048");

            int changed = 0;
            int skipped = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/LaundryLegends" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (importer.maxTextureSize <= MaxSize)
                {
                    skipped++;
                    continue;
                }

                report.AppendLine($"  {System.IO.Path.GetFileName(path)}: {importer.maxTextureSize} -> {MaxSize}");
                importer.maxTextureSize = MaxSize;
                importer.SaveAndReimport();
                changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.AppendLine($"[TEXTURE] changed={changed} already-ok={skipped}");
            Debug.Log(report.ToString());
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/texture-downscale.txt", report.ToString());
        }
    }
}
