using LaundryLegends.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// One-shot diagnostic for the NPC animation scale blow-up.
    /// Logs bounds with the Animator enabled and disabled so we can tell
    /// whether the retargeted clips are inflating the skeleton.
    /// </summary>
    public static class NpcDiagnostics
    {
        [MenuItem("Laundry Legends/Phase 4/Diagnose NPC Scale")]
        public static void DiagnoseNpcScale()
        {
            var scene = EditorSceneManager.OpenScene("Assets/LaundryLegends/Scenes/Laundromat.unity", OpenSceneMode.Single);
            GameObject root = GameObject.Find("AmbientActors");
            if (root == null)
            {
                Debug.LogError("[DIAG] No AmbientActors root.");
                return;
            }

            Transform attendant = root.transform.Find("Attendant");
            if (attendant == null)
            {
                Debug.LogError("[DIAG] No Attendant.");
                return;
            }

            Animator animator = attendant.GetComponentInChildren<Animator>(true);
            Debug.Log($"[DIAG] root pos={attendant.position} lossyScale={attendant.lossyScale} modelScale={attendant.Find("Model")?.localScale}");
            LogBounds("animator-on", attendant);

            if (animator != null)
            {
                Debug.Log($"[DIAG] avatar={animator.avatar?.name} isHuman={animator.avatar?.isHuman} valid={animator.avatar?.isValid} controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")}");

                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    Debug.Log($"[DIAG] hips world={hips.position} local={hips.localPosition} lossy={hips.lossyScale}");
                }

                animator.enabled = false;
                LogBounds("animator-off", attendant);
                if (hips != null)
                {
                    Debug.Log($"[DIAG] animator-off hips world={hips.position} local={hips.localPosition}");
                }
            }

            LogBounds("customer", root.transform.Find("StreetCustomer"));
            LogBounds("regular", root.transform.Find("PhoneRegular"));

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void LogBounds(string label, Transform target)
        {
            if (target == null)
            {
                Debug.Log($"[DIAG] {label}: missing");
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                Debug.Log($"[DIAG] {label} {renderer.GetType().Name} '{renderer.name}' center={bounds.center} size={bounds.size} enabled={renderer.enabled} activeInHierarchy={renderer.gameObject.activeInHierarchy}");
            }
        }
    }
}
