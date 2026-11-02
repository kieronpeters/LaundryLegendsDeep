using System.Collections;
using LaundryLegends.Arcade;
using LaundryLegends.Customers;
using LaundryLegends.Progression;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LaundryLegends.Tests.PlayMode
{
    /// <summary>
    /// End-to-end Phase 9 scene checks: the shipped room has five slots, two are
    /// open at the start, buying/playing a second machine works, and the expansion
    /// purchase opens a walkable, visible bay with two more anchors.
    /// </summary>
    public sealed class ArcadeExpansionPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("Laundromat");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneHasSecondMachineAndVisibleExpansion()
        {
            ArcadeService service = Object.FindFirstObjectByType<ArcadeService>();
            SecretRoomController room = Object.FindFirstObjectByType<SecretRoomController>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            ArcadePanelView panel = Object.FindFirstObjectByType<ArcadePanelView>();
            LaundryReputation reputation = Object.FindFirstObjectByType<LaundryReputation>();
            PurchaseMenuView menu = Object.FindFirstObjectByType<PurchaseMenuView>();

            Assert.IsNotNull(service, "The scene needs the arcade service.");
            Assert.IsNotNull(room, "The scene needs the secret room controller.");
            Assert.IsNotNull(spawner, "The scene needs the customer spawner for economy.");
            Assert.IsNotNull(panel, "The Phase 9 HUD panel should be in the scene.");
            Assert.IsNotNull(reputation, "The Phase 9 reputation tracker should be in the scene.");
            Assert.IsNotNull(menu, "The U purchase menu should be in the scene.");
            Assert.IsNotNull(panel.Service, "The HUD must be wired to the arcade service.");
            Assert.IsNotNull(panel.Reputation, "The HUD must have a reputation source.");
            Assert.IsNotNull(service.Reputation, "The service must be wired to the reputation tracker.");
            Assert.AreEqual(20, service.Reputation.Value, "The authored starting reputation should be 20.");
            Assert.IsNotNull(spawner.Reputation, "The spawner must feed the same reputation tracker.");

            room.UnlockByPurchase();
            yield return null;

            Assert.IsTrue(service.IsRoomReady);
            Assert.AreEqual(5, service.SlotCount, "The room should have the starter plus four authored slots.");
            Assert.AreEqual(3, service.Capacity, "Three anchors should be open before the expansion.");
            Assert.IsTrue(service.IsSlotActive(0), "The shipped cabinet should be powered.");
            StringAssert.Contains("Machine: Racer", menu.BuildText(), "The U menu should list the second machine.");
            StringAssert.Contains("Arcade expansion bay", menu.BuildText(), "The U menu should list the expansion.");

            GameObject bay = FindInactive("ExpansionBay");
            GameObject seal = FindInactive("ExpansionSeal");
            Assert.IsNotNull(bay, "The expansion bay must exist, disabled, in the scene.");
            Assert.IsNotNull(seal, "The sealing panel must exist in the scene.");
            Assert.IsFalse(bay.activeSelf, "The bay must start closed.");
            Assert.IsTrue(seal.activeSelf, "The bay must start sealed.");

            Assert.IsTrue(spawner.Economy.TryEarn(5000, "Phase 9 scene test credit", Time.time));

            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(1, Time.time),
                "The service should buy the second machine.");
            if (!service.IsSlotActive(1))
            {
                service.TryActivateSlot(1);
            }

            yield return null;
            Assert.IsTrue(service.IsSlotActive(1), "Buying the second machine should power it.");

            ArcadeCabinet secondCabinet = service.GetSlotCabinet(1);
            Assert.IsNotNull(secondCabinet);
            Assert.IsTrue(secondCabinet.gameObject.activeSelf, "The bought cabinet should be visible.");
            Assert.IsTrue(secondCabinet.TryStart(spawner.Economy, Time.time), "The second machine should be playable.");
            yield return null;
            secondCabinet.Stop();
            yield return null;

            ArcadeUpgradePoint neonKiosk = FindUpgradePoint("arcade.upgrade.neon");
            GameObject neonDecor = FindInactive("Decor_NeonSign");
            Assert.IsNotNull(neonKiosk, "The scene needs the neon upgrade kiosk.");
            Assert.IsNotNull(neonDecor, "The neon decor object should exist, disabled.");
            Assert.IsFalse(neonDecor.activeSelf);
            int beforeUpgrade = spawner.Economy.Balance;
            Assert.IsTrue(service.TryBuyUpgrade(neonKiosk.Upgrade, Time.time), "The menu should buy the upgrade once.");
            yield return null;
            Assert.IsTrue(service.HasUpgrade("arcade.upgrade.neon"), "The kiosk should buy the upgrade once.");
            Assert.IsTrue(neonDecor.activeSelf, "Buying the upgrade should reveal its decor.");
            Assert.Less(spawner.Economy.Balance, beforeUpgrade, "The upgrade should debit exactly once.");
            Assert.Greater(service.IncomeMultiplier, 1f, "The upgrade should raise the income multiplier.");

            ArcadeUpgradePoint carpetKiosk = FindUpgradePoint("arcade.upgrade.carpet");
            GameObject rugDecor = FindInactive("Decor_ArcadeRug");
            Assert.IsNotNull(carpetKiosk, "The scene needs the carpet upgrade kiosk.");
            Assert.IsNotNull(rugDecor, "The carpet decor should exist, disabled.");
            Assert.IsTrue(rugDecor.transform.IsChildOf(bay.transform), "The carpet must live in the expansion bay.");
            int beforeCarpet = spawner.Economy.Balance;
            Assert.IsFalse(service.TryBuyUpgrade(carpetKiosk.Upgrade, Time.time),
                "The carpet must be locked before the expansion.");
            Assert.AreEqual(beforeCarpet, spawner.Economy.Balance, "A locked carpet must not debit.");

            Assert.IsTrue(service.TryExpand(Time.time), "The menu should buy the expansion.");
            yield return null;
            Assert.IsTrue(service.TryBuyUpgrade(carpetKiosk.Upgrade, Time.time), "The carpet should unlock after expansion.");
            yield return null;
            Assert.IsTrue(rugDecor.activeSelf, "The carpet should appear in the new bay.");

            Assert.IsTrue(service.IsExpanded, "The expansion purchase should register.");
            Assert.IsTrue(bay.activeSelf, "The extension must become visible.");
            Assert.IsFalse(seal.activeSelf, "The sealing panel must open.");
            Assert.AreEqual(5, service.Capacity, "The bay should raise capacity from three to five.");
            Assert.IsTrue(service.IsSlotReachable(3) && service.IsSlotReachable(4), "Both bay anchors should open.");

            Transform bayFloor = FindChild(bay.transform, "BayFloor");
            Assert.IsNotNull(bayFloor, "The bay needs a floor.");
            Assert.IsNotNull(bayFloor.GetComponent<Collider>(), "The bay floor needs a collider to walk on.");
            Physics.SyncTransforms();
            Assert.IsTrue(
                Physics.Raycast(bayFloor.position + Vector3.up, Vector3.down, out RaycastHit hit, 3f),
                "A downward ray in the bay should hit the floor.");
            Assert.AreEqual(bayFloor.GetComponent<Collider>(), hit.collider, "The bay floor should be walkable.");

            Assert.AreEqual(ArcadeInstallResult.Success, service.TryInstall(3, Time.time),
                "The expansion should expose a fourth machine anchor.");
            if (!service.IsSlotActive(3))
            {
                service.TryActivateSlot(3);
            }

            yield return null;
            Assert.IsTrue(service.IsSlotActive(3), "A bay machine should be buyable after the expansion.");

            StringAssert.Contains("ARCADE", panel.BuildText());
            StringAssert.Contains("Reputation", panel.BuildText());
        }

        private static GameObject FindInactive(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static ArcadeUpgradePoint FindUpgradePoint(string upgradeId)
        {
            foreach (ArcadeUpgradePoint point in Object.FindObjectsByType<ArcadeUpgradePoint>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ArcadeUpgradeDefinition upgrade = point.Upgrade;
                if (upgrade != null && upgrade.Id == upgradeId)
                {
                    return point;
                }
            }

            return null;
        }
    }
}
