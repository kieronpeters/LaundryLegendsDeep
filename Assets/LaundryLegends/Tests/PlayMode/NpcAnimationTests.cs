using System.Collections;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Verifies the animated NPC layer: the shared humanoid controller, the
    /// Speed parameter driven by <see cref="NpcLocomotion"/>, the carry socket
    /// parenting contract, and the absence of single-frame position pops.
    /// </summary>
    public sealed class NpcAnimationTests
    {
        private const string SpeedParameter = "Speed";

        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator NpcAnimatorUsesHumanoidController()
        {
            Transform attendant = FindAttendant();
            Assert.IsNotNull(attendant, "The Laundromat scene should contain the Attendant NPC.");

            Animator animator = attendant.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator, "NPC_Attendant should have an Animator on its Model child.");
            Assert.IsNotNull(animator.runtimeAnimatorController, "The NPC Animator should use NPC_Locomotion.controller.");
            Assert.IsNotNull(animator.avatar, "The NPC Animator should reference a humanoid avatar.");
            Assert.IsTrue(animator.avatar.isHuman, "The NPC avatar should be a humanoid rig.");
            Assert.IsFalse(animator.applyRootMotion, "NPC root motion should stay disabled.");
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SpeedParameterRisesWhileWalkingAndFallsWhenIdle()
        {
            Transform attendant = FindAttendant();
            NpcLocomotion locomotion = attendant.GetComponent<NpcLocomotion>();
            Animator animator = attendant.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(locomotion, "NPC_Attendant needs NpcLocomotion.");
            Assert.IsNotNull(animator, "NPC_Attendant needs an Animator.");

            // The Attendant also runs the staff loop, which moves it on its own
            // and fights the scripted motion below. Disable it so the test owns
            // the movement and the Speed reading is deterministic.
            var staff = attendant.GetComponent<LaundryStaff>();
            if (staff != null)
            {
                staff.enabled = false;
            }

            float walkTime = 0f;
            while (walkTime < 1.0f)
            {
                locomotion.MoveTowards(attendant.position + attendant.forward * 10f, 1.1f);
                walkTime += Time.deltaTime;
                yield return null;
            }

            float walking = animator.GetFloat(SpeedParameter);
            Assert.Greater(walking, 0.5f, $"Speed should rise while walking (was {walking:0.00}).");

            float idleTime = 0f;
            while (idleTime < 1.5f)
            {
                idleTime += Time.deltaTime;
                yield return null;
            }

            float idle = animator.GetFloat(SpeedParameter);
            Assert.Less(idle, 0.2f, $"Speed should fall back to near zero when idle (was {idle:0.00}).");
        }

        [UnityTest]
        public IEnumerator CarriedItemIsParentedUnderCarrySocketAndPhysicsIsRestored()
        {
            Transform attendant = FindAttendant();
            CarrySocket socket = attendant.GetComponent<CarrySocket>();
            Assert.IsNotNull(socket, "NPC_Attendant needs a CarrySocket.");
            Assert.IsNotNull(socket.Anchor, "CarrySocket should resolve a CarryAnchor.");

            var probe = new GameObject("CarryProbe");
            Rigidbody body = probe.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            LaundryItem item = probe.AddComponent<LaundryItem>();
            item.Initialize("test.carry", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(socket.Attach(item), "CarrySocket should accept a free item.");
            Assert.IsTrue(socket.IsCarrying);
            Assert.AreSame(item, socket.Carried);
            Assert.IsTrue(IsParentedUnder(item.transform, socket.Anchor),
                "While carried, the item should be parented under the CarrySocket anchor.");
            Assert.IsTrue(body.isKinematic, "A carried item should be kinematic.");
            Assert.IsFalse(body.useGravity, "A carried item should ignore gravity.");

            yield return null;

            LaundryItem detached = socket.Detach();
            Assert.AreSame(item, detached, "Detach should return the same instance.");
            Assert.IsFalse(socket.IsCarrying);
            Assert.IsFalse(IsParentedUnder(item.transform, socket.Anchor),
                "A detached item should no longer be under the CarrySocket anchor.");
            Assert.IsFalse(body.isKinematic, "Detach should restore the item's kinematic flag.");
            Assert.IsTrue(body.useGravity, "Detach should restore the item's gravity flag.");

            Object.Destroy(probe);
        }

        [UnityTest]
        public IEnumerator NpcAndCarriedItemNeverMoveMoreThanHalfAMetrePerFrame()
        {
            Transform attendant = FindAttendant();
            NpcLocomotion locomotion = attendant.GetComponent<NpcLocomotion>();
            CarrySocket socket = attendant.GetComponent<CarrySocket>();
            Assert.IsNotNull(locomotion);
            Assert.IsNotNull(socket);

            var probe = new GameObject("CarryProbe");
            LaundryItem item = probe.AddComponent<LaundryItem>();
            item.Initialize("test.carry", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(socket.Attach(item));

            float maxNpcStep = 0f;
            float maxItemStep = 0f;
            Vector3 lastNpc = attendant.position;
            Vector3 lastItem = item.transform.position;

            for (int frame = 0; frame < 90; frame++)
            {
                locomotion.MoveTowards(attendant.position + attendant.forward * 10f, 1.1f);
                yield return null;

                maxNpcStep = Mathf.Max(maxNpcStep, new Vector2(
                    attendant.position.x - lastNpc.x,
                    attendant.position.z - lastNpc.z).magnitude);
                maxItemStep = Mathf.Max(maxItemStep, Vector3.Distance(item.transform.position, lastItem));

                lastNpc = attendant.position;
                lastItem = item.transform.position;
            }

            Assert.Less(maxNpcStep, 0.5f, $"The NPC jumped {maxNpcStep:0.00} m in one frame.");
            Assert.Less(maxItemStep, 0.5f, $"The carried item jumped {maxItemStep:0.00} m in one frame.");

            socket.Detach();
            Object.Destroy(probe);
        }

        private static Transform FindAttendant()
        {
            GameObject root = GameObject.Find("AmbientActors");
            return root != null ? root.transform.Find("Attendant") : null;
        }

        private static bool IsParentedUnder(Transform child, Transform parent)
        {
            for (Transform current = child; current != null; current = current.parent)
            {
                if (current == parent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
