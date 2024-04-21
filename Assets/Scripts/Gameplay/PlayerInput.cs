using UnityEngine;
using UnityEngine.InputSystem;


namespace Gameplay
{

    public class PlayerInput : MonoBehaviour
    {

        [Tooltip("Input actions associated with this Character." +
                 " If not assigned, this Character wont process any input so you can externally take control of this Character (e.g. a Controller).")]
        [SerializeField]
        private InputActionAsset inputActions;

        protected InputAction movementInputAction { get; set; }
        protected InputAction jumpInputAction { get; set; }

        private PlayerCharacter _playerCharacter;

        protected virtual void InitPlayerInput()
        {
            // Attempts to cache Character InputActions (if any)

            if (inputActions == null)
                return;

            // Movement input action (no handler, this is polled, e.g. GetMovementInput())

            movementInputAction = inputActions.FindAction("Movement");
            movementInputAction?.Enable();

            // Setup Jump input action handlers

            jumpInputAction = inputActions.FindAction("Jump");
            if (jumpInputAction != null)
            {
                jumpInputAction.started += OnJump;
                jumpInputAction.performed += OnJump;
                jumpInputAction.canceled += OnJump;

                jumpInputAction.Enable();
            }
        }

        /// <summary>
        /// Unsubscribe from input action events and disable input actions.
        /// </summary>

        protected virtual void DeinitPlayerInput()
        {
            // Unsubscribe from input action events and disable input actions

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

        /// <summary>
        /// Handle Player input, only if actions are assigned (eg: actions != null).
        /// </summary>

        protected virtual void HandleInput()
        {
            // Should this character handle input ?

            if (inputActions == null)
                return;

            // Poll movement InputAction

            Vector2 movementInput = GetMovementInput();

            Vector3 movementDirection = Vector3.zero;

            movementDirection += _playerCharacter.GetRightVector() * movementInput.x;
            movementDirection += _playerCharacter.GetForwardVector() * movementInput.y;

            _playerCharacter.SetMovementDirection(movementDirection);

            // if (GetComponent<Camera>())
            // {
            //     // If Camera is assigned, add input movement relative to camera look direction

            //     Vector3 movementDirection = Vector3.zero;

            //     movementDirection += Vector3.right * movementInput.x;
            //     movementDirection += Vector3.forward * movementInput.y;

            //     movementDirection = movementDirection.relativeTo(_playerCharacter.cameraTransform);

            //     _playerCharacter.SetMovementDirection(movementDirection);

            // }
            // else
            // {
            //     // If Camera is not assigned, add movement input relative to world axis

            //     Vector3 movementDirection = Vector3.zero;

            //     movementDirection += Vector3.right * movementInput.x;
            //     movementDirection += Vector3.forward * movementInput.y;

            //     _playerCharacter.SetMovementDirection(movementDirection);
            // }
        }

        void OnEnable()
        {
            InitPlayerInput();
        }

        void OnDisable()
        {
            DeinitPlayerInput();
        }

        private void Awake()
        {
            _playerCharacter = GetComponent<PlayerCharacter>();
        }

        private void Update()
        {
            // Movement input, relative to character's view direction
            HandleInput();

            // Vector2 inputMove = new Vector2()
            // {
            //     x = Input.GetAxisRaw("Horizontal"),
            //     y = Input.GetAxisRaw("Vertical")
            // };

            // Vector3 movementDirection = Vector3.zero;

            // movementDirection += _playerCharacter.GetRightVector() * inputMove.x;
            // movementDirection += _playerCharacter.GetForwardVector() * inputMove.y;

            // _playerCharacter.SetMovementDirection(movementDirection);

            // // Crouch input

            // if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            //     _playerCharacter.Crouch();
            // else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.C))
            //     _playerCharacter.UnCrouch();

            // // Jump input

            // if (Input.GetButtonDown("Jump"))
            //     _playerCharacter.Jump();
            // else if (Input.GetButtonUp("Jump"))
            //     _playerCharacter.StopJumping();
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
