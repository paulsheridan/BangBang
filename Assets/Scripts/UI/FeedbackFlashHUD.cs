using UnityEngine;
using UnityEngine.UI;
using EasyCharacterMovement;
using Gameplay;

namespace UI
{
    public class FeedbackFlashHUD : MonoBehaviour
    {
        [Header("References")] [Tooltip("Image component of the flash")]
        public Image FlashImage;

        [Tooltip("CanvasGroup to fade the damage flash, used when recieving damage end healing")]
        public CanvasGroup FlashCanvasGroup;

        [Tooltip("CanvasGroup to fade the critical health vignette")]
        public CanvasGroup VignetteCanvasGroup;

        [Header("Damage")] [Tooltip("Color of the damage flash")]
        public Color DamageFlashColor;

        [Tooltip("Duration of the damage flash")]
        public float DamageFlashDuration;

        [Tooltip("Max alpha of the damage flash")]
        public float DamageFlashMaxAlpha = 1f;

        [Header("Critical health")] [Tooltip("Max alpha of the critical vignette")]
        public float CriticaHealthVignetteMaxAlpha = .8f;

        [Tooltip("Frequency at which the vignette will pulse when at critical health")]
        public float PulsatingVignetteFrequency = 4f;

        [Header("Heal")] [Tooltip("Color of the heal flash")]
        public Color HealFlashColor;

        [Tooltip("Duration of the heal flash")]
        public float HealFlashDuration;

        [Tooltip("Max alpha of the heal flash")]
        public float HealFlashMaxAlpha = 1f;

        bool _flashActive;
        float _lastTimeFlashStarted = Mathf.NegativeInfinity;
        Health _playerHealth;
        GameFlowManager _gameFlowManager;

        void Start()
        {
            // Subscribe to player damage events
            PlayerCharacter playerCharacter = FindObjectOfType<PlayerCharacter>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerCharacter, FeedbackFlashHUD>(
                playerCharacter, this);

            _playerHealth = playerCharacter.GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, FeedbackFlashHUD>(_playerHealth, this,
                playerCharacter.gameObject);

            _gameFlowManager = FindObjectOfType<GameFlowManager>();
            DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, FeedbackFlashHUD>(_gameFlowManager, this);

            _playerHealth.OnDamaged += OnTakeDamage;
            _playerHealth.OnHealed += OnHealed;
        }

        void Update()
        {
            if (_playerHealth.IsCritical())
            {
                VignetteCanvasGroup.gameObject.SetActive(true);
                float vignetteAlpha =
                    (1 - (_playerHealth.CurrentHealth / _playerHealth.MaxHealth /
                          _playerHealth.CriticalHealthRatio)) * CriticaHealthVignetteMaxAlpha;

                if (_gameFlowManager.GameIsEnding)
                    VignetteCanvasGroup.alpha = vignetteAlpha;
                else
                    VignetteCanvasGroup.alpha =
                        ((Mathf.Sin(Time.time * PulsatingVignetteFrequency) / 2) + 0.5f) * vignetteAlpha;
            }
            else
            {
                VignetteCanvasGroup.gameObject.SetActive(false);
            }


            if (_flashActive)
            {
                float normalizedTimeSinceDamage = (Time.time - _lastTimeFlashStarted) / DamageFlashDuration;

                if (normalizedTimeSinceDamage < 1f)
                {
                    float flashAmount = DamageFlashMaxAlpha * (1f - normalizedTimeSinceDamage);
                    FlashCanvasGroup.alpha = flashAmount;
                }
                else
                {
                    FlashCanvasGroup.gameObject.SetActive(false);
                    _flashActive = false;
                }
            }
        }

        void ResetFlash()
        {
            _lastTimeFlashStarted = Time.time;
            _flashActive = true;
            FlashCanvasGroup.alpha = 0f;
            FlashCanvasGroup.gameObject.SetActive(true);
        }

        void OnTakeDamage(float dmg, GameObject damageSource)
        {
            ResetFlash();
            FlashImage.color = DamageFlashColor;
        }

        void OnHealed(float amount)
        {
            ResetFlash();
            FlashImage.color = HealFlashColor;
        }
    }
}
