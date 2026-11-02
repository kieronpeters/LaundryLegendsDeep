using System.Collections;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Regression cover for the Phase 8 playtest blocker "no HUD and no
    /// interaction prompt in the shipped scene". The views must exist, be
    /// enabled, and be wired to the live spawner and player interactor.
    /// </summary>
    public sealed class PlaytestHudTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudTicketAndPromptAreLiveAndWired()
        {
            yield return null;

            OrderTicketView ticket = Object.FindFirstObjectByType<OrderTicketView>();
            Assert.IsNotNull(ticket, "The HUD needs an OrderTicketView.");
            Assert.IsTrue(ticket.gameObject.activeInHierarchy, "The order ticket object must be active.");
            Assert.IsTrue(ticket.enabled, "The order ticket component must be enabled.");

            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner, "The scene needs a CustomerSpawner.");
            Assert.AreSame(spawner, ticket.Spawner, "The HUD ticket must read the scene's spawner.");

            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            Assert.IsNotNull(prompt, "The player needs an InteractionPromptView.");
            Assert.IsTrue(prompt.gameObject.activeInHierarchy, "The prompt object must be active.");
            Assert.IsTrue(prompt.enabled, "The prompt component must be enabled.");

            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "The scene needs a PlayerInteractor.");
            Assert.AreSame(interactor, prompt.Interactor, "The prompt must read the player interactor.");
            Assert.IsNotNull(
                interactor.GetComponentInChildren<Camera>(),
                "The player interactor needs a camera to aim from.");

            Assert.IsNotNull(
                Object.FindFirstObjectByType<FirstPersonController>(),
                "The player controller should be in the scene.");

            ControlsHintView controls = Object.FindFirstObjectByType<ControlsHintView>();
            Assert.IsNotNull(controls, "The HUD needs a controls legend.");
            Assert.IsTrue(controls.gameObject.activeInHierarchy, "The controls legend object must be active.");
            Assert.IsTrue(controls.enabled, "The controls legend component must be enabled.");
            string legend = controls.BuildText();
            StringAssert.Contains(interactor.InteractKeyLabel, legend, "The legend should show the interact key.");
            StringAssert.Contains("Pause", legend);
            StringAssert.Contains("Move", legend);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TicketTextTracksTheLiveLedger()
        {
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(spawner);
            Assert.IsNotNull(spawner.Ledger, "The spawner should own a ledger for the HUD to read.");

            var itemObject = new GameObject("HudLooseItem");
            try
            {
                var item = itemObject.AddComponent<LaundryItem>();
                item.Initialize("hud.test", LaundryItemKind.LooseLaundry);

                Assert.IsTrue(
                    spawner.Ledger.TryCreateOrder(item, 40, Time.time, out LaundryOrder order),
                    spawner.Ledger.LastResult);
                Assert.IsTrue(spawner.Ledger.TryAcceptOrder(order.OrderId, Time.time), spawner.Ledger.LastResult);

                string text = OrderTicketView.BuildText(spawner.Ledger);
                StringAssert.Contains(order.OrderId, text, "The ticket should name the live order.");
                StringAssert.Contains("waiting for drop-off", text);

                Assert.AreEqual(
                    $"Coins: {spawner.Ledger.Wallet.Money}",
                    OrderTicketView.BuildWalletLine(spawner.Ledger));
            }
            finally
            {
                Object.Destroy(itemObject);
            }

            yield return null;
        }
    }
}
