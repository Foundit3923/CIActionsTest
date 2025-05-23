using EFOV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityServiceLocator;
using static TimeTickSystem;
using static UnityEngine.UI.Image;

public class EnemyControllerContext : MonoBehaviour
{
    public enum EBodySide
    {
        LEFT,
        RIGHT,
        CENTER
    };
    private GameObject _self;
    private Animator animator;
    private int _maxAttackCount;
    private Transform _lobbyPosition, _cursedPlayer, _pinSource;
    private List<Transform> players_check;
    private List<GameObject> _players;
    public List<GameObject> Players => _players;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;
    private LayerMask _groundLayer, _playerLayer, _nightGauntLayer;
    private Vector3 _leftSideConstraint, _rightSideConstraint, _lastKillPosition, _fleeDirection;
    private List<Vector3> _hidingSpots;
    private string _interruptTag;
    private GameObject _mysticStone, _Lflashlight, _Rflashlight, _targetPlayer, _CapturedPlayer, _mysticStoneEnemy, _mysticStoneLobby;
    private GameObject[] spawnableGameObjectPool;
    private List<GameObject> _returnedItems;
    private bool _isDead, _isRunning, _interrupted, _isPlayerCaptured, _isStateMachineActive, _canSee, _isScanning, _updateAgent, _foundPlayer, _shouldKill, _hasTimeTickSystem, _appeased, _timerInUse, _lightLevelTripped, _monitoring;
    public bool _storeMysticStones, _isSequenceComplete;
    private float _range, _baseSpeed, _runningSpeed, _attackRange = 2.0f, _fleeCooldownTimer, _chaseSpeed = 2.0f, _fleeSpeed = 15.0f, _stopDistance = 1.5f, _fleeCooldown = 5.0f, _deathDistance = 2.0f, _elapsedTime, _targetTime;
    private Color _OrigColor;
    private FieldOfView _enemyFieldOfView;
    public List<RaycastHit> _visibleTargets;
    private EnemyProperties _enemyProperties;
    private BlackboardController _blackboardController;
    private Blackboard _blackboard;
    private Utility _utils;
    private EnemyControllerStateMachine.EEnemyState _lastState;
    private List<EnemyControllerStateMachine.EEnemyState> _mainStateSequence;
    private List<EnemyControllerStateMachine.EEnemyState> _prerequisiteStateSequence;
    private List<EnemyControllerStateMachine.EEnemyState> _sequenceBuffer;
    //private AnimatorController _animatorController;
    //private AnimationStateMachine _animationStateMachine;
    //public OnTrigger3DDelegator _EnvDelegator, _InterruptDelegator;

    public delegate void MonsterDeathEvent(GameObject go);
    public static event MonsterDeathEvent OnMonsterDeath;

    public delegate void TimerFinishedEvent(GameObject go);
    public static event TimerFinishedEvent OnTimerFinished;

    public delegate void DamagePlayerEvent(int d, GameObject a = default);
    public static event DamagePlayerEvent OnDamagePlayer;

    public delegate void HealPlayerEvent(int h, GameObject H = default);
    public static event HealPlayerEvent OnHealPlayer;

    private FizzyNetworkManager networkManager;
    private bool findNetworkManager = true;
    //Constructor
    public EnemyControllerContext(
                                    GameObject self,
                                    BlackboardController blackboardController,
                                    Blackboard blackboard,
                                    bool isStateMachineActive,
                                    Rigidbody rigidbody,
                                    LayerMask groundLayer,
                                    LayerMask playerLayer,
                                    NavMeshAgent agent,
                                    float range,
                                    Vector3 leftSideConstraint,
                                    Vector3 rightSideConstraint,
                                    List<Vector3> hidingSpots,
                                    GameObject Lflashlight,
                                    GameObject Rflashlight,
                                    FieldOfView enemyFieldOfView,
                                    EnemyProperties enemyProperties
        )
    {
        _self = self;
        _storeMysticStones = false;
        _blackboardController = blackboardController;
        _blackboard = blackboard;
        _blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Players), out _players);
        _isStateMachineActive = isStateMachineActive;
        _utils = ServiceLocator.Global.Get<Utility>();
        _mainStateSequence = new();
        _prerequisiteStateSequence = new();
        _sequenceBuffer = new();
        //_animatorController = animatorController;
        _rigidbody = rigidbody;
        animator = null;
        _rigidbody.gameObject.TryGetComponent<Animator>(out animator);
        _agent = agent;
        _groundLayer = groundLayer;
        _playerLayer = playerLayer;
        _range = range;
        _leftSideConstraint = leftSideConstraint;
        _rightSideConstraint = rightSideConstraint;
        _hidingSpots = hidingSpots;
        _Lflashlight = Lflashlight;
        _Rflashlight = Rflashlight;
        _baseSpeed = _agent.speed;
        _runningSpeed = _baseSpeed * 2f;
        _interrupted = false;
        _interruptTag = "None";
        _CapturedPlayer = null;
        _isPlayerCaptured = false;
        _enemyFieldOfView = enemyFieldOfView;
        _visibleTargets = new List<RaycastHit>();
        players_check = new List<Transform>();
        _enemyProperties = enemyProperties;
        _lastKillPosition = Vector3.zero;
        _hasTimeTickSystem = false;
        _returnedItems = new();
        if (rigidbody.TryGetComponent<TimeTickSystem>(out var timeTickSystem))
        {
            _hasTimeTickSystem = true;
        }
    }

    public void AdditionalStartUp(string name)
    {
        switch (name)
        {
            case string x when x.Contains("NightGaunt"):
                NightGauntStartup();
                break;
            case string x when x.Contains("LanternHead"):
                //LanternHeadStartup();
                break;
            case string x when x.Contains("GluttonousDemon"):
                GluttonousDemonStartup();
                break;
            case string x when x.Contains("Sjena"):
                SjenaStartup();
                break;
            default:
                break;
        }
    }

    public GameObject Self => _self;
    public Vector3 fleeDirection => _fleeDirection;
    public float itemCheckRadius;
    public Transform cursedPlayer => _cursedPlayer;
    public int nightGauntLayer => _nightGauntLayer;
    public bool foundPlayer => _foundPlayer;
    public bool IsSequenceComplete => _isSequenceComplete;
    public bool HasTimeTickSystem => _hasTimeTickSystem;
    public bool Appeased => _appeased;
    public bool lightLevelTripped => _lightLevelTripped;
    public int maxAttackCount => _maxAttackCount;
    public float fleeCooldownTimer => _fleeCooldownTimer;
    public float attackRange => _attackRange;
    public float chaseSpeed => _chaseSpeed;
    public float fleeSpeed => _fleeSpeed;
    public float stopDistance => _stopDistance;
    public float fleeCooldown => _fleeCooldown;
    public bool isStateMachineActive => _isStateMachineActive;
    public Rigidbody Rb => _rigidbody;
    public NavMeshAgent Agent => _agent;
    public LayerMask groundLayer => _groundLayer;
    public LayerMask playerLayer => _playerLayer;
    public float range => _range;
    public List<Vector3> hidingSpots => _hidingSpots;
    public EnemyControllerStateMachine.EEnemyState lastState => _lastState;
    public bool isRunning => _isRunning;
    public bool interrupted => _interrupted;
    public bool isPlayerCaptured => _isPlayerCaptured;
    public bool monitoring => _monitoring;
    public string interruptTag => _interruptTag;
    public Color OrigColor => _OrigColor;
    public GameObject CapturedPlayer => _CapturedPlayer;
    public GameObject TargetPlayer => _targetPlayer;
    public GameObject[] SpawnableGameObjectPool => spawnableGameObjectPool;
    public FieldOfView enemyFieldOfView => _enemyFieldOfView;
    //public List<RaycastHit> visibleTargets => _visibleTargets;
    public EnemyProperties enemyProperties => _enemyProperties;
    public bool canSee => _canSee;
    public bool isScanning => _isScanning;
    public GameObject throwable => _enemyProperties._throwable;
    public Utility utils => _utils;
    public List<EnemyControllerStateMachine.EEnemyState> MainStateSequence => _mainStateSequence;
    public List<EnemyControllerStateMachine.EEnemyState> PrerequisiteStateSequence => _prerequisiteStateSequence;
    public List<EnemyControllerStateMachine.EEnemyState> SequenceBuffer => _sequenceBuffer;
    //public AnimatorController AnimatorController => _animatorController;
    public Animator Animator => animator;

    public Transform targetTransform;
    public Vector3 lastKnownPos;

    public bool shouldKill => _shouldKill;

    //booleans for state transitions on Night Gaunt
    public bool shouldAttack = false, shouldReset = false, shouldFlee = false, shouldChase = false, isDead = false;
   
    public Vector3 lastKillPosition => _lastKillPosition;
    public string CurrentBodySide
    {
        get; private set;
    }

    //public void UpdateFleeCooldownTimer()
    //{ 
    // _fleeCooldownTimer -= Time.deltaTime;
    //}
    //public void ResetFleeCooldownTimer()
    //{ 
    // _fleeCooldownTimer = _fleeCooldown;
    //}
    public void SetLastKillPosition(Vector3 lastKillPosition) => _lastKillPosition = lastKillPosition;

    public void SetMaxAttackCount(int maxAttack) => _maxAttackCount = maxAttack;
    public void setStateMachineActive(bool isActive) => _isStateMachineActive = isActive;

    public void storeHidingSpot(Vector3 positionToCheck) => _hidingSpots.Add(positionToCheck);

    public void clearHidingSpots() => _hidingSpots.Clear();

    public void setLastState(EnemyControllerStateMachine.EEnemyState state) => _lastState = state;

    public void setBaseSpeed(float speed)
    {
        _baseSpeed = enemyProperties.Speed;
        _runningSpeed = speed * enemyProperties._runningSpeedModifier;
    }

    public void setIsRunning(bool isRunning)
    {
        if (_isRunning && !isRunning)
        {
            _agent.speed = _baseSpeed;
            //Animator.CrossFade("Run", 1f);
        }
        else if (!_isRunning && isRunning)
        {
            _agent.speed = _runningSpeed;
            //Animator.CrossFade("Run", 1f);
        }

        _isRunning = isRunning;
    }

    public void FindNetworkManager(ref FizzyNetworkManager networkManager)
    {
        if (networkManager != null)
            return; // Already assigned

        if (findNetworkManager)
        {
            findNetworkManager = false;
            GameObject networkingManagerGO = GameObject.Find("NetworkingManager");

            if (networkingManagerGO != null)
            {
                networkManager = networkingManagerGO.GetComponent<FizzyNetworkManager>();
            }
            else
            {
                Debug.LogWarning("NetworkManager not found in the scene");
            }
        }
    }

    public void setInterrupted(bool isInterrupted) => _interrupted = isInterrupted;

    public void setInterruptTag(string tag) => _interruptTag = tag;

    public void setDead(bool isDead)
    {
        _isDead = isDead;
        //Animator.SetBool("Death", isDead);
        if (_isDead)
        {
            SequenceBuffer.Clear();
            PrerequisiteStateSequence.Clear();
            MainStateSequence.Clear();
            PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
            MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Death);
            _targetPlayer.gameObject.GetComponent<PlayerInformationManager>().SetAttachedMonster(PlayerProperties.Persuer.Sjena, false);
            OnMonsterDeath?.Invoke(this.gameObject);
        }
    }

    public void StartVision()
    {
        if (_enemyFieldOfView != null)
        {
            TimeTickSystem.OnTick += OpenEyes;
            _canSee = true;
        }
        else
        {
            Debug.Log("Can't see _enemyFieldOfView is null");
        }
    }

    public bool CheckProximityToDeathItem(float itemCheckRadius)
    {
        Collider[] colliders = Physics.OverlapSphere(Self.transform.position, itemCheckRadius);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("DeathItem")) // Make sure the cube (death item) has the tag "DeathItem"
            {
                // Call "eat" animation here
                Debug.Log("Monster is near the death item!");

                return true;
            }
        }

        // Return false if no death item is detected in the area
        return false;
    }

    public void storeCapturedPlayer(GameObject player)
    {
        _isPlayerCaptured = true;
        _CapturedPlayer = player;
    }

    public void releaseCapturedPlayer()
    {
        _isPlayerCaptured = false;
        _CapturedPlayer = null;
    }

    Transform GetCursedPlayer()
    {
        // Randomly select a cursed player from the players array
        int index = UnityEngine.Random.Range(0, _players.Count);
        return _players[index].transform;
    }
    Transform GetNewCursedPlayer()
        // Randomly select a cursed player from the players array
        => players_check[UnityEngine.Random.Range(0, players_check.Count)];
    public void UpdateVisibility()
    {
        foreach (Camera cam in Camera.allCameras)
        {
            cam.cullingMask &= ~(1 << nightGauntLayer); // Hide the NightGaunt from all cameras
        }

        Camera cursedPlayerCamera = cursedPlayer.GetComponentInChildren<Camera>();
        if (cursedPlayerCamera != null)
        {
            cursedPlayerCamera.cullingMask |= 1 << nightGauntLayer; // Show the NightGaunt only to the cursed player
        }
        else
        {
            Debug.LogError("Cursed player does not have a camera.");
        }
    }
    public void MysticStoneProximity(object sender, TimeTickSystem.OnTickEventArgs e)
    {
        Debug.Log("Check mystic stone proximity");
        _blackboard.TryGetValue(_blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.MysticStoneLobby), out GameObject msl);
        _blackboard.TryGetValue(_blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.MysticStoneEnemy), out GameObject mse);
        float distance = Vector3.Distance(msl.transform.position, mse.transform.position);
        //Debug.Log($"Mystic Stone Proximity: {distance}");
        if (distance <= _deathDistance)
        {
            Debug.Log("Death condition met! Gaunt is killed.");
            this._shouldKill = true;
        }
    }
    private void NightGauntVision(object sender, TimeTickSystem.OnTickEventArgs e)
    {

        //players_check = (GameObject[])GameObject.FindGameObjectsWithTag("Player").Where(x => x.GetComponent<PlayerProperties>().isDead == false);
        PlayerProperties pp = new();
        int prevLivingCount = this.players_check.Count;
        this.players_check.Clear();
        bool livingCountHasChanged = false;

        foreach (var player in this._players)
        {
            pp = player.GetComponent<PlayerProperties>();
            if (pp != null && pp.isDead == false)
            {
                //list of living players
                this.players_check.Add(player.transform);
            }
        }

        livingCountHasChanged = prevLivingCount != this.players_check.Count;

        if (this.players_check.Count == 0)
        {
            //death animation if wanted
            this._shouldKill = true;
        }
        else if (livingCountHasChanged)
        {
            if (this.cursedPlayer.GetComponent<PlayerProperties>().isDead == true)
            {
                this._cursedPlayer = this.GetCursedPlayer();
                this.UpdateVisibility();
            }
        }
    }

    public (bool, Vector3) GetRandomWalkablePosInRange(float minRange, float maxRange)
    {
        Vector3 dest = Vector3.zero;

        (bool, Vector3) potentialDest = GetRandomPos(minRange, maxRange);

        bool canReachPoint = false;

        if (potentialDest.Item1)
        {
            //Ensure the monster can make it to the potentialDest
            NavMeshPath path = new();
            Agent.CalculatePath(potentialDest.Item2, path);
            canReachPoint = path.status == NavMeshPathStatus.PathComplete;
            if (canReachPoint)
            {
                return (true, potentialDest.Item2);
            }
        }

        return (false, dest);
    }

    private (bool, Vector3) GetRandomPos(float minRange, float maxRange)
    {
        Vector3 dest = Vector3.negativeInfinity;

        NavMeshHit hit = default;

        int maxAttempts = 50;

        //generate the correct navmeshareamask
        int navMeshAreaFlag = NavMesh.GetAreaFromName("Walkable");
        int navMeshAreaMask = 1 << navMeshAreaFlag;
        //Get a random position within the maxRange
        bool foundPosition = false;
        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            foundPosition = NavMesh.SamplePosition(
                                                        Self.transform.position + (UnityEngine.Random.insideUnitSphere * maxRange),
                                                        out hit,
                                                        UnityEngine.Random.Range(minRange, maxRange),
                                                        NavMesh.AllAreas
                                                    );
            if (foundPosition)
            {
                Vector3 pos = new(hit.position.x, Self.transform.position.y, hit.position.z);
                return (true, pos);
            }
        }

        return (false, dest);
    }

    public Vector3 GetRandomFleeDirection()
    {
        // Generate a random point, making sure it's 50 units from the last kill position
        Vector3 fleeDirection = UnityEngine.Random.insideUnitSphere * 40f; // 50f is the flee distance
        fleeDirection.y = 0;  // Make sure it stays on the ground

        // Ensure the random point is far enough from the last kill position
        while (Vector3.Distance(lastKillPosition, fleeDirection) < 10f)
        // 10f is the minimum distance to the kill point
        {
            fleeDirection = UnityEngine.Random.insideUnitSphere * 40f;
            fleeDirection.y = 0;
        }

        // Use NavMesh to find a valid position to flee to
        if (NavMesh.SamplePosition(_rigidbody.transform.position + fleeDirection, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return _rigidbody.transform.position; // If no valid spot, stay in place
    }
    public void EndVision()
    {
        TimeTickSystem.OnTick -= OpenEyes;
        _canSee = false;
    }

    public void SetCurrentSide(Vector3 positionToCheck)
    {
        _leftSideConstraint = _Lflashlight.transform.position;
        _rightSideConstraint = _Rflashlight.transform.position;
        bool isLeftCloser = Vector3.Distance(positionToCheck, _leftSideConstraint) < Vector3.Distance(positionToCheck, _rightSideConstraint);
        if (isLeftCloser)
        {
            CurrentBodySide = "left";
        }
        else
        {
            CurrentBodySide = "right";
        }
    }

    private void OpenEyes(object sender, TimeTickSystem.OnTickEventArgs e)
    {
        if (this._enemyFieldOfView != null)
        {
            if (this._isScanning)
            {
                Debug.Log("Scan Vision");
                this._enemyFieldOfView.FindAllTargets();
            }
            else
            {
                Debug.Log("Normal Vision");
                this._enemyFieldOfView.FindVisibleTargets();
            }
        }
        else
        {
            Debug.Log("Can't see _enemyFieldOfView is null");
        }
    }

    private void OnLightLevelTripped() => SetLightLevelTripped(true);

    public void SetLightLevelTripped(bool lightLevelTripped) => _lightLevelTripped = lightLevelTripped;

    public void setTargetPlayer(Transform player)
    {
        targetTransform = player;
        _targetPlayer = player.gameObject;
    }

    public void setIsScanning(bool isScanning)
    {
        _isScanning = isScanning;
        Animator.SetInteger("State", (int)EnemyControllerStateMachine.EEnemyState.Scan);
    }

    //public void ClearVisibleTargets()
    //{
    //    _visibleTargets.Clear();
    //}

    //public void AddVisibleTarget(RaycastHit target)
    //{
    //    _visibleTargets.Add(target);
    //}
    public void SetNightGauntTarget() => _targetPlayer = FindNearestNonCursedPlayer();

    public GameObject FindNearestNonCursedPlayer()
    {
        GameObject nearestPlayer = null;
        float shortestDistance = Mathf.Infinity;

        //log number of players in the list
        //Debug.Log("Number of players: " + _players.Count);
        //log the cursed player (check if it's unassigned)
        //Debug.Log("Cursed player: " + cursedPlayer);

        foreach (var player in _players) //filter out dead players
        {
            if (player != cursedPlayer)
            {
                float distance = Vector3.Distance(_rigidbody.transform.position, player.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestPlayer = player.gameObject;
                }
            }
        }

        return nearestPlayer;
    }

    //
    // Summary:
    //     Triggeres the OnTimerFinished(GameObject) event when the specified time has elapsed.
    //
    // Parameters:
    //   minutes:
    //
    //   seconds:
    //
    //   milliseconds:
    public bool SetTimer(int minutes = 0, float seconds = 0f, float milliseconds = 0f)
    {
        if (!_timerInUse)
        {
            _timerInUse = true;
            _elapsedTime = 0f;
            if (minutes > 0f)
            {
                float conversion = minutes * 60f;
                seconds += conversion;
            }

            if (milliseconds > 0f)
            {
                float conversion = milliseconds * 0.001f;
                seconds += conversion;
            }

            _targetTime = seconds;

            TimeTickSystem.OnTick += WaitForSeconds;
            return true;
        }

        return false;
    }

    public void EndTimer()
    {
        if (_timerInUse)
        {
            _timerInUse = false;
            TimeTickSystem.OnTick -= WaitForSeconds;
            OnTimerFinished?.Invoke(Self);
        }
    }

    private void WaitForSeconds(object sender, TimeTickSystem.OnTickEventArgs e)
    {
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _targetTime)
        {
            EndTimer();
        }
    }

    private void AcceptItem(GameObject go)
    {
        _returnedItems.Add(go);
        List<GameObject> itemsToBeReturned = Self.GetComponent<InitializeItems>().itemsToBeReturned.ToList();
        if (itemsToBeReturned.Intersect(_returnedItems).ToList().Count < itemsToBeReturned.Count)
        {
            _appeased = true;
            _interrupted = true;
        }
    }

    public void SjenaStartup()
    {
        _monitoring = false;
        SjenaMonitor();
        SjenaLightExposure();
    }

    public void SjenaMonitor(bool restartOverride = false)
    {
        if (!_monitoring)
        {
            foreach (GameObject player in _players)
            {
                if (player.TryGetComponent<EvalSjenaShouldAttach>(out EvalSjenaShouldAttach component))
                {
                    component.shouldEval = true;
                    if (restartOverride)
                    {
                        component.restartOverride = restartOverride;
                    }
                }
            }

            EvalSjenaShouldAttach.OnSjenaShouldAttach += AttachSjena;
            _monitoring = true;
        }
    }

    private void AttachSjena(GameObject targetPlayer)
    {
        EvalSjenaShouldAttach.OnSjenaShouldAttach -= AttachSjena;
        foreach (GameObject player in _players)
        {
            if (player.TryGetComponent<EvalSjenaShouldAttach>(out EvalSjenaShouldAttach component))
            {
                component.shouldEval = false;
                component.InvokeCallback();
            }
        }

        _lightLevelTripped = true;
        _monitoring = false;
        _targetPlayer = targetPlayer;
        _targetPlayer.gameObject.GetComponent<PlayerInformationManager>().SetAttachedMonster(PlayerProperties.Persuer.Sjena, true);
    }

    private void SjenaLightExposure()
    {
        
    }

    public void NightGauntStartup()
    {
        _nightGauntLayer = LayerMask.NameToLayer("NightGauntLayer");
        //_mysticStoneLobby = UnityEngine.Object.Instantiate(_mysticStone, Vector3.zero, Quaternion.identity); //need to identify lobby
        //_mysticStoneEnemy = UnityEngine.Object.Instantiate(_mysticStone, new Vector3(_rigidbody.position.x, _rigidbody.position.y + 2f, _rigidbody.position.z), Quaternion.identity);
        //Physics.IgnoreCollision(_mysticStoneEnemy.GetComponent<Collider>(), _mysticStoneLobby.GetComponent<Collider>());
        _cursedPlayer = GetCursedPlayer();
        UpdateVisibility();
        //TimeTickSystem.OnTick_2 += MysticStoneProximity;
        TimeTickSystem.OnTick_2 += NightGauntVision;
    }

    public void GluttonousDemonStartup()
    {
        if (_maxAttackCount > 100)
        {
            throw new Exception("Fuck you, lower Gluttonous Demon's max attack count");
        }
        else
        {
            GameObject Bloat;
            GameObject acidPool = default;
            Bloat = GameObject.FindGameObjectWithTag("GluttonousDemon");
            if (Bloat != null)
            {
                InitializeItems initItems = Bloat.GetComponent<InitializeItems>();
                if (initItems != null)
                {
                    acidPool = initItems.itemsToSpawnAsNeeded.FirstOrDefault();
                }
            }

            spawnableGameObjectPool = new GameObject[_maxAttackCount];
            for (int i = 0; i < spawnableGameObjectPool.Length; i++)
            {
                spawnableGameObjectPool[i] = GameObject.Instantiate(acidPool);
                spawnableGameObjectPool[i].SetActive(false);
            }
        }

        return;
    }

    public void VodyanoiStartup() => TimeTickSystem.OnTick_12 += EvalTarget;

    private void EvalTarget(object sender, OnTickEventArgs e)
    {
        //Replace with navmesh check
        List<GameObject> playersInLevel = _players.Where(p => p.gameObject.GetComponent<PlayerInformationManager>().playerProperties.inLevel == true).ToList();

        if (!_timerInUse)
        {
            if (_targetPlayer == null)
            {
                var index = utils.GetRandomNumber(0, playersInLevel.Count);
                if (index != -1)
                {
                    _targetPlayer = playersInLevel[(int)index];
                }
            }
            else if (!playersInLevel.Contains(_targetPlayer))
            {
                var index = utils.GetRandomNumber(0, playersInLevel.Count);
                if (index != -1)
                {
                    _targetPlayer = playersInLevel[(int)index];
                }
            }
        }

        if (playersInLevel.Count > 0)
        {
            if (_targetPlayer == null)
            {
                var index = utils.GetRandomNumber(0, playersInLevel.Count);
                if (index != -1)
                {
                    _targetPlayer = playersInLevel[(int)index].gameObject;
                }
            }
            else if (playersInLevel.Contains(_targetPlayer))
            {
                //do nothing?
            }
            else
            {
                var index = utils.GetRandomNumber(0, playersInLevel.Count);
                if (index != -1)
                {
                    _targetPlayer = playersInLevel[(int)index];
                }
            }
        }
        else
        {
            if (!_timerInUse)
            {
                var index = utils.GetRandomNumber(0, _players.Count);
                if (index != -1)
                {
                    _targetPlayer = _players[(int)index];
                }

                SetTimer(1, 0, 0);
            }
        }
    }

    public void KillTargetPlayer()
    {
        if (_targetPlayer != null)
        {
            if (OnDamagePlayer != null)
            {
                OnDamagePlayer(100, _self.gameObject);
            }
            else
            {
                _targetPlayer.GetComponent<PlayerInformationManager>().SetAsDead();
            }

            _targetPlayer = null;
        }
    }

    public void SetStateSequence(EnemyControllerStateMachine.EEnemyState state) => _mainStateSequence.Add(state);

    public void SetStateSequence(List<EnemyControllerStateMachine.EEnemyState> states) => _mainStateSequence.AddRange(states.ToList());

    public void SetAsFirstStateSequence(EnemyControllerStateMachine.EEnemyState state) => _mainStateSequence.Insert(0, state);

    public void SetAsFirstStateSequence(List<EnemyControllerStateMachine.EEnemyState> states)
    {
        int count = states.Count();
        _mainStateSequence.InsertRange(0, states.ToList());
    }

    public void SetPrerequisiteStateSequence(EnemyControllerStateMachine.EEnemyState state) => _prerequisiteStateSequence.Add(state);

    public void SetPrerequisiteStateSequence(List<EnemyControllerStateMachine.EEnemyState> states) => _prerequisiteStateSequence.AddRange(states.ToList());
    public void SetAsFirstPrerequisiteStateSequence(EnemyControllerStateMachine.EEnemyState state) => _prerequisiteStateSequence.Insert(0, state);

    public void SetAsFirstPrerequisiteStateSequence(List<EnemyControllerStateMachine.EEnemyState> states) => _prerequisiteStateSequence.InsertRange(0, states.ToList());

    public string PrintPrerequesiteStateSequence()
    {
        string preSequence = string.Join(",", _prerequisiteStateSequence.ToArray());
        return "Prerequisite State Sequence: " + preSequence;
    }

    public string PrintMainStateSequence()
    {
        string preSequence = string.Join(",", _mainStateSequence.ToArray());
        return "Main State Sequence: " + preSequence;
    }

    public void UnPinToTarget()
    {
        _self.gameObject.transform.position = new Vector3(0, -100, 0);
        PositionConstraint constraint = _self.GetComponent<PositionConstraint>();
        if (constraint is not null)
        {
            constraint.constraintActive = false;
            constraint.SetSources(new System.Collections.Generic.List<ConstraintSource>());
            constraint.weight = 0f;
        }

        RotationConstraint rotationConstraint = _self.GetComponent<RotationConstraint>();
        if (rotationConstraint is not null)
        {
            rotationConstraint.constraintActive = false;
            rotationConstraint.SetSources(new System.Collections.Generic.List<ConstraintSource>());
        }
    }

    public void PinToTarget(GameObject target, TargetDirectory.TargetType type)
    {
        _self.gameObject.transform.position = target.transform.position;
        _self.gameObject.transform.rotation = target.transform.rotation;
        Transform pinTarget = target.GetComponentInChildren<TargetDirectory>().GetTarget(type).target.transform;
        if (pinTarget is not null)
        {
            PositionConstraint constraint = _self.GetComponent<PositionConstraint>();
            if (constraint is not null)
            {
                constraint.constraintActive = false;
                constraint.SetSources(new System.Collections.Generic.List<ConstraintSource>());

                ConstraintSource source = new()
                {
                    sourceTransform = pinTarget,
                    weight = 1.0f
                };
                constraint.locked = false;
                constraint.translationAtRest = Vector3.zero;
                constraint.translationOffset = new Vector3(0f, -4.6f, -0.2f);
                constraint.translationAxis = Axis.X | Axis.Y | Axis.Z;                

                constraint.AddSource(source);
                constraint.locked = true;
                constraint.constraintActive = true;
            }
            else
            {
                utils.DebugOut("No PositionConstraint on object");
            }

            RotationConstraint rotationConstraint = _self.GetComponent<RotationConstraint>();
            if (rotationConstraint is not null)
            {
                rotationConstraint.constraintActive = false;
                rotationConstraint.SetSources(new System.Collections.Generic.List<ConstraintSource>());

                ConstraintSource source = new()
                {
                    sourceTransform = pinTarget,
                    weight = 1.0f
                };
                rotationConstraint.locked = false;
                rotationConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;

                rotationConstraint.AddSource(source);
                rotationConstraint.locked = true;
                rotationConstraint.constraintActive = true;
            }
            else
            {
                utils.DebugOut("No RotationConstraint on object");
            }
        }
        else
        {
            utils.DebugOut("Target anchor not found on player");
        }
    }

    private void Pin(GameObject target)
    {
        Self.transform.position = target.transform.position;// _pinSource.position;
        Self.transform.rotation = target.transform.rotation;// _pinSource.rotation;
    }

    //private void OnDestroy() => TimeTickSystem.OnTick -= Pin;
}