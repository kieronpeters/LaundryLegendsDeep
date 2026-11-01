using System.IO;
using System.Text;
using LaundryLegends.Arcade;
using LaundryLegends.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Prints the measured layout of the shop: where the hidden room is, what it
    /// contains, and where the arcade machine and its purchase point currently sit.
    /// Read-only; it opens and discards the scene.
    /// </summary>
    public static class LayoutReportTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Diagnostics/Report Shop Layout")]
        public static void Report()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var report = new StringBuilder();
            report.AppendLine("=== SHOP LAYOUT ===");

            var room = Object.FindFirstObjectByType<SecretRoomController>();
            if (room != null)
            {
                var serialized = new SerializedObject(room);
                GameObject content = serialized.FindProperty("_roomContent")?.objectReferenceValue as GameObject;
                GameObject door = serialized.FindProperty("_sealedDoor")?.objectReferenceValue as GameObject;

                report.AppendLine($"hidden room: controller='{room.name}' stage={room.Stage}");
                report.AppendLine($"  door object: {(door != null ? door.name : "<none>")} at {(door != null ? door.transform.position.ToString() : "-")}");
                report.AppendLine($"  room content: {(content != null ? content.name : "<none>")} at {(content != null ? content.transform.position.ToString() : "-")}");

                if (content != null)
                {
                    Bounds bounds = new Bounds(content.transform.position, Vector3.zero);
                    bool first = true;
                    foreach (Renderer renderer in content.GetComponentsInChildren<Renderer>(true))
                    {
                        if (first)
                        {
                            bounds = renderer.bounds;
                            first = false;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }

                    report.AppendLine($"  room content bounds centre {bounds.center} size {bounds.size}");
                    report.AppendLine("  children:");
                    foreach (Transform child in content.GetComponentsInChildren<Transform>(true))
                    {
                        if (child != content.transform)
                        {
                            report.AppendLine($"    - {child.name} at {child.position}");
                        }
                    }
                }
            }
            else
            {
                report.AppendLine("hidden room: NOT FOUND");
            }

            AppendObject(report, "arcade cabinet", Object.FindFirstObjectByType<ArcadeCabinet>(FindObjectsInactive.Include));
            AppendObject(report, "install point", Object.FindFirstObjectByType<ArcadeInstallPoint>());
            AppendObject(report, "arcade traffic", Object.FindFirstObjectByType<ArcadeTraffic>(FindObjectsInactive.Include));

            foreach (string name in new[] { "Player", "CustomerLane", "CustomerSpawner", "ArcadePlaceholder", "GreyboxRoom" })
            {
                Transform found = FindTransform(scene, name);
                report.AppendLine($"{name}: {(found != null ? found.position.ToString() : "<none>")}");

                if (found != null && name == "GreyboxRoom")
                {
                    Bounds bounds = new Bounds(found.position, Vector3.zero);
                    bool first = true;
                    foreach (Renderer renderer in found.GetComponentsInChildren<Renderer>(true))
                    {
                        if (first)
                        {
                            bounds = renderer.bounds;
                            first = false;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }

                    report.AppendLine($"  laundry bounds centre {bounds.center} size {bounds.size}");
                }
            }

            report.AppendLine("=== END ===");
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/layout-report.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void AppendObject<T>(StringBuilder report, string label, T component) where T : Component
        {
            report.AppendLine(component != null
                ? $"{label}: {component.name} at {component.transform.position}"
                : $"{label}: <none>");
        }

        private static Transform FindTransform(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}

