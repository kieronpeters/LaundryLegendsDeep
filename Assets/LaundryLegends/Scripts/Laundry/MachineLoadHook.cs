using System;
using LaundryLegends.Audio;
using System.Collections.Generic;
using LaundryLegends.Business;
using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.Laundry
{
    public enum MachineKind
    {
        Washer,
        Dryer
    }

    public enum MachineState
    {
        Empty,
        Loaded,
        Running,
        Complete
    }

    /// <summary>
    /// Phase 3 washer boundary: accepts one loose-laundry item and returns the
    /// same instance on unload. This is a load/unload hook only; cycles, timers,
    /// water, soap, and payment belong to later phases.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MachineLoadHook : MonoBehaviour, IInteractable
    {
        [SerializeField] private CarrySlot _slot;
        [SerializeField] private MachineKind _kind = MachineKind.Washer;
        [SerializeField] private float _washerDuration = 20f;
        [SerializeField] private float _dryerDuration = 25f;
        [SerializeField] private string _emptyPrompt = "Load washer";
        [SerializeField] private string _occupiedPrompt = "Take laundry";
        [SerializeField] private ConsumableStockService _stock;
        [SerializeField] private Transform _drumVisual;
        [SerializeField] private Renderer[] _clothRenderers = new Renderer[0];
        [SerializeField, Min(0f)] private float _drumDegreesPerSecond = 240f;

        private MachineState _state = MachineState.Empty;
        private float _elapsed;
        private float _durationScale = 1f;
        private Vector3 _drumPivot;
        private Vector3 _drumAxis = Vector3.forward;
        private bool _hasDrumPivot;
        private readonly HashSet<string> _appliedUpgrades = new HashSet<string>(StringComparer.Ordinal);

        public MachineKind Kind => _kind;
        public MachineState State => _state;
        public bool IsRunning => _state == MachineState.Running;
        public bool IsComplete => _state == MachineState.Complete;

        /// <summary>Consumable consumed when this machine starts a cycle.</summary>
        public ConsumableType RequiredConsumable =>
            _kind == MachineKind.Washer ? ConsumableType.Detergent : ConsumableType.Fragrance;

        public ConsumableStockService Stock => _stock;

        public Transform DrumVisual => _drumVisual;

        /// <summary>Authored cycle time, before any upgrade multiplier.</summary>
        public float Duration => _kind == MachineKind.Washer ? _washerDuration : _dryerDuration;

        /// <summary>Multiplier applied by owned upgrades. Starts at 1.</summary>
        public float DurationScale => _durationScale;

        /// <summary>The cycle time actually used by the timer, including upgrades.</summary>
        public float EffectiveDuration => Mathf.Max(0.05f, Duration * _durationScale);

        public float Progress => _state == MachineState.Complete ? 1f
            : _state == MachineState.Running ? Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, EffectiveDuration))
            : 0f;

        /// <summary>Last interaction result, exposed for tests and placeholder feedback.</summary>
        public string LastResult { get; private set; } = string.Empty;

        public CarrySlot Slot => _slot;

        public LaundryItem LoadedItem => _slot != null ? _slot.Occupant : null;

        public bool IsLoaded => LoadedItem != null;

        public string InteractionPrompt
        {
            get
            {
                if (_state == MachineState.Running)
                {
                    string verb = _kind == MachineKind.Washer ? "Washing" : "Drying";
                    return $"{verb} {Progress:P0}";
                }

                if (_state == MachineState.Complete)
                {
                    return "Take laundry";
                }

                if (_state == MachineState.Loaded)
                {
                    return _kind == MachineKind.Washer ? "Start washer" : "Start dryer";
                }

                return _kind == MachineKind.Washer ? "Load washer" : "Load dryer";
            }
        }

        private void Update()
        {
            if (_drumVisual != null && _state == MachineState.Running)
            {
                float degrees = _drumDegreesPerSecond * Time.deltaTime;
                if (_hasDrumPivot)
                {
                    _drumVisual.RotateAround(_drumPivot, _drumAxis, degrees);
                }
                else
                {
                    _drumVisual.Rotate(0f, degrees, 0f, Space.Self);
                }
            }

            if (_state != MachineState.Running)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= EffectiveDuration)
            {
                _elapsed = EffectiveDuration;
                _state = MachineState.Complete;
                LastResult = _kind == MachineKind.Washer ? "Wash complete" : "Dry complete";
                SetLoadedItemVisible(true);
                SetClothesVisible(true);
                GameAudio.Play(Sfx.MachineDone);
                LaundryItem item = LoadedItem;
                if (item != null)
                {
                    item.SetStage(_kind == MachineKind.Washer ? LaundryStage.Washed : LaundryStage.Dried);
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled && _slot != null;
        }

        /// <summary>Authoring/test wiring for the load slot; setup tools save the reference.</summary>
        public void Configure(CarrySlot slot)
        {
            _slot = slot;
        }

        public void Configure(CarrySlot slot, MachineKind kind)
        {
            _slot = slot;
            _kind = kind;
        }

        public void SetStockService(ConsumableStockService stock)
        {
            _stock = stock;
        }

        public void SetDrumVisual(Transform drum)
        {
            _drumVisual = drum;
            _hasDrumPivot = false;
            if (drum == null)
            {
                return;
            }

            Renderer[] renderers = drum.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            _drumPivot = bounds.center;
            _drumAxis = drum.forward.sqrMagnitude > 0.01f ? drum.forward : Vector3.forward;
            _hasDrumPivot = true;
        }

        /// <summary>Cloth bundles that belong to the machine, shown only when occupied.</summary>
        public void SetClothVisuals(Renderer[] cloths)
        {
            _clothRenderers = cloths ?? new Renderer[0];
            SetClothesVisible(_state != MachineState.Empty);
        }

        private void SetClothesVisible(bool visible)
        {
            foreach (Renderer cloth in _clothRenderers)
            {
                if (cloth != null)
                {
                    cloth.enabled = visible;
                }
            }
        }

        private void SetLoadedItemVisible(bool visible)
        {
            LaundryItem item = LoadedItem;
            if (item != null)
            {
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null)
                    {
                        renderer.enabled = visible;
                    }
                }
            }
        }

        private void Awake()
        {
            if (_stock == null)
            {
                _stock = FindFirstObjectByType<ConsumableStockService>();
            }

            SetClothesVisible(false);
        }

        public bool StartCycle()
        {
            if (_state != MachineState.Loaded)
            {
                LastResult = "Nothing ready to start";
                return false;
            }

            if (_stock != null && !_stock.TryConsume(RequiredConsumable))
            {
                LastResult = $"Out of {_stock.DisplayName(RequiredConsumable)}";
                return false;
            }

            _state = MachineState.Running;
            _elapsed = 0f;
            SetLoadedItemVisible(false);
            SetClothesVisible(true);
            LastResult = _kind == MachineKind.Washer ? "Wash started" : "Dry started";
            GameAudio.Play(Sfx.MachineStart);
            return true;
        }

        /// <summary>Test/authoring shortcut to finish a running cycle immediately.</summary>
        public void CompleteCycle()
        {
            if (_state == MachineState.Running || _state == MachineState.Loaded)
            {
                _elapsed = EffectiveDuration;
                _state = MachineState.Complete;
                LastResult = _kind == MachineKind.Washer ? "Wash complete" : "Dry complete";
                SetLoadedItemVisible(true);
                SetClothesVisible(true);
                GameAudio.Play(Sfx.MachineDone);
            }
        }

        /// <summary>Test/authoring shortcut to shorten or lengthen a cycle.</summary>
        public void SetDuration(float duration)
        {
            if (_kind == MachineKind.Washer)
            {
                _washerDuration = Mathf.Max(0.05f, duration);
            }
            else
            {
                _dryerDuration = Mathf.Max(0.05f, duration);
            }
        }

        /// <summary>
        /// Applies one upgrade multiplier to this machine's cycle. Returns false
        /// for an invalid multiplier or a repeat application, so callers can
        /// re-run the effect safely.
        /// </summary>
        public bool ApplyUpgrade(string upgradeId, float durationMultiplier)
        {
            if (string.IsNullOrWhiteSpace(upgradeId)
                || durationMultiplier <= 0f
                || float.IsNaN(durationMultiplier)
                || float.IsInfinity(durationMultiplier))
            {
                return false;
            }

            if (!_appliedUpgrades.Add(upgradeId))
            {
                return false;
            }

            _durationScale = Mathf.Max(0.05f, _durationScale * durationMultiplier);
            return true;
        }

        /// <summary>True once <paramref name="upgradeId"/> has been applied to this machine.</summary>
        public bool HasAppliedUpgrade(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && _appliedUpgrades.Contains(upgradeId);
        }

        /// <summary>Staff path: remove a completed item and empty the machine.</summary>
        /// <summary>
        /// Staff/cleanup path: empty the machine regardless of state and return
        /// whatever it held. Used when an order is abandoned so the pipeline
        /// cannot stay blocked.
        /// </summary>
        public LaundryItem ForceUnload()
        {
            LaundryItem item = _slot != null ? _slot.Take() : null;
            _state = MachineState.Empty;
            _elapsed = 0f;
            SetLoadedItemVisible(false);
            SetClothesVisible(false);
            return item;
        }

        public LaundryItem TakeProcessedItem()
        {
            if (_state != MachineState.Complete || _slot == null)
            {
                return null;
            }

            LaundryItem item = _slot.Take();
            if (item != null)
            {
                _state = MachineState.Empty;
                _elapsed = 0f;
                SetLoadedItemVisible(false);
                SetClothesVisible(false);
            }

            return item;
        }

        /// <summary>Staff path: place an item and mark the machine loaded.</summary>
        public bool LoadProcessedItem(LaundryItem item)
        {
            if (_state != MachineState.Empty || item == null || _slot == null)
            {
                return false;
            }

            if (!_slot.TryPlace(item))
            {
                return false;
            }

            _state = MachineState.Loaded;
            _elapsed = 0f;
            SetLoadedItemVisible(true);
            SetClothesVisible(true);
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null || _slot == null)
            {
                return;
            }

            CarryController carry = interactor.GetComponent<CarryController>();
            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                Debug.Log($"[Machine] {LastResult}.");
                return;
            }

            if (_state == MachineState.Running)
            {
                LastResult = "Cycle already running";
                Debug.Log($"[Machine] {LastResult}.");
                return;
            }

            if (_state == MachineState.Complete)
            {
                TryUnload(carry);
            }
            else if (_state == MachineState.Loaded)
            {
                StartCycle();
            }
            else
            {
                TryLoad(carry);
            }

            Debug.Log($"[Machine] {LastResult}.");
        }

        /// <summary>
        /// Moves the carried item into the machine. Rejects a missing item, an
        /// occupied machine, or anything that is not washer-loadable, without
        /// changing the item's owner.
        /// </summary>
        public bool TryLoad(CarryController carry)
        {
            if (_slot == null)
            {
                LastResult = "Machine has no load slot";
                return false;
            }

            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                return false;
            }

            if (_state != MachineState.Empty || IsLoaded)
            {
                LastResult = "Machine not empty";
                return false;
            }

            LaundryItem item = carry.HeldItem;
            if (item == null)
            {
                LastResult = "Nothing to load";
                return false;
            }

            if (!item.IsWasherLoadable)
            {
                LastResult = $"{item.Kind} cannot go in the washer; only loose laundry";
                return false;
            }

            bool placed = carry.TryPlaceInto(_slot);
            if (placed)
            {
                _state = MachineState.Loaded;
                _elapsed = 0f;
            }

            LastResult = placed ? "Loaded loose laundry" : "Machine rejected the load";
            return placed;
        }

        /// <summary>
        /// Returns the exact stored instance to an empty-handed carrier. Rejects
        /// an empty machine or a carrier that is already holding something, and
        /// leaves the slot unchanged on rejection.
        /// </summary>
        public LaundryItem TryUnload(CarryController carry)
        {
            if (_slot == null)
            {
                LastResult = "Machine has no load slot";
                return null;
            }

            if (carry == null)
            {
                LastResult = "Interactor cannot carry laundry";
                return null;
            }

            if (_state == MachineState.Running)
            {
                LastResult = "Cannot unload while running";
                return null;
            }

            LaundryItem item = LoadedItem;
            if (item == null)
            {
                LastResult = "Machine empty";
                return null;
            }

            if (carry.IsCarrying)
            {
                LastResult = "Hands full; put the held item down first";
                return null;
            }

            LaundryItem taken = _slot.Take();
            if (taken == null)
            {
                LastResult = "Machine empty";
                return null;
            }

            if (!carry.TryPickUp(taken))
            {
                _slot.TryPlace(taken);
                LastResult = "Unload rejected";
                return null;
            }

            LastResult = "Unloaded loose laundry";
            _state = MachineState.Empty;
            _elapsed = 0f;
            return taken;
        }
    }
}
