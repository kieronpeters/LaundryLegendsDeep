using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Read-only visual audit: dumps world rotation, scale, and renderer bounds for
    /// the props the owner reported as lying down, floating, or malformed. Used to
    /// distinguish a placement bug (wrong rotation on the instance root) from an
    /// import bug (wrong axis conversion inside the model).
    /// </summary>
    public static class VisualAuditTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        private static readonly string[] Patterns =
        {
            "StreetDressing", "StreetLamp", "Plant_Street", "Bench_Street", "Chair_Waiting", "Curb_",
            "Marking_", "Signage", "Blade_", "Counter_Desk", "DropOff_", "OpenSign", "HoursSign",
            "Facade_", "Entrance_Door", "SecretDoor", "Waiting_Planter", "PlantPot", "PlantLeaves",
            "Sign_", "Window_", "Wall_South", "Entrance_",
            "Dirt", "Grime", "Shadow", "Decal", "Phone", "ChestBadge"
        };

        [MenuItem("Laundry Legends/Audit Visual Transforms")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            log.AppendLine("[VISUAL] scene transform audit");

            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Dump(root.transform, log, 0);
            }

            log.AppendLine("[VISUAL] prefab assets");
            DumpPrefab("Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab", log);
            DumpPrefab("Assets/LaundryLegends/Prefabs/Gameplay/Laundry/WrappedBundle.prefab", log);
            DumpPrefab("Assets/LaundryLegends/Prefabs/Gameplay/Laundry/Basket.prefab", log);
            DumpPrefab("Assets/LaundryLegends/Prefabs/Gameplay/Laundry/Bag.prefab", log);

            Debug.Log(log.ToString());
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/visual-audit.txt", log.ToString());
        }

        private static void Dump(Transform transform, StringBuilder log, int depth)
        {
            if (!Matches(transform.name))
            {
                foreach (Transform child in transform)
                {
                    Dump(child, log, depth);
                }

                return;
            }

            var renderer = transform.GetComponent<Renderer>();
            string bounds = string.Empty;
            if (renderer != null)
            {
                Bounds b = renderer.bounds;
                var meshFilter = transform.GetComponent<MeshFilter>();
                string mesh = meshFilter != null && meshFilter.sharedMesh != null
                    ? $"{meshFilter.sharedMesh.name}({meshFilter.sharedMesh.vertexCount}v)"
                    : "none";
                bounds = $" bounds.center={b.center.ToString("0.00")} size={b.size.ToString("0.00")} mesh={mesh}";
            }

            log.AppendLine(
                $"[VISUAL] '{transform.name}' pos={transform.position.ToString("0.00")} " +
                $"euler={transform.eulerAngles.ToString("0.0")} lossyScale={transform.lossyScale.ToString("0.00")} " +
                $"active={transform.gameObject.activeInHierarchy}{bounds}");

            foreach (Transform child in transform)
            {
                // One level down is enough detail for prefab instances.
                if (depth < 3)
                {
                    Dump(child, log, depth + 1);
                }
            }
        }

        private static void DumpPrefab(string path, StringBuilder log)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                log.AppendLine($"[VISUAL] prefab {path} rootEuler={contents.transform.eulerAngles.ToString("0.0")} " +
                               $"rootScale={contents.transform.localScale.ToString("0.00")}");
                foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    string mesh = meshFilter != null && meshFilter.sharedMesh != null
                        ? $"{meshFilter.sharedMesh.name}({meshFilter.sharedMesh.vertexCount}v)"
                        : "none";
                    var materials = new List<string>();
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        materials.Add(material != null ? material.name : "null");
                    }

                    log.AppendLine(
                        $"[VISUAL]   '{renderer.name}' localPos={renderer.transform.localPosition.ToString("0.00")} " +
                        $"localEuler={renderer.transform.localEulerAngles.ToString("0.0")} " +
                        $"localScale={renderer.transform.localScale.ToString("0.00")} mesh={mesh} " +
                        $"materials={string.Join(",", materials)}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool Matches(string name)
        {
            foreach (string pattern in Patterns)
            {
                if (name.StartsWith(pattern, System.StringComparison.OrdinalIgnoreCase)
                    || name.Contains(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Lists every renderer whose bounds intersect a world box, with its material.</summary>
        [MenuItem("Laundry Legends/Audit Region")]
        public static void AuditRegion()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            log.AppendLine("[REGION] renderers by world region");

            DumpRegion(log, "door-slab", new Bounds(new Vector3(0f, 2.4f, -5.6f), new Vector3(4f, 1.6f, 2.4f)));
            DumpRegion(log, "blade-bar", new Bounds(new Vector3(3.6f, 2.2f, -6.3f), new Vector3(2f, 0.8f, 1.2f)));
            DumpRegion(log, "backroom-east-wall", new Bounds(new Vector3(16.0f, 1.3f, 0.7f), new Vector3(1.2f, 1.8f, 1.8f)));

            Debug.Log(log.ToString());
            System.IO.File.WriteAllText("TestResults/visual-region.txt", log.ToString());
        }

        /// <summary>Dumps renderers under the back-room root, including inactive objects.</summary>
        [MenuItem("Laundry Legends/Audit Back Room")]
        public static void AuditBackRoom()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            var root = GameObject.Find("SecretRoomSystem");
            if (root == null)
            {
                log.AppendLine("[BACK] SecretRoomSystem not found");
            }
            else
            {
                foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!renderer.transform.IsChildOf(root.transform))
                    {
                        continue;
                    }

                    string material = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null";
                    log.AppendLine(
                        $"[BACK] '{renderer.name}' root='{renderer.transform.root.name}' " +
                        $"active={renderer.gameObject.activeInHierarchy} enabled={renderer.enabled} " +
                        $"center={renderer.bounds.center.ToString("0.00")} size={renderer.bounds.size.ToString("0.00")} " +
                        $"material={material}");
                }
            }

            Debug.Log(log.ToString());
            System.IO.File.WriteAllText("TestResults/backroom-audit.txt", log.ToString());
        }

        private static void DumpRegion(StringBuilder log, string label, Bounds region)
        {
            log.AppendLine($"[REGION] {label} center={region.center} size={region.size}");
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.bounds.Intersects(region))
                {
                    continue;
                }

                var materials = new List<string>();
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        materials.Add("null");
                        continue;
                    }

                    string color = material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor").ToString()
                        : "-";
                    materials.Add($"{material.name}{color}");
                }

                log.AppendLine(
                    $"[REGION]   '{renderer.name}' root='{renderer.transform.root.name}' " +
                    $"center={renderer.bounds.center.ToString("0.00")} size={renderer.bounds.size.ToString("0.00")} " +
                    $"materials={string.Join(" | ", materials)}");
            }
        }

        /// <summary>Dumps import-root rotations for the street models and every TextMesh.</summary>
        [MenuItem("Laundry Legends/Audit Models")]
        public static void AuditModels()
        {
            var log = new StringBuilder();
            log.AppendLine("[MODEL] street model roots");
            foreach (string path in System.IO.Directory.GetFiles("Assets/LaundryLegends/Art/Models/Street", "*.fbx"))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    continue;
                }

                log.AppendLine(
                    $"[MODEL] '{System.IO.Path.GetFileName(path)}' euler={model.transform.localEulerAngles.ToString("0.0")} " +
                    $"scale={model.transform.localScale.ToString("0.000")}");
                foreach (Transform child in model.transform)
                {
                    log.AppendLine($"[MODEL]   child '{child.name}' euler={child.localEulerAngles.ToString("0.0")} " +
                                   $"scale={child.localScale.ToString("0.000")}");
                }
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            log.AppendLine("[MODEL] scene TextMeshes");
            foreach (TextMesh text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var renderer = text.GetComponent<MeshRenderer>();
                log.AppendLine(
                    $"[MODEL] '{text.name}' text='{text.text}' pos={text.transform.position.ToString("0.00")} " +
                    $"euler={text.transform.eulerAngles.ToString("0.0")} charSize={text.characterSize} " +
                    $"fontSize={text.fontSize} color={text.color} rendererEnabled={(renderer != null && renderer.enabled)} " +
                    $"material={(renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")} " +
                    $"bounds={(renderer != null ? renderer.bounds.size.ToString("0.00") : "-")}");
            }

            Debug.Log(log.ToString());
            System.IO.File.WriteAllText("TestResults/visual-models.txt", log.ToString());
        }
    }
}
