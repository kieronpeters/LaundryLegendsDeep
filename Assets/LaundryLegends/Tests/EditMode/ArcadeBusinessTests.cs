using System.Collections.Generic;
using LaundryLegends.Arcade;
using LaundryLegends.Core;
using LaundryLegends.Economy;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;

namespace LaundryLegends.Tests.EditMode
{
    /// <summary>
    /// Guards the Phase 8 arcade business loop: one purchase, one install, and
    /// arcade income that can never be credited twice.
    /// </summary>
    public sealed class ArcadeBusinessTests
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

        private SecretRoomController CreateRoom(bool usable)
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

        private ArcadeService CreateService(
            SecretRoomController room,
            int money,
            out ArcadeCabinet cabinet,
            out ArcadeAttractMode attract,
            out EconomyService economy)
        {
            GameObject root = Track(new GameObject("ArcadeRoot"));

            GameObject cabinetObject = new GameObject("Cabinet");
            cabinetObject.transform.SetParent(root.transform, false);
            cabinet = cabinetObject.AddComponent<ArcadeCabinet>();

            GameObject attractObject = new GameObject("Attract");
            attractObject.transform.SetParent(root.transform, false);
            attract = attractObject.AddComponent<ArcadeAttractMode>();

            economy = new EconomyService(new GameState(money));

            ArcadeService service = root.AddComponent<ArcadeService>();
            service.Configure(room, cabinet, attract, cabinet.transform, economy);
            service.SetMachineCost(350);
            service.SetInstallSeconds(0f);
            return service;
        }

        [Test]
        public void PurchaseDebitsOnceAndActivatesTheMachine()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 1000, out _, out ArcadeAttractMode attract, out EconomyService economy);

            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(0f));
            Assert.AreEqual(650, economy.Balance, "The machine should debit its price exactly once.");
            Assert.IsTrue(service.IsInstalled);
            Assert.AreEqual(ArcadePhase.Active, service.Phase);
            Assert.IsTrue(attract.IsAttracting, "An active, idle machine should attract.");
        }

        [Test]
        public void BuyingTheMachineTwiceIsRejectedWithoutASecondDebit()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 1000, out _, out _, out EconomyService economy);

            service.TryInstall(0f);
            int afterFirst = economy.Balance;

            Assert.AreEqual(ArcadeInstallResult.AlreadyInstalled, service.TryInstall(0f));
            Assert.AreEqual(afterFirst, economy.Balance, "A rejected purchase must not debit.");
        }

        [Test]
        public void UnaffordablePurchaseLeavesTheWalletAndMachineUntouched()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 100, out _, out ArcadeAttractMode attract, out EconomyService economy);

            Assert.AreEqual(ArcadeInstallResult.Unaffordable, service.TryInstall(0f));
            Assert.AreEqual(100, economy.Balance);
            Assert.IsFalse(service.IsInstalled);
            Assert.IsFalse(attract.IsAttracting);
        }

        [Test]
        public void PurchaseIsRefusedBeforeTheRoomIsPrepared()
        {
            ArcadeService service = CreateService(
                CreateRoom(false), 1000, out _, out _, out EconomyService economy);

            Assert.AreEqual(ArcadeInstallResult.RoomNotReady, service.TryInstall(0f));
            Assert.AreEqual(1000, economy.Balance);
            StringAssert.Contains("room", service.DescribeInstallBlocker());
        }

        [Test]
        public void VisitIncomeIsCreditedExactlyOnce()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 1000, out _, out _, out EconomyService economy);
            service.TryInstall(0f);

            string visitId = service.CreateVisitId();
            int before = economy.Balance;

            Assert.IsTrue(service.TryCollectVisitIncome(visitId, 5, 0f));
            Assert.AreEqual(before + 5, economy.Balance);
            Assert.AreEqual(1, service.VisitCount);

            Assert.IsFalse(service.TryCollectVisitIncome(visitId, 5, 0f), "A repeated visit id must be rejected.");
            Assert.AreEqual(before + 5, economy.Balance, "A repeated visit id must not pay again.");
        }

        [Test]
        public void TwoDifferentVisitsBothPay()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 1000, out _, out _, out EconomyService economy);
            service.TryInstall(0f);
            int before = economy.Balance;

            string first = service.CreateVisitId();
            string second = service.CreateVisitId();

            Assert.AreNotEqual(first, second);
            Assert.IsTrue(service.TryCollectVisitIncome(first, 3, 0f));
            Assert.IsTrue(service.TryCollectVisitIncome(second, 3, 0f));
            Assert.AreEqual(before + 6, economy.Balance);
            Assert.AreEqual(2, service.VisitCount);
        }

        [Test]
        public void IncomeIsRejectedWhileTheMachineIsOffline()
        {
            ArcadeService service = CreateService(
                CreateRoom(true), 1000, out _, out _, out EconomyService economy);

            string visitId = service.CreateVisitId();
            Assert.IsFalse(service.TryCollectVisitIncome(visitId, 5, 0f));
            Assert.AreEqual(1000, economy.Balance);
        }

        [Test]
        public void AttractModeOnlyRunsWhenPoweredAndIdle()
        {
            CreateService(CreateRoom(true), 1000, out _, out ArcadeAttractMode attract, out _);

            Assert.IsFalse(attract.IsAttracting);

            attract.SetPowered(true);
            Assert.IsTrue(attract.IsAttracting);

            attract.SetBusy(true);
            Assert.IsFalse(attract.IsAttracting, "A played machine should not attract.");

            attract.SetBusy(false);
            Assert.IsTrue(attract.IsAttracting);

            attract.SetPowered(false);
            Assert.IsFalse(attract.IsAttracting);
        }

        [Test]
        public void TrafficOnlyBringsVisitorsWhenTheMachineIsActive()
        {
            SecretRoomController room = CreateRoom(true);
            ArcadeService service = CreateService(
                room, 1000, out ArcadeCabinet cabinet, out _, out _);

            GameObject trafficObject = Track(new GameObject("Traffic"));
            ArcadeTraffic traffic = trafficObject.AddComponent<ArcadeTraffic>();
            GameObject spawn = Track(new GameObject("Spawn"));
            traffic.Configure(service, spawn.transform, cabinet.transform, null);
            traffic.SetInterval(1f);

            traffic.Tick(5f);
            Assert.AreEqual(0, traffic.VisitorsSpawned, "No visitors before the machine is installed.");

            service.TryInstall(0f);
            traffic.Tick(1.1f);
            Assert.AreEqual(1, traffic.VisitorsSpawned, "An active machine should draw a visitor.");

            ArcadeVisitor visitor = traffic.ActiveVisitor;
            Assert.IsNotNull(visitor);
            Assert.AreEqual(ArcadeVisitorState.Approaching, visitor.State);

            if (visitor != null)
            {
                Object.DestroyImmediate(visitor.gameObject);
            }
        }
    }
}
