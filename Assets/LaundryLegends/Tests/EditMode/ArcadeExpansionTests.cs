using System.Collections.Generic;
using LaundryLegends.Arcade;
using LaundryLegends.Core;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Laundry;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Guards the Phase 9 arcade business: multiple machines, upgrades, reputation,
    /// income/upkeep, expansion and milestones. The Phase 8 single-machine tests
    /// stay in ArcadeBusinessTests and must remain unchanged.
    /// </summary>
    public sealed class ArcadeExpansionTests
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

        private SecretRoomController CreateRoom(bool usable = true)
        {
            var go = Track(new GameObject("Room"));
            var room = go.AddComponent<SecretRoomController>();
            room.Configure(1, 1);
            room.TryCollectClue();
            room.RecordCompletedOrder();
            room.TryReveal();

            if (usable)
            {
                for (int i = 0; i < 8 && room.Stage != SecretRoomStage.Usable; i++)
                {
                    room.TryPrepare();
                }
            }

            return room;
        }

        private ArcadeMachineDefinition CreateDefinition(string id, string name, int cost, int income, float weight)
        {
            var definition = ScriptableObject.CreateInstance<ArcadeMachineDefinition>();
            definition.Configure(id, name, cost, income, weight);
            return definition;
        }

        private ArcadeService CreateService(
            SecretRoomController room,
            int money,
            out EconomyService economy,
            out ArcadeCabinet cabinet)
        {
            GameObject root = Track(new GameObject("ArcadeRoot"));

            GameObject cabinetObject = new GameObject("Cabinet");
            cabinetObject.transform.SetParent(root.transform, false);
            cabinetObject.transform.position = new Vector3(2f, 0f, 0f);
            cabinet = cabinetObject.AddComponent<ArcadeCabinet>();

            GameObject attractObject = new GameObject("Attract");
            attractObject.transform.SetParent(root.transform, false);
            var attract = attractObject.AddComponent<ArcadeAttractMode>();

            economy = new EconomyService(new GameState(money));

            ArcadeService service = root.AddComponent<ArcadeService>();
            service.Configure(room, cabinet, attract, cabinetObject.transform, economy);
            service.SetMachineCost(350);
            service.SetInstallSeconds(0f);
            return service;
        }

        private ArcadeExtraMachine BuildExtraSlot(
            ArcadeService service,
            ArcadeMachineDefinition definition,
            bool reachable,
            out GameObject cabinet,
            out Transform anchor)
        {
            GameObject slotRoot = Track(new GameObject($"Slot_{definition.DisplayName}"));
            slotRoot.SetActive(reachable);

            var anchorObject = new GameObject($"Anchor_{definition.DisplayName}");
            anchorObject.transform.SetParent(slotRoot.transform, false);

            var installObject = new GameObject($"Install_{definition.DisplayName}");
            installObject.transform.SetParent(slotRoot.transform, false);
            installObject.AddComponent<ArcadeInstallPoint>();

            var cabinetObject = new GameObject($"Cabinet_{definition.DisplayName}");
            cabinetObject.transform.SetParent(slotRoot.transform, false);
            cabinetObject.AddComponent<ArcadeCabinet>();
            cabinet = cabinetObject;

            service.AddSlot(anchorObject.transform, installObject, definition, cabinetObject);
            anchor = anchorObject.transform;
            return service.GetExtraMachine(service.SlotCount - 1);
        }

        [Test]
        public void SecondMachinePurchaseDebitsOnceAndPaysItsOwnIncome()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 2000, out EconomyService economy, out _);
            ArcadeMachineDefinition racer = CreateDefinition("arcade.racer", "Racer", 250, 5, 1.4f);
            BuildExtraSlot(service, racer, true, out GameObject cabinet, out _);

            Assert.AreEqual(2, service.SlotCount);
            Assert.AreEqual(ArcadeInstallResult.Success, service.EvaluateInstall(1));
            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(1, 0f));
            Assert.AreEqual(1750, economy.Balance, "The second machine should debit its own price once.");
            Assert.IsTrue(service.IsSlotActive(1));
            Assert.IsTrue(cabinet.activeSelf, "A bought machine should spawn visibly.");

            Assert.AreEqual(ArcadeInstallResult.AlreadyInstalled, service.TryInstall(1, 0f));
            Assert.AreEqual(1750, economy.Balance, "A repeat purchase must not debit again.");

            string visitId = service.CreateVisitId(1);
            StringAssert.StartsWith("arcade-s1-visit-", visitId);
            Assert.AreEqual(5, service.GetVisitIncome(1));

            Assert.IsTrue(service.TryCollectVisitIncome(1, visitId, 5, 0f));
            Assert.AreEqual(1755, economy.Balance);
            Assert.AreEqual(5, service.LifetimeIncome);

            Assert.IsFalse(service.TryCollectVisitIncome(1, visitId, 5, 0f));
            Assert.AreEqual(1755, economy.Balance, "One visit must pay exactly once.");
        }

        [Test]
        public void SealedAnchorRejectsPurchaseWithoutDebiting()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 2000, out EconomyService economy, out _);
            ArcadeMachineDefinition puzzle = CreateDefinition("arcade.puzzle", "Puzzle", 400, 8, 1.8f);
            BuildExtraSlot(service, puzzle, false, out _, out _);

            Assert.AreEqual(ArcadeInstallResult.SlotUnavailable, service.EvaluateInstall(1));
            Assert.AreEqual(ArcadeInstallResult.SlotUnavailable, service.TryInstall(1, 0f));
            Assert.AreEqual(2000, economy.Balance);
            Assert.IsFalse(service.IsSlotInstalled(1));
        }

        [Test]
        public void UpgradePurchaseRaisesIncomeAndTrafficAndRejectsRepeat()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 1000, out EconomyService economy, out _);
            service.SetStartActive(true);
            service.SetStarterDefinition(CreateDefinition("arcade.fighter", "Fighter", 120, 10, 1f));

            var decor = Track(new GameObject("NeonDecor"));
            decor.SetActive(false);
            var upgrade = ScriptableObject.CreateInstance<ArcadeUpgradeDefinition>();
            upgrade.Configure("arcade.upgrade.neon", "Neon sign", 150, 1.1f, 1.2f, decor);

            Assert.AreEqual(10, service.GetVisitIncome(0), "Starter income should use the authored definition before upgrades.");

            Assert.IsTrue(service.TryBuyUpgrade(upgrade, 0f));
            Assert.AreEqual(850, economy.Balance);
            Assert.IsTrue(service.HasUpgrade("arcade.upgrade.neon"));
            Assert.IsTrue(decor.activeSelf, "Buying an upgrade should reveal its decor.");
            Assert.AreEqual(11, service.GetVisitIncome(0), "The income multiplier should apply to every machine.");
            Assert.AreEqual(1.2f, service.TrafficFactor, 0.001f);

            Assert.IsFalse(service.TryBuyUpgrade(upgrade, 0f));
            Assert.AreEqual(850, economy.Balance, "A repeat upgrade must not debit again.");
        }

        [Test]
        public void UpkeepDebitsActiveMachinesAndNeverGoesNegative()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 5, out EconomyService economy, out _);
            service.SetStartActive(true);
            service.SetUpkeep(120f, 1);

            service.Tick(120f);
            Assert.AreEqual(4, economy.Balance);
            Assert.AreEqual(1, service.UpkeepPaid);

            service.Tick(120f);
            Assert.AreEqual(3, economy.Balance);

            Assert.IsTrue(economy.TrySpend(3, "Test drain", 0f));
            Assert.AreEqual(0, economy.Balance);

            service.Tick(120f);
            Assert.AreEqual(0, economy.Balance, "Unpayable upkeep must not create a negative balance.");
            Assert.AreEqual(1, service.UpkeepSkipped);
        }

        [Test]
        public void ReputationMovesFootfallAndDecaysToZero()
        {
            var go = Track(new GameObject("Reputation"));
            var reputation = go.AddComponent<LaundryReputation>();

            reputation.SetValue(0);
            Assert.AreEqual(0.8f, reputation.FootfallMultiplier, 0.001f);

            reputation.RecordPaidOrder();
            reputation.RecordTip();
            reputation.RecordAbandonedOrder();
            Assert.AreEqual(4, reputation.Value);

            reputation.SetValue(100);
            Assert.AreEqual(1.4f, reputation.FootfallMultiplier, 0.001f);

            reputation.SetValue(10);
            reputation.Tick(120f);
            Assert.AreEqual(9, reputation.Value);
            reputation.Tick(12000f);
            Assert.AreEqual(0, reputation.Value, "Decay must floor at zero.");
        }

        [Test]
        public void OrderLedgerFeedsReputationOnPayTipAndAbandon()
        {
            var go = Track(new GameObject("Reputation"));
            var reputation = go.AddComponent<LaundryReputation>();
            var economy = new EconomyService(new GameState(0));
            var ledger = new OrderLedger(economy.Wallet, economy);
            ledger.SetReputation(reputation);

            LaundryOrder PayOneOrder(int price)
            {
                var itemObject = Track(new GameObject("LaundryItem"));
                var item = itemObject.AddComponent<LaundryItem>();
                Assert.IsTrue(ledger.TryCreateOrder(item, price, 0f, out LaundryOrder order));
                Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 0f));
                Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 0f));
                Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 0f));
                Assert.IsTrue(ledger.TryCollect(order.OrderId, 0f));
                return order;
            }

            LaundryOrder paid = PayOneOrder(40);
            Assert.IsTrue(ledger.TryPay(paid.OrderId, 0f));
            Assert.AreEqual(4, reputation.Value);

            Assert.IsTrue(ledger.TryAddTip(paid.OrderId, 10, 0f));
            Assert.AreEqual(6, reputation.Value);

            var abandonedItemObject = Track(new GameObject("LaundryItem2"));
            var abandonedItem = abandonedItemObject.AddComponent<LaundryItem>();
            Assert.IsTrue(ledger.TryCreateOrder(abandonedItem, 40, 0f, out LaundryOrder abandoned));
            Assert.IsTrue(ledger.TryAcceptOrder(abandoned.OrderId, 0f));
            Assert.IsTrue(ledger.TryAbandon(abandoned.OrderId, 0f));
            Assert.AreEqual(4, reputation.Value);
        }

        [Test]
        public void MilestonesFireOnceAcrossMachinesIncomeReputationAndExpansion()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 2000, out EconomyService economy, out _);
            var reputation = Track(new GameObject("Reputation")).AddComponent<LaundryReputation>();
            service.SetReputation(reputation);

            var fired = new List<string>();
            service.MilestoneReached += id => fired.Add(id);

            service.SetStartActive(true);
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.FirstMachine));

            ArcadeMachineDefinition racer = CreateDefinition("arcade.racer", "Racer", 250, 5, 1f);
            BuildExtraSlot(service, racer, true, out _, out _);
            service.TryInstall(1, 0f);
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.SecondMachine));

            string visitId = service.CreateVisitId(0);
            Assert.IsTrue(service.TryCollectVisitIncome(0, visitId, 250, 0f));
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.Income250));

            reputation.SetValue(50);
            service.Tick(0f);
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.Reputation50));

            service.SetExpansionBay(null, null, 300);
            Assert.IsTrue(service.TryExpand(0f));
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.Expanded));

            Assert.AreEqual(5, fired.Count, "Each milestone should fire exactly once.");
            service.Tick(0f);
            Assert.AreEqual(5, fired.Count, "Re-evaluating must not re-fire milestones.");
            Assert.AreEqual(5, service.State.Milestones.Count);
        }

        [Test]
        public void ExpansionOpensVisibleBayAndUnlocksSealedAnchors()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 1000, out EconomyService economy, out _);

            var bay = Track(new GameObject("ExpansionBay"));
            var seal = Track(new GameObject("ExpansionSeal"));
            bay.SetActive(false);
            service.SetExpansionBay(bay, seal, 300);

            var anchor = new GameObject("BayAnchor");
            anchor.transform.SetParent(bay.transform, false);
            var install = new GameObject("BayInstall");
            install.transform.SetParent(bay.transform, false);
            install.AddComponent<ArcadeInstallPoint>();
            var cabinet = new GameObject("BayCabinet");
            cabinet.transform.SetParent(bay.transform, false);
            cabinet.AddComponent<ArcadeCabinet>();
            service.AddSlot(anchor.transform, install, CreateDefinition("arcade.puzzle", "Puzzle", 400, 8, 1f), cabinet);

            Assert.IsFalse(service.IsSlotReachable(1));
            Assert.AreEqual(ArcadeInstallResult.SlotUnavailable, service.EvaluateInstall(1));

            Assert.IsTrue(service.TryExpand(0f));
            Assert.AreEqual(700, economy.Balance);
            Assert.IsTrue(service.IsExpanded);
            Assert.IsTrue(bay.activeSelf, "The extension must become visible.");
            Assert.IsFalse(seal.activeSelf, "The sealing panel must open.");
            Assert.IsTrue(service.IsSlotReachable(1));
            Assert.AreEqual(2, service.Capacity);

            Assert.IsFalse(service.TryExpand(0f));
            Assert.AreEqual(700, economy.Balance, "A repeat expansion must not debit again.");
        }

        [Test]
        public void TrafficIntervalUsesFootfallButNeverExceedsTheAuthoredInterval()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 1000, out _, out ArcadeCabinet cabinet);
            service.SetStartActive(true);

            var reputation = Track(new GameObject("Reputation")).AddComponent<LaundryReputation>();
            reputation.SetValue(100);
            service.SetReputation(reputation);

            var trafficObject = Track(new GameObject("Traffic"));
            var traffic = trafficObject.AddComponent<ArcadeTraffic>();
            traffic.Configure(service, cabinet.transform, cabinet.transform, null);
            traffic.SetInterval(20f);

            Assert.AreEqual(20f / 1.4f, traffic.EffectiveIntervalSeconds, 0.01f,
                "High reputation should shorten the cadence.");
            Assert.Less(traffic.EffectiveIntervalSeconds, 20f, "High reputation should shorten the cadence.");

            traffic.SetInterval(1f);
            Assert.AreEqual(1f, traffic.EffectiveIntervalSeconds, 0.001f,
                "A deliberately short test interval must not be lengthened by the balance floor.");
        }

        [Test]
        public void VisitorToSecondMachinePaysThatSlotExactlyOnce()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 2000, out EconomyService economy, out ArcadeCabinet cabinet);
            service.SetStartActive(true);
            ArcadeMachineDefinition racer = CreateDefinition("arcade.racer", "Racer", 250, 5, 1f);
            ArcadeExtraMachine slot = BuildExtraSlot(service, racer, true, out GameObject slotCabinet, out _);
            service.TryInstall(1, 0f);

            var trafficObject = Track(new GameObject("Traffic"));
            trafficObject.transform.position = Vector3.zero;
            var traffic = trafficObject.AddComponent<ArcadeTraffic>();
            traffic.Configure(service, trafficObject.transform, cabinet.transform, null);
            traffic.SetPlaySeconds(0.1f);
            traffic.SetInterval(9999f);

            int before = economy.Balance;
            ArcadeVisitor visitor = traffic.SpawnVisitor(1);
            Assert.IsNotNull(visitor);
            Assert.AreEqual(1, visitor.SlotIndex);

            for (int i = 0; i < 200 && !visitor.IsFinished; i++)
            {
                visitor.Tick(0.25f);
            }

            Assert.IsTrue(visitor.IsFinished, "The visitor should reach the machine, play and leave.");
            Assert.IsTrue(visitor.WasPaid);
            Assert.AreEqual(before + 5, economy.Balance);
            Assert.AreEqual(1, slot.State.VisitCount);
            Assert.AreEqual(0, service.VisitCount, "Slot 1 income must not be booked to slot 0.");

            Object.DestroyImmediate(visitor.gameObject);
        }

        [Test]
        public void LongSessionLinksLaundryReputationTrafficIncomeSecondMachineAndExpansion()
        {
            SecretRoomController room = CreateRoom();
            ArcadeService service = CreateService(room, 1000, out EconomyService economy, out ArcadeCabinet cabinet);
            service.SetInstallSeconds(0f);
            service.SetStartActive(true);
            var reputation = Track(new GameObject("Reputation")).AddComponent<LaundryReputation>();
            service.SetReputation(reputation);

            var ledger = new OrderLedger(economy.Wallet, economy);
            ledger.SetReputation(reputation);

            // Laundry side: nine paid orders with tips lift reputation to 54 and pay $450.
            for (int i = 0; i < 9; i++)
            {
                SettleOrder(ledger, 40, 10);
            }

            Assert.AreEqual(1450, economy.Balance, "Nine paid orders plus tips should add $450.");
            Assert.AreEqual(54, reputation.Value, "Paid orders (+4) and tips (+2) should reach 54.");
            service.Tick(0f);
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.Reputation50));

            // Reputation shortens the arcade cadence.
            var trafficObject = Track(new GameObject("Traffic"));
            var traffic = trafficObject.AddComponent<ArcadeTraffic>();
            traffic.Configure(service, cabinet.transform, cabinet.transform, null);
            traffic.SetInterval(20f);
            Assert.Less(traffic.EffectiveIntervalSeconds, 20f, "Reputation should raise footfall.");

            // Slot 0 visits fund the second machine.
            int slot0Income = service.GetVisitIncome(0);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(service.TryCollectVisitIncome(0, service.CreateVisitId(0), slot0Income, 0f));
            }

            ArcadeMachineDefinition racer = CreateDefinition("arcade.racer", "Racer", 250, 5, 1f);
            BuildExtraSlot(service, racer, true, out _, out _);
            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(1, 0f));
            Assert.IsTrue(service.State.HasMilestone(ArcadeMilestones.SecondMachine));

            // Slot 1 visits fund the expansion and the lifetime-income milestone.
            int slot1Income = service.GetVisitIncome(1);
            for (int i = 0; i < 40; i++)
            {
                Assert.IsTrue(service.TryCollectVisitIncome(1, service.CreateVisitId(1), slot1Income, 0f));
            }

            service.SetExpansionBay(null, null, 300);
            Assert.IsTrue(service.TryExpand(0f));
            service.Tick(0f);

            int expected = 1450 + slot0Income * 20 - 250 + slot1Income * 40 - 300;
            Assert.AreEqual(expected, economy.Balance, "The wallet must reconcile every laundry and arcade transaction.");
            Assert.AreEqual(slot0Income * 20 + slot1Income * 40, service.LifetimeIncome);
            Assert.AreEqual(5, service.State.Milestones.Count, "The full session should earn every milestone.");
            Assert.AreEqual(expected - 1000, new EconomyServiceSnapshot(economy).NetChange(),
                "The transaction log must reconcile with the wallet change.");
        }

        private void SettleOrder(OrderLedger ledger, int price, int tip)
        {
            var itemObject = Track(new GameObject("LongSessionItem"));
            var item = itemObject.AddComponent<LaundryItem>();
            Assert.IsTrue(ledger.TryCreateOrder(item, price, 0f, out LaundryOrder order));
            Assert.IsTrue(ledger.TryAcceptOrder(order.OrderId, 0f));
            Assert.IsTrue(ledger.TryMarkProcessing(order.OrderId, 0f));
            Assert.IsTrue(ledger.TryMarkReady(order.OrderId, 0f));
            Assert.IsTrue(ledger.TryCollect(order.OrderId, 0f));
            Assert.IsTrue(ledger.TryPay(order.OrderId, 0f));
            Assert.IsTrue(ledger.TryAddTip(order.OrderId, tip, 0f));
        }

        /// <summary>Small helper that re-derives the wallet from the transaction log.</summary>
        private readonly struct EconomyServiceSnapshot
        {
            private readonly EconomyService _economy;

            public EconomyServiceSnapshot(EconomyService economy)
            {
                _economy = economy;
            }

            public int NetChange()
            {
                int balance = 0;
                foreach (Transaction transaction in _economy.Transactions)
                {
                    balance += transaction.Kind == TransactionKind.Income ? transaction.Amount : -transaction.Amount;
                }

                return balance;
            }
        }
    }
}
