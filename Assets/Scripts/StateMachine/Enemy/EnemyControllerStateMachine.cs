using UnityEngine;
using UnityEngine.Assertions;
using StateMachine;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using EFOV;
using UnityServiceLocator;
using static UnityEngine.Rendering.GPUSort;
using Mirror.BouncyCastle.Pqc.Crypto.Falcon;
using Mirror;

[RequireComponent(typeof(EnemyProperties))]
[RequireComponent(typeof(FieldOfView))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(TimeTickSystem))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkTransformReliable))]
[RequireComponent(typeof(NetworkIdentity))]
public class EnemyControllerStateMachine : StateMachine<EnemyControllerStateMachine.EEnemyState>
{

    public void Constructor(Rigidbody rigidbody,
                                        LayerMask groundLayer,
                                        LayerMask playerLayer,
                                        int range,
                                        FieldOfView fieldOfView,
                                        EnemyProperties enemyProperties//,
                                                                       //AnimatorController animatorController
                            )
    {
        _rigidbody = rigidbody;
        _groundLayer = groundLayer;
        _playerLayer = playerLayer;
        _range = range;
        _enemyFieldOfView = fieldOfView;
        _enemyProperties = enemyProperties;
        //_animatorController = animatorController;
        if (!base.isStateMachineActive)
        {
            StartUp();
        }
    }

    public enum EEnemyState
    {
        Animation,
        Appeased,
        Attach,
        Attack,
        Chase,
        ChaseAndThrow,
        CheckTargets,
        Death,
        EnvAtk,
        Flee,
        Idle,
        Interrupt,
        Kill,
        Patrol,
        PlayerCapture,
        PlayerTransform,
        Reset,
        Scan,
        SearchForHost,
    }

    private EnemyControllerContext _context;
    private string _namespace;
    private Rigidbody _rigidbody;
    private LayerMask _groundLayer, _playerLayer;
    private float _range;
    private GameObject _Lflashlight;
    private GameObject _Rflashlight;
    private FieldOfView _enemyFieldOfView;
    private EnemyProperties _enemyProperties;
    //[SerializeField] private AnimatorController _animatorController;
    private NavMeshAgent _agent;
    private Vector3 _leftSideConstraint;
    private Vector3 _rightSideConstraint;
    private List<Vector3> _hidingSpots;
    private bool _isStateMachineActive;
    private System.Collections.Generic.IEnumerable<Type> _classes;
    private int _lastHealthLevel;
    public int _debug;

    public EEnemyState _availableStates = new();
    private BlackboardController _blackboardController;
    private Blackboard _blackboard;

    public delegate void MonsterCreatedEvent(EnemyControllerStateMachine e);
    public static event MonsterCreatedEvent OnMonsterCreated;

    public delegate void MonsterDeathEvent(EnemyControllerStateMachine e);
    public static event MonsterDeathEvent OnMonsterDeath;
    public Utility utils;

    public override void OnAwake()
    {
        _blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        _blackboard = _blackboardController.GetBlackboard();
        _blackboard.SetListValue(BlackboardController.BlackboardKeyStrings.Monsters, this.gameObject);
        utils = ServiceLocator.Global.Get<Utility>();
    }

    public override void OnStart() => StartUp();
    void StartUp()
    {
        //Should keep anything from running if any required values are not available.
        //This can be reversed by calling the constructor and passing in the correct values
        //If all values are av
        InitializeComponents();
        ValidateConstraints();
        if (base.isStateMachineActive && !_finishedSetup)
        {
            //object[] args = new object[18] { _blackboardController, _blackboard, base.isStateMachineActive, _rigidbody, _rootCollider, _groundLayer, _playerLayer, _agent, _range, _leftSideConstraint, _rightSideConstraint, _hidingSpots, _Lflashlight, _Rflashlight, _renderer, _enemyFieldOfView, _enemyProperties, _animatorController };
            object[] args = new object[16] {this.gameObject, _blackboardController, _blackboard, base.isStateMachineActive, _rigidbody, _groundLayer, _playerLayer, _agent, _range, _leftSideConstraint, _rightSideConstraint, _hidingSpots, _Lflashlight, _Rflashlight, _enemyFieldOfView, _enemyProperties };
            _context = (EnemyControllerContext)Activator.CreateInstance(typeof(EnemyControllerContext), args);
            //_context = new EnemyControllerContext(_blackboardController, _blackboard, _isStateMachineActive, _rigidbody, _rootCollider, _groundLayer, _playerLayer, _agent, _range, _leftSideConstraint, _rightSideConstraint, _hidingSpots, _Lflashlight, _Rflashlight, _renderer, _enemyFieldOfView, _enemyProperties);
            _context.AdditionalStartUp(_namespace);
            bool state = _context == null;

            InitializeStates();

            InitializeVision();

            Debug.Log("Enemy State Machine Initialized");

            CurrentState.EnterState();

            _finishedSetup = true;

            OnMonsterCreated?.Invoke(this);
        }

        //ServiceLocator.For(this).Get<BlackboardController>().RegisterWithPriorityGroup(this);
    }

    private void ValidateConstraints()
    {
        try
        {
            Debug.Log(_namespace);
            Assert.IsNotNull(_blackboard, "Could not retrieve blackboard");
            Assert.IsTrue(base.isStateMachineActive, "State Machine active var is false");
            Assert.IsNotNull(_rigidbody, "Rigidbody used to control character is not assigned.");
            Assert.IsTrue(LayerMask.GetMask("NavMesh") == _groundLayer, "GroundLayer attached to character is not assigned.");
            Assert.IsTrue(LayerMask.GetMask("Player") == _playerLayer, "GroundLayer attached to character is not assigned.");
            Assert.IsNotNull(_agent, "NavMeshAgent attached to character is not assigned.");
            Assert.IsTrue(_range != 0, "Float Range attached to character is not assigned, or is zero.");
            Assert.IsTrue(_leftSideConstraint != Vector3.zero, "Left side constraint is 0");
            Assert.IsTrue(_rightSideConstraint != Vector3.zero, "Right side constraint is 0");
            Assert.IsNotNull(_Rflashlight, "Left Flashlight object attached to character could not be found.");
            Assert.IsNotNull(_Rflashlight, "Right Flashlight object attached to character could not be found.");
            Assert.IsNotNull(_hidingSpots, "Hiding spots list was not able to be initialized.");
            Assert.IsNotNull(_enemyFieldOfView, "Enemy Field of View was not able to be initialized.");
            Assert.IsNotNull(_enemyProperties, "Enemy Properties was not able to be initialized.");
        }
        catch
        {
            base.isStateMachineActive = false;
        }
    }

    private void InitializeComponents()
    {
        base.isStateMachineActive = true;
        if (TryGetComponent<Rigidbody>(out _rigidbody))
        {
            if (!gameObject.TryGetComponent<NetworkRigidbodyReliable>(out NetworkRigidbodyReliable reliableRb))
            {
                gameObject.AddComponent<NetworkRigidbodyReliable>();
            }            
        }

        _enemyFieldOfView = GetComponent<FieldOfView>();
        _enemyProperties = GetComponent<EnemyProperties>();
        _agent = GetComponent<NavMeshAgent>();
        _namespace = _enemyProperties.Name;
        _range = _enemyProperties.range;
        _groundLayer = _enemyProperties.navmeshMask;
        _playerLayer = _enemyProperties.targetMask;
        _leftSideConstraint = new Vector3(transform.position.x - .5f, transform.position.y * .75f, transform.position.z);
        _rightSideConstraint = new Vector3(transform.position.x + .5f, transform.position.y * .75f, transform.position.z);
        _hidingSpots = new List<Vector3>();
        _rigidbody.detectCollisions = true;
        if (_Lflashlight == null)
        {
            _Lflashlight = new GameObject();
        }

        if (_Rflashlight == null)
        {
            _Rflashlight = new GameObject();
        }
    }

    public static TTo ConvertEnumByName<TFrom, TTo>(TFrom value, bool ignoreCase = false)
        where TFrom : struct
        where TTo : struct
        => System.Enum.Parse<TTo>(value.ToString(), ignoreCase);

    private void InitializeStates()
    {
        Debug.Log("Namespace: " + _namespace);
        //Add States to inherited StateManager "States" dictionary and Set Initial State
        Dictionary<string, List<Type>> classDictionary = new();
        if (_blackboard.TryGetValue(_blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.ClassDictionary), out classDictionary))
        {
            EEnemyState reset = new();
            if (classDictionary.ContainsKey(_namespace + "States"))
            {
                _classes = classDictionary[_namespace + "States"];
            }
            else
            {
                throw new Exception($"namespace: {_namespace + "States"} not found in classDictionary. Available keys are: {classDictionary.Keys.ToCommaSeparatedString()}");
            }

            foreach (var t in _classes)
            {
                object[] args = new object[2];
                args[0] = _context;
                foreach (EEnemyState state in EEnemyState.GetValues(typeof(EEnemyState)))
                {
                    if (t.ToString().Contains(state.ToString()))
                    {
                        if (state.ToString().Contains("Reset"))
                        {
                            reset = state;
                        }

                        args[1] = state;
                        //Type type = t.GetType();
                        States.Add(state, (BaseState.BaseState<EnemyControllerStateMachine.EEnemyState>)Activator.CreateInstance(t, args));
                        break;
                    }
                }
            }

            CurrentState = States[reset];
        }
        else
        {
            throw new Exception("ClassDictionary not found");
        }
    }

    public EnemyControllerContext getContext() => _context;

    private void InitializeVision()
    {
        _enemyFieldOfView.InitializeFOV(_context, _enemyProperties);
        _context.StartVision();
        Debug.Log("Vision Intialized");
    }

    public override void Draw()
    {
    }

    public void ForceNextState() => base._forceNextState = true;

    public EEnemyState GetCurrentState() => CurrentState.StateKey;

    public void ApplyDamage(int damage) => _enemyProperties.health -= damage;
    public override void OnUpdate()
    {
        if (_context.isDead == false)
        {
            if (_enemyProperties.health <= 0)
            {
                _context.setDead(true);
            }
            else
            {
                if (!(_enemyProperties.health < _lastHealthLevel))
                {
                    _enemyProperties.health = 100;
                }

                _lastHealthLevel = _enemyProperties.health;
            }
        }
    }
}