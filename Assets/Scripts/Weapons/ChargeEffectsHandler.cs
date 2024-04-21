using UnityEngine;

namespace Weapons
{
    [RequireComponent(typeof(AudioSource))]
    public class ChargeEffectsHandler : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Object that will be affected by charging scale & color changes")]
        public GameObject ChargingObject;

        [Tooltip("Scale of the charged object based on charge")]
        public MinMaxVector3 Scale;

        [Header("Particles")]
        [Tooltip("Particles to create when charging")]
        public GameObject ParticlePrefab;

        [Tooltip("Local position offset of the charge particles (relative to this transform)")]
        public Vector3 Offset;

        [Tooltip("Parent transform for the particles (Optional)")]
        public Transform ParentTransform;

        [Tooltip("Radius of the charge particles based on charge")]
        public MinMaxVector3 Radius;

        [Header("Sound")] [Tooltip("Audio clip for charge SFX")]
        public AudioClip ChargeSound;

        [Tooltip("Sound played in loop after the change is full for this weapon")]
        public AudioClip LoopChargeWeaponSfx;

        [Tooltip("Duration of the cross fade between the charge and the loop sound")]
        public float FadeLoopDuration = 0.5f;

        [Tooltip("The emission rate for the effect when fully charged")]
        public float ChargeParticlesEmissionRateMax = 8f;

        [Tooltip(
            "If true, the ChargeSound will be ignored and the pitch on the LoopSound will be procedural, based on the charge amount")]
        public bool UseProceduralPitchOnLoopSfx;

        [Range(1.0f, 5.0f), Tooltip("Maximum procedural Pitch value")]
        public float MaxProceduralPitchValue = 2.0f;

        public GameObject ParticleInstance { get; set; }

        ParticleSystem _chargeParticles;
        WeaponController _weaponController;
        ParticleSystem.VelocityOverLifetimeModule _velocityOverTimeModule;
        ParticleSystem.EmissionModule _chargeParticlesEmissionModule;

        AudioSource _audioSource;
        AudioSource _audioSourceLoop;

        float m_LastChargeTriggerTimestamp;
        float m_ChargeRatio;
        float m_EndchargeTime;

        void Awake()
        {
            m_LastChargeTriggerTimestamp = 0.0f;

            _chargeParticlesEmissionModule = _chargeParticles.emission;
            _chargeParticlesEmissionModule.rateOverTimeMultiplier = 0f;

            // The charge effect needs it's own AudioSources, since it will play on top of the other gun sounds
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = ChargeSound;
            _audioSource.playOnAwake = false;
            _audioSource.outputAudioMixerGroup =
                AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponChargeBuildup);

            // create a second audio source, to play the sound with a delay
            _audioSourceLoop = gameObject.AddComponent<AudioSource>();
            _audioSourceLoop.clip = LoopChargeWeaponSfx;
            _audioSourceLoop.playOnAwake = false;
            _audioSourceLoop.loop = true;
            _audioSourceLoop.outputAudioMixerGroup =
                AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponChargeLoop);
        }

        void SpawnParticleSystem()
        {
            ParticleInstance = Instantiate(ParticlePrefab,
                ParentTransform != null ? ParentTransform : transform);
            ParticleInstance.transform.localPosition += Offset;

            FindReferences();
        }

        public void FindReferences()
        {
            _chargeParticles = ParticleInstance.GetComponent<ParticleSystem>();
            DebugUtility.HandleErrorIfNullGetComponent<ParticleSystem, ChargeEffectsHandler>(_chargeParticles,
                this, ParticleInstance.gameObject);

            _weaponController = GetComponent<WeaponController>();
            DebugUtility.HandleErrorIfNullGetComponent<WeaponController, ChargeEffectsHandler>(
                _weaponController, this, gameObject);

            _velocityOverTimeModule = _chargeParticles.velocityOverLifetime;
        }

        void Update()
        {
            float currentCharge = _weaponController.CurrentCharge;

            if (ParticleInstance == null)
                SpawnParticleSystem();

            _chargeParticles.gameObject.SetActive(_weaponController.IsWeaponActive);
            m_ChargeRatio = _weaponController.CurrentCharge;

            ChargingObject.transform.localScale = Scale.GetValueFromRatio(m_ChargeRatio);

            _chargeParticles.transform.localScale = Radius.GetValueFromRatio(m_ChargeRatio * 1.1f);

            _chargeParticlesEmissionModule.rateOverTimeMultiplier = ChargeParticlesEmissionRateMax * (1f - currentCharge);

            // update sound's volume and pitch
            if (m_ChargeRatio > 0)
            {
                if (!_audioSourceLoop.isPlaying &&
                    _weaponController.LastChargeTriggerTimestamp > m_LastChargeTriggerTimestamp)
                {
                    m_LastChargeTriggerTimestamp = _weaponController.LastChargeTriggerTimestamp;
                    if (!UseProceduralPitchOnLoopSfx)
                    {
                        m_EndchargeTime = Time.time + ChargeSound.length;
                        _audioSource.Play();
                    }

                    _audioSourceLoop.Play();
                }

                if (!UseProceduralPitchOnLoopSfx)
                {
                    float volumeRatio =
                        Mathf.Clamp01((m_EndchargeTime - Time.time - FadeLoopDuration) / FadeLoopDuration);
                    _audioSource.volume = volumeRatio;
                    _audioSourceLoop.volume = 1 - volumeRatio;
                }
                else
                {
                    _audioSourceLoop.pitch = Mathf.Lerp(1.0f, MaxProceduralPitchValue, m_ChargeRatio);
                }
            }
            else
            {
                _audioSource.Stop();
                _audioSourceLoop.Stop();
            }
        }
    }
}
