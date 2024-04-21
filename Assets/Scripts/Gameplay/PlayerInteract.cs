using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using ECM2;

namespace Gameplay
{
    public class PlayerInteract : MonoBehaviour
    {
        private new Camera camera;
        [SerializeField]
        private float distance = 3f;

        [SerializeField]
        private LayerMask mask;
        public UnityAction<string> UpdateText;

        [SerializeField]
        public InputActionAsset inputActions;

        private InputAction interactInputAction { get; set; }

        private bool _isInteracting = false;

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
                Interact();
            else if (context.canceled)
                StopInteracting();
        }

        void Start()
        {
            camera = GetComponent<PlayerCharacter>().camera;
        }

        void Update()
        {
            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, distance, mask))
            {
                if (hitInfo.collider.gameObject.GetComponent<Interactable>() != null)
                {
                    Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
                    UpdateText?.Invoke(interactable.promptMessage);
                    if (_isInteracting)
                    {
                        interactable.BaseInteract();
                    }
                }
            } else
            {
                UpdateText?.Invoke("");
            }
        }

        public void Interact()
        {
            _isInteracting = true;
        }

        public void StopInteracting()
        {
            _isInteracting = false;
        }

        void OnEnable()
        {
            InitPlayerInput();
        }

        void OnDisable()
        {
            DeinitPlayerInput();
        }

        protected void InitPlayerInput()
        {
            interactInputAction = inputActions.FindAction("Interact");
            if (interactInputAction != null)
            {
                interactInputAction.started += OnInteract;
                interactInputAction.canceled += OnInteract;

                interactInputAction.Enable();
            }
        }

        protected void DeinitPlayerInput()
        {
            if (interactInputAction != null)
            {
                interactInputAction.started -= OnInteract;
                interactInputAction.canceled -= OnInteract;

                interactInputAction.Disable();
                interactInputAction = null;
            }
        }
    }
}
