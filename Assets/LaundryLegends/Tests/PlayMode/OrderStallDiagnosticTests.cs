using System.Collections;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Diagnostic for the owner report that orders stop when the player walks
    /// from the shop into the hidden room. It runs a real order, teleports the
    /// player into the back room, and logs the pipeline state.
    /// </summary>
    public sealed class OrderStallDiagnosticTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrderContinuesWhenPlayerEntersBackRoom()
        {
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>();
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(room);
            Assert.IsNotNull(spawner);
            Assert.IsNotNull(staff);
            Assert.IsNotNull(player);

            room.UnlockByPurchase();
            spawner.SetAutoStart(false);
            spawner.SetMaxCustomers(1);
            spawner.DisposeAll();
            yield return null;

            CustomerAgent customer = spawner.ActiveCustomer ?? spawner.SpawnNow();
            Assert.IsNotNull(customer, "The scene should spawn an order customer.");
            customer.SetWalkSpeed(6f);
            staff.SetWalkSpeed(8f);

            MachineLoadHook washer = FindHook("Machine_1");
            MachineLoadHook dryer = FindHook("Machine_2");
            if (washer != null) washer.SetDuration(0.3f);
            if (dryer != null) dryer.SetDuration(0.3f);

            yield return new WaitForSeconds(3f);
            Debug.Log($"[STALL] before teleport customer={customer.State} staff={staff.transform.position} dropOff={FindDropOffOccupant()}");
            player.transform.position = new Vector3(10f, 0.1f, 3f);
            Physics.SyncTransforms();
            yield return null;

            float deadline = Time.time + 60f;
            while (customer != null && customer.State != CustomerState.Done && Time.time < deadline)
            {
                yield return new WaitForSeconds(2f);
                Debug.Log($"[STALL] t={Time.time:0.0} customer={customer.State} staff={staff.transform.position} dropOff={FindDropOffOccupant()} folding={FindFoldingReady()}");
            }

            Assert.IsNotNull(customer);
            Assert.AreEqual(CustomerState.Done, customer.State,
                "The order should complete even with the player in the back room.");
        }

        private static MachineLoadHook FindHook(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<MachineLoadHook>() : null;
        }

        private static string FindDropOffOccupant()
        {
            GameObject target = GameObject.Find("DropOffSlot");
            CarrySlot slot = target != null ? target.GetComponent<CarrySlot>() : null;
            return slot != null && slot.Occupant != null ? slot.Occupant.Stage.ToString() : "empty";
        }

        private static string FindFoldingReady()
        {
            FoldingStation folding = Object.FindFirstObjectByType<FoldingStation>();
            return folding != null && folding.ReadyItem != null ? folding.ReadyItem.Stage.ToString() : "none";
        }
    }
}
