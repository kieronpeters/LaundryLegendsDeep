using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Keeps a cabinet's marquee light in sync with the machine: the bulb is lit
    /// while the slot is powered and switches off when the cabinet is offline.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcadeMarqueeLight : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private ArcadeAttractMode _attract;

        public bool IsLit => _light != null && _light.enabled;

        public void Configure(Light light, ArcadeAttractMode attract)
        {
            _light = light;
            _attract = attract;
        }

        private void Awake()
        {
            if (_attract == null)
            {
                _attract = GetComponent<ArcadeAttractMode>();
            }
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            bool powered = _attract != null && _attract.IsPowered;
            if (_light.enabled != powered)
            {
                _light.enabled = powered;
            }
        }
    }
}
