using UnityEngine;

namespace LaundryLegends.Interaction
{
    /// <summary>
    /// Camera raycast stub: reports the interactable in range and forwards the E key.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 3f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        public GameObject CurrentTarget { get; private set; }

        public string CurrentPrompt { get; private set; }

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            CurrentTarget = null;
            CurrentPrompt = null;

            if (Time.timeScale == 0f || _camera == null)
            {
                return;
            }

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _range, _interactionMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            CurrentTarget = hit.collider.gameObject;
            CurrentPrompt = interactable.InteractionPrompt;

            if (Input.GetKeyDown(_interactKey))
            {
                interactable.Interact(gameObject);
            }
        }
    }
}
