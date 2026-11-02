using System.Collections.Generic;
using LaundryLegends.Business;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>Phase 10 contracts: clock, consumable stock, rack and billing.</summary>
    public sealed class BusinessOperationsTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject root in _created)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _created.Clear();
        }

        private GameObject Track(GameObject root)
        {
            _created.Add(root);
            return root;
        }

        private ConsumableShelf CreateShelf(ConsumableType type, int cost, int count)
        {
            var definition = ScriptableObject.CreateInstance<ConsumableDefinition>();
            definition.Configure($"consumable.{type}", type.ToString(), type, cost, 10);
            var root = Track(new GameObject($"Shelf_{type}"));
            var stack = new GameObject("Stack");
            stack.transform.SetParent(root.transform, false);
            var shelf = root.AddComponent<ConsumableShelf>();
            shelf.Configure(definition, null, stack.transform, count);
            return shelf;
        }

        [Test]
        public void ClockRollsOverAndSkipsToMorning()
        {
            var go = Track(new GameObject("Clock"));
            var clock = go.AddComponent<BusinessClock>();
            clock.Configure(1, 480, 1f);
            int dayChanges = 0;
            clock.DayChanged += _ => dayChanges++;

            Assert.AreEqual("Day 1  08:00", clock.DisplayText);
            clock.AdvanceMinutes(1439);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(479, clock.MinuteOfDay);
            Assert.AreEqual(1, dayChanges);

            clock.SkipToNextDay();
            Assert.AreEqual(3, clock.Day);
            Assert.AreEqual(480, clock.MinuteOfDay);
            Assert.AreEqual(2, dayChanges);
        }

        [Test]
        public void ShelfBuysOneUnitAndConsumesIt()
        {
            ConsumableShelf shelf = CreateShelf(ConsumableType.Detergent, 2, 5);
            var economy = new EconomyService(new GameState(20));

            Assert.IsTrue(shelf.TryBuyOne(economy, 0f));
            Assert.AreEqual(6, shelf.Count);
            Assert.AreEqual(18, economy.Balance);

            shelf.SetCount(10);
            Assert.IsFalse(shelf.TryBuyOne(economy, 0f), "A full shelf rejects the purchase.");
            Assert.AreEqual(18, economy.Balance);

            Assert.IsTrue(shelf.TryConsumeOne());
            Assert.AreEqual(9, shelf.Count);
            shelf.SetCount(0);
            Assert.IsFalse(shelf.TryConsumeOne(), "An empty shelf blocks consumption.");
        }

        [Test]
        public void MachineRefusesToStartWithoutStockAndRecovers()
        {
            ConsumableShelf shelf = CreateShelf(ConsumableType.Detergent, 2, 0);
            var stock = Track(new GameObject("Stock")).AddComponent<ConsumableStockService>();
            stock.Configure(new[] { shelf });

            var machineObject = Track(new GameObject("Machine"));
            var slotObject = new GameObject("DrumSlot");
            slotObject.transform.SetParent(machineObject.transform, false);
            var slot = slotObject.AddComponent<CarrySlot>();
            slot.SetAcceptedKinds(LaundryItemKind.LooseLaundry);
            var machine = machineObject.AddComponent<MachineLoadHook>();
            machine.Configure(slot, MachineKind.Washer);
            machine.SetStockService(stock);

            var itemObject = Track(new GameObject("Item"));
            var item = itemObject.AddComponent<LaundryItem>();
            item.Initialize("test.item", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(machine.LoadProcessedItem(item));

            Assert.IsFalse(machine.StartCycle(), "The wash must be blocked when detergent is empty.");
            Assert.AreEqual(MachineState.Loaded, machine.State);

            shelf.SetCount(1);
            Assert.IsTrue(machine.StartCycle(), "Restocking should let the loaded wash start.");
            Assert.AreEqual(MachineState.Running, machine.State);
            Assert.AreEqual(0, shelf.Count);
        }

        [Test]
        public void BillIsGeneratedEveryThirtyDaysAndLateFeeIsOneTime()
        {
            var clock = Track(new GameObject("Clock")).AddComponent<BusinessClock>();
            clock.Configure(1, 480, 1f);
            var billing = Track(new GameObject("Billing")).AddComponent<BillingService>();
            billing.Configure(clock, null);
            billing.ConfigureAmounts(250, 90, 60, 30, 3, 0.1f);

            clock.AdvanceMinutes(29 * 1440);
            Assert.AreEqual(1, billing.Bills.Count);
            Assert.AreEqual(400, billing.CurrentBill.Total);
            Assert.AreEqual(3, billing.CurrentBill.Lines.Count);

            var economy = new EconomyService(new GameState(500));
            Assert.IsTrue(billing.TryPayCurrent(economy, 0f));
            Assert.AreEqual(100, economy.Balance);
            Assert.IsTrue(billing.CurrentBill == null);

            clock.AdvanceMinutes(30 * 1440);
            Assert.AreEqual(2, billing.Bills.Count, "The paid bill stays in history; a new bill is generated.");
            Assert.AreEqual(400, billing.TotalDue, "Only the new unpaid bill is due.");

            var broke = new EconomyService(new GameState(0));
            Assert.IsFalse(billing.TryPayCurrent(broke, 0f));
            Assert.AreEqual(0, broke.Balance, "An unaffordable bill cannot make the wallet negative.");

            clock.AdvanceMinutes(4 * 1440);
            Assert.IsTrue(billing.CurrentBill.LateFeeApplied);
            Assert.AreEqual(440, billing.CurrentBill.Total);
        }

        [Test]
        public void RackReservesAndReleasesSlots()
        {
            var rackObject = Track(new GameObject("Rack"));
            var rack = rackObject.AddComponent<DropOffRack>();
            var slots = new List<CarrySlot>();
            for (int i = 0; i < 10; i++)
            {
                var slotObject = new GameObject($"Slot_{i}");
                slotObject.transform.SetParent(rackObject.transform, false);
                var slot = slotObject.AddComponent<CarrySlot>();
                slot.AcceptAllKinds();
                slots.Add(slot);
            }

            rack.Configure(slots);
            Assert.AreEqual(10, rack.Capacity);
            CarrySlot reserved = rack.ReserveFreeSlot();
            Assert.IsNotNull(reserved);
            Assert.IsTrue(rack.IsReserved(reserved));
            rack.Release(reserved);
            Assert.IsFalse(rack.IsReserved(reserved));
            Assert.AreEqual(0, rack.ReadyCount);
        }
    }
}
