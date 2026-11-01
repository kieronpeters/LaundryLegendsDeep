using System.Collections;
using LaundryLegends.Arcade;
using LaundryLegends.Core;
using LaundryLegends.Economy;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// End-to-end checks for the Phase 8 arcade loop (install, attract, one paid
    /// visitor) and for the first-person hands following what the player carries.
    /// </summary>
    public sealed class ArcadeBusinessPlayModeTests
    {
        private GameObject _room;
        private GameObject _arcade;
        private GameObject _trafficObject;

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject root in new[] { _room, _arcade, _trafficObject })
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }
        }

        private SecretRoomController CreateUsableRoom()
        {
            _room = new GameObject("Room");
            var room = _room.AddComponent<SecretRoomController>();
            room.Configure(1, 1);
            room.TryCollectClue();
            room.RecordCompletedOrder();
            room.TryReveal();
            for (int i = 0; i < 8 && room.Stage != SecretRoomStage.Usable; i++)
            {
                room.TryPrepare();
            }

            return room;
        }

        private ArcadeService CreateService(SecretRoomController room, int money, out ArcadeCabinet cabinet, out ArcadeAttractMode attract, out EconomyService economy)
        {
            _arcade = new GameObject("ArcadeRoot");

            GameObject cabinetObject = new GameObject("Cabinet");
            cabinetObject.transform.SetParent(_arcade.transform, false);
            cabinetObject.transform.position = new Vector3(2f, 0f, 2f);
            cabinet = cabinetObject.AddComponent<ArcadeCabinet>();

            GameObject attractObject = new GameObject("Attract");
            attractObject.transform.SetParent(_arcade.transform, false);
            attract = attractObject.AddComponent<ArcadeAttractMode>();

            economy = new EconomyService(new GameState(money));

            ArcadeService service = _arcade.AddComponent<ArcadeService>();
            service.Configure(room, cabinet, attract, cabinetObject.transform, economy);
            service.SetMachineCost(350);
            service.SetInstallSeconds(0f);
            return service;
        }

        [UnityTest]
        public IEnumerator InstalledMachineAttractsAndPaysOneVisitExactlyOnce()
        {
            SecretRoomController room = CreateUsableRoom();
            ArcadeService service = CreateService(
                room, 1000, out ArcadeCabinet cabinet, out ArcadeAttractMode attract, out EconomyService economy);

            yield return null;

            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(Time.time));
            yield return null;

            Assert.IsTrue(service.IsActive, "The machine should be active after a zero-second install.");
            Assert.IsTrue(attract.IsAttracting, "An active idle machine should be attracting.");
            Assert.AreEqual(650, economy.Balance);

            _trafficObject = new GameObject("Traffic");
            ArcadeTraffic traffic = _trafficObject.AddComponent<ArcadeTraffic>();
            GameObject spawn = new GameObject("Spawn");
            spawn.transform.SetParent(_trafficObject.transform, false);
            spawn.transform.position = cabinet.transform.position + new Vector3(0.4f, 0f, 0f);
            traffic.Configure(service, spawn.transform, cabinet.transform, null);
            traffic.SetInterval(9999f);
            traffic.SetPlaySeconds(0.2f);

            int before = economy.Balance;
            ArcadeVisitor visitor = traffic.SpawnVisitor();
            Assert.IsNotNull(visitor);

            float deadline = Time.time + 15f;
            while (visitor != null && visitor.State == ArcadeVisitorState.Approaching && Time.time < deadline)
            {
                yield return null;
            }

            StringAssert.StartsWith("arcade-visit-", visitor.VisitId);

            while (visitor != null && !visitor.IsFinished && Time.time < deadline)
            {
                yield return null;
            }

            yield return null;

            Assert.AreEqual(before + visitor.Income, economy.Balance, "One visit should credit its income exactly once.");
            Assert.AreEqual(1, service.VisitCount, "One visit should be recorded exactly once.");
            Assert.IsFalse(
                service.TryCollectVisitIncome(visitor.VisitId, visitor.Income, Time.time),
                "Collecting the same visit again must be rejected.");
            Assert.AreEqual(before + visitor.Income, economy.Balance);

            Object.DestroyImmediate(spawn);
        }

        [UnityTest]
        public IEnumerator HandsAreVisibleOnlyWhileCarrying()
        {
            var player = new GameObject("Player");
            var carry = player.AddComponent<CarryController>();

            var hands = new GameObject("FirstPersonHands");
            hands.transform.SetParent(player.transform, false);

            var view = player.AddComponent<FirstPersonHandsView>();
            view.Configure(hands, carry);

            yield return null;
            Assert.IsFalse(view.AreHandsVisible, "Empty hands should be hidden.");

            var itemObject = new GameObject("Towel");
            var item = itemObject.AddComponent<LaundryItem>();
            Assert.IsTrue(carry.TryPickUp(item), "The item should be carryable.");

            yield return null;
            Assert.IsTrue(view.AreHandsVisible, "Hands should appear while carrying.");

            Assert.IsTrue(carry.TryDrop());
            yield return null;
            Assert.IsFalse(view.AreHandsVisible, "Hands should hide again once the item is dropped.");

            Object.Destroy(player);
            Object.Destroy(itemObject);
        }
    }
}
