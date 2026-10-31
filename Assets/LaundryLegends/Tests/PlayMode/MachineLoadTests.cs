using System.Collections;
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
    public sealed class MachineLoadTests
    {
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

        [UnityTest]
        public IEnumerator WasherHookIsTargetableAndShowsLoadPrompt()
        {
            MachineLoadHook machine = FindMachine();
            FirstPersonController player = FindPlayer();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            Assert.IsNotNull(interactor, "Player should have a PlayerInteractor.");
            Assert.IsNotNull(prompt, "Player should have an InteractionPromptView.");

            PositionPlayerAtMachineFront(player, machine);
            yield return null;
            yield return null;

            Assert.AreSame(machine, interactor.CurrentInteractable,
                "The washer collider should resolve to its load hook.");
            Assert.AreEqual("Load washer", interactor.CurrentPrompt);
            Assert.IsTrue(prompt.IsVisible);
            Assert.AreEqual($"Load washer [{interactor.InteractKeyLabel}]", prompt.CurrentLabel);
        }

        [UnityTest]
        public IEnumerator LoadAndUnloadReturnTheExactSameItem()
        {
            MachineLoadHook machine = FindMachine();
            FirstPersonController player = FindPlayer();
            CarryController carry = player.GetComponent<CarryController>();
            Assert.IsNotNull(carry, "Player should have a CarryController.");
            Assert.IsFalse(machine.IsLoaded, "The scene washer should start empty.");

            LaundryItem item = CreateItem("test.loose", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(carry.TryPickUp(item));

            machine.Interact(player.gameObject);

            Assert.IsTrue(machine.IsLoaded);
            Assert.AreSame(item, machine.LoadedItem);
            Assert.IsFalse(carry.IsCarrying, "Loading should empty the player's hands.");
            Assert.AreEqual("Start washer", machine.InteractionPrompt);
            machine.CompleteCycle();
            Assert.AreEqual("Take laundry", machine.InteractionPrompt);

            machine.Interact(player.gameObject);

            Assert.IsFalse(machine.IsLoaded, "Unloading should empty the machine.");
            Assert.AreSame(item, carry.HeldItem, "Unloading must return the exact same instance.");
            Assert.AreEqual("Load washer", machine.InteractionPrompt);

            carry.TryDrop();
            Object.Destroy(item.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BasketLoadIsRejectedAndLeavesOwnershipUnchanged()
        {
            MachineLoadHook machine = FindMachine();
            FirstPersonController player = FindPlayer();
            CarryController carry = player.GetComponent<CarryController>();
            Assert.IsNotNull(carry, "Player should have a CarryController.");

            LaundryItem basket = CreateItem("test.basket", LaundryItemKind.Basket);
            Assert.IsTrue(carry.TryPickUp(basket));

            machine.Interact(player.gameObject);

            Assert.IsFalse(machine.IsLoaded, "A basket must not be loaded.");
            Assert.AreSame(basket, carry.HeldItem, "Rejected loads must not change ownership.");
            StringAssert.Contains("Basket", machine.LastResult);

            carry.TryDrop();
            Object.Destroy(basket.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OccupiedMachineRejectsASecondLoad()
        {
            MachineLoadHook machine = FindMachine();
            FirstPersonController player = FindPlayer();
            CarryController carry = player.GetComponent<CarryController>();
            Assert.IsNotNull(carry, "Player should have a CarryController.");

            LaundryItem first = CreateItem("test.loose-a", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(carry.TryPickUp(first));
            machine.Interact(player.gameObject);
            Assert.AreSame(first, machine.LoadedItem);
            machine.CompleteCycle();

            LaundryItem second = CreateItem("test.loose-b", LaundryItemKind.LooseLaundry);
            Assert.IsTrue(carry.TryPickUp(second));
            machine.Interact(player.gameObject);

            Assert.AreSame(first, machine.LoadedItem, "A second load must not replace the occupant.");
            Assert.AreSame(second, carry.HeldItem, "A rejected load must not change ownership.");
            StringAssert.Contains("Hands full", machine.LastResult);

            carry.TryDrop();
            Object.Destroy(first.gameObject);
            Object.Destroy(second.gameObject);
            yield return null;
        }

        private static MachineLoadHook FindMachine()
        {
            MachineLoadHook[] hooks = Object.FindObjectsByType<MachineLoadHook>(FindObjectsSortMode.None);
            foreach (MachineLoadHook hook in hooks)
            {
                if (hook.Kind == MachineKind.Washer)
                {
                    return hook;
                }
            }

            Assert.Fail("Laundromat should contain a washer MachineLoadHook.");
            return null;
        }

        private static FirstPersonController FindPlayer()
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            Assert.IsNotNull(player, "Laundromat should contain a player.");
            return player;
        }

        private static void PositionPlayerAtMachineFront(FirstPersonController player, MachineLoadHook machine)
        {
            Vector3 machinePosition = machine.transform.position;
            player.transform.position = new Vector3(machinePosition.x, 0.05f, machinePosition.z - 1.6f);
            player.transform.rotation = Quaternion.identity;
            player.Look(new Vector2(0f, -12f)); // Pitch about 24 degrees down toward the drum.
            Physics.SyncTransforms();
        }

        private static LaundryItem CreateItem(string templateId, LaundryItemKind kind)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = templateId;
            LaundryItem item = target.AddComponent<LaundryItem>();
            item.Initialize(templateId, kind);
            return item;
        }
    }
}
