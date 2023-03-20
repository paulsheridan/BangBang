// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AI;
// using UnityEngine.InputSystem;
// using UnityEngine.Events;

// using EasyCharacterMovement;

// namespace AI
// {
//     public class EnemyAgent : AgentCharacter
//     {
//         [System.Serializable]
//         public struct RendererIndexData
//         {
//             public Renderer Renderer;
//             public int MaterialIndex;

//             public RendererIndexData(Renderer renderer, int index)
//             {
//                 Renderer = renderer;
//                 MaterialIndex = index;
//             }
//         }

//         [Header("Parameters")]
//         [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
//         public float SelfDestructYHeight = -20f;

//         [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
//         public float PathReachingRadius = 2f;

//         [Tooltip("The speed at which the enemy rotates")]
//         public float OrientationSpeed = 10f;

//         [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
//         public float DeathDuration = 0f;


//         [Header("Weapons Parameters")]
//         [Tooltip("Allow weapon swapping for this enemy")]
//         public bool SwapToNextWeapon = false;

//         [Tooltip("Time delay between a weapon swap and the next attack")]
//         public float DelayAfterWeaponSwap = 0f;

//         [Header("Eye color")]
//         [Tooltip("Material for the eye color")]
//         public Material EyeColorMaterial;

//         [Tooltip("The default color of the bot's eye")]
//         [ColorUsageAttribute(true, true)]
//         public Color DefaultEyeColor;

//         [Tooltip("The attack color of the bot's eye")]
//         [ColorUsageAttribute(true, true)]
//         public Color AttackEyeColor;

//         [Header("Flash on hit")]
//         [Tooltip("The material used for the body of the hoverbot")]
//         public Material BodyMaterial;

//         [Tooltip("The gradient representing the color of the flash on hit")]
//         [GradientUsageAttribute(true)]
//         public Gradient OnHitBodyGradient;

//         [Tooltip("The duration of the flash on hit")]
//         public float FlashOnHitDuration = 0.5f;

//         [Header("Sounds")]
//         [Tooltip("Sound played when recieving damages")]
//         public AudioClip DamageTick;

//         [Header("VFX")]
//         [Tooltip("The VFX prefab spawned when the enemy dies")]
//         public GameObject DeathVfx;

//         [Tooltip("The point at which the death VFX is spawned")]
//         public Transform DeathVfxSpawnPoint;

//         [Header("Loot")]
//         [Tooltip("The object this enemy can drop when dying")]
//         public GameObject LootPrefab;

//         [Tooltip("The chance the object has to drop")]
//         [Range(0, 1)]
//         public float DropRate = 1f;

//         [Header("Debug Display")]
//         [Tooltip("Color of the sphere gizmo representing the path reaching range")]
//         public Color PathReachingRangeColor = Color.yellow;

//         [Tooltip("Color of the sphere gizmo representing the attack range")]
//         public Color AttackRangeColor = Color.red;

//         [Tooltip("Color of the sphere gizmo representing the detection range")]
//         public Color DetectionRangeColor = Color.blue;

//         public UnityAction onAttack;
//         public UnityAction onDetectedTarget;
//         public UnityAction onLostTarget;
//         public UnityAction onDamaged;

//         List<RendererIndexData> _bodyRenderers = new List<RendererIndexData>();
//         MaterialPropertyBlock _bodyFlashMaterialPropertyBlock;
//         float _lastTimeDamaged = float.NegativeInfinity;

//         RendererIndexData _eyeRendererData;
//         MaterialPropertyBlock _eyeColorMaterialPropertyBlock;

//         public PatrolPath PatrolPath { get; set; }
//         public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
//         public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
//         public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
//         public bool HadKnownTarget => DetectionModule.HadKnownTarget;
//         public UnityEngine.AI.NavMeshAgent NavMeshAgent { get; private set; }
//         public DetectionModule DetectionModule { get; private set; }

//         int _pathDestinationNodeIndex;
//         EnemyManager _enemyManager;
//         ActorsManager _actorsManager;
//         Health _health;
//         Actor _actor;
//         Collider[] _selfColliders;
//         GameFlowManager _gameFlowManager;
//         bool _wasDamagedThisFrame;
//         float _lastTimeWeaponSwapped = Mathf.NegativeInfinity;
//         int _currentWeaponIndex;
//         WeaponController _currentWeapon;
//         WeaponController[] _weapons;
//         NavigationModule _navigationModule;

//         protected override void Start()
//         {
//             base.Start();
//             _enemyManager = FindObjectOfType<EnemyManager>();
//             DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyAgent>(_enemyManager, this);

//             _actorsManager = FindObjectOfType<ActorsManager>();
//             DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyAgent>(_actorsManager, this);

//             _enemyManager.RegisterEnemy(this);

//             _health = GetComponent<Health>();
//             DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyAgent>(_health, this, gameObject);

//             _actor = GetComponent<Actor>();
//             DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyAgent>(_actor, this, gameObject);

//             NavMeshAgent = GetComponent<NavMeshAgent>();
//             _selfColliders = GetComponentsInChildren<Collider>();

//             _gameFlowManager = FindObjectOfType<GameFlowManager>();
//             DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyAgent>(_gameFlowManager, this);

//             // Subscribe to damage & death actions
//             _health.OnDie += OnDie;
//             _health.OnDamaged += OnDamaged;

//             // Find and initialize all weapons
//             FindAndInitializeAllWeapons();
//             var weapon = GetCurrentWeapon();
//             weapon.ShowWeapon(true);

//             var detectionModules = GetComponentsInChildren<DetectionModule>();
//             DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyAgent>(detectionModules.Length, this,
//                 gameObject);
//             DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyAgent>(detectionModules.Length,
//                 this, gameObject);
//             // Initialize detection module
//             DetectionModule = detectionModules[0];
//             DetectionModule.onDetectedTarget += OnDetectedTarget;
//             DetectionModule.onLostTarget += OnLostTarget;
//             onAttack += DetectionModule.OnAttack;

//             var navigationModules = GetComponentsInChildren<NavigationModule>();
//             DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyAgent>(detectionModules.Length,
//                 this, gameObject);
//             // Override navmesh agent data
//             if (navigationModules.Length > 0)
//             {
//                 _navigationModule = navigationModules[0];
//                 NavMeshAgent.speed = _navigationModule.MoveSpeed;
//                 NavMeshAgent.angularSpeed = _navigationModule.AngularSpeed;
//                 NavMeshAgent.acceleration = _navigationModule.Acceleration;
//             }

//             foreach (var renderer in GetComponentsInChildren<Renderer>(true))
//             {
//                 for (int i = 0; i < renderer.sharedMaterials.Length; i++)
//                 {
//                     if (renderer.sharedMaterials[i] == EyeColorMaterial)
//                     {
//                         _eyeRendererData = new RendererIndexData(renderer, i);
//                     }

//                     if (renderer.sharedMaterials[i] == BodyMaterial)
//                     {
//                         _bodyRenderers.Add(new RendererIndexData(renderer, i));
//                     }
//                 }
//             }

//             _bodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

//             // Check if we have an eye renderer for this enemy
//             if (_eyeRendererData.Renderer != null)
//             {
//                 _eyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
//                 _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
//                 _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
//                     _eyeRendererData.MaterialIndex);
//             }
//         }

//         protected override void Update()
//         {
//             base.Update();
//             EnsureIsWithinLevelBounds();

//             DetectionModule.HandleTargetDetection(_actor, _selfColliders);

//             Color currentColor = OnHitBodyGradient.Evaluate((Time.time - _lastTimeDamaged) / FlashOnHitDuration);
//             _bodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
//             foreach (var data in _bodyRenderers)
//             {
//                 data.Renderer.SetPropertyBlock(_bodyFlashMaterialPropertyBlock, data.MaterialIndex);
//             }

//             _wasDamagedThisFrame = false;
//         }

//         void EnsureIsWithinLevelBounds()
//         {
//             // at every frame, this tests for conditions to kill the enemy
//             if (transform.position.y < SelfDestructYHeight)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//         }

//         void OnLostTarget()
//         {
//             onLostTarget.Invoke();

//             // Set the eye attack color and property block if the eye renderer is set
//             if (_eyeRendererData.Renderer != null)
//             {
//                 _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
//                 _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
//                     _eyeRendererData.MaterialIndex);
//             }
//         }

//         void OnDetectedTarget()
//         {
//             // THIS IS WHERE IT'S FAILING
//             onDetectedTarget.Invoke();

//             // Set the eye default color and property block if the eye renderer is set
//             if (_eyeRendererData.Renderer != null)
//             {
//                 _eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
//                 _eyeRendererData.Renderer.SetPropertyBlock(_eyeColorMaterialPropertyBlock,
//                     _eyeRendererData.MaterialIndex);
//             }
//         }

//         public void OrientTowards(Vector3 lookPosition)
//         {
//             Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
//             if (lookDirection.sqrMagnitude != 0f)
//             {
//                 Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
//                 transform.rotation =
//                     Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
//             }
//         }

//         bool IsPathValid()
//         {
//             return PatrolPath && PatrolPath.PathNodes.Count > 0;
//         }

//         public void ResetPathDestination()
//         {
//             _pathDestinationNodeIndex = 0;
//         }

//         public void SetPathDestinationToClosestNode()
//         {
//             if (IsPathValid())
//             {
//                 int closestPathNodeIndex = 0;
//                 for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
//                 {
//                     float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
//                     if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
//                     {
//                         closestPathNodeIndex = i;
//                     }
//                 }

//                 _pathDestinationNodeIndex = closestPathNodeIndex;
//             }
//             else
//             {
//                 _pathDestinationNodeIndex = 0;
//             }
//         }

//         public Vector3 GetDestinationOnPath()
//         {
//             if (IsPathValid())
//             {
//                 return PatrolPath.GetPositionOfPathNode(_pathDestinationNodeIndex);
//             }
//             else
//             {
//                 return transform.position;
//             }
//         }

//         public void SetNavDestination(Vector3 destination)
//         {
//             if (NavMeshAgent)
//             {
//                 NavMeshAgent.SetDestination(destination);
//             }
//         }

//         public void UpdatePathDestination(bool inverseOrder = false)
//         {
//             if (IsPathValid())
//             {
//                 // Check if reached the path destination
//                 if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
//                 {
//                     // increment path destination index
//                     _pathDestinationNodeIndex =
//                         inverseOrder ? (_pathDestinationNodeIndex - 1) : (_pathDestinationNodeIndex + 1);
//                     if (_pathDestinationNodeIndex < 0)
//                     {
//                         _pathDestinationNodeIndex += PatrolPath.PathNodes.Count;
//                     }

//                     if (_pathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
//                     {
//                         _pathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
//                     }
//                 }
//             }
//         }

//         void OnDamaged(float damage, GameObject damageSource)
//         {
//             // test if the damage source is the player
//             if (damageSource && !damageSource.GetComponent<EnemyAgent>())
//             {
//                 // pursue the player
//                 DetectionModule.OnDamaged(damageSource);
//                 onDamaged?.Invoke();
//                 _lastTimeDamaged = Time.time;

//                 // play the damage tick sound
//                 if (DamageTick && !_wasDamagedThisFrame)
//                     AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

//                 _wasDamagedThisFrame = true;
//             }
//         }

//         void OnDie()
//         {
//             // spawn a particle system when dying
//             var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
//             Destroy(vfx, 5f);

//             // tells the game flow manager to handle the enemy destuction
//             _enemyManager.UnregisterEnemy(this);

//             // loot an object
//             if (TryDropItem())
//             {
//                 Instantiate(LootPrefab, transform.position, Quaternion.identity);
//             }

//             // this will call the OnDestroy function
//             Destroy(gameObject, DeathDuration);
//         }

//         void OnDrawGizmosSelected()
//         {
//             // Path reaching range
//             Gizmos.color = PathReachingRangeColor;
//             Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

//             if (DetectionModule != null)
//             {
//                 // Detection range
//                 Gizmos.color = DetectionRangeColor;
//                 Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

//                 // Attack range
//                 Gizmos.color = AttackRangeColor;
//                 Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
//             }
//         }

//         public void OrientWeaponsTowards(Vector3 lookPosition)
//         {
//             for (int i = 0; i < _weapons.Length; i++)
//             {
//                 // orient weapon towards player
//                 Vector3 weaponForward = (lookPosition - _weapons[i].WeaponRoot.transform.position).normalized;
//                 _weapons[i].transform.forward = weaponForward;
//             }
//         }

//         public bool TryAtack(Vector3 enemyPosition)
//         {
//             Debug.Log("TryAttack");
//             if (_gameFlowManager.GameIsEnding)
//                 return false;

//             OrientWeaponsTowards(enemyPosition);

//             if ((_lastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
//                 return false;

//             // Shoot the weapon
//             bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);
//             Debug.Log(didFire);

//             if (didFire && onAttack != null)
//             {
//                 Debug.Log("onAttacked");
//                 onAttack.Invoke();

//                 if (SwapToNextWeapon && _weapons.Length > 1)
//                 {
//                     int nextWeaponIndex = (_currentWeaponIndex + 1) % _weapons.Length;
//                     SetCurrentWeapon(nextWeaponIndex);
//                 }
//             }

//             return didFire;
//         }

//         public bool TryDropItem()
//         {
//             if (DropRate == 0 || LootPrefab == null)
//                 return false;
//             else if (DropRate == 1)
//                 return true;
//             else
//                 return (Random.value <= DropRate);
//         }

//         void FindAndInitializeAllWeapons()
//         {
//             // Check if we already found and initialized the weapons
//             if (_weapons == null)
//             {
//                 _weapons = GetComponentsInChildren<WeaponController>();
//                 DebugUtility.HandleErrorIfNoComponentFound<WeaponController, EnemyAgent>(_weapons.Length, this,
//                     gameObject);

//                 for (int i = 0; i < _weapons.Length; i++)
//                 {
//                     _weapons[i].Owner = gameObject;
//                 }
//             }
//         }

//         public WeaponController GetCurrentWeapon()
//         {
//             FindAndInitializeAllWeapons();
//             // Check if no weapon is currently selected
//             if (_currentWeapon == null)
//             {
//                 // Set the first weapon of the weapons list as the current weapon
//                 SetCurrentWeapon(0);
//             }

//             DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyAgent>(_currentWeapon, this,
//                 gameObject);

//             return _currentWeapon;
//         }

//         void SetCurrentWeapon(int index)
//         {
//             _currentWeaponIndex = index;
//             _currentWeapon = _weapons[_currentWeaponIndex];
//             if (SwapToNextWeapon)
//             {
//                 _lastTimeWeaponSwapped = Time.time;
//             }
//             else
//             {
//                 _lastTimeWeaponSwapped = Mathf.NegativeInfinity;
//             }
//         }
//     }
// }
