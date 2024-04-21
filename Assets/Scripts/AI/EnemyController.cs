using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Gameplay;
using Weapons;

namespace AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;

        [Tooltip("Parent transform where all weapon will be added in the hierarchy")]
        public Transform WeaponParentSocket;

        [Header("Eye color")]
        [Tooltip("Material for the eye color")]
        public Material EyeColorMaterial;

        [Tooltip("The default color of the bot's eye")]
        [ColorUsageAttribute(true, true)]
        public Color DefaultEyeColor;

        [Tooltip("The attack color of the bot's eye")]
        [ColorUsageAttribute(true, true)]
        public Color AttackEyeColor;

        [Header("Flash on hit")]
        [Tooltip("The material used for the body of the hoverbot")]
        public Material BodyMaterial;

        [Tooltip("The gradient representing the color of the flash on hit")]
        [GradientUsageAttribute(true)]
        public Gradient OnHitBodyGradient;

        [Tooltip("The duration of the flash on hit")]
        public float FlashOnHitDuration = 0.5f;

        [Header("Sounds")]
        [Tooltip("Sound played when recieving damages")]
        public AudioClip DamageTick;

        [Header("VFX")]
        [Tooltip("The VFX prefab spawned when the enemy dies")]
        public GameObject DeathVfx;

        [Tooltip("The point at which the death VFX is spawned")]
        public Transform DeathVfxSpawnPoint;

        [Header("Loot")]
        [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")]
        [Range(0, 1)]
        public float DropRate = 1f;

        [Header("Debug Display")]
        [Tooltip("Color of the sphere gizmo representing the path reaching range")]
        public Color PathReachingRangeColor = Color.yellow;

        [Tooltip("Color of the sphere gizmo representing the attack range")]
        public Color AttackRangeColor = Color.red;

        [Tooltip("Color of the sphere gizmo representing the detection range")]
        public Color DetectionRangeColor = Color.blue;

        [Header("Combat Behavior")]
        [Tooltip("The time delay between two attacks")]
        [Range(0, 30)]
        public float CoolDownTime = 5;

        public UnityAction onAttack;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;
        public UnityAction reachedAttackLoc;

        List<RendererIndexData> _bodyRenderers = new List<RendererIndexData>();
        MaterialPropertyBlock _bodyFlashMaterialPropertyBlock;
        float _lastTimeDamaged = float.NegativeInfinity;

        RendererIndexData _eyeRendererData;
        MaterialPropertyBlock _eyeColorMaterialPropertyBlock;

        public PatrolPath PatrolPath { get; set; }
        public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }

        int _pathDestinationNodeIndex;
        ActorsManager _actorsManager;
        Health _health;
        Actor _actor;
        Collider[] _selfColliders;
        GameFlowManager _gameFlowManager;
        bool _wasDamagedThisFrame;
        WeaponController _weapon;
        NavigationModule _navigationModule;

        float _timeRemaining;
        bool _canAttack;

        void Start()
        {
            _actorsManager = FindObjectOfType<ActorsManager>();
            DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(_actorsManager, this);

            _health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(_health, this, gameObject);

            _actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(_actor, this, gameObject);

            NavMeshAgent = GetComponent<NavMeshAgent>();
            _selfColliders = GetComponentsInChildren<Collider>();

            _gameFlowManager = FindObjectOfType<GameFlowManager>();
            DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyController>(_gameFlowManager, this);

            // Subscribe to damage & death actions
            _health.OnDie += OnDie;
            _health.OnDamaged += OnDamaged;

            GetCurrentWeapon();

            var detectionModules = GetComponentsInChildren<DetectionModule>();
            DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this,
                gameObject);
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,
                this, gameObject);
            // Initialize detection module
            DetectionModule = detectionModules[0];
            DetectionModule.onDetectedTarget += OnDetectedTarget;
            DetectionModule.onLostTarget += OnLostTarget;
            onAttack += DetectionModule.OnAttack;

            var navigationModules = GetComponentsInChildren<NavigationModule>();
            DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,
                this, gameObject);
            // Override navmesh agent data
            if (navigationModules.Length > 0)
            {
                _navigationModule = navigationModules[0];
                NavMeshAgent.speed = _navigationModule.MoveSpeed;
                NavMeshAgent.angularSpeed = _navigationModule.AngularSpeed;
                NavMeshAgent.acceleration = _navigationModule.Acceleration;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == EyeColorMaterial)
                    {
                        _eyeRendererData = new RendererIndexData(renderer, i);
                    }

                    if (renderer.sharedMaterials[i] == BodyMaterial)
                    {
                        _bodyRenderers.Add(new RendererIndexData(renderer, i));
                    }
                }
            }

            _bodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

            // Check if we have an eye renderer for this enemy
            if (_eyeRendererData.Renderer != null)
            {
                _eyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
                _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
                    _eyeRendererData.MaterialIndex);
            }
        }

        void Update()
        {
            EnsureIsWithinLevelBounds();

            DetectionModule.HandleTargetDetection(_actor, _selfColliders);

            Color currentColor = OnHitBodyGradient.Evaluate((Time.time - _lastTimeDamaged) / FlashOnHitDuration);
            _bodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
            foreach (var data in _bodyRenderers)
            {
                data.Renderer.SetPropertyBlock(_bodyFlashMaterialPropertyBlock, data.MaterialIndex);
            }

            _wasDamagedThisFrame = false;
        }

        void EnsureIsWithinLevelBounds()
        {
            // at every frame, this tests for conditions to kill the enemy
            if (transform.position.y < SelfDestructYHeight)
            {
                // Destroy(gameObject);
                TriggerRespawn();
                return;
            }
        }

        void OnLostTarget()
        {
            onLostTarget?.Invoke();

            // Set the eye attack color and property block if the eye renderer is set
            if (_eyeRendererData.Renderer != null)
            {
                _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
                    _eyeRendererData.MaterialIndex);
            }
        }

        void OnDetectedTarget()
        {
            onDetectedTarget?.Invoke();

            // Set the eye default color and property block if the eye renderer is set
            if (_eyeRendererData.Renderer != null)
            {
                _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
                _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
                    _eyeRendererData.MaterialIndex);
            }
        }

        public void OrientTowards(Vector3 lookPosition)
        {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        bool IsPathValid()
        {
            return PatrolPath && PatrolPath.PathNodes.Count > 0;
        }

        public void ResetPathDestination()
        {
            _pathDestinationNodeIndex = 0;
        }

        public void SetPathDestinationToClosestNode()
        {
            if (IsPathValid())
            {
                int closestPathNodeIndex = 0;
                for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
                {
                    float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                    if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                    {
                        closestPathNodeIndex = i;
                    }
                }

                _pathDestinationNodeIndex = closestPathNodeIndex;
            }
            else
            {
                _pathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            if (IsPathValid())
            {
                return PatrolPath.GetPositionOfPathNode(_pathDestinationNodeIndex);
            }
            else
            {
                return transform.position;
            }
        }

        public void SetNavDestination(Vector3 destination)
        {
            if (NavMeshAgent)
            {
                NavMeshAgent.SetDestination(destination);
            }
        }

        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathValid())
            {
                // Check if reached the path destination
                if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
                {
                    // increment path destination index
                    _pathDestinationNodeIndex =
                        inverseOrder ? (_pathDestinationNodeIndex - 1) : (_pathDestinationNodeIndex + 1);
                    if (_pathDestinationNodeIndex < 0)
                    {
                        _pathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                    }

                    if (_pathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
                    {
                        _pathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                    }
                }
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // test if the damage source is the player
            if (damageSource && !damageSource.GetComponent<EnemyController>())
            {
                // pursue the damage source
                DetectionModule.OnDamaged(damageSource);
                onDamaged?.Invoke();
                _lastTimeDamaged = Time.time;

                // play the damage tick sound
                if (DamageTick && !_wasDamagedThisFrame)
                    AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

                _wasDamagedThisFrame = true;
            }
        }

        void OnDie()
        {
            // spawn a particle system when dying
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);

            // loot an object
            if (TryDropItem())
            {
                Instantiate(LootPrefab, transform.position, Quaternion.identity);
            }

            TriggerRespawn();
        }

        void TriggerRespawn()
        {
            _actorsManager.RespawnActor(this._actor);
            _health.ResetHealth();
        }

        void OnDrawGizmosSelected()
        {
            // Path reaching range
            Gizmos.color = PathReachingRangeColor;
            Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

            if (DetectionModule != null)
            {
                // Detection range
                Gizmos.color = DetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

                // Attack range
                Gizmos.color = AttackRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
            }
        }

        public void OrientWeaponsTowards(Vector3 lookPosition)
        {
            Vector3 weaponForward = (lookPosition - _weapon.WeaponRoot.transform.position).normalized;
            _weapon.transform.forward = weaponForward;
        }

        public bool TryAttack(Vector3 enemyPosition)
        {
            if (_gameFlowManager.GameIsEnding)
                return false;

            OrientWeaponsTowards(enemyPosition);

            bool didFire = false;

            if (_canAttack)
            {
                // Shoot the weapon
                didFire = _weapon.HandleShootInputs(false, true, false);

                if (didFire && onAttack != null)
                {
                    onAttack?.Invoke();
                }
            }
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0)
            {
                _canAttack = !_canAttack;
                _timeRemaining = CoolDownTime;
            }
            return didFire;
        }

        public bool TryDropItem()
        {
            if (DropRate == 0 || LootPrefab == null)
                return false;
            else if (DropRate == 1)
                return true;
            else
                return (Random.value <= DropRate);
        }

        public WeaponController GetCurrentWeapon()
        {
            // Check if we already found and initialized the weapons
            if (_weapon == null)
            {
                _weapon = GetComponentInChildren<WeaponController>();
            }
            _weapon.Owner = gameObject;
            return _weapon;
        }
    }
}
