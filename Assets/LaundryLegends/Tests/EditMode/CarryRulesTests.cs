using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class CarryRulesTests
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
        public void ItemIdentityIsUniquePerInstanceAndStable()
        {
            LaundryItem first = CreateItem("loose-a", LaundryItemKind.LooseLaundry);
            LaundryItem second = CreateItem("loose-a", LaundryItemKind.LooseLaundry);

            Assert.AreEqual("loose-a", first.TemplateId);
            Assert.IsNotEmpty(first.InstanceId);
            Assert.IsNotEmpty(second.InstanceId);
            Assert.AreNotEqual(first.InstanceId, second.InstanceId, "Two instances must not share an ID.");
            Assert.AreEqual(first.InstanceId, first.InstanceId, "An instance ID must be stable.");
        }

        [Test]
        public void OnlyLooseLaundryIsWasherLoadable()
        {
            Assert.IsTrue(CreateItem("loose", LaundryItemKind.LooseLaundry).IsWasherLoadable);
            Assert.IsFalse(CreateItem("basket", LaundryItemKind.Basket).IsWasherLoadable);
            Assert.IsFalse(CreateItem("bag", LaundryItemKind.Bag).IsWasherLoadable);
        }

        [Test]
        public void NonCarryableItemIsRejected()
        {
            LaundryItem item = CreateItem("fixed", LaundryItemKind.LooseLaundry, false);

            Assert.IsFalse(item.IsCarryable);
            Assert.IsFalse(item.IsWasherLoadable);
        }

        [Test]
        public void SlotAcceptsOnlyCompatibleItemsAndOnlyOnce()
        {
            CarrySlot slot = CreateSlot();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);

            LaundryItem loose = CreateItem("loose", LaundryItemKind.LooseLaundry);
            LaundryItem secondLoose = CreateItem("loose-2", LaundryItemKind.LooseLaundry);
            LaundryItem basket = CreateItem("basket", LaundryItemKind.Basket);

            Assert.IsTrue(slot.IsEmpty);
            Assert.IsFalse(slot.CanAccept(basket), "A slot limited to loose laundry must reject a basket.");
            Assert.IsTrue(slot.TryPlace(loose));
            Assert.IsFalse(slot.IsEmpty);
            Assert.AreSame(loose, slot.Occupant);
            Assert.IsFalse(slot.TryPlace(secondLoose), "An occupied slot must reject a second item.");

            Assert.AreSame(loose, slot.Take());
            Assert.IsTrue(slot.IsEmpty);
            Assert.IsNull(loose.transform.parent, "Taking an item should detach it from the slot.");
        }

        [Test]
        public void SlotDefaultsToAcceptingAllKinds()
        {
            CarrySlot slot = CreateSlot();
            LaundryItem bag = CreateItem("bag", LaundryItemKind.Bag);

            Assert.IsTrue(slot.CanAccept(bag));
        }

        [Test]
        public void RemovingAMismatchedItemLeavesTheOccupant()
        {
            CarrySlot slot = CreateSlot();
            LaundryItem loose = CreateItem("loose", LaundryItemKind.LooseLaundry);
            LaundryItem other = CreateItem("other", LaundryItemKind.Bag);

            Assert.IsTrue(slot.TryPlace(loose));
            Assert.IsFalse(slot.TryRemove(other));
            Assert.AreSame(loose, slot.Occupant);
        }

        private LaundryItem CreateItem(string templateId, LaundryItemKind kind, bool carryable = true)
        {
            var target = new GameObject(templateId);
            _created.Add(target);
            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize(templateId, kind, carryable);
            return item;
        }

        private CarrySlot CreateSlot()
        {
            var target = new GameObject("CarrySlot");
            _created.Add(target);
            return target.AddComponent<CarrySlot>();
        }
    }
}
