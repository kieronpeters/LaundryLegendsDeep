using System.Collections;
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
    public sealed class ItemPickupTests
    {
        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator BasketIsTargetedAndPickedUpThroughInteraction()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            CarryController carry = player.GetComponent<CarryController>();
            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            LaundryItem basket = FindItem(LaundryItemKind.Basket);

            Assert.IsNotNull(basket, "The scene should contain a basket.");
            Assert.IsNotNull(interactor, "Player should have a PlayerInteractor.");
            Assert.IsNotNull(carry, "Player should have a CarryController.");
            Assert.IsNotNull(prompt, "Player should have an InteractionPromptView.");
            Assert.IsFalse(carry.IsCarrying, "The player should start empty-handed.");

            Vector3 basketPosition = basket.transform.position;
            player.transform.position = new Vector3(basketPosition.x, 0.05f, basketPosition.z + 1.0f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.Look(new Vector2(0f, -27f)); // Look down at the floor item from the machine aisle.
            Physics.SyncTransforms();
            yield return null;
            yield return null;

            Assert.AreSame(basket.GetComponent<LaundryItemInteractable>(), interactor.CurrentInteractable,
                "The basket should resolve to its pickup interaction.");
            Assert.AreEqual("Pick up basket", interactor.CurrentPrompt);
            Assert.IsTrue(prompt.IsVisible);
            Assert.AreEqual($"Pick up basket [{interactor.InteractKeyLabel}]", prompt.CurrentLabel);

            Assert.IsTrue(interactor.TryInteract());
            Assert.AreSame(basket, carry.HeldItem, "Interaction should pick up the basket.");
            Assert.AreNotEqual(null, basket.transform.parent, "A held item should follow the hold anchor.");

            carry.TryDrop();
            yield return null;
        }

        private static LaundryItem FindItem(LaundryItemKind kind)
        {
            LaundryItem[] items = Object.FindObjectsByType<LaundryItem>(FindObjectsSortMode.None);
            foreach (LaundryItem item in items)
            {
                if (item.Kind == kind)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
