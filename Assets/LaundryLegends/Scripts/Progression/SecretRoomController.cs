using UnityEngine;

namespace LaundryLegends.Progression
{
    /// <summary>
    /// Scene-side owner of <see cref="SecretRoomState"/>. It holds the gating
    /// rules, exposes the player-facing actions, and projects the resulting
    /// stage onto a handful of optional scene references: the sealed door, the
    /// cover that conceals it, the room lights, one prop group per preparation
    /// stage, and the "clues found" readout.
    /// <para>
    /// Every reference is optional, so the component still works (and is still
    /// testable) on a bare GameObject. Nothing is looked up at runtime and no
    /// external event is subscribed to; <see cref="RecordCompletedOrder"/> is
    /// called by whoever owns order completion.
    /// </para>
    /// <para>
    /// Do not place this component under <c>_roomContent</c>: that object is
    /// disabled until the room reaches <see cref="SecretRoomStage.Usable"/>.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SecretRoomController : MonoBehaviour
    {
        [Header("Gating")]
        [Tooltip("Clues the player must find before the room can be revealed.")]
        [SerializeField, Min(1)] private int _requiredClues = 3;

        [Tooltip("Completed laundry orders needed before the room can be revealed.")]
        [SerializeField, Min(1)] private int _requiredOrders = 3;

        [Header("Sealed door (optional)")]
        [Tooltip("Door leaf rotated open once the room is revealed.")]
        [SerializeField] private GameObject _sealedDoor;

        [Tooltip("Local Y rotation applied to the door leaf when it opens.")]
        [SerializeField] private float _doorOpenAngle = 90f;

        [Tooltip("Service panel or shelf covering the door; disabled once revealed.")]
        [SerializeField] private GameObject _sealedDoorCover;

        [Header("Room presentation (optional)")]
        [Tooltip("Light objects enabled from the Powered stage onward.")]
        [SerializeField] private GameObject[] _roomLights;

        [Tooltip("Walkable room contents; enabled only at the Usable stage.")]
        [SerializeField] private GameObject _roomContent;

        [SerializeField] private GameObject _cleanedProps;
        [SerializeField] private GameObject _poweredProps;
        [SerializeField] private GameObject _repairedProps;

        [Header("Clue readout (optional)")]
        [Tooltip("Group shown while at least one clue is known.")]
        [SerializeField] private GameObject _cluesFoundGroup;

        [Tooltip("Mesh text updated with the clue count, when assigned.")]
        [SerializeField] private TextMesh _cluesFoundText;

        private SecretRoomState _state;
        private Quaternion _closedDoorRotation;
        private bool _hasClosedDoorRotation;

        /// <summary>Clues collected so far.</summary>
        public int ClueCount
        {
            get
            {
                EnsureState();
                return _state.Clues;
            }
        }

        /// <summary>Clues required before the room can be revealed.</summary>
        public int RequiredClues
        {
            get
            {
                EnsureState();
                return _state.RequiredClues;
            }
        }

        /// <summary>Completed orders required before the room can be revealed.</summary>
        public int RequiredOrders
        {
            get
            {
                EnsureState();
                return _state.RequiredOrders;
            }
        }

        /// <summary>True when both the clue and order thresholds are met.</summary>
        public bool IsEligible
        {
            get
            {
                EnsureState();
                return _state.IsEligible;
            }
        }

        /// <summary>Current discovery/preparation stage.</summary>
        public SecretRoomStage Stage
        {
            get
            {
                EnsureState();
                return _state.Stage;
            }
        }

        /// <summary>True once the room has been revealed, at any later stage.</summary>
        public bool IsRevealed => Stage != SecretRoomStage.Hidden;

        private void Awake()
        {
            EnsureState();
            CaptureClosedDoorRotation();
            ApplyVisualState();
        }

        /// <summary>
        /// Records one clue. Returns false once every clue is already known, in
        /// which case nothing changes.
        /// </summary>
        public bool TryCollectClue()
        {
            EnsureState();
            if (!_state.TryAddClue())
            {
                return false;
            }

            ApplyVisualState();
            return true;
        }

        /// <summary>
        /// Reports one finished laundry order. Order completion is owned
        /// elsewhere; this method is the only entry point the room exposes.
        /// </summary>
        public void RecordCompletedOrder()
        {
            EnsureState();
            _state.RecordCompletedOrder();
            ApplyVisualState();
        }

        /// <summary>
        /// Reveals the room when the player is eligible. Returns false when the
        /// room is not <see cref="SecretRoomStage.Hidden"/> or the threshold is
        /// not met.
        /// </summary>
        public bool TryReveal()
        {
            EnsureState();
            if (!_state.TryReveal())
            {
                return false;
            }

            ApplyVisualState();
            return true;
        }

        /// <summary>
        /// Advances preparation one stage: Cleaned, Powered, Repaired, Usable.
        /// Returns false before reveal or once the room is usable.
        /// </summary>
        public bool TryPrepare()
        {
            EnsureState();
            if (!_state.TryPrepare())
            {
                return false;
            }

            ApplyVisualState();
            return true;
        }

        /// <summary>Copyable progress for save data.</summary>
        public SecretRoomSnapshot Snapshot()
        {
            EnsureState();
            return _state.Snapshot();
        }

        /// <summary>
        /// Replaces the current progress with <paramref name="snapshot"/> and
        /// re-applies the scene state. Requirements stay as authored on this
        /// component.
        /// </summary>
        public void Restore(SecretRoomSnapshot snapshot)
        {
            _state = SecretRoomState.Restore(snapshot, _requiredClues, _requiredOrders);
            ApplyVisualState();
        }

        /// <summary>
        /// Authoring and test wiring for the thresholds. Resets progress, so
        /// call it before any clue or order is recorded.
        /// </summary>
        public void Configure(int requiredClues, int requiredOrders)
        {
            _requiredClues = Mathf.Max(1, requiredClues);
            _requiredOrders = Mathf.Max(1, requiredOrders);
            _state = new SecretRoomState(_requiredClues, _requiredOrders);
            ApplyVisualState();
        }

        private void EnsureState()
        {
            if (_state == null)
            {
                _state = new SecretRoomState(_requiredClues, _requiredOrders);
            }
        }

        private void CaptureClosedDoorRotation()
        {
            if (_hasClosedDoorRotation || _sealedDoor == null)
            {
                return;
            }

            _closedDoorRotation = _sealedDoor.transform.localRotation;
            _hasClosedDoorRotation = true;
        }

        /// <summary>
        /// Projects the current stage onto the optional scene references. Runs
        /// after every state change, so a restored save and a live action share
        /// exactly one code path.
        /// </summary>
        private void ApplyVisualState()
        {
            SecretRoomStage stage = _state != null ? _state.Stage : SecretRoomStage.Hidden;
            bool revealed = stage != SecretRoomStage.Hidden;
            int clues = _state != null ? _state.Clues : 0;
            int requiredClues = _state != null ? _state.RequiredClues : 0;

            // Door: sealed until the room is revealed, and never resealed afterwards.
            SetDoorOpen(revealed);
            SetActive(_sealedDoorCover, !revealed);

            // Room: prepared props appear as each stage is reached; the lights
            // only come on once power is restored; playable space appears last.
            SetActive(_cleanedProps, stage >= SecretRoomStage.Cleaned);
            SetActive(_poweredProps, stage >= SecretRoomStage.Powered);
            SetActive(_repairedProps, stage >= SecretRoomStage.Repaired);
            SetGroupActive(_roomLights, stage >= SecretRoomStage.Powered);
            SetActive(_roomContent, stage >= SecretRoomStage.Usable);

            // Clue readout.
            SetActive(_cluesFoundGroup, clues > 0);
            if (_cluesFoundText != null)
            {
                _cluesFoundText.text = $"Clues {clues}/{requiredClues}";
            }
        }

        private void SetDoorOpen(bool open)
        {
            if (_sealedDoor == null)
            {
                return;
            }

            CaptureClosedDoorRotation();
            _sealedDoor.transform.localRotation = open
                ? _closedDoorRotation * Quaternion.Euler(0f, _doorOpenAngle, 0f)
                : _closedDoorRotation;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetGroupActive(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                SetActive(target, active);
            }
        }
    }
}
