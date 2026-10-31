using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class ItemPickupRulesTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (GameObject created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void PromptNamesTheItemKind()
        {
            Assert.AreEqual("Pick up basket", CreatePickup(LaundryItemKind.Basket).InteractionPrompt);
            Assert.AreEqual("Pick up laundry", CreatePickup(LaundryItemKind.LooseLaundry).InteractionPrompt);
            Assert.AreEqual("Pick up bag", CreatePickup(LaundryItemKind.Bag).InteractionPrompt);
        }

        [Test]
        public void FreeItemCanBePickedUpThroughItsInteraction()
        {
            LaundryItemInteractable pickup = CreatePickup(LaundryItemKind.LooseLaundry);
            CarryController carry = CreateCarry();

            Assert.IsTrue(pickup.CanInteract(carry.gameObject));
            pickup.Interact(carry.gameObject);

            Assert.AreSame(pickup.Item, carry.HeldItem);
        }

        [Test]
        public void CarryingOneItemDoesNotStealASecond()
        {
            LaundryItemInteractable first = CreatePickup(LaundryItemKind.LooseLaundry);
            LaundryItemInteractable second = CreatePickup(LaundryItemKind.LooseLaundry);
            CarryController carry = CreateCarry();

            first.Interact(carry.gameObject);
            Assert.AreSame(first.Item, carry.HeldItem);

            second.Interact(carry.gameObject);
            Assert.AreSame(first.Item, carry.HeldItem, "A second interaction must not replace the held item.");
            Assert.IsNull(second.Item.transform.parent, "A rejected pickup must leave the second item in place.");
        }

        [Test]
        public void HeldOrSlottedItemsAreNotPickupEligible()
        {
            LaundryItemInteractable held = CreatePickup(LaundryItemKind.LooseLaundry);
            CarryController carry = CreateCarry();
            held.Interact(carry.gameObject);

            Assert.IsFalse(held.CanInteract(carry.gameObject), "A held item must not offer pickup.");

            LaundryItemInteractable slottedPickup = CreatePickup(LaundryItemKind.LooseLaundry);
            CarrySlot slot = CreateSlot();
            Assert.IsTrue(slot.TryPlace(slottedPickup.Item));

            Assert.IsFalse(slottedPickup.CanInteract(carry.gameObject), "A slotted item must not offer pickup.");
        }

        [Test]
        public void InteractorWithoutCarryCannotPickUp()
        {
            LaundryItemInteractable pickup = CreatePickup(LaundryItemKind.LooseLaundry);
            var interactor = new GameObject("NoCarry");
            _created.Add(interactor);

            Assert.IsFalse(pickup.CanInteract(interactor));
        }

        private LaundryItemInteractable CreatePickup(LaundryItemKind kind)
        {
            var target = new GameObject($"{kind}Item");
            _created.Add(target);
            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize($"test.{kind}", kind);
            LaundryItemInteractable pickup = target.AddComponent<LaundryItemInteractable>();
            pickup.Configure(item);
            return pickup;
        }

        private CarryController CreateCarry()
        {
            var player = new GameObject("Player");
            _created.Add(player);
            return player.AddComponent<CarryController>();
        }

        private CarrySlot CreateSlot()
        {
            var slotObject = new GameObject("Slot");
            _created.Add(slotObject);
            return slotObject.AddComponent<CarrySlot>();
        }
    }
}
