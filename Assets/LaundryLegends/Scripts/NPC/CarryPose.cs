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
        [SerializeField] private Vector3 _leftArmEuler = new Vector3(28f, -12f, -58f);
        [SerializeField] private Vector3 _rightArmEuler = new Vector3(28f, 12f, 58f);
        [SerializeField] private Vector3 _leftForeArmEuler = new Vector3(-62f, 0f, -18f);
        [SerializeField] private Vector3 _rightForeArmEuler = new Vector3(-62f, 0f, 18f);

        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftForeArm;
        private Transform _rightForeArm;
        private float _weight;

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
                if (bone.name.EndsWith("LeftForeArm", System.StringComparison.Ordinal))
                {
                    _leftForeArm = bone;
                }
                else if (bone.name.EndsWith("RightForeArm", System.StringComparison.Ordinal))
                {
                    _rightForeArm = bone;
                }
                else if (bone.name.EndsWith("LeftArm", System.StringComparison.Ordinal))
                {
                    _leftArm = bone;
                }
                else if (bone.name.EndsWith("RightArm", System.StringComparison.Ordinal))
                {
                    _rightArm = bone;
                }
            }

            if (_leftArm == null || _rightArm == null)
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            bool carrying = _locomotion != null && _locomotion.Carrying;
            _weight = Mathf.MoveTowards(_weight, carrying ? 1f : 0f, _blendSpeed * Time.deltaTime);
            if (_weight <= 0.0001f)
            {
                return;
            }

            Apply(_leftArm, _leftArmEuler);
            Apply(_rightArm, _rightArmEuler);
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
