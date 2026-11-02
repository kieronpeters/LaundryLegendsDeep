using System.Collections;
using System.Collections.Generic;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Drives one customer through the whole order lifecycle and checks the
    /// ledger pays exactly once for the same laundry item it carried in.
    /// </summary>
    public sealed class CustomerLifecycleTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CustomerWalksInWaitsPaysOnceAndLeaves()
        {
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner, "Laundromat needs a CustomerSpawner.");

            spawner.SetAutoStart(false);
            spawner.SetMaxCustomers(1);
            spawner.DisposeAll();

            var wallet = new GameState(100);
            var ledger = new OrderLedger(wallet);
            spawner.Initialize(ledger, false);
            spawner.SetRespawnDelay(0f);

            CustomerAgent agent = spawner.SpawnNow();
            Assert.IsNotNull(agent, "Spawner should create a customer.");
            agent.SetWalkSpeed(6f);

            // Let the real staff + machine pipeline process the order.
            var staff = Object.FindFirstObjectByType<LaundryStaff>();
            if (staff != null)
            {
                staff.SetWalkSpeed(8f);
            }

            var washer = FindHook("Machine_1");
            var dryer = FindHook("Machine_2");
            if (washer != null)
            {
                washer.SetDuration(0.3f);
            }

            if (dryer != null)
            {
                dryer.SetDuration(0.3f);
            }

            var states = new List<CustomerState>();
            agent.StateEntered += (_, state) => states.Add(state);

            float elapsed = 0f;
            while (elapsed < 45f && agent.State != CustomerState.Done)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(CustomerState.Done, agent.State, "Customer should finish the loop.");
            CollectionAssert.Contains(states, CustomerState.Arrive);
            CollectionAssert.Contains(states, CustomerState.HandOff);
            CollectionAssert.Contains(states, CustomerState.Waiting);
            CollectionAssert.Contains(states, CustomerState.Collect);
            CollectionAssert.Contains(states, CustomerState.Pay);
            CollectionAssert.Contains(states, CustomerState.Leave);

            Assert.AreEqual(150, wallet.Money, "Wallet should be credited once, plus the fast-service tip.");
            Assert.AreEqual(10, agent.LastTip, "A fast order should tip.");
            Assert.AreEqual(1, ledger.CompletedOrders.Count);
        }

        private static MachineLoadHook FindHook(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<MachineLoadHook>() : null;
        }

        [UnityTest]
        public IEnumerator CustomerRespawnsAfterLeaving()
        {
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner);
            spawner.SetAutoStart(false);
            spawner.SetMaxCustomers(1);
            spawner.DisposeAll();

            var wallet = new GameState(100);
            var ledger = new OrderLedger(wallet);
            spawner.SetRespawnDelay(0.5f);
            spawner.Initialize(ledger, true);

            var staff = Object.FindFirstObjectByType<LaundryStaff>();
            if (staff != null)
            {
                staff.SetWalkSpeed(8f);
            }

            var washer = FindHook("Machine_1");
            if (washer != null)
            {
                washer.SetDuration(0.3f);
            }

            var dryer = FindHook("Machine_2");
            if (dryer != null)
            {
                dryer.SetDuration(0.3f);
            }

            CustomerAgent first = spawner.SpawnNow();
            Assert.IsNotNull(first);
            first.SetWalkSpeed(6f);

            float elapsed = 0f;
            while (elapsed < 45f && first.State != CustomerState.Done)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(CustomerState.Done, first.State, "First customer should finish.");

            float respawn = 0f;
            while (respawn < 10f
                   && (spawner.ActiveCustomer == null || spawner.ActiveCustomer == first))
            {
                respawn += Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(spawner.ActiveCustomer, "A second customer should spawn after the first leaves.");
            Assert.AreNotSame(first, spawner.ActiveCustomer, "The respawned customer should be a new instance.");
            Assert.GreaterOrEqual(wallet.Money, 140, "The first order should have paid out before respawning.");
        }

        [UnityTest]
        public IEnumerator ImpatientCustomerAbandonsAndLeavesWithoutPaying()
        {
            var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner);
            spawner.SetAutoStart(false);
            spawner.SetMaxCustomers(1);
            spawner.DisposeAll();

            var wallet = new GameState(100);
            var ledger = new OrderLedger(wallet);
            spawner.Initialize(ledger, false);
            spawner.SetRespawnDelay(0f);

            CustomerAgent agent = spawner.SpawnNow();
            Assert.IsNotNull(agent);
            agent.SetWalkSpeed(6f);
            agent.SetPatience(1f);

            float elapsed = 0f;
            while (elapsed < 20f && agent.State != CustomerState.Done)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(CustomerState.Done, agent.State, "Impatient customer should leave.");
            Assert.AreEqual(100, wallet.Money, "An abandoned order must not pay out.");

            yield return new WaitForSeconds(0.4f);
            var folding = Object.FindFirstObjectByType<FoldingStation>();
            Assert.IsNotNull(folding);
            Assert.IsFalse(folding.IsReady, "Abandoned laundry must not clog the folding station.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PayingTheSameOrderTwiceCreditsOnce()
        {
            var wallet = new GameState(100);
            var ledger = new OrderLedger(wallet);

            var itemObject = new GameObject("OrderItem");
            LaundryItem item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.order", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(ledger.TryCreateOrder(item, 40, 0f, out LaundryOrder order));
            Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 1f));
            Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 2f));
            Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 3f));
            Assert.IsTrue(ledger.TryCollect(order.OrderId, 4f));
            Assert.IsTrue(ledger.TryPay(order.OrderId, 5f));

            Assert.IsFalse(ledger.TryPay(order.OrderId, 6f), "Second payment must be rejected.");
            Assert.AreEqual(140, wallet.Money, "Second payment must not credit money.");

            Object.Destroy(itemObject);
            yield return null;
        }
    }
}
