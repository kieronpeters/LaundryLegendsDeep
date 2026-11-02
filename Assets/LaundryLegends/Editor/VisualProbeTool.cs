using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Read-only probe: raycasts from the known capture cameras through the screen
    /// regions the owner flagged (black slabs by the counter, the entrance door) and
    /// reports exactly which objects the camera sees there.
    /// </summary>
    public static class VisualProbeTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Probe Visual Regions")]
        public static void Probe()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();

            var log = new StringBuilder();
            log.AppendLine("[PROBE] visual region raycasts");

            ProbeView(log, "counter", new Vector3(-3.60f, 1.62f, -0.60f), new Vector3(-4.50f, 1.05f, -2.60f), 65f,
                new[]
                {
                    new Vector2(0.40f, 0.62f), new Vector2(0.52f, 0.55f), new Vector2(0.62f, 0.50f),
                    new Vector2(0.46f, 0.72f), new Vector2(0.70f, 0.45f)
                });

            ProbeView(log, "wide", new Vector3(-6.60f, 2.60f, -4.80f), new Vector3(1.00f, 0.90f, 3.40f), 65f,
                new[]
                {
                    new Vector2(0.45f, 0.45f), new Vector2(0.52f, 0.40f), new Vector2(0.60f, 0.42f)
                });

            ProbeView(log, "entrance-inside", new Vector3(0f, 1.60f, -1.00f), new Vector3(0f, 1.20f, -5.00f), 60f,
                new[]
                {
                    new Vector2(0.30f, 0.45f), new Vector2(0.50f, 0.45f), new Vector2(0.70f, 0.45f),
                    new Vector2(0.50f, 0.75f), new Vector2(0.50f, 0.20f)
                });

            ProbeView(log, "facade", new Vector3(6.50f, 2.20f, -13.00f), new Vector3(0f, 2.00f, -6f), 50f,
                new[]
                {
                    new Vector2(0.50f, 0.55f), new Vector2(0.72f, 0.52f), new Vector2(0.66f, 0.45f),
                    new Vector2(0.38f, 0.40f), new Vector2(0.84f, 0.38f)
                });

            ProbeView(log, "entrance-outside", new Vector3(0f, 1.70f, -9.00f), new Vector3(0f, 1.40f, -6.00f), 45f,
                new[]
                {
                    new Vector2(0.50f, 0.74f), new Vector2(0.45f, 0.76f), new Vector2(0.55f, 0.72f),
                    new Vector2(0.50f, 0.80f), new Vector2(0.30f, 0.55f), new Vector2(0.70f, 0.55f)
                });

            ProbeView(log, "door-leaves", new Vector3(0f, 1.60f, -1.20f), new Vector3(0f, 1.20f, -5.60f), 55f,
                new[]
                {
                    new Vector2(0.22f, 0.50f), new Vector2(0.78f, 0.50f), new Vector2(0.32f, 0.30f),
                    new Vector2(0.68f, 0.30f)
                });

            ProbeView(log, "floor-oval", new Vector3(0f, 1.60f, -1.20f), new Vector3(0f, 1.20f, -5.60f), 55f,
                new[]
                {
                    new Vector2(0.78f, 0.20f), new Vector2(0.75f, 0.22f), new Vector2(0.81f, 0.18f),
                    new Vector2(0.80f, 0.23f)
                });

            ProbeView(log, "left-window", new Vector3(0f, 1.60f, -1.20f), new Vector3(-5f, 1.65f, -6f), 55f,
                new[]
                {
                    new Vector2(0.50f, 0.50f), new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.45f)
                });

            ProbeView(log, "right-window", new Vector3(0f, 1.60f, -1.20f), new Vector3(5f, 1.65f, -6f), 55f,
                new[]
                {
                    new Vector2(0.50f, 0.50f), new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.45f)
                });

            ProbeView(log, "secret-passage", new Vector3(6.0f, 1.20f, 3.0f), new Vector3(9.0f, 1.20f, 3.0f), 60f,
                new[]
                {
                    new Vector2(0.50f, 0.50f), new Vector2(0.50f, 0.40f), new Vector2(0.50f, 0.60f)
                });

            Debug.Log(log.ToString());
            System.IO.File.WriteAllText("TestResults/visual-probe.txt", log.ToString());
        }

        private static void ProbeView(StringBuilder log, string label, Vector3 position, Vector3 lookAt, float fov,
            Vector2[] viewportPoints)
        {
            var cameraObject = new GameObject("ProbeCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
            camera.fieldOfView = fov;
            camera.aspect = 16f / 9f;

            log.AppendLine($"[PROBE] view={label} cam={position} look={lookAt} fov={fov}");

            foreach (Vector2 point in viewportPoints)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(point.x, point.y, 0f));
                log.AppendLine($"[PROBE]   vp=({point.x:0.00},{point.y:0.00}) ray={ray.origin.ToString("0.00")}->{ray.direction.ToString("0.000")}");

                RaycastHit[] hits = Physics.RaycastAll(ray, 30f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                int shown = 0;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null)
                    {
                        continue;
                    }

                    log.AppendLine(
                        $"[PROBE]     hit '{hit.collider.name}' (root '{hit.collider.transform.root.name}') " +
                        $"d={hit.distance:0.00} point={hit.point.ToString("0.00")} " +
                        $"layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} " +
                        $"renderer={(hit.collider.GetComponent<Renderer>() != null ? "yes" : "no")}");
                    if (++shown >= 4)
                    {
                        break;
                    }
                }
            }

            Object.DestroyImmediate(cameraObject);
        }
    }
}
