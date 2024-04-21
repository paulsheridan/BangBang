using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Gameplay
{
    public class PlayerLookInput : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;
        [Space(15.0f)]
        public bool invertLook = true;
        [Tooltip("Mouse look sensitivity")]
        public Vector2 mouseSensitivity = new Vector2(1.0f, 1.0f);
        [Tooltip("Controller look sensitivity")]
        public Vector2 controllerSensitivity = new Vector2(1.0f, 1.0f);

        [Space(15.0f)]
        [Tooltip("How far in degrees can you move the camera down.")]
        public float minPitch = -80.0f;
        [Tooltip("How far in degrees can you move the camera up.")]
        public float maxPitch = 80.0f;

        private PlayerCharacter _playerCharacter;

        protected InputAction mouseLookInputAction { get; set; }
        protected InputAction controllerLookInputAction { get; set; }
        protected InputAction cursorLockInputAction { get; set; }
        protected InputAction cursorUnlockInputAction { get; set; }

        private void Awake()
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            Vector2 mouseLookInput = mouseLookInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (mouseLookInput.sqrMagnitude > 0.0f)
            {
                mouseLookInput *= mouseSensitivity;
                _playerCharacter.AddControlYawInput(mouseLookInput.x);
                _playerCharacter.AddControlPitchInput(invertLook ? -mouseLookInput.y : mouseLookInput.y);
            }
            else
            {
                Vector2 controllerLookInput = controllerLookInputAction?.ReadValue<Vector2>() ?? Vector2.zero;

                controllerLookInput *= controllerSensitivity;
                _playerCharacter.AddControlYawInput(controllerLookInput.x);
                _playerCharacter.AddControlPitchInput(invertLook ? -controllerLookInput.y : controllerLookInput.y);
            }
        }

        protected void InitPlayerInput()
        {
            if (inputActions == null)
                return;

            mouseLookInputAction = inputActions.FindAction("Mouse Look");
            mouseLookInputAction?.Enable();

            controllerLookInputAction = inputActions.FindAction("Controller Look");
            controllerLookInputAction?.Enable();
        }

        protected void DeinitPlayerInput()
        {
            if (mouseLookInputAction != null)
            {
                mouseLookInputAction.Disable();
                mouseLookInputAction = null;
            }

            if (controllerLookInputAction != null)
            {
                controllerLookInputAction.Disable();
                controllerLookInputAction = null;
            }
        }

        void OnEnable()
        {
            InitPlayerInput();
        }

        void OnDisable()
        {
            DeinitPlayerInput();
        }
    }
}
