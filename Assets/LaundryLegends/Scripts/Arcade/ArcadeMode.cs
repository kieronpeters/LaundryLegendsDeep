using LaundryLegends.Interaction;
using LaundryLegends.Player;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Takes the player out of the shop and into the cabinet: freezes walking,
    /// looking, carrying, interaction and pause, and frees the cursor so the
    /// arcade can own the screen. Exit restores all of it.
    /// </summary>
    public static class ArcadeMode
    {
        public static bool IsActive { get; private set; }

        private static FirstPersonController _movement;
        private static PlayerInteractor _interactor;
        private static CarryController _carry;
        private static PauseController _pause;

        public static void Enter()
        {
            if (IsActive)
            {
                return;
            }

            _movement = Object.FindFirstObjectByType<FirstPersonController>();
            _interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            _carry = Object.FindFirstObjectByType<CarryController>();
            _pause = Object.FindFirstObjectByType<PauseController>();

            if (_movement != null)
            {
                _movement.enabled = false;
            }

            if (_interactor != null)
            {
                _interactor.enabled = false;
            }

            if (_carry != null)
            {
                _carry.enabled = false;
            }

            if (_pause != null)
            {
                _pause.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            IsActive = true;
        }

        public static void Exit()
        {
            if (!IsActive)
            {
                return;
            }

            if (_movement != null)
            {
                _movement.enabled = true;
            }

            if (_interactor != null)
            {
                _interactor.enabled = true;
            }

            if (_carry != null)
            {
                _carry.enabled = true;
            }

            if (_pause != null)
            {
                _pause.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            IsActive = false;
        }
    }
}
