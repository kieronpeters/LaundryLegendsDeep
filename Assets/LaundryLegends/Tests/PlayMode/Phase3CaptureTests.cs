using System.Collections;
using System.IO;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.Player;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// Acceptance evidence capture. Run explicitly:
    /// -runTests -testPlatform PlayMode -testFilter LaundryLegends.Tests.PlayMode.Phase3CaptureTests
    /// Must not run with -nographics.
    /// </summary>
    public sealed class Phase3CaptureTests
    {
        private static string CaptureFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));

        [UnitySetUp]
        public IEnumerator LoadLaundromat()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest, Explicit("Evidence capture; run with -testFilter Phase3CaptureTests without -nographics.")]
        public IEnumerator CaptureCarryPromptAndMachine()
        {
            Directory.CreateDirectory(CaptureFolder);

            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            CarryController carry = player.GetComponent<CarryController>();
            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            LaundryItem basket = FindItem(LaundryItemKind.Basket);
            LaundryItem loose = FindItem(LaundryItemKind.LooseLaundry);
            MachineLoadHook machine = Object.FindFirstObjectByType<MachineLoadHook>();

            Assert.IsNotNull(player);
            Assert.IsNotNull(interactor);
            Assert.IsNotNull(carry);
            Assert.IsNotNull(prompt);
            Assert.IsNotNull(basket);
            Assert.IsNotNull(loose);
            Assert.IsNotNull(machine);

            // 1. Pickup prompt: look down at the floor basket.
            Vector3 basketPosition = basket.transform.position;
            player.transform.position = new Vector3(basketPosition.x, 0.05f, basketPosition.z + 1.0f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            SetPitch(player, 54f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.IsTrue(prompt.IsVisible, "Pickup prompt must be visible before capture.");
            yield return Capture("phase3-prompt.png");

            // 2. Carrying: hold a loose item and look down-right at it.
            Assert.IsTrue(carry.TryPickUp(loose));
            Assert.AreSame(loose, carry.HeldItem);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            SetPitch(player, 30f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            yield return Capture("phase3-carry.png");

            // 3. Machine: load the loose item, then face the washer for the take prompt.
            carry.TryDrop();
            Assert.IsTrue(carry.TryPickUp(loose));
            machine.Interact(player.gameObject);
            Assert.IsTrue(machine.IsLoaded);

            Vector3 machinePosition = machine.transform.position;
            player.transform.position = new Vector3(machinePosition.x, 0.05f, machinePosition.z - 1.6f);
            player.transform.rotation = Quaternion.identity;
            SetPitch(player, 24f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.IsTrue(prompt.IsVisible, "Machine prompt must be visible before capture.");
            Assert.AreEqual("Take laundry", interactor.CurrentPrompt);
            yield return Capture("phase3-machine.png");
        }

        private static void SetPitch(FirstPersonController player, float degrees)
        {
            // Look accumulates pitch. Clamp to the upper limit first so the next
            // call can set an absolute downward angle from a known value.
            player.Look(new Vector2(0f, 100f));
            player.Look(new Vector2(0f, -(85f + degrees) / 2f));
        }

        private static IEnumerator Capture(string fileName)
        {
            string path = Path.Combine(CaptureFolder, fileName);
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
            yield return null;
            Assert.IsTrue(File.Exists(path), $"Capture was not written: {path}");
            Debug.Log($"[Phase3] Captured {path}");
        }

        private static LaundryItem FindItem(LaundryItemKind kind)
        {
            LaundryItem[] items = Object.FindObjectsByType<LaundryItem>(FindObjectsSortMode.None);
            foreach (LaundryItem item in items)
            {
                if (item.Kind == kind)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
