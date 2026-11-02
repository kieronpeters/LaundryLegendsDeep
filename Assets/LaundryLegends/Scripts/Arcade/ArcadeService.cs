using System;
using System.Collections.Generic;
using LaundryLegends.Customers;
using LaundryLegends.Economy;
using LaundryLegends.Progression;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    public enum ArcadeInstallResult
    {
        None,
        Success,
        AlreadyInstalled,
        RoomNotReady,
        NoMachine,
        InvalidPlacement,
        NoEconomy,
        Unaffordable,
        SlotUnavailable,
        NoDefinition
    }

    /// <summary>
    /// Owns the arcade business loop: purchasing and installing machines through
    /// <see cref="EconomyService"/>, the Off -> Installing -> Active lifecycle,
    /// attract-mode driving, exactly-once income, upgrades, reputation-fed
    /// traffic, upkeep, expansion and milestones.
    /// <para>
    /// Slot 0 is the original Phase 8 machine and keeps the legacy API. Additional
    /// machines live in <see cref="_extraMachines"/> and are addressed by index.
    /// All money movement goes through the economy service; the serializable
    /// <see cref="ArcadeState"/> holds the saveable facts.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeService : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private SecretRoomController _room;
        [SerializeField] private ArcadeCabinet _cabinet;
        [SerializeField] private ArcadeAttractMode _attractMode;
        [SerializeField] private CustomerSpawner _spawner;

        [Header("Installation")]
        [Tooltip("Install point used as the placement anchor for the first cabinet.")]
        [SerializeField] private Transform _placementAnchor;
        [SerializeField, Min(0.1f)] private float _placementRadius = 2.5f;
        [SerializeField, Min(0)] private int _machineCost = 120;
        [SerializeField, Min(0f)] private float _installSeconds = 2.5f;
        [SerializeField] private ArcadeMachineDefinition _starterDefinition;

        [Header("State")]
        [SerializeField] private ArcadeState _state = new ArcadeState();

        [Tooltip("Start already installed and powered, for a machine that ships inside the room.")]
        [SerializeField] private bool _startActive;

        [Header("Extra machines")]
        [SerializeField] private List<ArcadeExtraMachine> _extraMachines = new List<ArcadeExtraMachine>();

        [Header("Expansion")]
        [SerializeField] private GameObject _expansionBay;
        [SerializeField] private GameObject _expansionSeal;
        [SerializeField, Min(0)] private int _expansionCost = 300;
        [SerializeField] private bool _expansionPurchased;

        [Header("Upgrades and reputation")]
        [SerializeField] private List<ArcadeUpgradeDefinition> _upgradeCatalogue = new List<ArcadeUpgradeDefinition>();
        [SerializeField] private LaundryReputation _reputation;

        [Header("Operating cost")]
        [SerializeField, Min(1f)] private float _upkeepIntervalSeconds = 120f;
        [SerializeField, Min(0)] private int _upkeepPerMachine = 1;

        private EconomyService _economy;
        private float _installRemaining;
        private float _upkeepTimer;
        private bool _upkeepSkipLogged;
        private ArcadeMachineDefinition _fallbackStarter;

        /// <summary>Raised whenever the first machine's lifecycle phase changes.</summary>
        public event Action<ArcadePhase> PhaseChanged;

        /// <summary>Raised after any slot's lifecycle phase changes.</summary>
        public event Action<int, ArcadePhase> SlotPhaseChanged;

        /// <summary>Raised after a visit is paid, with its stable id and amount.</summary>
        public event Action<string, int> VisitPaid;

        /// <summary>Raised after an arcade upgrade is bought.</summary>
        public event Action<ArcadeUpgradeDefinition> UpgradePurchased;

        /// <summary>Raised after the expansion bay is bought and opened.</summary>
        public event Action ExpansionOpened;

        /// <summary>Raised once per newly earned milestone.</summary>
        public event Action<string> MilestoneReached;

        public ArcadeState State
        {
            get
            {
                EnsureState();
                return _state;
            }
        }

        public ArcadePhase Phase
        {
            get
            {
                EnsureState();
                return _state.Phase;
            }
        }

        public bool IsInstalled
        {
            get
            {
                EnsureState();
                return _state.IsInstalled;
            }
        }

        public bool IsActive
        {
            get
            {
                EnsureState();
                return _state.IsActive;
            }
        }

        /// <summary>Income recorded on the original machine.</summary>
        public int TotalIncome
        {
            get
            {
                EnsureState();
                return _state.TotalIncome;
            }
        }

        /// <summary>Visits recorded on the original machine.</summary>
        public int VisitCount
        {
            get
            {
                EnsureState();
                return _state.VisitCount;
            }
        }

        /// <summary>Income across every machine, used by milestones and the HUD.</summary>
        public int LifetimeIncome
        {
            get
            {
                int total = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    ArcadeState state = GetSlotState(i);
                    if (state != null)
                    {
                        total += state.TotalIncome;
                    }
                }

                return total;
            }
        }

        /// <summary>Visits across every machine.</summary>
        public int TotalVisits
        {
            get
            {
                int total = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    ArcadeState state = GetSlotState(i);
                    if (state != null)
                    {
                        total += state.VisitCount;
                    }
                }

                return total;
            }
        }

        public int MachineCost => _machineCost;

        /// <summary>How much more money the player needs for the first machine; 0 when affordable.</summary>
        public int InstallShortfall
        {
            get
            {
                EconomyService economy = ResolveEconomy();
                int balance = economy != null ? economy.Balance : 0;
                return Mathf.Max(0, _machineCost - balance);
            }
        }

        /// <summary>True once the hidden room has been prepared to Usable.</summary>
        public bool IsRoomReady => _room != null && _room.Stage == SecretRoomStage.Usable;

        /// <summary>The scene-authored attract mode for the starter cabinet.</summary>
        public ArcadeAttractMode AttractMode => _attractMode;

        /// <summary>True when the first cabinet exists and sits at its install point.</summary>
        public bool IsPlacementValid
        {
            get
            {
                if (_cabinet == null)
                {
                    return false;
                }

                if (_placementAnchor == null)
                {
                    return true;
                }

                Vector3 offset = _cabinet.transform.position - _placementAnchor.position;
                offset.y = 0f;
                return offset.magnitude <= Mathf.Max(0.1f, _placementRadius);
            }
        }

        /// <summary>Last purchase outcome, exposed for feedback and tests.</summary>
        public ArcadeInstallResult LastInstallResult { get; private set; } = ArcadeInstallResult.None;

        /// <summary>Last interaction or income result, exposed for tests.</summary>
        public string LastResult { get; private set; } = string.Empty;

        /// <summary>Number of machines the room can hold right now.</summary>
        public int Capacity
        {
            get
            {
                int capacity = 1;
                for (int i = 0; i < _extraMachines.Count; i++)
                {
                    if (_extraMachines[i] != null && _extraMachines[i].IsReachable)
                    {
                        capacity++;
                    }
                }

                return capacity;
            }
        }

        /// <summary>Slot 0 plus every extra slot, including sealed expansion slots.</summary>
        public int SlotCount => 1 + _extraMachines.Count;

        public int ActiveMachineCount
        {
            get
            {
                int count = IsActive ? 1 : 0;
                for (int i = 0; i < _extraMachines.Count; i++)
                {
                    if (_extraMachines[i] != null && _extraMachines[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int InstalledMachineCount
        {
            get
            {
                int count = IsInstalled ? 1 : 0;
                for (int i = 0; i < _extraMachines.Count; i++)
                {
                    if (_extraMachines[i] != null && _extraMachines[i].IsInstalled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasActiveMachine => ActiveMachineCount > 0;

        public float IncomeMultiplier => State.IncomeMultiplier;

        public float TrafficMultiplier => State.TrafficMultiplier;

        /// <summary>
        /// Combined traffic factor: footfall x upgrade multiplier x sub-linear
        /// machine factor. The traffic component divides its base interval by it.
        /// </summary>
        public float TrafficFactor
        {
            get
            {
                float footfall = _reputation != null ? _reputation.FootfallMultiplier : 1f;
                float machines = Mathf.Sqrt(Mathf.Max(1, ActiveMachineCount));
                return Mathf.Max(0.01f, footfall * TrafficMultiplier * machines);
            }
        }

        public bool IsExpanded => _expansionPurchased;

        /// <summary>Save/load restore of the bay purchase without charging again.</summary>
        public void RestoreExpansion(bool expanded)
        {
            if (!expanded || _expansionPurchased)
            {
                return;
            }

            _expansionPurchased = true;
            if (_expansionSeal != null)
            {
                _expansionSeal.SetActive(false);
            }

            if (_expansionBay != null)
            {
                _expansionBay.SetActive(true);
            }
        }

        public int ExpansionCost => _expansionCost;

        public int ExpansionShortfall
        {
            get
            {
                EconomyService economy = ResolveEconomy();
                int balance = economy != null ? economy.Balance : 0;
                return Mathf.Max(0, _expansionCost - balance);
            }
        }

        public LaundryReputation Reputation => _reputation;

        public IReadOnlyList<string> Milestones => State.Milestones;

        public string NextMilestone => ArcadeMilestones.Next(Milestones);

        public int UpkeepPaid { get; private set; }

        public int UpkeepSkipped { get; private set; }

        public string LastUpkeepResult { get; private set; } = string.Empty;

        /// <summary>Authoring/test wiring for the scene objects and shared spawner economy.</summary>
        public void Configure(
            SecretRoomController room,
            ArcadeCabinet cabinet,
            ArcadeAttractMode attractMode,
            Transform placementAnchor,
            CustomerSpawner spawner)
        {
            _economy = null;
            _spawner = spawner;
            ApplyWiring(room, cabinet, attractMode, placementAnchor);
        }

        /// <summary>Authoring/test wiring with an explicit economy instead of a spawner.</summary>
        public void Configure(
            SecretRoomController room,
            ArcadeCabinet cabinet,
            ArcadeAttractMode attractMode,
            Transform placementAnchor,
            EconomyService economy)
        {
            _spawner = null;
            _economy = economy;
            ApplyWiring(room, cabinet, attractMode, placementAnchor);
        }

        public void SetStarterDefinition(ArcadeMachineDefinition definition)
        {
            _starterDefinition = definition;
            ConfigureStarterGame();
        }

        private void ConfigureStarterGame()
        {
            if (_cabinet == null || _starterDefinition == null)
            {
                return;
            }

            _cabinet.ConfigureGame(
                _starterDefinition.GameKind,
                _starterDefinition.OpponentName,
                _starterDefinition.AccentColor,
                _starterDefinition.OpponentColor);
        }

        public void SetReputation(LaundryReputation reputation)
        {
            _reputation = reputation;
        }

        public void SetUpgradeCatalogue(IEnumerable<ArcadeUpgradeDefinition> upgrades)
        {
            _upgradeCatalogue.Clear();
            if (upgrades == null)
            {
                return;
            }

            foreach (ArcadeUpgradeDefinition upgrade in upgrades)
            {
                if (upgrade != null)
                {
                    _upgradeCatalogue.Add(upgrade);
                }
            }
        }

        public void SetExpansionBay(GameObject bay, GameObject seal, int cost)
        {
            _expansionBay = bay;
            _expansionSeal = seal;
            _expansionCost = Mathf.Max(0, cost);
        }

        public void SetUpkeep(float intervalSeconds, int perMachine)
        {
            _upkeepIntervalSeconds = Mathf.Max(1f, intervalSeconds);
            _upkeepPerMachine = Mathf.Max(0, perMachine);
        }

        /// <summary>Test/authoring hook for the first machine's price.</summary>
        public void SetMachineCost(int cost)
        {
            _machineCost = Mathf.Max(0, cost);
        }

        /// <summary>Test/authoring hook for the installation delay.</summary>
        public void SetInstallSeconds(float seconds)
        {
            _installSeconds = Mathf.Max(0f, seconds);
            if (Phase == ArcadePhase.Installing)
            {
                _installRemaining = Mathf.Min(_installRemaining, _installSeconds);
            }
        }

        /// <summary>Authoring hook for how far the first cabinet may sit from its install point.</summary>
        public void SetPlacementRadius(float radius)
        {
            _placementRadius = Mathf.Max(0.1f, radius);
        }

        /// <summary>
        /// Adds an extra machine slot. Used by tests and by the scene builder.
        /// Returns the slot index (1 or greater), or -1 for invalid wiring.
        /// </summary>
        public int AddSlot(
            Transform anchor,
            GameObject installPoint,
            ArcadeMachineDefinition definition,
            GameObject cabinet = null,
            ArcadeAttractMode attractMode = null)
        {
            if (anchor == null || definition == null)
            {
                return -1;
            }

            var slot = new ArcadeExtraMachine();
            slot.Configure(anchor, installPoint, definition);
            if (cabinet != null)
            {
                slot.SetCabinet(cabinet, attractMode);
                ArcadeCabinet cabinetBehaviour = cabinet.GetComponent<ArcadeCabinet>();
                if (cabinetBehaviour != null)
                {
                    cabinetBehaviour.SetService(this, _extraMachines.Count + 1);
                    cabinetBehaviour.ConfigureGame(
                        definition.GameKind,
                        definition.OpponentName,
                        definition.AccentColor,
                        definition.OpponentColor);
                }

                if (cabinet.activeSelf)
                {
                    cabinet.SetActive(false);
                }
            }

            ArcadeInstallPoint point = installPoint != null ? installPoint.GetComponent<ArcadeInstallPoint>() : null;
            if (point != null)
            {
                point.SetSlot(this, _extraMachines.Count + 1);
            }

            _extraMachines.Add(slot);
            return _extraMachines.Count;
        }

        public void ClearExtraMachines()
        {
            _extraMachines.Clear();
        }

        public ArcadeExtraMachine GetExtraMachine(int slotIndex)
        {
            int index = slotIndex - 1;
            return index >= 0 && index < _extraMachines.Count ? _extraMachines[index] : null;
        }

        public ArcadeState GetSlotState(int slotIndex)
        {
            if (slotIndex == 0)
            {
                EnsureState();
                return _state;
            }

            ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
            return slot != null ? slot.State : null;
        }

        public ArcadeMachineDefinition GetSlotDefinition(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return ResolveStarterDefinition();
            }

            ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
            return slot != null ? slot.Definition : null;
        }

        public int GetSlotCost(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return _machineCost;
            }

            ArcadeMachineDefinition definition = GetSlotDefinition(slotIndex);
            return definition != null ? definition.Cost : 0;
        }

        public bool IsSlotInstalled(int slotIndex)
        {
            ArcadeState state = GetSlotState(slotIndex);
            return state != null && state.IsInstalled;
        }

        public bool IsSlotActive(int slotIndex)
        {
            ArcadeState state = GetSlotState(slotIndex);
            return state != null && state.IsActive;
        }

        public bool IsSlotReachable(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return true;
            }

            ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
            return slot != null && slot.IsReachable;
        }

        public ArcadeCabinet GetSlotCabinet(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return _cabinet;
            }

            ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
            return slot != null && slot.Cabinet != null ? slot.Cabinet.GetComponent<ArcadeCabinet>() : null;
        }

        public Transform GetSlotCabinetTransform(int slotIndex)
        {
            if (slotIndex == 0)
            {
                return _cabinet != null ? _cabinet.transform : _placementAnchor;
            }

            ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
            return slot != null ? slot.TargetTransform : null;
        }

        /// <summary>
        /// Marks the first machine as already installed and powered. Used when the
        /// cabinet is authored inside the hidden room and only the room has to be
        /// unlocked.
        /// </summary>
        public void SetStartActive(bool startActive)
        {
            _startActive = startActive;
            EnsureState();
            if (!startActive || IsInstalled)
            {
                return;
            }

            State.BeginInstall();
            State.Activate();
            if (IsActive && _attractMode != null)
            {
                _attractMode.SetPowered(true);
            }

            EvaluateMilestones();
        }

        /// <summary>True when the first machine ships already installed in the room.</summary>
        public bool StartActive => _startActive;

        /// <summary>
        /// Checks every purchase condition without spending. Used by
        /// <see cref="TryInstall(float)"/> and by the install point's prompt.
        /// </summary>
        public ArcadeInstallResult EvaluateInstall()
        {
            return EvaluateInstall(0);
        }

        /// <summary>Checks a purchase for <paramref name="slotIndex"/> without spending.</summary>
        public ArcadeInstallResult EvaluateInstall(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                return ArcadeInstallResult.InvalidPlacement;
            }

            if (IsSlotInstalled(slotIndex))
            {
                return ArcadeInstallResult.AlreadyInstalled;
            }

            if (!IsRoomReady)
            {
                return ArcadeInstallResult.RoomNotReady;
            }

            if (slotIndex == 0)
            {
                if (_cabinet == null)
                {
                    return ArcadeInstallResult.NoMachine;
                }

                if (!IsPlacementValid)
                {
                    return ArcadeInstallResult.InvalidPlacement;
                }
            }
            else
            {
                ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
                if (slot == null || !slot.IsReachable || slot.Definition == null)
                {
                    return ArcadeInstallResult.SlotUnavailable;
                }

                if (slot.Cabinet == null && slot.Definition.Prefab == null)
                {
                    return ArcadeInstallResult.NoMachine;
                }
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                return ArcadeInstallResult.NoEconomy;
            }

            if (economy.Balance < GetSlotCost(slotIndex))
            {
                return ArcadeInstallResult.Unaffordable;
            }

            return ArcadeInstallResult.Success;
        }

        /// <summary>
        /// Buys and installs the first machine. Kept for Phase 8 compatibility.
        /// </summary>
        public ArcadeInstallResult TryInstall(float atSeconds)
        {
            return TryInstall(0, atSeconds);
        }

        /// <summary>
        /// Buys and installs the machine in <paramref name="slotIndex"/>. A rejected
        /// purchase has no side effects; a successful one debits exactly once and
        /// moves the slot to <see cref="ArcadePhase.Installing"/>.
        /// </summary>
        public ArcadeInstallResult TryInstall(int slotIndex, float atSeconds)
        {
            ArcadeInstallResult evaluation = EvaluateInstall(slotIndex);
            if (evaluation != ArcadeInstallResult.Success)
            {
                LastInstallResult = evaluation;
                LastResult = Describe(evaluation);
                return evaluation;
            }

            ArcadeMachineDefinition definition = GetSlotDefinition(slotIndex);
            string machineName = definition != null ? definition.DisplayName : "arcade machine";
            int cost = GetSlotCost(slotIndex);
            EconomyService economy = ResolveEconomy();
            if (cost > 0 && !economy.TrySpend(
                    cost,
                    slotIndex == 0 ? "Arcade machine" : $"Arcade machine: {machineName}",
                    atSeconds))
            {
                LastInstallResult = ArcadeInstallResult.Unaffordable;
                LastResult = Describe(ArcadeInstallResult.Unaffordable);
                return LastInstallResult;
            }

            if (slotIndex > 0)
            {
                ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
                EnsureExtraCabinet(slotIndex, slot);
                slot.State.BeginInstall();
                slot.SetInstallRemaining(_installSeconds);
            }
            else
            {
                State.BeginInstall();
                _installRemaining = Mathf.Max(0f, _installSeconds);
            }

            LastInstallResult = ArcadeInstallResult.Success;
            LastResult = $"{machineName} installing";
            if (slotIndex == 0)
            {
                PhaseChanged?.Invoke(ArcadePhase.Installing);
            }

            SlotPhaseChanged?.Invoke(slotIndex, ArcadePhase.Installing);

            if (_installSeconds <= 0f)
            {
                ActivateSlot(slotIndex);
            }

            return ArcadeInstallResult.Success;
        }

        /// <summary>
        /// Advances installation timers, reputation decay and operating cost.
        /// Public so tests can simulate minutes without waiting real seconds.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            float step = Mathf.Max(0f, deltaSeconds);
            EnsureState();

            if (Phase == ArcadePhase.Installing)
            {
                _installRemaining -= step;
                if (_installRemaining <= 0f)
                {
                    ActivateSlot(0);
                }
            }

            for (int i = 0; i < _extraMachines.Count; i++)
            {
                ArcadeExtraMachine slot = _extraMachines[i];
                if (slot == null || slot.State.Phase != ArcadePhase.Installing)
                {
                    continue;
                }

                slot.TickInstall(step);
                if (slot.InstallRemaining <= 0f)
                {
                    ActivateSlot(i + 1);
                }
            }

            _reputation?.Tick(step);
            TickUpkeep(step);
            EvaluateMilestones();
        }

        /// <summary>Test/authoring shortcut to finish the first install immediately.</summary>
        public bool TryActivate()
        {
            return TryActivateSlot(0);
        }

        /// <summary>Test/authoring shortcut to finish one slot's install immediately.</summary>
        public bool TryActivateSlot(int slotIndex)
        {
            ArcadeState state = GetSlotState(slotIndex);
            if (state == null || state.Phase != ArcadePhase.Installing)
            {
                LastResult = "The arcade machine is not installing";
                return false;
            }

            ActivateSlot(slotIndex);
            return true;
        }

        /// <summary>
        /// Creates a stable visit id for the first machine. The id is recorded in
        /// the state, so a later save keeps it unique.
        /// </summary>
        public string CreateVisitId()
        {
            return CreateVisitId(0);
        }

        /// <summary>Creates a stable visit id for one machine, unique across slots.</summary>
        public string CreateVisitId(int slotIndex)
        {
            ArcadeState state = GetSlotState(slotIndex);
            if (state == null)
            {
                return string.Empty;
            }

            return slotIndex == 0
                ? state.CreateVisitId()
                : state.CreateVisitId($"arcade-s{slotIndex}-visit");
        }

        /// <summary>
        /// Credits one completed visit for the first machine. Kept for Phase 8
        /// compatibility.
        /// </summary>
        public bool TryCollectVisitIncome(string visitId, int amount, float atSeconds)
        {
            return TryCollectVisitIncome(0, visitId, amount, atSeconds);
        }

        /// <summary>
        /// Credits one completed visit for <paramref name="slotIndex"/> through the
        /// economy service. The visit id is the transaction id, and both the state
        /// and the economy service reject a repeat, so a retried collection pays at
        /// most once.
        /// </summary>
        public bool TryCollectVisitIncome(int slotIndex, string visitId, int amount, float atSeconds)
        {
            ArcadeState state = GetSlotState(slotIndex);
            if (state == null || !state.IsActive)
            {
                LastResult = "The arcade machine is offline";
                return false;
            }

            if (string.IsNullOrWhiteSpace(visitId) || amount <= 0)
            {
                LastResult = "Invalid arcade income";
                return false;
            }

            if (state.HasPaidVisit(visitId))
            {
                LastResult = "Arcade visit already paid";
                return false;
            }

            if (_room != null && !IsRoomReady)
            {
                LastResult = "The hidden room is not ready";
                return false;
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                LastResult = "No economy service is available";
                return false;
            }

            // Record first, then credit: if the economy rejects the credit the visit is
            // forgotten again, so TotalIncome and the wallet can never disagree.
            if (!state.RecordVisit(visitId, amount))
            {
                LastResult = "Arcade income could not be recorded";
                return false;
            }

            if (!economy.TryEarnOnce(visitId, amount, "Arcade visit", atSeconds))
            {
                state.ForgetVisit(visitId, amount);
                LastResult = "Arcade income rejected";
                return false;
            }

            LastResult = $"Arcade income +${amount}";
            if (slotIndex == 0)
            {
                VisitPaid?.Invoke(visitId, amount);
            }

            EvaluateMilestones();
            return true;
        }

        /// <summary>Expected income for one completed visit, including upgrades and footfall.</summary>
        public int GetVisitIncome(int slotIndex)
        {
            ArcadeMachineDefinition definition = GetSlotDefinition(slotIndex);
            int baseIncome = definition != null ? definition.BaseVisitIncome : 3;
            float footfall = _reputation != null ? _reputation.FootfallMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(baseIncome * IncomeMultiplier * footfall));
        }

        public bool HasUpgrade(string upgradeId)
        {
            return State.HasUpgrade(upgradeId);
        }

        /// <summary>
        /// Buys one arcade upgrade. Repeats and unaffordable purchases are rejected
        /// before any money moves; a successful purchase enables its decor object.
        /// </summary>
        public bool TryBuyUpgrade(ArcadeUpgradeDefinition upgrade, float atSeconds)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id))
            {
                LastResult = "Invalid arcade upgrade";
                return false;
            }

            EnsureState();
            if (_state.HasUpgrade(upgrade.Id))
            {
                LastResult = $"{upgrade.DisplayName} is already installed";
                return false;
            }

            if (!IsRoomReady)
            {
                LastResult = "The hidden room is not ready";
                return false;
            }

            if (upgrade.RequiresExpansion && !IsExpanded)
            {
                LastResult = "Build the arcade expansion first";
                return false;
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                LastResult = "No economy service is available";
                return false;
            }

            if (upgrade.Cost > 0 && !economy.TrySpend(
                    upgrade.Cost,
                    $"Arcade upgrade: {upgrade.DisplayName}",
                    atSeconds))
            {
                LastResult = $"Not enough money for {upgrade.DisplayName}";
                return false;
            }

            _state.RecordUpgrade(upgrade.Id, upgrade.IncomeMultiplier, upgrade.TrafficMultiplier);
            if (upgrade.DecorObject != null)
            {
                upgrade.DecorObject.SetActive(true);
            }

            UpgradePurchased?.Invoke(upgrade);
            EvaluateMilestones();
            LastResult = $"{upgrade.DisplayName} installed";
            return true;
        }

        /// <summary>Buys the cheapest unowned upgrade from the configured catalogue.</summary>
        public bool TryBuyNextUpgrade(float atSeconds)
        {
            ArcadeUpgradeDefinition next = null;
            for (int i = 0; i < _upgradeCatalogue.Count; i++)
            {
                ArcadeUpgradeDefinition candidate = _upgradeCatalogue[i];
                if (candidate == null || HasUpgrade(candidate.Id))
                {
                    continue;
                }

                if (next == null || candidate.Cost < next.Cost)
                {
                    next = candidate;
                }
            }

            return next != null && TryBuyUpgrade(next, atSeconds);
        }

        public bool CanAffordUpgrade(ArcadeUpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return false;
            }

            EconomyService economy = ResolveEconomy();
            return economy != null && economy.Balance >= upgrade.Cost;
        }

        /// <summary>Short reason an upgrade cannot be bought, or an empty string.</summary>
        public string DescribeUpgradeBlocker(ArcadeUpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return "no upgrade selected";
            }

            if (HasUpgrade(upgrade.Id))
            {
                return "already installed";
            }

            if (!IsRoomReady)
            {
                return "room not ready";
            }

            if (upgrade.RequiresExpansion && !IsExpanded)
            {
                return "needs expansion";
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                return "no economy";
            }

            int shortfall = Mathf.Max(0, upgrade.Cost - economy.Balance);
            return shortfall > 0 ? $"need ${shortfall} more" : string.Empty;
        }

        /// <summary>
        /// Buys the second bay. Debits once, opens the visible extension and makes
        /// the extra anchors reachable. Repeats are rejected without spending.
        /// </summary>
        public bool TryExpand(float atSeconds)
        {
            if (_expansionPurchased)
            {
                LastResult = "The arcade expansion bay is already open";
                return false;
            }

            if (!IsRoomReady)
            {
                LastResult = "The hidden room is not ready";
                return false;
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                LastResult = "No economy service is available";
                return false;
            }

            if (_expansionCost > 0 && !economy.TrySpend(_expansionCost, "Arcade expansion", atSeconds))
            {
                LastResult = "Not enough money for the arcade expansion";
                return false;
            }

            _expansionPurchased = true;
            if (_expansionSeal != null)
            {
                _expansionSeal.SetActive(false);
            }

            if (_expansionBay != null)
            {
                _expansionBay.SetActive(true);
            }

            ExpansionOpened?.Invoke();
            EvaluateMilestones();
            LastResult = "The arcade expansion bay is open";
            return true;
        }

        /// <summary>Short reason the expansion cannot be bought, or an empty string.</summary>
        public string DescribeExpansionBlocker()
        {
            if (_expansionPurchased)
            {
                return "already open";
            }

            if (!IsRoomReady)
            {
                return "room not ready";
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                return "no economy";
            }

            int shortfall = ExpansionShortfall;
            return shortfall > 0 ? $"need ${shortfall} more" : string.Empty;
        }

        /// <summary>Short reason the install point is not actionable, or an empty string.</summary>
        public string DescribeInstallBlocker()
        {
            return DescribeInstallBlocker(0);
        }

        /// <summary>Short reason a slot's install point is not actionable, or an empty string.</summary>
        public string DescribeInstallBlocker(int slotIndex)
        {
            ArcadeInstallResult evaluation = EvaluateInstall(slotIndex);
            switch (evaluation)
            {
                case ArcadeInstallResult.Success:
                    return string.Empty;
                case ArcadeInstallResult.Unaffordable:
                    EconomyService economy = ResolveEconomy();
                    int shortfall = economy != null ? Mathf.Max(0, GetSlotCost(slotIndex) - economy.Balance) : 0;
                    return shortfall > 0 ? $"need ${shortfall} more" : "not affordable";
                case ArcadeInstallResult.RoomNotReady:
                    return "room not ready";
                case ArcadeInstallResult.NoMachine:
                    return "no machine placed";
                case ArcadeInstallResult.InvalidPlacement:
                    return "machine not in place";
                case ArcadeInstallResult.SlotUnavailable:
                    return "anchor sealed";
                case ArcadeInstallResult.NoEconomy:
                    return "no economy";
                default:
                    return Describe(evaluation);
            }
        }

        /// <summary>Player-facing text for a purchase outcome.</summary>
        public static string Describe(ArcadeInstallResult result)
        {
            switch (result)
            {
                case ArcadeInstallResult.None:
                    return "No install attempt yet";
                case ArcadeInstallResult.Success:
                    return "Arcade machine installed";
                case ArcadeInstallResult.AlreadyInstalled:
                    return "The arcade machine is already installed";
                case ArcadeInstallResult.RoomNotReady:
                    return "The hidden room is not ready";
                case ArcadeInstallResult.NoMachine:
                    return "No arcade machine is placed";
                case ArcadeInstallResult.InvalidPlacement:
                    return "The arcade machine is not in its install spot";
                case ArcadeInstallResult.NoEconomy:
                    return "No economy service is available";
                case ArcadeInstallResult.Unaffordable:
                    return "Not enough money for the arcade machine";
                case ArcadeInstallResult.SlotUnavailable:
                    return "That machine anchor is not available";
                case ArcadeInstallResult.NoDefinition:
                    return "No machine definition is assigned";
                default:
                    return "Unknown arcade install result";
            }
        }

        private void Awake()
        {
            EnsureState();
            if (_cabinet != null)
            {
                _cabinet.SetService(this, 0);
            }

            ConfigureStarterGame();
            WireExtraCabinets();
            UpdateExtraAttractModes();

            if (_attractMode != null)
            {
                _attractMode.SetPowered(IsActive);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (_attractMode != null)
            {
                _attractMode.SetPowered(IsActive);
                _attractMode.SetBusy(_cabinet != null && _cabinet.IsPlaying);
            }

            UpdateExtraAttractModes();
        }

        private void ActivateSlot(int slotIndex)
        {
            ArcadeState state = GetSlotState(slotIndex);
            if (state == null || !state.Activate())
            {
                return;
            }

            if (slotIndex == 0)
            {
                _installRemaining = 0f;
                if (_attractMode != null)
                {
                    _attractMode.SetPowered(true);
                }

                LastResult = "The arcade machine is online";
                PhaseChanged?.Invoke(ArcadePhase.Active);
            }
            else
            {
                ArcadeExtraMachine slot = GetExtraMachine(slotIndex);
                if (slot != null)
                {
                    slot.SetInstallRemaining(0f);

                    // The purchase pedestal sits in front of the anchor. Hide it once
                    // the machine is bought so the cabinet is visible and targetable.
                    if (slot.InstallPoint != null && slot.InstallPoint.activeSelf)
                    {
                        slot.InstallPoint.SetActive(false);
                    }

                    if (slot.Cabinet != null)
                    {
                        slot.Cabinet.SetActive(true);
                        ArcadeCabinet cabinet = slot.Cabinet.GetComponent<ArcadeCabinet>();
                        if (cabinet != null)
                        {
                            cabinet.SetService(this, slotIndex);
                        }
                    }

                    if (slot.AttractMode != null)
                    {
                        slot.AttractMode.SetPowered(true);
                    }
                }

                LastResult = $"{GetSlotDefinition(slotIndex)?.DisplayName ?? "Arcade machine"} online";
            }

            SlotPhaseChanged?.Invoke(slotIndex, ArcadePhase.Active);
            EvaluateMilestones();
        }

        private void EnsureExtraCabinet(int slotIndex, ArcadeExtraMachine slot)
        {
            if (slot == null || slot.Cabinet != null || slot.Definition == null || slot.Definition.Prefab == null)
            {
                return;
            }

            Transform anchor = slot.Anchor;
            GameObject instance = Instantiate(
                slot.Definition.Prefab,
                anchor != null ? anchor.position : transform.position,
                anchor != null ? anchor.rotation : Quaternion.identity);
            instance.name = $"ArcadeCabinet_{slot.Definition.DisplayName}";

            ArcadeCabinet cabinet = instance.GetComponent<ArcadeCabinet>();
            if (cabinet == null)
            {
                cabinet = instance.AddComponent<ArcadeCabinet>();
            }

            cabinet.SetService(this, slotIndex);
            ArcadeAttractMode attract = instance.GetComponent<ArcadeAttractMode>();
            slot.SetCabinet(instance, attract);
        }

        private void TickUpkeep(float deltaSeconds)
        {
            int active = ActiveMachineCount;
            if (active <= 0 || _upkeepPerMachine <= 0)
            {
                _upkeepTimer = 0f;
                return;
            }

            _upkeepTimer += deltaSeconds;
            while (_upkeepTimer >= _upkeepIntervalSeconds)
            {
                _upkeepTimer -= _upkeepIntervalSeconds;
                PayUpkeep(active * _upkeepPerMachine);
            }
        }

        private void PayUpkeep(int amount)
        {
            EconomyService economy = ResolveEconomy();
            if (economy != null && economy.TrySpend(amount, "Arcade upkeep", Time.time))
            {
                UpkeepPaid += amount;
                LastUpkeepResult = $"Arcade upkeep -${amount}";
                _upkeepSkipLogged = false;
                return;
            }

            UpkeepSkipped += 1;
            LastUpkeepResult = $"Arcade upkeep skipped (${amount} unpayable)";
            if (!_upkeepSkipLogged)
            {
                Debug.LogWarning($"[ARCADE] Upkeep of ${amount} could not be paid; skipping until the wallet recovers.");
                _upkeepSkipLogged = true;
            }
        }

        private void EvaluateMilestones()
        {
            EnsureState();
            TryRecordMilestone(ArcadeMilestones.FirstMachine, IsActive);
            TryRecordMilestone(ArcadeMilestones.SecondMachine, ActiveMachineCount >= 2);
            TryRecordMilestone(ArcadeMilestones.Income250, LifetimeIncome >= 250);
            TryRecordMilestone(ArcadeMilestones.Reputation50, _reputation != null && _reputation.Value >= 50);
            TryRecordMilestone(ArcadeMilestones.Expanded, _expansionPurchased);
        }

        private void TryRecordMilestone(string id, bool earned)
        {
            if (!earned || _state.HasMilestone(id))
            {
                return;
            }

            if (_state.RecordMilestone(id))
            {
                MilestoneReached?.Invoke(id);
                LastResult = $"Milestone: {ArcadeMilestones.Describe(id)}";
            }
        }

        private EconomyService ResolveEconomy()
        {
            if (_economy != null)
            {
                return _economy;
            }

            return _spawner != null ? _spawner.Economy : null;
        }

        private ArcadeMachineDefinition ResolveStarterDefinition()
        {
            if (_starterDefinition != null)
            {
                return _starterDefinition;
            }

            if (_fallbackStarter == null)
            {
                _fallbackStarter = ScriptableObject.CreateInstance<ArcadeMachineDefinition>();
                _fallbackStarter.hideFlags = HideFlags.HideAndDontSave;
                _fallbackStarter.Configure("arcade.fighter", "Fighter", _machineCost, 3, 1f);
            }

            return _fallbackStarter;
        }

        private void ApplyWiring(
            SecretRoomController room,
            ArcadeCabinet cabinet,
            ArcadeAttractMode attractMode,
            Transform placementAnchor)
        {
            _room = room;
            _cabinet = cabinet;
            _attractMode = attractMode;
            _placementAnchor = placementAnchor;

            if (_cabinet != null)
            {
                _cabinet.SetService(this, 0);
            }

            ConfigureStarterGame();
            WireExtraCabinets();

            if (_attractMode != null)
            {
                _attractMode.SetPowered(IsActive);
            }
        }

        private void WireExtraCabinets()
        {
            for (int i = 0; i < _extraMachines.Count; i++)
            {
                ArcadeExtraMachine slot = _extraMachines[i];
                if (slot == null || slot.Cabinet == null)
                {
                    continue;
                }

                ArcadeCabinet cabinet = slot.Cabinet.GetComponent<ArcadeCabinet>();
                if (cabinet != null)
                {
                    cabinet.SetService(this, i + 1);
                    ArcadeMachineDefinition definition = slot.Definition;
                    if (definition != null)
                    {
                        cabinet.ConfigureGame(
                            definition.GameKind,
                            definition.OpponentName,
                            definition.AccentColor,
                            definition.OpponentColor);
                    }
                }
            }
        }

        /// <summary>
        /// Keeps each extra cabinet's attract pulse in sync with its own power and
        /// play state. Slot 0 is handled separately because it has a scene-authored
        /// attract component.
        /// </summary>
        private void UpdateExtraAttractModes()
        {
            for (int i = 0; i < _extraMachines.Count; i++)
            {
                ArcadeExtraMachine slot = _extraMachines[i];
                if (slot == null || slot.AttractMode == null)
                {
                    continue;
                }

                bool active = slot.State.IsActive;
                slot.AttractMode.SetPowered(active);
                ArcadeCabinet cabinet = slot.Cabinet != null ? slot.Cabinet.GetComponent<ArcadeCabinet>() : null;
                slot.AttractMode.SetBusy(active && cabinet != null && cabinet.IsPlaying);
            }
        }

        private void EnsureState()
        {
            if (_state == null)
            {
                _state = new ArcadeState();
            }
        }
    }
}
