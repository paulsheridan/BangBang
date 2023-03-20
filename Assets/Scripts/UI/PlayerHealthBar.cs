using UnityEngine;
using UnityEngine.UI;
using EasyCharacterMovement;
using Gameplay;

namespace UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Image component dispplaying current health")]
        public Image HealthFillImage;

        Health _playerHealth;

        void Start()
        {
            PlayerCharacter playerCharacter =
                GameObject.FindObjectOfType<PlayerCharacter>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerCharacter, PlayerHealthBar>(
                playerCharacter, this);

            _playerHealth = playerCharacter.GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerHealthBar>(_playerHealth, this,
                playerCharacter.gameObject);
        }

        void Update()
        {
            // update health bar value
            HealthFillImage.fillAmount = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
        }
    }
}
