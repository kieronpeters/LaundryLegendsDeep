using System.Collections;
using LaundryLegends.Business;
using LaundryLegends.Customers;
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
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            DropOffRack rack = Object.FindFirstObjectByType<DropOffRack>();
            GameObject dropOffObject = GameObject.Find("DropOffSlot");
            Assert.IsNotNull(washer);
            Assert.IsNotNull(dryer);
            Assert.IsNotNull(folding);
            Assert.IsNotNull(staff, "The Laundromat scene should have a staff NPC.");
            Assert.IsNotNull(spawner);

            spawner.SetAutoStart(false);
            spawner.SetMaxCustomers(1);
            spawner.DisposeAll();

            CarrySlot dropOff = rack != null && rack.Slots.Count > 0
                ? rack.Slots[0]
                : (dropOffObject != null ? dropOffObject.GetComponent<CarrySlot>() : null);
            Assert.IsNotNull(dropOff, "The scene needs an order drop-off slot.");

            if (rack != null)
            {
                foreach (CarrySlot slot in rack.Slots)
                {
                    LaundryItem leftover = slot != null ? slot.Take() : null;
                    if (leftover != null)
                    {
                        Object.Destroy(leftover.gameObject);
                    }
                }
            }

            washer.SetDuration(0.25f);
            dryer.SetDuration(0.25f);

            // Let any auto-spawned order finish and the staff go idle before the
            // test places its own item, otherwise the machines are still occupied.
            float settle = 0f;
            while (settle < 10f
                   && (washer.State != MachineState.Empty
                       || dryer.State != MachineState.Empty
                       || staff.IsCarrying
                       || folding.ReadyItem != null))
            {
                yield return null;
                settle += Time.deltaTime;
            }

            if (rack != null)
            {
                foreach (CarrySlot slot in rack.Slots)
                {
                    LaundryItem leftover = slot != null ? slot.Take() : null;
                    if (leftover != null)
                    {
                        Object.Destroy(leftover.gameObject);
                    }
                }
            }

            // The staff now walks the full route with animated steps; speed it up
            // so the play mode test does not depend on real walking time.
            staff.SetWalkSpeed(8f);

            var itemObject = new GameObject("LoopLoose");
            LaundryItem item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.loop", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(dropOff.TryPlace(item));

            float elapsed = 0f;
            while (elapsed < 30f && (dropOff.Occupant == null || dropOff.Occupant.Stage != LaundryStage.Wrapped))
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.IsNotNull(dropOff.Occupant, "The staff should return the wrapped item to its order slot.");
            Assert.AreSame(item, dropOff.Occupant, "The wrapped bundle should be the same item instance.");
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
