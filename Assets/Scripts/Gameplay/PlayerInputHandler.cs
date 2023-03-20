using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EasyCharacterMovement;

namespace Gameplay
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;
        private InputAction fireWeaponInputAction { get; set; }
        private InputAction aimWeaponInputAction { get; set; }
        private InputAction reloadWeaponInputAction { get; set; }
        private InputAction switchWeaponInputAction { get; set; }
        private bool _isFiring;
        private bool _isAiming;
        private bool _isReloading;
        private int _switchWeaponInput;

        private GameFlowManager _gameFlowManager;
        private bool _fireInputWasHeld;


        void OnEnable()
        {
            InitPlayerInput();
        }

        void OnDisable()
        {
            DeinitPlayerInput();
        }

        void Start()
        {
            _gameFlowManager = FindObjectOfType<GameFlowManager>();
            DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, PlayerInputHandler>(_gameFlowManager, this);
        }

        void LateUpdate()
        {
            _fireInputWasHeld = GetFireInputHeld();
            _switchWeaponInput = 0;
        }

        public bool GetFireInputDown()
        {
            return GetFireInputHeld() && !_fireInputWasHeld;
        }

        public bool GetFireInputReleased()
        {
            return !GetFireInputHeld() && _fireInputWasHeld;
        }

        public bool GetFireInputHeld()
        {
            return _isFiring;
        }

        public bool GetAimInputHeld()
        {
            return _isAiming;
        }

        public bool GetReloadButtonDown()
        {
            return _isReloading;
        }

        public int GetSwitchWeaponInput()
        {
            return _switchWeaponInput;
        }

        public int GetSelectWeaponInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                return 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                return 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                return 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                return 4;
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                return 5;
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                return 6;
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                return 7;
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                return 8;
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                return 9;
            else
                return 0;
        }

        public void FireWeapon()
        {
            _isFiring = true;
        }

        public void StopFiringWeapon()
        {
            _isFiring = false;
        }

        public void AimWeapon()
        {
            _isAiming = true;
        }

        public void StopAimingWeapon()
        {
            _isAiming = false;
        }

        public void ReloadWeapon()
        {
            _isReloading = true;
        }

        public void StopReloadingWeapon()
        {
            _isReloading = false;
        }

        private void OnFireWeapon(InputAction.CallbackContext context)
        {
            if (context.started)
                FireWeapon();
            else if (context.canceled)
                StopFiringWeapon();
        }

        private void OnAimWeapon(InputAction.CallbackContext context)
        {
            if (context.started)
                AimWeapon();
            else if (context.canceled)
                StopAimingWeapon();
        }

        private void OnReloadWeapon(InputAction.CallbackContext context)
        {
            if (context.started)
                ReloadWeapon();
            else if (context.canceled)
                StopReloadingWeapon();
        }

        private void OnSwitchWeapon(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (context.ReadValue<float>() > 0f)
                {
                    _switchWeaponInput = 1;
                }
                else
                {
                    _switchWeaponInput = -1;
                }
            }
        }

        private void InitPlayerInput()
        {
            fireWeaponInputAction = inputActions.FindAction("FireWeapon");
            if (fireWeaponInputAction != null)
            {
                fireWeaponInputAction.started += OnFireWeapon;
                fireWeaponInputAction.canceled += OnFireWeapon;

                fireWeaponInputAction.Enable();
            }

            aimWeaponInputAction = inputActions.FindAction("AimWeapon");
            if (aimWeaponInputAction != null)
            {
                aimWeaponInputAction.started += OnAimWeapon;
                aimWeaponInputAction.canceled += OnAimWeapon;

                aimWeaponInputAction.Enable();
            }

            reloadWeaponInputAction = inputActions.FindAction("ReloadWeapon");
            if (reloadWeaponInputAction != null)
            {
                reloadWeaponInputAction.started += OnReloadWeapon;
                reloadWeaponInputAction.canceled += OnReloadWeapon;

                reloadWeaponInputAction.Enable();
            }

            switchWeaponInputAction = inputActions.FindAction("CycleWeapon");
            if (switchWeaponInputAction != null)
            {
                switchWeaponInputAction.started += OnSwitchWeapon;
                switchWeaponInputAction.canceled += OnSwitchWeapon;

                switchWeaponInputAction.Enable();
            }
        }

        private void DeinitPlayerInput()
        {
            if (fireWeaponInputAction != null)
            {
                fireWeaponInputAction.started -= OnFireWeapon;
                fireWeaponInputAction.canceled -= OnFireWeapon;

                fireWeaponInputAction.Disable();
                fireWeaponInputAction = null;
            }

            if (aimWeaponInputAction != null)
            {
                aimWeaponInputAction.started -= OnAimWeapon;
                aimWeaponInputAction.canceled -= OnAimWeapon;

                aimWeaponInputAction.Disable();
                aimWeaponInputAction = null;
            }

            if (reloadWeaponInputAction != null)
            {
                reloadWeaponInputAction.started -= OnReloadWeapon;
                reloadWeaponInputAction.canceled -= OnReloadWeapon;

                reloadWeaponInputAction.Disable();
            }

            if (switchWeaponInputAction != null)
            {
                switchWeaponInputAction.started -= OnSwitchWeapon;
                switchWeaponInputAction.canceled -= OnSwitchWeapon;

                switchWeaponInputAction.Disable();
            }
        }
    }
}
