using System;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// One additional machine slot: where it stands, which definition it sells,
    /// and its own lifecycle state. Slot 0 stays on the service's legacy fields.
    /// </summary>
    [Serializable]
    public sealed class ArcadeExtraMachine
    {
        [SerializeField] private Transform _anchor;
        [SerializeField] private GameObject _installPoint;
        [SerializeField] private GameObject _cabinet;
        [SerializeField] private ArcadeAttractMode _attractMode;
        [SerializeField] private ArcadeMachineDefinition _definition;
        [SerializeField] private ArcadeState _state = new ArcadeState();
        [SerializeField] private float _installRemaining;

        public Transform Anchor => _anchor;
        public GameObject InstallPoint => _installPoint;
        public GameObject Cabinet => _cabinet;
        public ArcadeAttractMode AttractMode => _attractMode;
        public ArcadeMachineDefinition Definition => _definition;
        public ArcadeState State
        {
            get
            {
                if (_state == null)
                {
                    _state = new ArcadeState();
                }

                return _state;
            }
        }

        public bool IsInstalled => State.IsInstalled;
        public bool IsActive => State.IsActive;
        public float InstallRemaining => _installRemaining;

        /// <summary>
        /// True when the anchor is available. The install point is a purchase
        /// pedestal, not part of reachability: it is hidden once the machine is
        /// bought so it cannot block the cabinet behind it.
        /// </summary>
        public bool IsReachable => _anchor != null && _anchor.gameObject.activeInHierarchy;

        /// <summary>Where a visitor should stand to play this machine.</summary>
        public Transform TargetTransform => _cabinet != null ? _cabinet.transform : _anchor;

        public void Configure(Transform anchor, GameObject installPoint, ArcadeMachineDefinition definition)
        {
            _anchor = anchor;
            _installPoint = installPoint;
            _definition = definition;
            _cabinet = null;
            _attractMode = null;
            _state = new ArcadeState();
            _installRemaining = 0f;
        }

        public void SetAnchor(Transform anchor)
        {
            _anchor = anchor;
        }

        public void SetInstallPoint(GameObject installPoint)
        {
            _installPoint = installPoint;
        }

        public void SetCabinet(GameObject cabinet, ArcadeAttractMode attractMode = null)
        {
            _cabinet = cabinet;
            _attractMode = attractMode;
        }

        public void SetDefinition(ArcadeMachineDefinition definition)
        {
            _definition = definition;
        }

        public void SetInstallRemaining(float seconds)
        {
            _installRemaining = Mathf.Max(0f, seconds);
        }

        public void TickInstall(float deltaSeconds)
        {
            if (!IsInstalled || State.IsActive)
            {
                return;
            }

            _installRemaining = Mathf.Max(0f, _installRemaining - Mathf.Max(0f, deltaSeconds));
        }
    }
}
