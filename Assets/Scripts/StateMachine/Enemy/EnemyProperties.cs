using System.Collections.Generic;
using EFOV;
using Mirror;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityServiceLocator;

[RequireComponent(typeof(InitializeItems))]
public class EnemyProperties : NetworkBehaviour
{
    private Utility utils;
    [SerializeField] public string Name;
    [SerializeField] public int viewRadius = 20;
    [UnityEngine.Range(0, 360)]
    [SerializeField] public int viewAngle = 150;
    [SerializeField] private int _speed = 5;
    [SerializeField] public int _runningSpeedModifier = 2;
    [SerializeField] public GameObject _throwable;
    [SerializeField] public int Cost;
    [SerializeField] public int NavMeshAgentId;
    [SerializeField] public List<GameObject> spawnItems;
    public int health = 100;
#if UNITY_EDITOR
    [SerializeField] public AnimatorController _animatorController;
#endif
    [SerializeField] public int range;
    public bool isDead = false;
    private int _speed_ = 5;
    public int Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            if (value != _speed_ && assertionsValidated)
            {
                fsmContext.Agent.speed = _speed;
                fsmContext.setBaseSpeed((float)value);
                Debug.Log("Speed: " + fsmContext.Agent.speed);
            }

            _speed_ = _speed;
        }
    }
    [SerializeField] private int _angularSpeed = 20;
    private int _angularSpeed_ = 20;
    private int AngularSpeed
    {
        get => _angularSpeed;
        set
        {
            _angularSpeed = value;
            if (value != _angularSpeed_ && assertionsValidated)
            {
                fsmContext.Agent.angularSpeed = _angularSpeed;
                Debug.Log("Angular Speed: " + fsmContext.Agent.angularSpeed);
            }

            _angularSpeed_ = _angularSpeed;
        }
    }
    [SerializeField] private int _acceleration = 12;
    private int _acceleration_ = 12;
    private int Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = value;
            if (value != _acceleration_ && assertionsValidated)
            {
                fsmContext.Agent.acceleration = _acceleration;
                Debug.Log("Acceleration: " + fsmContext.Agent.acceleration);
            }

            _acceleration_ = _acceleration;
        }
    }
    [SerializeField] private int _stoppingDistance = 0;
    private int _stoppingDistance_ = 0;
    private int StoppingDistance
    {
        get => _stoppingDistance;
        set
        {
            _stoppingDistance = value;
            if (value != _stoppingDistance_ && assertionsValidated)
            {
                fsmContext.Agent.stoppingDistance = _stoppingDistance;
                Debug.Log("Stopping Distance: " + fsmContext.Agent.stoppingDistance);
            }

            _stoppingDistance_ = _stoppingDistance;
        }
    }
    [SerializeField] private bool _autoBreaking = true;
    private bool _autoBreaking_ = true;
    private bool AutoBreaking
    {
        get => _autoBreaking;
        set
        {
            _autoBreaking = value;
            if (value != _autoBreaking_ && assertionsValidated)
            {
                fsmContext.Agent.autoBraking = _autoBreaking;
                Debug.Log("AutoBreaking: " + fsmContext.Agent.autoBraking);
            }

            _autoBreaking_ = _autoBreaking;
        }
    }
    [SerializeField] public LayerMask targetMask;
    [SerializeField] public LayerMask obstacleMask;
    [SerializeField] public LayerMask navmeshMask;

    private EnemyControllerStateMachine fsm = null;
    private EnemyControllerContext fsmContext = null;

    private bool assertionsValidated = false;
    private bool checkIfFsmSetupIsDone;

    private void Awake()
    {

    }

    private void Start()
    {
        if (ServiceLocator.Global.Get<Utility>().DevMode)
        {
            fsm = GetComponent<EnemyControllerStateMachine>();
            LayerMask target = LayerMask.NameToLayer("Player");

            updateAgent();
        }
        else
        {
            if (isServer)
            {
                fsm = GetComponent<EnemyControllerStateMachine>();
                LayerMask target = LayerMask.NameToLayer("Player");

                updateAgent();
            }
            else
            {
                fsm = GetComponent<EnemyControllerStateMachine>();
                fsm.enabled = false;
                FieldOfView fov = GetComponent<FieldOfView>();
                fov.enabled = false;
                NavMeshAgent agent = GetComponent<NavMeshAgent>();
                agent.enabled = false;

            }
        }
    }

    private void Update()
    {
        if (checkIfFsmSetupIsDone && isServer)
        {
            if (fsm.isSetupFinished())
            {
                fsmContext = fsm.getContext();
                ValidateConstraints();
                checkIfFsmSetupIsDone = false;
            }
        }
    }

    private void OnValidate() => updateAgent();

    private void updateAgent()
    {
        Speed = _speed;
        AngularSpeed = _angularSpeed;
        Acceleration = _acceleration;
        StoppingDistance = _stoppingDistance;
        AutoBreaking = _autoBreaking;
    }

    private void ValidateConstraints()
    {

        Assert.IsNotNull(fsm, "FSM used to control character is not assigned.");
        Assert.IsNotNull(fsmContext, "Context could not be assigned.");
        assertionsValidated = true;
    }
}

