using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EasyCharacterMovement;
using Gameplay;
using Weapons;

namespace Weapons
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerWeaponsManager : MonoBehaviour
    {
        public enum WeaponSwitchState
        {
            Up,
            Down,
            PutDownPrevious,
            PutUpNew,
        }

        [Tooltip("List of weapon the player will start with")]
        public List<WeaponController> StartingWeapons = new List<WeaponController>();

        [Header("References")]
        [Tooltip("Secondary camera used to avoid seeing weapon go throw geometries")]
        public Camera WeaponCamera;

        [Tooltip("Primary camera attached to the player")]
        public Camera PlayerCamera;

        [Tooltip("Parent transform where all weapon will be added in the hierarchy")]
        public Transform WeaponParentSocket;

        [Tooltip("Position for weapons when active but not actively aiming")]
        public Transform DefaultWeaponPosition;

        [Tooltip("Position for weapons when aiming")]
        public Transform AimingWeaponPosition;

        [Tooltip("Position for innactive weapons")]
        public Transform DownWeaponPosition;

        [Header("Weapon Bob")]
        [Tooltip("Frequency at which the weapon will move around in the screen when the player is in movement")]
        public float BobFrequency = 10f;

        [Tooltip("How fast the weapon bob is applied, the bigger value the fastest")]
        public float BobSharpness = 10f;

        [Tooltip("Distance the weapon bobs when not aiming")]
        public float DefaultBobAmount = 0.05f;

        [Tooltip("Distance the weapon bobs when aiming")]
        public float AimingBobAmount = 0.02f;

        [Header("Weapon Recoil")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest")]
        public float RecoilSharpness = 50f;

        [Tooltip("Maximum distance the recoil can affect the weapon")]
        public float MaxRecoilDistance = 0.5f;

        [Tooltip("How fast the weapon goes back to it's original position after the recoil is finished")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("Misc")]
        [Tooltip("Speed at which the aiming animatoin is played")]
        public float AimingAnimationSpeed = 10f;

        [Tooltip("Field of view when not aiming")]
        public float DefaultFov = 60f;

        [Tooltip("Portion of the regular FOV to apply to the weapon camera")]
        public float WeaponFovMultiplier = 1f;

        [Tooltip("Delay before switching weapon a second time, to avoid recieving multiple inputs from mouse wheel")]
        public float WeaponSwitchDelay = 1f;

        [Tooltip("Layer to set FPS weapon gameObjects to")]
        public LayerMask FpsWeaponLayer;

        public bool IsAiming { get; private set; }
        public bool IsPointingAtEnemy { get; private set; }
        public int ActiveWeaponIndex { get; private set; }

        // TODO: These unityactions aren't being added correctly. they're null down below!!!!!!!!!
        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;

        WeaponController[] _weaponSlots = new WeaponController[9]; // 9 available weapon slots
        PlayerInputHandler _inputHandler;
        PlayerCharacter _character;
        float _weaponBobFactor;
        Vector3 _lastCharacterPosition;
        Vector3 _weaponMainLocalPosition;
        Vector3 _weaponBobLocalPosition;
        Vector3 _weaponRecoilLocalPosition;
        Vector3 _accumulatedRecoil;
        float _timeStartedWeaponSwitch;
        WeaponSwitchState _weaponSwitchState;
        int _weaponSwitchNewWeaponIndex;

        void Start()
        {
            ActiveWeaponIndex = -1;
            _weaponSwitchState = WeaponSwitchState.Down;

            _inputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWeaponsManager>(_inputHandler, this,
                gameObject);

            _character = GetComponent<PlayerCharacter>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacter, PlayerWeaponsManager>(
                _character, this, gameObject);

            SetFov(DefaultFov);

            OnSwitchedToWeapon += OnWeaponSwitched;

            // Add starting weapons
            foreach (var weapon in StartingWeapons)
            {
                AddWeapon(weapon);
            }

            SwitchWeapon(true);
            Debug.Log(OnSwitchedToWeapon);
            Debug.Log(OnAddedWeapon);
            Debug.Log(OnRemovedWeapon);
        }

        void Update()
        {
            // shoot handling
            WeaponController activeWeapon = GetActiveWeapon();

            if (activeWeapon != null && activeWeapon.IsReloading)
                return;

            if (activeWeapon != null && _weaponSwitchState == WeaponSwitchState.Up)
            {
                if (!activeWeapon.AutomaticReload && _inputHandler.GetReloadButtonDown() && activeWeapon.CurrentAmmoRatio < 1.0f)
                {
                    IsAiming = false;
                    activeWeapon.StartReloadAnimation();
                    return;
                }
                // // handle aiming down sights
                IsAiming = _inputHandler.GetAimInputHeld();

                // // handle shooting
                bool hasFired = activeWeapon.HandleShootInputs(
                    _inputHandler.GetFireInputDown(),
                    _inputHandler.GetFireInputHeld(),
                    _inputHandler.GetFireInputReleased());

                // Handle accumulating recoil
                if (hasFired)
                {
                    _accumulatedRecoil += Vector3.back * activeWeapon.RecoilForce;
                    _accumulatedRecoil = Vector3.ClampMagnitude(_accumulatedRecoil, MaxRecoilDistance);
                }
            }

            // weapon switch handling
            if (!IsAiming &&
                (activeWeapon == null || !activeWeapon.IsCharging) &&
                (_weaponSwitchState == WeaponSwitchState.Up || _weaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = _inputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    switchWeaponInput = _inputHandler.GetSelectWeaponInput();
                    if (switchWeaponInput != 0)
                    {
                        if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                            SwitchToWeaponIndex(switchWeaponInput - 1);
                    }
                }
            }

            // Pointing at enemy handling
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                if (Physics.Raycast(WeaponCamera.transform.position, WeaponCamera.transform.forward, out RaycastHit hit,
                    1000, -1, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Health>() != null)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }


        // Update various animated features in LateUpdate because it needs to override the animated arm position
        void LateUpdate()
        {
            UpdateWeaponAiming();
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponSwitching();

            // Set final weapon socket position based on all the combined animation influences
            WeaponParentSocket.localPosition =
                _weaponMainLocalPosition + _weaponBobLocalPosition + _weaponRecoilLocalPosition;
        }

        // Sets the FOV of the main camera and the weapon camera simultaneously
        public void SetFov(float fov)
        {
            PlayerCamera.fieldOfView = fov;
            WeaponCamera.fieldOfView = fov * WeaponFovMultiplier;
        }

        // Iterate on all weapon slots to find the next valid weapon to switch to
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = _weaponSlots.Length;
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // If the weapon at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
                // and select it if it's the closest distance yet
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            // Handle switching to the new weapon index
            SwitchToWeaponIndex(newWeaponIndex);
        }

        // Switches to the given weapon index in weapon slots if the new index is a valid weapon that is different from our current one
        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                // Store data related to weapon switching animation
                _weaponSwitchNewWeaponIndex = newWeaponIndex;
                _timeStartedWeaponSwitch = Time.time;

                // Handle case of switching to a valid weapon for the first time (simply put it up without putting anything down first)
                if (GetActiveWeapon() == null)
                {
                    _weaponMainLocalPosition = DownWeaponPosition.localPosition;
                    _weaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = _weaponSwitchNewWeaponIndex;

                    WeaponController newWeapon = GetWeaponAtSlotIndex(_weaponSwitchNewWeaponIndex);
                    OnSwitchedToWeapon?.Invoke(newWeapon);
                }
                // otherwise, remember we are putting down our current weapon for switching to the next one
                else
                {
                    _weaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        public WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            // Checks if we already have a weapon coming from the specified prefab
            for (var index = 0; index < _weaponSlots.Length; index++)
            {
                var w = _weaponSlots[index];
                if (w != null && w.SourcePrefab == weaponPrefab.gameObject)
                {
                    return w;
                }
            }

            return null;
        }

        // Updates weapon position and camera FoV for the aiming transition
        void UpdateWeaponAiming()
        {
            if (_weaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();
                if (IsAiming && activeWeapon)
                {
                    _weaponMainLocalPosition = Vector3.Lerp(_weaponMainLocalPosition,
                        AimingWeaponPosition.localPosition + activeWeapon.AimOffset,
                        AimingAnimationSpeed * Time.deltaTime);
                    SetFov(Mathf.Lerp(PlayerCamera.fieldOfView,
                        activeWeapon.AimZoomRatio * DefaultFov, AimingAnimationSpeed * Time.deltaTime));
                }
                else
                {
                    _weaponMainLocalPosition = Vector3.Lerp(_weaponMainLocalPosition,
                        DefaultWeaponPosition.localPosition, AimingAnimationSpeed * Time.deltaTime);
                    SetFov(Mathf.Lerp(PlayerCamera.fieldOfView, DefaultFov,
                        AimingAnimationSpeed * Time.deltaTime));
                }
            }
        }

        // Updates the weapon bob animation based on character speed
        void UpdateWeaponBob()
        {
            if (Time.deltaTime > 0f)
            {
                Vector3 playerCharacterVelocity =
                    (_character.transform.position - _lastCharacterPosition) / Time.deltaTime;

                // calculate a smoothed weapon bob amount based on how close to our max grounded movement velocity we are
                float characterMovementFactor = 0f;
                if (_character.IsGrounded())
                {
                    characterMovementFactor =
                        Mathf.Clamp01(playerCharacterVelocity.magnitude /
                                        (_character.GetMaxSpeed() *
                                        _character.sprintSpeedModifier));
                }

                _weaponBobFactor =
                    Mathf.Lerp(_weaponBobFactor, characterMovementFactor, BobSharpness * Time.deltaTime);

                // Calculate vertical and horizontal weapon bob values based on a sine function
                float bobAmount = IsAiming ? AimingBobAmount : DefaultBobAmount;
                float frequency = BobFrequency;
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * _weaponBobFactor;
                float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount *
                                    _weaponBobFactor;

                // Apply weapon bob
                _weaponBobLocalPosition.x = hBobValue;
                _weaponBobLocalPosition.y = Mathf.Abs(vBobValue);

                _lastCharacterPosition = _character.transform.position;
            }
        }

        // Updates the weapon recoil animation
        void UpdateWeaponRecoil()
        {
            // if the accumulated recoil is further away from the current position, make the current position move towards the recoil target
            if (_weaponRecoilLocalPosition.z >= _accumulatedRecoil.z * 0.99f)
            {
                _weaponRecoilLocalPosition = Vector3.Lerp(_weaponRecoilLocalPosition, _accumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            // otherwise, move recoil position to make it recover towards its resting pose
            else
            {
                _weaponRecoilLocalPosition = Vector3.Lerp(_weaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                _accumulatedRecoil = _weaponRecoilLocalPosition;
            }
        }

        // Updates the animated transition of switching weapons
        void UpdateWeaponSwitching()
        {
            // Calculate the time ratio (0 to 1) since weapon switch was triggered
            float switchingTimeFactor = 0f;
            if (WeaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - _timeStartedWeaponSwitch) / WeaponSwitchDelay);
            }

            // Handle transiting to new switch state
            if (switchingTimeFactor >= 1f)
            {
                if (_weaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    // Deactivate old weapon
                    WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = _weaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    // Activate new weapon
                    WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    OnSwitchedToWeapon?.Invoke(newWeapon);

                    if (newWeapon)
                    {
                        _timeStartedWeaponSwitch = Time.time;
                        _weaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        // if new weapon is null, don't follow through with putting weapon back up
                        _weaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if (_weaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    _weaponSwitchState = WeaponSwitchState.Up;
                }
            }

            // Handle moving the weapon socket position for the animated weapon switching
            if (_weaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                _weaponMainLocalPosition = Vector3.Lerp(DefaultWeaponPosition.localPosition,
                    DownWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (_weaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                _weaponMainLocalPosition = Vector3.Lerp(DownWeaponPosition.localPosition,
                    DefaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        // Adds a weapon to our inventory
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            // if we already hold this weapon type (a weapon coming from the same source prefab), don't add the weapon
            if (HasWeapon(weaponPrefab) != null)
            {
                return false;
            }

            // search our weapon slots for the first free one, assign the weapon to it, and return true if we found one. Return false otherwise
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // only add the weapon if the slot is free
                if (_weaponSlots[i] == null)
                {
                    // spawn the weapon prefab as child of the weapon socket
                    WeaponController weaponInstance = Instantiate(weaponPrefab, WeaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    // Set owner to this gameObject so the weapon can alter projectile/damage logic accordingly
                    weaponInstance.Owner = gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    // Assign the first person layer to the weapon
                    int layerIndex =
                        Mathf.RoundToInt(Mathf.Log(FpsWeaponLayer.value,
                            2)); // This function converts a layermask to a layer index
                    foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.layer = layerIndex;
                    }

                    _weaponSlots[i] = weaponInstance;
                    OnAddedWeapon?.Invoke(weaponInstance, i);

                    return true;
                }
            }

            // Handle auto-switching to weapon if no weapons currently
            if (GetActiveWeapon() == null)
            {
                SwitchWeapon(true);
            }

            return false;
        }

        public bool RemoveWeapon(WeaponController weaponInstance)
        {
            // Look through our slots for that weapon
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // when weapon found, remove it
                if (_weaponSlots[i] == weaponInstance)
                {
                    _weaponSlots[i] = null;

                    OnRemovedWeapon?.Invoke(weaponInstance, i);

                    Destroy(weaponInstance.gameObject);

                    // Handle case of removing active weapon (switch to next weapon)
                    if (i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }

        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            // find the active weapon in our weapon slots based on our active weapon index
            if (index >= 0 &&
                index < _weaponSlots.Length)
            {
                return _weaponSlots[index];
            }

            // if we didn't find a valid active weapon in our weapon slots, return null
            return null;
        }

        // Calculates the "distance" between two weapon slot indexes
        // For example: if we had 5 weapon slots, the distance between slots #2 and #4 would be 2 in ascending order, and 3 in descending order
        int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = _weaponSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}
