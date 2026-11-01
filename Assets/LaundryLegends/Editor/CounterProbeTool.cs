using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Read-only probe for the customer counter standoff: lists every collider
    /// the NPC capsule would touch at the counter arrival point, plus the bounds
    /// of the counter itself.
    /// </summary>
    public static class CounterProbeTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Probe Counter Colliders")]
        public static void Probe()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();

            var log = new StringBuilder();
            Vector3 target = new Vector3(-3.30f, 0f, -2.60f);
            log.AppendLine($"[COUNTER] arrival target {target}");

            foreach (string name in new[] { "Counter", "CounterTop", "Counter_Apron", "SecretDoor_Foreshadowed" })
            {
                Transform found = Find(name);
                if (found == null)
                {
                    continue;
                }

                foreach (Collider collider in found.GetComponentsInChildren<Collider>(true))
                {
                    log.AppendLine(
                        $"[COUNTER] '{collider.name}' type={collider.GetType().Name} enabled={collider.enabled} " +
                        $"trigger={collider.isTrigger} bounds={collider.bounds.center.ToString("0.00")}/{collider.bounds.size.ToString("0.00")}");
                }
            }

            Vector3 bottom = target + Vector3.up * 0.35f;
            Vector3 top = target + Vector3.up * 1.3f;
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, 0.3f, ~0, QueryTriggerInteraction.Ignore);
            log.AppendLine($"[COUNTER] capsule overlaps at arrival point: {overlaps.Length}");
            foreach (Collider collider in overlaps)
            {
                Vector3 point = collider.ClosestPoint(target + Vector3.up * 0.8f);
                log.AppendLine(
                    $"[COUNTER]   '{collider.name}' root='{collider.transform.root.name}' type={collider.GetType().Name} " +
                    $"bounds={collider.bounds.center.ToString("0.00")}/{collider.bounds.size.ToString("0.00")} " +
                    $"closest={point.ToString("0.00")} pointY={point.y:0.00}");
            }

            Debug.Log(log.ToString());
            System.IO.File.WriteAllText("TestResults/counter-probe.txt", log.ToString());
        }

        private static Transform Find(string name)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
