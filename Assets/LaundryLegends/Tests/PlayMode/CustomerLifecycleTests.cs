using System.Collections;
using System.Collections.Generic;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Laundry;
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

            spawner.DisposeCurrent();

            var wallet = new GameState(100);
            var ledger = new OrderLedger(wallet);
            spawner.Initialize(ledger, false);
            spawner.SetRespawnDelay(0f);

            CustomerAgent agent = spawner.SpawnNow();
            Assert.IsNotNull(agent, "Spawner should create a customer.");
            agent.SetWalkSpeed(6f);

            var states = new List<CustomerState>();
            agent.StateEntered += (_, state) => states.Add(state);

            float elapsed = 0f;
            while (elapsed < 20f && agent.State != CustomerState.Done)
            {
                if (agent.State == CustomerState.Waiting && agent.Item != null
                    && agent.Item.Stage != LaundryStage.Wrapped)
                {
                    // Stand in for the processing pipeline: mark the order ready.
                    agent.Item.SetStage(LaundryStage.Wrapped);
                }

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

            Assert.AreEqual(140, wallet.Money, "Wallet should be credited exactly once.");
            Assert.AreEqual(1, ledger.CompletedOrders.Count);
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
