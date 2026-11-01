using System.Collections;
using System.Text;
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
    /// Long economy run: the shipped shop loop must pay enough orders for the
    /// $200 back-room shelf, buy it, open the passage, and let the player walk
    /// into the room. Explicit because it simulates several minutes of play.
    /// <para>
    /// Run with:
    /// -runTests -testPlatform PlayMode -testFilter ProgressionPlayModeTests
    /// </para>
    /// </summary>
    public sealed class ProgressionPlayModeTests
    {
        private const int ShelfPrice = 200;
        private const int TargetWallet = 300;
        private const float MaxSimulatedSeconds = 900f;

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
        [Explicit("Long economy run; simulates several minutes of production timing.")]
        [Timeout(900000)]
        public IEnumerator OrdersFundTheShelfAndTheRoomOpensToThePlayer()
        {
            Time.captureFramerate = 60;

            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            HiddenRoomPurchasePoint purchase = Object.FindFirstObjectByType<HiddenRoomPurchasePoint>();
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(spawner, "Laundromat needs a CustomerSpawner.");
            Assert.IsNotNull(purchase, "The shelf needs a purchase point.");
            Assert.IsNotNull(room, "Laundromat needs a SecretRoomController.");
            Assert.IsNotNull(player, "Laundromat needs a player.");
            Assert.IsNotNull(spawner.Ledger, "The spawner should own the shop ledger.");
            Assert.IsNotNull(spawner.Economy, "The spawner should own the shop economy.");

            OrderLedger ledger = spawner.Ledger;
            int startMoney = ledger.Wallet.Money;
            int payoutsBefore = CountPaidOrders(ledger);

            float elapsed = 0f;
            float nextSample = 0f;
            while (elapsed < MaxSimulatedSeconds && ledger.Wallet.Money < TargetWallet)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= nextSample)
                {
                    nextSample = elapsed + 10f;
                    CustomerAgent agent = spawner.ActiveCustomer;
                    LaundryStaff staff = Object.FindFirstObjectByType<LaundryStaff>();
                    CarrySlot dropOff = GameObject.Find("DropOffSlot") != null
                        ? GameObject.Find("DropOffSlot").GetComponent<CarrySlot>()
                        : null;
                    MachineLoadHook washer = GameObject.Find("Machine_1") != null
                        ? GameObject.Find("Machine_1").GetComponent<MachineLoadHook>()
                        : null;
                    MachineLoadHook dryer = GameObject.Find("Machine_2") != null
                        ? GameObject.Find("Machine_2").GetComponent<MachineLoadHook>()
                        : null;
                    FoldingStation folding = Object.FindFirstObjectByType<FoldingStation>();
                    Debug.Log(
                        $"[PROGRESSION] t={elapsed:0} customer={(agent != null ? agent.State.ToString() : "none")} " +
                        $"pos={(agent != null ? agent.transform.position.ToString("0.00") : "-")} " +
                        $"staffPos={(staff != null ? staff.transform.position.ToString("0.00") : "-")} " +
                        $"staffCarry={(staff != null && staff.IsCarrying)} " +
                        $"dropOffEmpty={(dropOff == null || dropOff.IsEmpty)} " +
                        $"washer={(washer != null ? washer.State.ToString() : "-")} " +
                        $"dryer={(dryer != null ? dryer.State.ToString() : "-")} " +
                        $"folding={(folding != null && folding.IsReady)} " +
                        $"money={ledger.Wallet.Money} orders={ledger.AllOrders.Count}");
                }

                yield return null;
            }

            int paidOrders = CountPaidOrders(ledger) - payoutsBefore;
            Debug.Log(
                $"[PROGRESSION] {elapsed:0}s simulated, wallet {startMoney} -> {ledger.Wallet.Money}, " +
                $"{paidOrders} paid order(s), {ledger.CompletedOrders.Count} completed. {Describe(ledger)}");

            Assert.GreaterOrEqual(
                ledger.Wallet.Money,
                TargetWallet,
                $"The shop must reach {TargetWallet} from repeat orders. {Describe(ledger)}");
            Assert.GreaterOrEqual(paidOrders, 5, "At least five orders should have paid out.");

            Assert.IsTrue(purchase.CanInteract(player.gameObject), "The shelf should be interactable once affordable.");
            Assert.GreaterOrEqual(spawner.Economy.Balance, ShelfPrice, "The purchase point must see the same wallet.");

            int beforePurchase = spawner.Economy.Balance;
            purchase.Interact(player.gameObject);

            Assert.IsTrue(purchase.IsPurchased, "Paying for the shelf should reveal the room.");
            Assert.AreEqual(SecretRoomStage.Usable, room.Stage, "The room should be prepared after purchase.");
            Assert.AreEqual(beforePurchase - ShelfPrice, spawner.Economy.Balance, "The shelf should debit exactly its price.");

            GameObject cover = purchase.gameObject;
            Assert.IsFalse(cover.activeSelf, "The whole concealment should leave the doorway.");

            yield return null;

            // Walk the player through the now-open passage.
            player.transform.position = new Vector3(6.4f, 0.1f, 3f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Physics.SyncTransforms();
            yield return null;

            float walkElapsed = 0f;
            while (walkElapsed < 3f && player.transform.position.x < 8.6f)
            {
                player.Move(Vector2.up);
                walkElapsed += Time.deltaTime;
                yield return null;
            }

            if (player.transform.position.x <= 7.9f)
            {
                var blockers = new StringBuilder();
                Vector3 p = player.transform.position;
                foreach (Collider collider in Physics.OverlapCapsule(
                             p + Vector3.up * 0.35f, p + Vector3.up * 1.3f, 0.3f, ~0,
                             QueryTriggerInteraction.Ignore))
                {
                    blockers.Append(collider.name).Append(" ");
                }

                Debug.Log($"[PROGRESSION] player blocked at {p.ToString("0.00")}: '{blockers}'");
                foreach (RaycastHit forward in Physics.RaycastAll(
                             new Ray(p + Vector3.up * 0.5f, Vector3.right), 1.5f, ~0,
                             QueryTriggerInteraction.Ignore))
                {
                    Debug.Log($"[PROGRESSION]   ahead '{forward.collider.name}' d={forward.distance:0.00} " +
                              $"point={forward.point.ToString("0.00")} bounds={forward.collider.bounds.center.ToString("0.00")}/{forward.collider.bounds.size.ToString("0.00")}");
                }

                var controller = player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    Debug.Log($"[PROGRESSION]   cc center={controller.center} height={controller.height} " +
                              $"radius={controller.radius} bounds={controller.bounds.center.ToString("0.00")}/{controller.bounds.size.ToString("0.00")}");
                }
            }

            Assert.Greater(
                player.transform.position.x,
                7.9f,
                $"The player should be able to walk through the open passage. " +
                $"Ended at {player.transform.position}, {Describe(ledger)}");
            Assert.Greater(player.transform.position.y, -0.5f, "The player should not fall through the back room floor.");
        }

        private static int CountPaidOrders(OrderLedger ledger)
        {
            int paid = 0;
            for (int i = 0; i < ledger.AllOrders.Count; i++)
            {
                if (ledger.AllOrders[i].State == OrderState.Paid)
                {
                    paid++;
                }
            }

            return paid;
        }

        private static string Describe(OrderLedger ledger)
        {
            var builder = new StringBuilder("orders=[");
            for (int i = 0; i < ledger.AllOrders.Count; i++)
            {
                LaundryOrder order = ledger.AllOrders[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(order.OrderId).Append(' ').Append(order.State);
            }

            builder.Append(']');
            return builder.ToString();
        }
    }
}
