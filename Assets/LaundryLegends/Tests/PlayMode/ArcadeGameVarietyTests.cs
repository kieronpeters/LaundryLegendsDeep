using System.Collections;
using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// LL-F-010: each arcade cabinet must launch a visibly different game. Fight
    /// cabinets keep the duel with per-machine opponents/colours; the Puzzle
    /// cabinet runs the maze game.
    /// </summary>
    public sealed class ArcadeGameVarietyTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator IdleCabinetShowsLitAttractImage()
        {
            ArcadeService service = Object.FindFirstObjectByType<ArcadeService>();
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            Assert.IsNotNull(service);
            Assert.IsNotNull(room);

            room.UnlockByPurchase();
            yield return null;

            ArcadeCabinet cabinet = service.GetSlotCabinet(0);
            Assert.IsNotNull(cabinet, "The starter cabinet should exist.");
            ArcadeAttractMode attract = service.AttractMode;
            Assert.IsNotNull(attract, "The starter cabinet should have an attract mode.");
            Assert.IsNotNull(attract.Screen, "Attract mode needs the cabinet screen renderer.");

            attract.SetPowered(true);
            attract.SetBusy(false);
            yield return null;

            Material material = attract.Screen.material;
            Assert.IsNotNull(material.mainTexture,
                "An idle cabinet must show an attract image instead of a black screen.");
            Color emission = material.GetColor("_EmissionColor");
            Assert.Greater(emission.r + emission.g + emission.b, 0.05f,
                "The attract screen should be emissive so it reads as powered.");
        }

        [UnityTest]
        public IEnumerator CabinetsLaunchDifferentGames()
        {
            ArcadeService service = Object.FindFirstObjectByType<ArcadeService>();
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(service);
            Assert.IsNotNull(room);
            Assert.IsNotNull(spawner);

            room.UnlockByPurchase();
            yield return null;
            Assert.IsTrue(service.IsRoomReady);
            Assert.IsTrue(spawner.Economy.TryEarn(5000, "Arcade variety test", Time.time));

            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(1, Time.time));
            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(2, Time.time));
            if (!service.IsSlotActive(1))
            {
                service.TryActivateSlot(1);
            }

            if (!service.IsSlotActive(2))
            {
                service.TryActivateSlot(2);
            }

            yield return null;

            ArcadeCabinet racer = service.GetSlotCabinet(1);
            ArcadeCabinet puzzle = service.GetSlotCabinet(2);
            Assert.IsNotNull(racer, "The Racer cabinet should exist after install.");
            Assert.IsNotNull(puzzle, "The Puzzle cabinet should exist after install.");

            Assert.AreEqual(ArcadeGameKind.Fight, racer.GameKind, "Racer should stay a fight cabinet.");
            Assert.AreEqual("TURBO", racer.OpponentName, "Racer should have its own opponent name.");
            Assert.AreEqual(ArcadeGameKind.Maze, puzzle.GameKind, "Puzzle should launch the maze game.");

            Assert.IsTrue(racer.TryStart(spawner.Economy, Time.time));
            Assert.IsNotNull(racer.Session);
            Assert.AreEqual("TURBO", racer.Session.OpponentName);
            Assert.IsTrue(racer.IsPlaying);
            racer.Stop();
            Assert.IsFalse(racer.IsPlaying);
            Assert.IsFalse(ArcadeMode.IsActive);

            Assert.IsTrue(puzzle.TryStart(spawner.Economy, Time.time));
            Assert.IsNotNull(puzzle.Maze, "The Puzzle cabinet should create a maze session.");
            Assert.IsNull(puzzle.Session, "A maze cabinet does not create a fight session.");
            Assert.IsNotNull(puzzle.GetComponent<MazeGameView>(), "The maze view should be added to the cabinet.");
            Assert.IsTrue(puzzle.IsPlaying);
            puzzle.Stop();
            Assert.IsFalse(puzzle.IsPlaying);
            Assert.IsFalse(ArcadeMode.IsActive);
            yield return null;
        }
    }
}
