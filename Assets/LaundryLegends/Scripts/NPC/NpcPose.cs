using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Procedural idle/walk pose for the CC0 humanoids: lowers the T-pose arms,
    /// adds a small arm/leg swing when moving, and keeps a gentle idle sway.
    /// This is a placeholder for retargeted clips.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcPose : MonoBehaviour
    {
        [SerializeField] private float _armDownAngle = 72f;
        [SerializeField] private float _swingAngle = 18f;
        [SerializeField] private float _speed = 1.4f;

        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftLegBase;
        private Quaternion _rightLegBase;
        private Vector3 _lastPosition;
        private float _phase;

        private void Awake()
        {
            SkinnedMeshRenderer skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned == null)
            {
                enabled = false;
                return;
            }

            foreach (Transform bone in skinned.bones)
            {
                if (bone.name.EndsWith("LeftArm"))
                {
                    _leftArm = bone;
                    _leftArmBase = bone.localRotation;
                }
                else if (bone.name.EndsWith("RightArm"))
                {
                    _rightArm = bone;
                    _rightArmBase = bone.localRotation;
                }
                else if (bone.name.EndsWith("LeftUpLeg"))
                {
                    _leftLeg = bone;
                    _leftLegBase = bone.localRotation;
                }
                else if (bone.name.EndsWith("RightUpLeg"))
                {
                    _rightLeg = bone;
                    _rightLegBase = bone.localRotation;
                }
            }

            _lastPosition = transform.position;
            ApplyPose(0f);
        }

        private void LateUpdate()
        {
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            float movement = new Vector2(delta.x, delta.z).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            float amount = Mathf.Clamp01(movement / _speed);
            _phase += Time.deltaTime * (2f + amount * 7f);
            ApplyPose(amount);
        }

        private void ApplyPose(float walkAmount)
        {
            float swing = Mathf.Sin(_phase) * _swingAngle * walkAmount;
            float idle = Mathf.Sin(_phase * 0.6f) * 2f;

            if (_leftArm != null)
            {
                _leftArm.localRotation = _leftArmBase
                    * Quaternion.Euler(0f, 0f, _armDownAngle)
                    * Quaternion.Euler(swing + idle, 0f, 0f);
            }

            if (_rightArm != null)
            {
                _rightArm.localRotation = _rightArmBase
                    * Quaternion.Euler(0f, 0f, -_armDownAngle)
                    * Quaternion.Euler(-swing + idle, 0f, 0f);
            }

            if (_leftLeg != null)
            {
                _leftLeg.localRotation = _leftLegBase * Quaternion.Euler(-swing * 0.5f, 0f, 0f);
            }

            if (_rightLeg != null)
            {
                _rightLeg.localRotation = _rightLegBase * Quaternion.Euler(swing * 0.5f, 0f, 0f);
            }
        }
    }
}
