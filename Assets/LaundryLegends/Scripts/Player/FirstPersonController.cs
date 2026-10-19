using UnityEngine;

namespace LaundryLegends.Player
{
    /// <summary>
    /// First-person movement and mouse look using the legacy Input Manager.
    /// Camera transform is assigned explicitly by the Player prefab.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private PauseController _pauseController;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _maxPitch = 85f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        public bool IsMoving { get; private set; }

        public Vector3 LastMoveDirection { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_pauseController == null)
            {
                _pauseController = GetComponent<PauseController>();
            }
        }

        private void Update()
        {
            if (_pauseController != null && _pauseController.IsPaused)
            {
                return;
            }

            Look(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
        }

        /// <summary>
        /// Applies one frame of mouse-look input. Public so tests and future
        /// rebinding layers can drive the controller without reading Input directly.
        /// </summary>
        public void Look(Vector2 delta)
        {
            float yaw = delta.x * _mouseSensitivity;
            float pitchDelta = delta.y * _mouseSensitivity;

            transform.Rotate(Vector3.up * yaw, Space.World);
            _pitch = Mathf.Clamp(_pitch - pitchDelta, -_maxPitch, _maxPitch);

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        /// <summary>
        /// Applies one frame of movement input. Public for the same reason as <see cref="Look"/>.
        /// </summary>
        public void Move(Vector2 input)
        {
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            LastMoveDirection = direction;
            IsMoving = direction.sqrMagnitude > 0.0001f;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 velocity = direction * _moveSpeed;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
