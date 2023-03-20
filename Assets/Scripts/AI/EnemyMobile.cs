using UnityEngine;

namespace AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyMobile : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
        }

        public Animator Animator;

        [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 0.5f;

        [Tooltip("The random hit damage effects")]
        public ParticleSystem[] RandomHitSparks;

        public ParticleSystem[] OnDetectVfx;
        public AudioClip OnDetectSfx;

        [Header("Sound")] public AudioClip MovementSound;
        public MinMaxFloat PitchDistortionMovementSpeed;

        public AIState AiState { get; private set; }
        EnemyController _enemyController;
        AudioSource _audioSource;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        void Start()
        {
            _enemyController = GetComponent<EnemyController>();
            DebugUtility.HandleErrorIfNullGetComponent<EnemyController, EnemyMobile>(_enemyController, this,
                gameObject);

            _enemyController.onAttack += OnAttack;
            _enemyController.onDetectedTarget += OnDetectedTarget;
            _enemyController.onLostTarget += OnLostTarget;
            _enemyController.SetPathDestinationToClosestNode();
            _enemyController.onDamaged += OnDamaged;

            // Start patrolling
            AiState = AIState.Patrol;

            // adding a audio source to play the movement sound on it
            _audioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(_audioSource, this, gameObject);
            _audioSource.clip = MovementSound;
            _audioSource.Play();
        }

        void Update()
        {
            UpdateAiStateTransitions();
            UpdateCurrentAiState();

            float moveSpeed = _enemyController.NavMeshAgent.velocity.magnitude;

            // Update animator speed parameter
            // Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

            // changing the pitch of the movement sound depending on the movement speed
            // _audioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
            //     moveSpeed / _enemyController.NavMeshAgent.speed);
        }

        void UpdateAiStateTransitions()
        {
            // Handle transitions
            switch (AiState)
            {
                case AIState.Follow:
                    // Transition to attack when there is a line of sight to the target
                    if (_enemyController.IsSeeingTarget && _enemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        _enemyController.SetNavDestination(transform.position);
                    }

                    break;
                case AIState.Attack:
                    // Transition to follow when no longer a target in attack range
                    if (!_enemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Follow;
                    }

                    break;
            }
        }

        void UpdateCurrentAiState()
        {
            // Handle logic
            switch (AiState)
            {
                case AIState.Patrol:
                    _enemyController.UpdatePathDestination();
                    _enemyController.SetNavDestination(_enemyController.GetDestinationOnPath());
                    break;
                case AIState.Follow:
                    _enemyController.SetNavDestination(_enemyController.KnownDetectedTarget.transform.position);
                    _enemyController.OrientTowards(_enemyController.KnownDetectedTarget.transform.position);
                    _enemyController.OrientWeaponsTowards(_enemyController.KnownDetectedTarget.transform.position);
                    break;
                case AIState.Attack:
                    if (Vector3.Distance(_enemyController.KnownDetectedTarget.transform.position,
                            _enemyController.DetectionModule.DetectionSourcePoint.position)
                        >= (AttackStopDistanceRatio * _enemyController.DetectionModule.AttackRange))
                    {
                        _enemyController.SetNavDestination(_enemyController.KnownDetectedTarget.transform.position);
                    }
                    else
                    {
                        _enemyController.SetNavDestination(transform.position);
                    }

                    _enemyController.OrientTowards(_enemyController.KnownDetectedTarget.transform.position);
                    _enemyController.TryAtack(_enemyController.KnownDetectedTarget.transform.position);
                    break;
            }
        }

        void OnAttack()
        {
            // Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnDetectedTarget()
        {
            if (AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Play();
            }

            if (OnDetectSfx)
            {
                AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
            }

            // Animator.SetBool(k_AnimAlertedParameter, true);
        }

        void OnLostTarget()
        {
            if (AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                OnDetectVfx[i].Stop();
            }

            // Animator.SetBool(k_AnimAlertedParameter, false);
        }

        void OnDamaged()
        {
            if (RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length - 1);
                RandomHitSparks[n].Play();
            }

            // Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }
}
