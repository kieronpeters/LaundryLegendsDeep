using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    public sealed class LaundryLoopTests
    {
        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StaffRunsWasherDryerAndWrapLoop()
        {
            MachineLoadHook washer = FindHook("Machine_1");
            MachineLoadHook dryer = FindHook("Machine_2");
            FoldingStation folding = Object.FindFirstObjectByType<FoldingStation>();
            LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>();
            GameObject dropOffObject = GameObject.Find("DropOffSlot");
            Assert.IsNotNull(washer);
            Assert.IsNotNull(dryer);
            Assert.IsNotNull(folding);
            Assert.IsNotNull(staff, "The Laundromat scene should have a staff NPC.");
            Assert.IsNotNull(dropOffObject);

            washer.SetDuration(0.25f);
            dryer.SetDuration(0.25f);

            // The staff now walks the full route with animated steps; speed it up
            // so the play mode test does not depend on real walking time.
            staff.SetWalkSpeed(8f);

            var itemObject = new GameObject("LoopLoose");
            LaundryItem item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.loop", LaundryItemKind.LooseLaundry);
            CarrySlot dropOff = dropOffObject.GetComponent<CarrySlot>();
            Assert.IsTrue(dropOff.TryPlace(item));

            float elapsed = 0f;
            while (elapsed < 20f && !folding.IsReady)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.IsTrue(folding.IsReady, "Staff should wash, dry, fold, and wrap the item.");
            Assert.AreSame(item, folding.ReadyItem, "The wrapped bundle should be the same item instance.");
            Assert.AreEqual(LaundryStage.Wrapped, item.Stage);
            Assert.IsNotNull(item.GetComponentInChildren<Renderer>());

            Object.Destroy(itemObject);
        }

        private static MachineLoadHook FindHook(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<MachineLoadHook>() : null;
        }
    }
}
