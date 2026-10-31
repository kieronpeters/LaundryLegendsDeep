using System.Collections.Generic;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class MachineLoadRulesTests
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
        public void EmptyMachineLoadsOneLooseItemAndRejectsASecond()
        {
            MachineLoadHook machine = CreateMachine(out CarrySlot slot);
            CarryController first = CreateCarry();
            CarryController second = CreateCarry();
            LaundryItem loose = CreateItem("loose", LaundryItemKind.LooseLaundry);
            LaundryItem other = CreateItem("loose-2", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(first.TryPickUp(loose));
            Assert.IsTrue(machine.TryLoad(first));
            Assert.AreSame(loose, machine.LoadedItem);
            Assert.IsFalse(first.IsCarrying);

            Assert.IsTrue(second.TryPickUp(other));
            Assert.IsFalse(machine.TryLoad(second), "An occupied machine must reject a second load.");
            Assert.AreSame(loose, slot.Occupant, "A rejected load must not replace the occupant.");
            Assert.AreSame(other, second.HeldItem, "A rejected load must not change ownership.");
        }

        [Test]
        public void BasketAndBagAreRejectedWithoutChangingOwnership()
        {
            MachineLoadHook machine = CreateMachine(out _);
            CarryController carry = CreateCarry();
            LaundryItem basket = CreateItem("basket", LaundryItemKind.Basket);

            Assert.IsTrue(carry.TryPickUp(basket));
            Assert.IsFalse(machine.TryLoad(carry));
            Assert.IsNull(machine.LoadedItem);
            Assert.AreSame(basket, carry.HeldItem);
            StringAssert.Contains("Basket", machine.LastResult);
        }

        [Test]
        public void UnloadReturnsTheExactInstanceAndCannotRunTwice()
        {
            MachineLoadHook machine = CreateMachine(out _);
            CarryController loader = CreateCarry();
            CarryController unloader = CreateCarry();
            LaundryItem loose = CreateItem("loose", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(loader.TryPickUp(loose));
            Assert.IsTrue(machine.TryLoad(loader));

            LaundryItem returned = machine.TryUnload(unloader);
            Assert.AreSame(loose, returned, "Unload must return the stored instance.");
            Assert.AreSame(loose, unloader.HeldItem);
            Assert.IsNull(machine.LoadedItem, "Unload must empty the slot.");
            Assert.IsNull(machine.TryUnload(CreateCarry()), "A second unload must return nothing.");
        }

        [Test]
        public void UnloadIsRejectedWhileHandsAreFull()
        {
            MachineLoadHook machine = CreateMachine(out CarrySlot slot);
            CarryController loader = CreateCarry();
            CarryController fullHands = CreateCarry();
            LaundryItem stored = CreateItem("loose", LaundryItemKind.LooseLaundry);
            LaundryItem carried = CreateItem("loose-2", LaundryItemKind.LooseLaundry);

            Assert.IsTrue(loader.TryPickUp(stored));
            Assert.IsTrue(machine.TryLoad(loader));
            Assert.IsTrue(fullHands.TryPickUp(carried));

            Assert.IsNull(machine.TryUnload(fullHands));
            Assert.AreSame(stored, slot.Occupant, "A rejected unload must leave the machine loaded.");
            Assert.AreSame(carried, fullHands.HeldItem);
            StringAssert.Contains("Hands full", machine.LastResult);
        }

        [Test]
        public void PromptReflectsLoadState()
        {
            MachineLoadHook machine = CreateMachine(out _);
            CarryController carry = CreateCarry();
            LaundryItem loose = CreateItem("loose", LaundryItemKind.LooseLaundry);

            Assert.AreEqual("Load washer", machine.InteractionPrompt);
            Assert.IsTrue(carry.TryPickUp(loose));
            Assert.IsTrue(machine.TryLoad(carry));
            Assert.AreEqual("Take laundry", machine.InteractionPrompt);
            Assert.AreSame(loose, machine.TryUnload(carry));
            Assert.AreEqual("Load washer", machine.InteractionPrompt);
        }

        private MachineLoadHook CreateMachine(out CarrySlot slot)
        {
            var machineObject = new GameObject("Machine");
            _created.Add(machineObject);

            var slotObject = new GameObject("DrumSlot");
            slotObject.transform.SetParent(machineObject.transform, false);
            slot = slotObject.AddComponent<CarrySlot>();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);

            MachineLoadHook machine = machineObject.AddComponent<MachineLoadHook>();
            machine.Configure(slot);
            return machine;
        }

        private CarryController CreateCarry()
        {
            var player = new GameObject("Player");
            _created.Add(player);
            return player.AddComponent<CarryController>();
        }

        private LaundryItem CreateItem(string templateId, LaundryItemKind kind, bool carryable = true)
        {
            var target = new GameObject(templateId);
            _created.Add(target);
            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize(templateId, kind, carryable);
            return item;
        }
    }
}
