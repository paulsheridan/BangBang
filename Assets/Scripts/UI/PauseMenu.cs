using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gameplay;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Root GameObject of the menu used to toggle its activation")]
        public GameObject MenuRoot;

        [Tooltip("Master volume when menu is open")]
        [Range(0.001f, 1f)]
        public float VolumeWhenMenuOpen = 0.5f;

        [Tooltip("Slider component for look sensitivity")]
        public Slider LookSensitivitySlider;

        [Tooltip("Toggle component for shadows")]
        public Toggle ShadowsToggle;

        [Tooltip("Toggle component for invincibility")]
        public Toggle InvincibilityToggle;

        [Tooltip("Toggle component for framerate display")]
        public Toggle FramerateToggle;

        // [Tooltip("GameObject for the controls")]
        // public GameObject ControlImage;

        PlayerWeaponInput _inputHandler;
        Health _playerHealth;
        // FramerateCounter _framerateCounter;

        void Start()
        {
            _inputHandler = FindObjectOfType<PlayerWeaponInput>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponInput, PauseMenu>(_inputHandler,
                this);

            _playerHealth = _inputHandler.GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PauseMenu>(_playerHealth, this, gameObject);

            MenuRoot.SetActive(false);

            // LookSensitivitySlider.value = _inputHandler.LookSensitivity;
            // LookSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);

            // ShadowsToggle.isOn = QualitySettings.shadows != ShadowQuality.Disable;
            // ShadowsToggle.onValueChanged.AddListener(OnShadowsChanged);

            // InvincibilityToggle.isOn = _playerHealth.Invincible;
            // InvincibilityToggle.onValueChanged.AddListener(OnInvincibilityChanged);

            // FramerateToggle.isOn = _framerateCounter.UIText.gameObject.activeSelf;
            // FramerateToggle.onValueChanged.AddListener(OnFramerateCounterChanged);
        }

        void Update()
        {
            // Lock cursor when clicking outside of menu
            if (!MenuRoot.activeSelf && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_inputHandler.GetPauseGameInputDown()
                || (MenuRoot.activeSelf && _inputHandler.GetPauseGameInputDown()))
            {
                // if (ControlImage.activeSelf)
                // {
                //     ControlImage.SetActive(false);
                //     return;
                // }

                SetPauseMenuActivation(!MenuRoot.activeSelf);

            }

            // if (Input.GetAxisRaw(GameConstants.k_AxisNameVertical) != 0)
            // {
            //     if (EventSystem.current.currentSelectedGameObject == null)
            //     {
            //         EventSystem.current.SetSelectedGameObject(null);
            //         // LookSensitivitySlider.Select();
            //     }
            // }
        }

        public void ClosePauseMenu()
        {
            SetPauseMenuActivation(false);
        }

        void SetPauseMenuActivation(bool active)
        {
            MenuRoot.SetActive(active);

            if (MenuRoot.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 0f;
                AudioUtility.SetMasterVolume(VolumeWhenMenuOpen);

                EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
                AudioUtility.SetMasterVolume(1);
            }
        }

        public void Home()
        {
            SceneManager.LoadScene("MainMenu");
            Time.timeScale = 1;
        }

        public void Resume()
        {
            MenuRoot.SetActive(false);
            Time.timeScale = 1;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Time.timeScale = 1;
        }

        void OnMouseSensitivityChanged(float newValue)
        {
            Debug.Log("Changed Sensitivity to " + newValue);
        }

        void OnShadowsChanged(bool newValue)
        {
            Debug.Log("Changed Shadows to " + newValue);
        }

        void OnInvincibilityChanged(bool newValue)
        {
            _playerHealth.Invincible = newValue;
        }

        void OnFramerateCounterChanged(bool newValue)
        {
            Debug.Log("Changed Framerate Counter to " + newValue);
        }

        public void OnShowControlButtonClicked(bool show)
        {
            Debug.Log("Show Controls Image");
        }
    }
}
