using System.Collections;
using System.Text;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// End-to-end regression for the Phase 8 playtest blocker "one order, then
    /// nothing, and the wallet never grows".
    /// <para>
    /// Two real customers go through the real staff and machine pipeline with
    /// production durations: no shortened machine timers and no walk-speed
    /// shortcuts. The frame delta is pinned to 1/60 s so the run matches how a
    /// player sees the shop instead of the near-zero delta of an uncapped batch
    /// run.
    /// </para>
    /// </summary>
    public sealed class TwoOrderPlayModeTests
    {
        private const float CaptureFrameRate = 60f;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
        }

        [UnityTest]
        [Timeout(420000)]
        public IEnumerator TwoCustomersCompleteAndPayInProductionTiming()
        {
            Time.captureFramerate = (int)CaptureFrameRate;

            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner, "Laundromat needs a CustomerSpawner.");

            spawner.DisposeCurrent();

            var wallet = new GameState(0);
            var economy = new EconomyService(wallet);
            var ledger = new OrderLedger(wallet, economy);
            spawner.Initialize(ledger, true);
            spawner.SetRespawnDelay(0.5f);

            // Production defaults: the machines keep their authored durations.
            float elapsed = 0f;
            while (elapsed < 300f && ledger.CompletedOrders.Count < 2)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(
                2,
                ledger.CompletedOrders.Count,
                $"Two orders should finish in production timing. After {elapsed:0}s: {Describe(spawner, ledger)}");

            for (int i = 0; i < ledger.CompletedOrders.Count; i++)
            {
                LaundryOrder order = ledger.CompletedOrders[i];
                Assert.AreEqual(
                    OrderState.Paid,
                    order.State,
                    $"Order {order.OrderId} must be paid, not {order.State}. {Describe(spawner, ledger)}");
            }

            Assert.AreEqual(2, ledger.AllOrders.Count, "Exactly two customers should have opened a ticket.");
            Assert.AreNotEqual(
                ledger.AllOrders[0].OrderId,
                ledger.AllOrders[1].OrderId,
                "The second customer must get its own ticket.");

            // The wallet must actually be credited twice, through the economy log.
            int payouts = 0;
            int payoutTotal = 0;
            foreach (Transaction transaction in economy.Transactions)
            {
                if (transaction.Kind == TransactionKind.Income
                    && transaction.Reason.IndexOf("paid", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    payouts++;
                    payoutTotal += transaction.Amount;
                }
            }

            Assert.AreEqual(2, payouts, $"Two payments should appear in the economy log. {Describe(spawner, ledger)}");
            Assert.AreEqual(80, payoutTotal, "Both 40-coin orders should reach the wallet.");
            Assert.GreaterOrEqual(wallet.Money, 80, "The wallet should hold both payouts.");

            Debug.Log(
                $"[TEST] two orders paid; wallet={wallet.Money}; payouts={payouts}; finished at {elapsed:0}s; " +
                Describe(spawner, ledger));
        }

        private static string Describe(CustomerSpawner spawner, OrderLedger ledger)
        {
            var builder = new StringBuilder();
            builder.Append("orders=[");
            for (int i = 0; i < ledger.AllOrders.Count; i++)
            {
                LaundryOrder order = ledger.AllOrders[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(order.OrderId).Append(' ').Append(order.State);
            }

            builder.Append("] activeCustomer=");
            builder.Append(spawner != null && spawner.ActiveCustomer != null
                ? spawner.ActiveCustomer.State.ToString()
                : "none");
            return builder.ToString();
        }
    }
}
