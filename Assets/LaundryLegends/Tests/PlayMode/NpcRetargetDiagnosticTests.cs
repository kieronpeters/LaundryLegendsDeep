using System.Collections;
using LaundryLegends.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Temporary diagnostic: measures NPC renderer bounds and hip position with
    /// the Animator running and disabled, to prove whether the retargeted clips
    /// are inflating the skeleton at runtime.
    /// </summary>
    public sealed class NpcRetargetDiagnosticTests
    {
        [UnityTest]
        public IEnumerator LogBoundsWithAndWithoutAnimator()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;

            GameObject root = GameObject.Find("AmbientActors");
            Assert.IsNotNull(root);
            Transform attendant = root.transform.Find("Attendant");
            Assert.IsNotNull(attendant);

            Animator animator = attendant.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator);

            yield return new WaitForSeconds(1f);

            Log("running", attendant, animator);

            animator.enabled = false;
            yield return null;
            yield return null;
            Log("disabled", attendant, animator);

            animator.enabled = true;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }

            Log("re-enabled", attendant, animator);
        }

        private static void Log(string phase, Transform attendant, Animator animator)
        {
            SkinnedMeshRenderer skinned = attendant.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Bounds bounds = skinned != null ? skinned.bounds : default;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            string hipsText = hips != null
                ? $"hipsLocal={hips.localPosition} hipsWorld={hips.position} hipsScale={hips.lossyScale}"
                : "hips=null";
            Debug.Log($"[DIAG] {phase} center={bounds.center} size={bounds.size} {hipsText}");
        }
    }
}
