using UnityEngine;
using UnityEngine.InputSystem;


namespace Gameplay
{

    public class PlayerMovementInput : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

        protected InputAction movementInputAction { get; set; }
        protected InputAction jumpInputAction { get; set; }

        private PlayerCharacter _playerCharacter;

        private void Awake()
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
        }

        private void Update()
        {
            if (inputActions == null)
                return;

            Vector2 movementInput = GetMovementInput();

            Vector3 movementDirection = Vector3.zero;

            movementDirection += _playerCharacter.GetRightVector() * movementInput.x;
            movementDirection += _playerCharacter.GetForwardVector() * movementInput.y;

            _playerCharacter.SetMovementDirection(movementDirection);
        }

        protected virtual void InitPlayerInput()
        {
            if (inputActions == null)
                return;

            movementInputAction = inputActions.FindAction("Movement");
            movementInputAction?.Enable();

            jumpInputAction = inputActions.FindAction("Jump");
            if (jumpInputAction != null)
            {
                jumpInputAction.started += OnJump;
                jumpInputAction.performed += OnJump;
                jumpInputAction.canceled += OnJump;

                jumpInputAction.Enable();
            }
        }

        protected virtual void DeinitPlayerInput()
        {
            if (movementInputAction != null)
            {
                movementInputAction.Disable();
                movementInputAction = null;
            }

            if (jumpInputAction != null)
            {
                jumpInputAction.started -= OnJump;
                jumpInputAction.performed -= OnJump;
                jumpInputAction.canceled -= OnJump;

                jumpInputAction.Disable();
                jumpInputAction = null;
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

        protected virtual void OnJump(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
                _playerCharacter.Jump();
            else if (context.canceled)
                _playerCharacter.StopJumping();
        }

        protected virtual Vector2 GetMovementInput()
        {
            return movementInputAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }
    }
}
