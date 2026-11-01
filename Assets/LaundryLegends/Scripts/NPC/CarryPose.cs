using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>
    /// Small upper-body carry bias. Runs after the Animator evaluates so the
    /// legs, hips, and spine keep the real locomotion clip while the arms fold
    /// into a two-handed carry. Blends in and out smoothly and never touches
    /// bones when the NPC is not carrying.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CarryPose : MonoBehaviour
    {
        [SerializeField] private NpcLocomotion _locomotion;
        [SerializeField] private float _blendSpeed = 6f;

        /// <summary>Seconds the reach-in pose plays when a carry starts.</summary>
        [SerializeField] private float _reachSeconds = 0.28f;

        [SerializeField] private float _reachDegrees = 18f;
        [SerializeField] private Vector3 _leftArmEuler = new Vector3(28f, -12f, -58f);
        [SerializeField] private Vector3 _rightArmEuler = new Vector3(28f, 12f, 58f);
        [SerializeField] private Vector3 _leftForeArmEuler = new Vector3(-62f, 0f, -18f);
        [SerializeField] private Vector3 _rightForeArmEuler = new Vector3(-62f, 0f, 18f);

        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftForeArm;
        private Transform _rightForeArm;
        private float _weight;
        private float _reachTimer;
        private bool _wasCarrying;

        private void Awake()
        {
            if (_locomotion == null)
            {
                _locomotion = GetComponent<NpcLocomotion>();
            }

            SkinnedMeshRenderer skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned == null)
            {
                enabled = false;
                return;
            }

            foreach (Transform bone in skinned.bones)
            {
                string name = bone.name;

                if (Matches(name, "LeftForeArm", "forearm01.L", "lowerarm.L", "LowerArm_L", "LeftLowerArm"))
                {
                    _leftForeArm = bone;
                }
                else if (Matches(name, "RightForeArm", "forearm01.R", "lowerarm.R", "LowerArm_R", "RightLowerArm"))
                {
                    _rightForeArm = bone;
                }
                else if (Matches(name, "LeftArm", "upperarm01.L", "upperarm.L", "UpperArm_L", "LeftUpperArm"))
                {
                    _leftArm = bone;
                }
                else if (Matches(name, "RightArm", "upperarm01.R", "upperarm.R", "UpperArm_R", "RightUpperArm"))
                {
                    _rightArm = bone;
                }
            }

            if (_leftArm == null || _rightArm == null)
            {
                enabled = false;
            }
        }

        private static bool Matches(string boneName, string suffix, params string[] alternatives)
        {
            if (boneName.EndsWith(suffix, System.StringComparison.Ordinal))
            {
                return true;
            }

            foreach (string alternative in alternatives)
            {
                if (boneName.IndexOf(alternative, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void LateUpdate()
        {
            bool carrying = _locomotion != null && _locomotion.Carrying;
            _weight = Mathf.MoveTowards(_weight, carrying ? 1f : 0f, _blendSpeed * Time.deltaTime);            if (carrying && !_wasCarrying)
            {
                _reachTimer = _reachSeconds;
            }

            _wasCarrying = carrying;

            float reach = 0f;
            if (_reachTimer > 0f)
            {
                _reachTimer = Mathf.Max(0f, _reachTimer - Time.deltaTime);
                float phase = 1f - (_reachTimer / Mathf.Max(0.01f, _reachSeconds));
                reach = Mathf.Sin(phase * Mathf.PI) * _reachDegrees;
            }

            if (_weight <= 0.0001f)
            {
                return;
            }

            Apply(_leftArm, _leftArmEuler + new Vector3(-reach, 0f, 0f));
            Apply(_rightArm, _rightArmEuler + new Vector3(-reach, 0f, 0f));
            Apply(_leftForeArm, _leftForeArmEuler);
            Apply(_rightForeArm, _rightForeArmEuler);
        }

        private void Apply(Transform bone, Vector3 eulerOffset)
        {
            if (bone == null)
            {
                return;
            }

            Quaternion target = bone.localRotation * Quaternion.Euler(eulerOffset);
            bone.localRotation = Quaternion.Slerp(bone.localRotation, target, _weight);
        }
    }
}
