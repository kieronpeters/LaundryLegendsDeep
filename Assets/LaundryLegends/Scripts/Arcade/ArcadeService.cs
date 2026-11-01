using System;
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
        Unaffordable
    }

    /// <summary>
    /// Owns the first arcade machine's business loop: purchase and installation
    /// through <see cref="EconomyService"/>, the Off -> Installing -> Active
    /// lifecycle, attract-mode driving, and exactly-once arcade income.
    /// <para>
    /// All money movement goes through the economy service. The serializable
    /// <see cref="ArcadeState"/> holds the saveable facts; this component only
    /// projects them onto the scene.
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
        [Tooltip("Install point used as the placement anchor for the cabinet.")]
        [SerializeField] private Transform _placementAnchor;
        [SerializeField, Min(0.1f)] private float _placementRadius = 2.5f;
        [SerializeField, Min(0)] private int _machineCost = 120;
        [SerializeField, Min(0f)] private float _installSeconds = 2.5f;

        [Header("State")]
        [SerializeField] private ArcadeState _state = new ArcadeState();

        [Tooltip("Start already installed and powered, for a machine that ships inside the room.")]
        [SerializeField] private bool _startActive;

        private EconomyService _economy;
        private float _installRemaining;

        /// <summary>Raised whenever the machine lifecycle phase changes.</summary>
        public event Action<ArcadePhase> PhaseChanged;

        /// <summary>Raised after a visit is paid, with its stable id and amount.</summary>
        public event Action<string, int> VisitPaid;

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

        public int TotalIncome
        {
            get
            {
                EnsureState();
                return _state.TotalIncome;
            }
        }

        public int VisitCount
        {
            get
            {
                EnsureState();
                return _state.VisitCount;
            }
        }

        public int MachineCost => _machineCost;

        /// <summary>How much more money the player needs for the machine; 0 when affordable.</summary>
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

        /// <summary>True when the cabinet exists and sits at its install point.</summary>
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

        /// <summary>Test/authoring hook for the machine price.</summary>
        public void SetMachineCost(int cost)
        {
            _machineCost = Mathf.Max(0, cost);
        }

        /// <summary>
        /// Marks the machine as already installed and powered. Used when the cabinet
        /// is authored inside the hidden room and only the room has to be unlocked.
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
        }

        /// <summary>True when the machine ships already installed in the room.</summary>
        public bool StartActive => _startActive;

        /// <summary>Test/authoring hook for the installation delay.</summary>
        public void SetInstallSeconds(float seconds)
        {
            _installSeconds = Mathf.Max(0f, seconds);
            if (Phase == ArcadePhase.Installing)
            {
                _installRemaining = Mathf.Min(_installRemaining, _installSeconds);
            }
        }

        /// <summary>Authoring hook for how far the cabinet may sit from its install point.</summary>
        public void SetPlacementRadius(float radius)
        {
            _placementRadius = Mathf.Max(0.1f, radius);
        }

        /// <summary>
        /// Checks every purchase condition without spending. Used by
        /// <see cref="TryInstall"/> and by the install point's prompt.
        /// </summary>
        public ArcadeInstallResult EvaluateInstall()
        {
            if (IsInstalled)
            {
                return ArcadeInstallResult.AlreadyInstalled;
            }

            if (!IsRoomReady)
            {
                return ArcadeInstallResult.RoomNotReady;
            }

            if (_cabinet == null)
            {
                return ArcadeInstallResult.NoMachine;
            }

            if (!IsPlacementValid)
            {
                return ArcadeInstallResult.InvalidPlacement;
            }

            EconomyService economy = ResolveEconomy();
            if (economy == null)
            {
                return ArcadeInstallResult.NoEconomy;
            }

            if (economy.Balance < _machineCost)
            {
                return ArcadeInstallResult.Unaffordable;
            }

            return ArcadeInstallResult.Success;
        }

        /// <summary>
        /// Buys and installs the first machine. A rejected purchase has no side
        /// effects; a successful one debits exactly once and moves to
        /// <see cref="ArcadePhase.Installing"/>.
        /// </summary>
        public ArcadeInstallResult TryInstall(float atSeconds)
        {
            ArcadeInstallResult evaluation = EvaluateInstall();
            if (evaluation != ArcadeInstallResult.Success)
            {
                LastInstallResult = evaluation;
                LastResult = Describe(evaluation);
                return evaluation;
            }

            EconomyService economy = ResolveEconomy();
            if (_machineCost > 0 && !economy.TrySpend(_machineCost, "Arcade machine", atSeconds))
            {
                LastInstallResult = ArcadeInstallResult.Unaffordable;
                LastResult = Describe(ArcadeInstallResult.Unaffordable);
                return LastInstallResult;
            }

            State.BeginInstall();
            _installRemaining = Mathf.Max(0f, _installSeconds);
            LastInstallResult = ArcadeInstallResult.Success;
            LastResult = Describe(ArcadeInstallResult.Success);
            PhaseChanged?.Invoke(ArcadePhase.Installing);

            if (_installRemaining <= 0f)
            {
                Activate();
            }

            return ArcadeInstallResult.Success;
        }

        /// <summary>
        /// Advances the installation timer. Called from <see cref="Update"/>;
        /// public so tests can finish an install without waiting real seconds.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (Phase != ArcadePhase.Installing)
            {
                return;
            }

            _installRemaining -= Mathf.Max(0f, deltaSeconds);
            if (_installRemaining <= 0f)
            {
                Activate();
            }
        }

        /// <summary>Test/authoring shortcut to finish an install immediately.</summary>
        public bool TryActivate()
        {
            if (Phase != ArcadePhase.Installing)
            {
                LastResult = "The arcade machine is not installing";
                return false;
            }

            Activate();
            return true;
        }

        /// <summary>
        /// Creates a stable visit/transaction id. The id is recorded in the
        /// state, so a later save keeps it unique.
        /// </summary>
        public string CreateVisitId()
        {
            return State.CreateVisitId();
        }

        /// <summary>
        /// Credits one completed arcade visit through the economy service. The
        /// visit id is the transaction id, and both the state and the economy
        /// service reject a repeat, so a retried collection pays at most once.
        /// </summary>
        public bool TryCollectVisitIncome(string visitId, int amount, float atSeconds)
        {
            if (!IsActive)
            {
                LastResult = "The arcade machine is offline";
                return false;
            }

            if (string.IsNullOrWhiteSpace(visitId) || amount <= 0)
            {
                LastResult = "Invalid arcade income";
                return false;
            }

            if (State.HasPaidVisit(visitId))
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
            if (!State.RecordVisit(visitId, amount))
            {
                LastResult = "Arcade income could not be recorded";
                return false;
            }

            if (!economy.TryEarnOnce(visitId, amount, "Arcade visit", atSeconds))
            {
                State.ForgetVisit(visitId, amount);
                LastResult = "Arcade income rejected";
                return false;
            }

            LastResult = $"Arcade income +${amount}";
            VisitPaid?.Invoke(visitId, amount);
            return true;
        }

        /// <summary>Short reason the install point is not actionable, or an empty string.</summary>
        public string DescribeInstallBlocker()
        {
            ArcadeInstallResult evaluation = EvaluateInstall();
            switch (evaluation)
            {
                case ArcadeInstallResult.Success:
                    return string.Empty;
                case ArcadeInstallResult.Unaffordable:
                    int shortfall = InstallShortfall;
                    return shortfall > 0 ? $"need ${shortfall} more" : "not affordable";
                case ArcadeInstallResult.RoomNotReady:
                    return "room not ready";
                case ArcadeInstallResult.NoMachine:
                    return "no machine placed";
                case ArcadeInstallResult.InvalidPlacement:
                    return "machine not in place";
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
                default:
                    return "Unknown arcade install result";
            }
        }

        private void Awake()
        {
            EnsureState();
            if (_cabinet != null)
            {
                _cabinet.SetService(this);
            }

            if (_attractMode != null)
            {
                _attractMode.SetPowered(IsActive);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            if (_attractMode == null)
            {
                return;
            }

            _attractMode.SetPowered(IsActive);
            _attractMode.SetBusy(_cabinet != null && _cabinet.IsPlaying);
        }

        private void Activate()
        {
            if (!State.Activate())
            {
                return;
            }

            _installRemaining = 0f;
            if (_attractMode != null)
            {
                _attractMode.SetPowered(true);
            }

            LastResult = "The arcade machine is online";
            PhaseChanged?.Invoke(ArcadePhase.Active);
        }

        private EconomyService ResolveEconomy()
        {
            if (_economy != null)
            {
                return _economy;
            }

            return _spawner != null ? _spawner.Economy : null;
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
                _cabinet.SetService(this);
            }

            if (_attractMode != null)
            {
                _attractMode.SetPowered(IsActive);
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
