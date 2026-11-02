using LaundryLegends.Interaction;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// A kiosk/pedestal that sells one arcade upgrade. The service owns ownership
    /// and the single debit; this component only offers the interaction.
    /// <para>
    /// An authored <see cref="ArcadeUpgradeDefinition"/> can be assigned, or the
    /// kiosk can be configured inline by the scene builder. Inline mode keeps the
    /// decor reference in the scene instead of inside a ScriptableObject asset.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeUpgradePoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private ArcadeService _service;
        [SerializeField] private ArcadeUpgradeDefinition _upgrade;
        [SerializeField] private string _prompt = "Buy arcade upgrade";

        [Header("Inline upgrade (used when no asset is assigned)")]
        [SerializeField] private string _inlineId;
        [SerializeField] private string _inlineName;
        [SerializeField, Min(0)] private int _inlineCost;
        [SerializeField, Min(0.1f)] private float _inlineIncomeMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float _inlineTrafficMultiplier = 1f;
        [SerializeField] private GameObject _inlineDecor;

        private ArcadeUpgradeDefinition _runtimeUpgrade;

        public ArcadeService Service => _service;

        public ArcadeUpgradeDefinition Upgrade
        {
            get
            {
                if (_upgrade != null)
                {
                    return _upgrade;
                }

                if (string.IsNullOrWhiteSpace(_inlineId))
                {
                    return null;
                }

                if (_runtimeUpgrade == null)
                {
                    _runtimeUpgrade = ScriptableObject.CreateInstance<ArcadeUpgradeDefinition>();
                    _runtimeUpgrade.hideFlags = HideFlags.HideAndDontSave;
                    _runtimeUpgrade.Configure(
                        _inlineId,
                        string.IsNullOrWhiteSpace(_inlineName) ? _inlineId : _inlineName,
                        _inlineCost,
                        _inlineIncomeMultiplier,
                        _inlineTrafficMultiplier,
                        _inlineDecor);
                }

                return _runtimeUpgrade;
            }
        }

        public bool IsOwned
        {
            get
            {
                ArcadeUpgradeDefinition upgrade = Upgrade;
                return _service != null && upgrade != null && _service.HasUpgrade(upgrade.Id);
            }
        }

        public bool IsAvailable => _service != null && Upgrade != null && !IsOwned;

        public void SetUpgrade(ArcadeService service, ArcadeUpgradeDefinition upgrade, string prompt = null)
        {
            _service = service;
            _upgrade = upgrade;
            _inlineId = null;
            _runtimeUpgrade = null;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _prompt = prompt;
            }
        }

        /// <summary>
        /// Scene-builder wiring for an upgrade whose definition is not an asset.
        /// The decor object stays in the scene, so it cannot become a broken asset
        /// reference when the scene is reloaded.
        /// </summary>
        public void ConfigureInline(
            ArcadeService service,
            string id,
            string displayName,
            int cost,
            float incomeMultiplier,
            float trafficMultiplier,
            GameObject decor,
            string prompt = null)
        {
            _service = service;
            _upgrade = null;
            _inlineId = id;
            _inlineName = displayName;
            _inlineCost = Mathf.Max(0, cost);
            _inlineIncomeMultiplier = Mathf.Max(0.1f, incomeMultiplier);
            _inlineTrafficMultiplier = Mathf.Max(0.1f, trafficMultiplier);
            _inlineDecor = decor;
            _runtimeUpgrade = null;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _prompt = prompt;
            }
        }

        public string InteractionPrompt
        {
            get
            {
                ArcadeUpgradeDefinition upgrade = Upgrade;
                if (_service == null || upgrade == null)
                {
                    return "Arcade upgrade kiosk (unwired)";
                }

                if (IsOwned)
                {
                    return $"{upgrade.DisplayName} (installed)";
                }

                string blocker = _service.DescribeUpgradeBlocker(upgrade);
                string price = $"${upgrade.Cost}";
                return string.IsNullOrEmpty(blocker)
                    ? $"{_prompt}: {upgrade.DisplayName} ({price})"
                    : $"{_prompt}: {upgrade.DisplayName} ({price}) - {blocker}";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && IsAvailable;
        }

        public void Interact(GameObject interactor)
        {
            ArcadeUpgradeDefinition upgrade = Upgrade;
            if (_service == null || upgrade == null)
            {
                return;
            }

            _service.TryBuyUpgrade(upgrade, Time.time);
        }

        private void OnDestroy()
        {
            if (_runtimeUpgrade == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeUpgrade);
            }
            else
            {
                DestroyImmediate(_runtimeUpgrade);
            }

            _runtimeUpgrade = null;
        }
    }
}
