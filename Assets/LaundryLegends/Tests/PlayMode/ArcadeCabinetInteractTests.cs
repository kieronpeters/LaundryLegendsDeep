using System.Collections;
using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Interaction;
using LaundryLegends.Player;
using LaundryLegends.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// The arcade cabinet must be targetable from the floor of the back room.
    /// The model ships without colliders and the setup tool removes the screen
    /// quad's collider, which left the machine impossible to interact with.
    /// </summary>
    public sealed class ArcadeCabinetInteractTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CabinetIsTargetableAndStartsAPlay()
        {
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            ArcadeService service = Object.FindFirstObjectByType<ArcadeService>();
            ArcadeCabinet cabinet = service != null
                ? service.GetSlotCabinet(0)
                : Object.FindFirstObjectByType<ArcadeCabinet>(FindObjectsInactive.Include);
            PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Assert.IsNotNull(room);
            Assert.IsNotNull(service);
            Assert.IsNotNull(cabinet);
            Assert.IsNotNull(interactor);
            Assert.IsNotNull(player);
            Assert.IsNotNull(spawner);

            room.UnlockByPurchase();
            yield return null;

            Assert.IsTrue(cabinet.IsUnlocked, "The shipped machine should be powered.");
            Assert.IsNotNull(cabinet.GetComponent<Collider>(), "The cabinet needs a collider to be targetable.");

            // Stand 2 m in front of the screen and look at it.
            Vector3 cabinetPosition = cabinet.transform.position;
            player.transform.position = new Vector3(cabinetPosition.x - 2f, 0.1f, cabinetPosition.z);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Physics.SyncTransforms();
            yield return null;

            Assert.IsTrue(
                spawner.Economy.TryEarn(10, "Arcade test credit", Time.time),
                "The test needs money for the $2 play.");

            bool started = interactor.TryInteract();
            Assert.IsTrue(started, $"The interactor should dispatch to the cabinet (prompt '{interactor.CurrentPrompt}').");
            Assert.IsTrue(cabinet.IsPlaying, "The interaction should start a play.");
            Assert.AreEqual(1, cabinet.PlaysStarted);

            // One interaction drops straight into the fight and takes over the view.
            Assert.IsTrue(ArcadeMode.IsActive, "The arcade should take over the screen.");
            Assert.AreEqual(FightScreen.Fight, cabinet.Session.Screen, "The fight should start immediately.");
            Assert.IsFalse(player.enabled, "Player movement should be frozen while playing.");

            cabinet.Stop();
            yield return null;
            Assert.IsFalse(ArcadeMode.IsActive, "Leaving the cabinet should return control.");
            Assert.IsTrue(player.enabled, "Player movement should be restored.");
        }
    }
}
