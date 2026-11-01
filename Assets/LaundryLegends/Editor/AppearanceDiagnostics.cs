using System.Text;
using UnityEditor;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Read-only diagnostics: prints every renderer in the NPC and player prefabs
    /// together with the materials it actually uses, so appearance fixes can be
    /// targeted at real names instead of guessed ones.
    /// </summary>
    public static class AppearanceDiagnostics
    {
        private static readonly string[] Prefabs =
        {
            "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Regular.prefab",
            "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Attendant.prefab",
            "Assets/LaundryLegends/Prefabs/Gameplay/NPC/NPC_Customer.prefab",
            "Assets/LaundryLegends/Prefabs/Gameplay/Player.prefab"
        };

        [MenuItem("Laundry Legends/Diagnostics/Dump Renderer Materials")]
        public static void Dump()
        {
            var report = new StringBuilder();
            report.AppendLine("=== RENDERER MATERIAL DUMP ===");

            foreach (string path in Prefabs)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    report.AppendLine();
                    report.AppendLine($"--- {path}");

                    var renderers = contents.GetComponentsInChildren<Renderer>(true);
                    report.AppendLine($"    renderers: {renderers.Length}");

                    foreach (Renderer renderer in renderers)
                    {
                        string materials = "none";
                        if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
                        {
                            var parts = new StringBuilder();
                            foreach (Material material in renderer.sharedMaterials)
                            {
                                if (material == null)
                                {
                                    parts.Append("<null>; ");
                                    continue;
                                }

                                Color baseColor = material.HasProperty("_BaseColor")
                                    ? material.GetColor("_BaseColor")
                                    : Color.magenta;
                                bool hasMap = material.HasProperty("_BaseMap")
                                    && material.GetTexture("_BaseMap") != null;

                                parts.Append($"{material.name}[rgb {baseColor.r:0.00},{baseColor.g:0.00},{baseColor.b:0.00} tex={(hasMap ? "yes" : "no")}]; ");
                            }

                            materials = parts.ToString();
                        }

                        bool skinned = renderer is SkinnedMeshRenderer;
                        report.AppendLine($"    {(skinned ? "Skinned" : "Mesh")} '{renderer.name}' verts={(renderer is MeshRenderer ? "-" : ((SkinnedMeshRenderer)renderer).sharedMesh?.vertexCount.ToString() ?? "0")} -> {materials}");
                    }

                    foreach (Transform child in contents.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.IndexOf("hand", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            report.AppendLine($"    HAND NODE: '{child.name}' active={child.gameObject.activeSelf} pos={child.localPosition}");
                        }
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            report.AppendLine("=== END DUMP ===");
            Debug.Log(report.ToString());
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/appearance-dump.txt", report.ToString());
        }
    }
}
