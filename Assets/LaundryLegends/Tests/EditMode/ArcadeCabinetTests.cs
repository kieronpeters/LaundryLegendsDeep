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
    /// Covers the cabinet itself: it must not take money before the machine is
    /// bought and powered, and the maze must actually be drawn on its screen.
    /// </summary>
    public sealed class ArcadeCabinetTests
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

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        private SecretRoomController CreateUsableRoom()
        {
            var roomObject = Track(new GameObject("Room"));
            var room = roomObject.AddComponent<SecretRoomController>();
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

        [Test]
        public void CabinetTakesNoMoneyUntilTheMachineIsInstalledAndActive()
        {
            var root = Track(new GameObject("Arcade"));
            var cabinetObject = new GameObject("Cabinet");
            cabinetObject.transform.SetParent(root.transform, false);
            var cabinet = cabinetObject.AddComponent<ArcadeCabinet>();

            var attractObject = new GameObject("Attract");
            attractObject.transform.SetParent(root.transform, false);
            var attract = attractObject.AddComponent<ArcadeAttractMode>();

            var economy = new EconomyService(new GameState(1000));
            var service = root.AddComponent<ArcadeService>();
            service.Configure(CreateUsableRoom(), cabinet, attract, cabinetObject.transform, economy);
            service.SetMachineCost(120);
            service.SetInstallSeconds(0f);

            Assert.IsFalse(cabinet.IsUnlocked, "An unbought cabinet must be locked.");
            Assert.IsFalse(cabinet.TryStart(economy, 0f), "A locked cabinet must not take a play payment.");
            Assert.AreEqual(1000, economy.Balance);

            service.TryInstall(0f);
            Assert.IsTrue(cabinet.IsUnlocked, "A bought, active cabinet should be playable.");

            Assert.IsTrue(cabinet.TryStart(economy, 0f), "An active cabinet should accept a paid play.");
            Assert.AreEqual(1000 - 120 - cabinet.Cost, economy.Balance);
            Assert.IsTrue(cabinet.IsPlaying);
        }

        [Test]
        public void CabinetRefusesPlayWhenUploadedMachineIsAbsent()
        {
            var go = Track(new GameObject("Cabinet"));
            var cabinet = go.AddComponent<ArcadeCabinet>();
            var economy = new EconomyService(new GameState(50));

            // No service wired: authoring/tests may still play, and the charge applies once.
            Assert.IsTrue(cabinet.TryStart(economy, 0f));
            Assert.AreEqual(50 - cabinet.Cost, economy.Balance);
            Assert.IsFalse(cabinet.TryStart(economy, 0f), "A play already in progress must not charge again.");
        }

        [Test]
        public void FightIsDrawnOnTheCabinetScreenAndClearedWhenStopped()
        {
            var screen = Track(GameObject.CreatePrimitive(PrimitiveType.Quad));
            var viewObject = Track(new GameObject("View"));
            var view = viewObject.AddComponent<FightGameView>();
            view.Configure(screen.GetComponent<Renderer>());

            var cabinetObject = Track(new GameObject("Cabinet"));
            var cabinet = cabinetObject.AddComponent<ArcadeCabinet>();
            cabinet.Configure(view, null);

            var economy = new EconomyService(new GameState(50));
            Assert.IsTrue(cabinet.TryStart(economy, 0f));
            Assert.AreEqual(
                FightScreen.Fight,
                cabinet.Session.Screen,
                "A play should drop straight into the fight (Arcade Paradise flow).");

            Texture2D texture = view.Texture;
            Assert.IsNotNull(texture, "Starting a play should create the screen texture.");

            Color32[] lit = texture.GetPixels32();
            int litCount = 0;
            foreach (Color32 pixel in lit)
            {
                if (pixel.r > 40 || pixel.g > 40 || pixel.b > 90)
                {
                    litCount++;
                }
            }

            Assert.Greater(litCount, 0, "The maze should actually be drawn, not left blank.");

            cabinet.Stop();
            Assert.IsFalse(cabinet.IsPlaying);

            int afterClear = 0;
            foreach (Color32 pixel in texture.GetPixels32())
            {
                if (pixel.r > 40 || pixel.g > 40 || pixel.b > 90)
                {
                    afterClear++;
                }
            }

            Assert.AreEqual(0, afterClear, "Stopping should clear the screen instead of freezing the maze.");
        }
    }
}
