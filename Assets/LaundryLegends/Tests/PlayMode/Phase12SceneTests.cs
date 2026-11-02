using System.Collections;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Phase 12 art contract: every machine uses a detailed appliance model, the
    /// drum visibly turns while a cycle runs, the carpet stays gated behind the
    /// expansion, and the waiting-area NPCs sit clear of the chairs and counter.
    /// </summary>
    public sealed class Phase12SceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryMachineHasDetailedModelAndTurnsItsDrum()
        {
            int checkedMachines = 0;
            for (int i = 1; i <= 8; i++)
            {
                GameObject machine = GameObject.Find($"Machine_{i}");
                Assert.IsNotNull(machine, $"Machine_{i} is missing.");

                Transform model = machine.transform.Find("ApplianceModel");
                Assert.IsNotNull(model, $"Machine_{i} has no detailed ApplianceModel.");
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Assert.GreaterOrEqual(renderers.Length, 3,
                    $"Machine_{i} should be a detailed multi-part model, found {renderers.Length} renderers.");

                bool urp = false;
                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && material.shader != null
                            && material.shader.name.Contains("Universal Render Pipeline"))
                        {
                            urp = true;
                            break;
                        }
                    }

                    if (urp)
                    {
                        break;
                    }
                }

                Assert.IsTrue(urp, $"Machine_{i} still uses a non-URP material.");
                checkedMachines++;
            }

            Assert.AreEqual(8, checkedMachines);

            GameObject target = GameObject.Find("Machine_1");
            var hook = target.GetComponent<MachineLoadHook>();
            Assert.IsNotNull(hook);
            Assert.IsNotNull(hook.DrumVisual, "Machine_1 needs a drum visual to animate.");

            hook.SetDuration(20f);
            // A production customer may already be using this washer; clear it so
            // the test controls the cycle without disturbing the queue.
            hook.ForceUnload();
            var itemObject = new GameObject("DrumTumbleItem");
            var item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.tumble", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(hook.LoadProcessedItem(item));
            Assert.IsTrue(hook.StartCycle(), "The washer should start with stock available.");

            Quaternion before = hook.DrumVisual.localRotation;
            yield return new WaitForSeconds(1.2f);
            Quaternion after = hook.DrumVisual.localRotation;
            Assert.Greater(Quaternion.Angle(before, after), 45f,
                "The drum should visibly rotate while washing.");

            hook.ForceUnload();
            Object.Destroy(itemObject);
        }

        [UnityTest]
        public IEnumerator MachineCollidersCoverEyeHeight()
        {
            for (int i = 1; i <= 8; i++)
            {
                GameObject machine = GameObject.Find($"Machine_{i}");
                Assert.IsNotNull(machine, $"Machine_{i} is missing.");
                var collider = machine.GetComponent<BoxCollider>();
                Assert.IsNotNull(collider, $"Machine_{i} needs a BoxCollider for interaction.");
                Assert.GreaterOrEqual(collider.bounds.max.y, 1.55f,
                    $"Machine_{i} collider stops at {collider.bounds.max.y:0.00}; the eye-level ray passes over it.");
                Assert.GreaterOrEqual(collider.bounds.size.y, 1.5f,
                    $"Machine_{i} collider is too short for the detailed model.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpansionBayStaysSealedUntilPurchased()
        {
            GameObject bay = FindInactive("ExpansionBay");
            GameObject seal = FindInactive("ExpansionSeal");
            GameObject carpet = FindInactive("Decor_ArcadeCarpet");

            Assert.IsNotNull(bay, "The expansion bay should exist.");
            Assert.IsNotNull(seal, "The sealing panel should exist.");
            Assert.IsNotNull(carpet, "The arcade carpet should exist.");

            Assert.IsFalse(bay.activeSelf, "The bay must stay closed before purchase.");
            Assert.IsTrue(seal.activeSelf, "The seal must block the bay before purchase.");
            Assert.IsFalse(carpet.activeSelf, "The carpet is bought after the expansion.");
            Assert.IsTrue(carpet.transform.IsChildOf(bay.transform),
                "The carpet may only be placed inside the expansion bay.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaitingAreaNpcsAreClearOfFurniture()
        {
            GameObject phone = GameObject.Find("PhoneRegular");
            GameObject waypoint = GameObject.Find("Waypoint_Waiting");
            Assert.IsNotNull(phone, "The phone regular should be in the scene.");
            Assert.IsNotNull(waypoint, "The ambient route should end in the waiting area.");
            Assert.IsNull(GameObject.Find("Waypoint_Counter"),
                "The ambient customer must not route into the counter.");

            Vector3 phonePosition = phone.transform.position;
            Vector3 waypointPosition = waypoint.transform.position;
            Assert.Less(Vector3.Distance(waypointPosition, new Vector3(-3.9f, 0f, -2.0f)), 10f);
            Assert.Greater(Vector3.Distance(waypointPosition, new Vector3(-3.9f, 0f, -2.0f)), 6f,
                "The waiting waypoint must be away from the old counter position.");

            foreach (string chair in new[] { "Waiting_Chair_1", "Waiting_Chair_2", "Chair_Waiting_A", "Chair_Waiting_B" })
            {
                GameObject chairObject = GameObject.Find(chair);
                if (chairObject == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    new Vector3(phonePosition.x, 0f, phonePosition.z),
                    new Vector3(chairObject.transform.position.x, 0f, chairObject.transform.position.z));
                Assert.Greater(distance, 0.5f, $"{phone} overlaps {chair}.");
            }

            yield return null;
        }

        private static GameObject FindInactive(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
