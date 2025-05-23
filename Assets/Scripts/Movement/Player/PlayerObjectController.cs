using Mirror;
using Mirror.Examples.BilliardsPredicted;
using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriggerDelegator;
using UnityEngine;
using UnityEngine.Animations.Rigging;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using static UnityEngine.UI.GridLayoutGroup;
#endif

#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(UnityEngine.InputSystem.PlayerInput))]
[RequireComponent(typeof(CharacterController))]
#endif
public class PlayerObjectController : NetworkBehaviour
{
    BlackboardKey spawnerKey;
    Blackboard _blackboard;
    Blackboard blackboard
    {
        get
        {
            if (_blackboard != null)
            {
                return _blackboard;
            }

            return _blackboard = blackboardController.GetBlackboard();
        }
        set => _blackboard = value;
    }
    BlackboardController _blackboardController;
    BlackboardController blackboardController
    {
        get
        {
            if (_blackboardController != null)
            {
                return _blackboardController;
            }

            return _blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        }
        set => _blackboardController = value;
    }
    public Transform spawnerPoint;
    public GameObject playerPrefab;
    [SerializeField] private GameObject _mainCamera;
    [SerializeField] private GameObject _HUD;
    [SerializeField] private GameObject _cameraRoot;
    [SerializeField] private GameObject _followCamera;
    [SerializeField] private GameObject _mountPoint;
    [SerializeField] private GameObject _rigContainer;
    private FizzyLobbyController lobbyController;
    public Vector3 inputDirection;
    private GamesPanel invertMovment;
    public GameObject followCamera;
    public class PlayerData
    {
        public string playerName;
        public int score;
        public bool soundOn;
    }

    public class SaveLoadExample : MonoBehaviour
    {
        private const string PLAYER_DATA_KEY = "PlayerData";

        // Save data
        public void SavePlayerData(PlayerData playerData)
        {
            string jsonString = JsonConvert.SerializeObject(playerData); // Or JsonUtility.ToJson(playerData)
            PlayerPrefs.SetString(PLAYER_DATA_KEY, jsonString);
            PlayerPrefs.Save();
        }

        // Load data
        public PlayerData LoadPlayerData()
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                string jsonString = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                return JsonConvert.DeserializeObject<PlayerData>(jsonString); // Or JsonUtility.FromJson<PlayerData>(jsonString)
            }
            else
            {
                return new PlayerData(); // Or return null if no data exists
            }
        }
    }
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 4.0f;
    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 6.0f;
    [Tooltip("Rotation speed of the character")]
    public float RotationSpeed = 1f;
    public float lookSensitivityMultiplier = 1f;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    Vector3 dirVector = Vector3.zero;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity Value. The engine default is -9.81f")]
    public float Gravity = -9.81f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.1f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player B_Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = false;
    public bool LeftGrounded = false;
    public bool RightGrounded = false;
    public bool PersistentGrounded = false;

    [SerializeField] LayerMask landingMask;
    [SerializeField] float landingDistance = 1f;

    [SerializeField]
    public SphereCollider PersistentGroundedCollider;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;

    Coroutine AnticipateLandingCoroutine;

    // cinemachine
    private float _cinemachineTargetPitch;

    // init
    public LoadManager.State state;
    private Utility utils;

    //local player needs no body
    private SkinnedMeshRenderer playerRender;

    // player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = -53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // jump
    private float _decentModifier = 0f;
    [SerializeField] float _decentMultiplier = 1f;
    private Rigidbody _rb;

    // push
    public float pushStrength = 5000f;
    public float pushRange = 300f;
    private float pushCooldown = 0.5f;
    private float pushCooldownTimer = 0f;
    private float angleThreshold = 45f;

    // object throwing
    public float throwForce = 10f;
    public float pickupRange = 10f;
    private GameObject heldObject = null;
    private Rigidbody heldObjectRb = null;
    Ray ray = default;
    Vector3 _rayDir = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
    private UnityEngine.InputSystem.PlayerInput _playerInput;
#endif
    private CharacterController _controller;
    public AssetsInputs _input;
    private PlayerInformationManager pim;

    private const float _threshold = 0.01f;

    private bool IsCurrentDeviceMouse =>
#if ENABLE_INPUT_SYSTEM
            false;// _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif

    [Client]
    private void Awake()
    {
        state = LoadManager.State.Awake;
        utils = ServiceLocator.For(this).Get<Utility>();
        if (isServer)
        {
            blackboard.SetListValue(BlackboardController.BlackboardKeyStrings.Players, this.gameObject);
        }
        // get a reference to our main camera
    }

    [Client]
    private void Start()
    {
        followCamera = _followCamera;
        playerRender = GetComponentInChildren<SkinnedMeshRenderer>();
        pickupRange = 10f;  // Make sure it's always set to 10f when the game starts
        if (!isLocalPlayer) { return; }
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        _playerInput.enabled = true;
#else
        Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
        _rb = GetComponent<Rigidbody>();
        pim = GetComponent<PlayerInformationManager>();
        lookSensitivityMultiplier = pim.settings.mouseSensitivity.value * pim.settings.sensitivityMultiplier;
        _controller = GetComponent<CharacterController>();
        _controller.enabled = true;
        _input = GetComponent<AssetsInputs>();
        _input.enabled = true;

        _mainCamera = Instantiate(_mainCamera, this.gameObject.transform);
        _HUD = Instantiate(_HUD, this.gameObject.transform);
        _cameraRoot = this.gameObject.GetComponentInChildren<TargetDirectory>().GetTarget(TargetDirectory.TargetType.Camera).target.gameObject;
        _followCamera = Instantiate(_followCamera, _cameraRoot.transform.position, _followCamera.transform.rotation);
        _followCamera.transform.SetParent(_cameraRoot.transform);
        CinemachineCameraTarget = _cameraRoot;
        _mainCamera.GetComponent<Camera>().targetTexture = pim.settings.activeTexture;

        _rigContainer = Instantiate(_rigContainer, this.gameObject.transform);
        RigBuilder rigBuilder = GetComponent<RigBuilder>();
        TargetDirectory rigContainerTargetDir = _rigContainer.gameObject.GetComponent<TargetDirectory>();
        TargetDirectory playerdTargetDir = GetComponent<TargetDirectory>();
        TwoBoneIKConstraint rightArm = rigContainerTargetDir.GetTarget(TargetDirectory.TargetType.RightArm).target.GetComponent<TwoBoneIKConstraint>();
        TwoBoneIKConstraint leftArm = rigContainerTargetDir.GetTarget(TargetDirectory.TargetType.LeftArm).target.GetComponent<TwoBoneIKConstraint>();

        rightArm.data.root = playerdTargetDir.GetTarget(TargetDirectory.TargetType.RightArmRoot).target.transform;
        rightArm.data.mid = playerdTargetDir.GetTarget(TargetDirectory.TargetType.RightArmMid).target.transform;
        rightArm.data.tip = playerdTargetDir.GetTarget(TargetDirectory.TargetType.RightArmTip).target.transform;

        leftArm.data.root = playerdTargetDir.GetTarget(TargetDirectory.TargetType.LeftArmRoot).target.transform;
        leftArm.data.mid = playerdTargetDir.GetTarget(TargetDirectory.TargetType.LeftArmMid).target.transform;
        leftArm.data.tip = playerdTargetDir.GetTarget(TargetDirectory.TargetType.LeftArmTip).target.transform;

        rigBuilder.layers.Add(new RigLayer(_rigContainer.GetComponent<Rig>(), true)); 
        rigBuilder.enabled = true;
        rigBuilder.Build();

        lobbyController = ServiceLocator.For(this).Get<FizzyLobbyController>();
        InteractableBase.OnAuthorityGranted += InteractWithAuthority;
        state = LoadManager.State.Initialized;
        utils.CatchupOnEventActions();
        invertMovment = GetComponent<GamesPanel>();
        blackboard.SetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, this.gameObject);
        PlayerInformationManager infoManager = GetComponent<PlayerInformationManager>();
        PlayerInformationManager offlineInfoManager = utils.gameObject.GetComponent<PlayerInformationManager>() as PlayerInformationManager;
        infoManager.SetValues(offlineInfoManager);
        offlineInfoManager.enabled = false;
        utils.gameObject.GetComponent<InventoryManager>().enabled = false;
        ServiceLocator.Global.TryGet<LogSender>(out LogSender sender);
        sender?.cmdTest();
        //check if player is local, and top draw the arm if they are and body if not, awaiting on arm assets to finish. 
     
    }

    private void OnDestroy()
    {
        PlayerInformationManager infoManager = GetComponent<PlayerInformationManager>();
        PlayerInformationManager offlineInfoManager = utils.gameObject.GetComponent<PlayerInformationManager>() as PlayerInformationManager;
        offlineInfoManager.enabled = true;
        utils.gameObject.GetComponent<InventoryManager>().enabled = true;
        offlineInfoManager.SetValues(infoManager);

    }

    [Client]
    private void Update()
    {
        if (isLocalPlayer)
        {
            playerRender.enabled = false;
        }
        else
        {
            Debug.LogError("Not local player!");
        }

        Debug.DrawRay(ray.origin, _rayDir, Color.yellow, 5f, false);
        if (state != LoadManager.State.Initialized) { return; }

        if (!isLocalPlayer) { Destroy(this); return; }

        JumpAndGravity();
        Move();
        //if (_input.interact)
        //{
        //    InteractWithObject();
        //}

        if (pushCooldownTimer > 0f)
        {
            pushCooldownTimer -= Time.deltaTime;
        }

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.F))
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, pushRange, LayerMask.GetMask("Player")))
            {
                Debug.Log("Raycast hit: " + hit.collider.name);

                if (hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject)
                {
                    Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        Vector3 toTarget = hit.collider.transform.position - transform.position;
                        toTarget.y = 0;  // We only care about horizontal direction

                        float angle = Vector3.Angle(transform.forward, toTarget);
                        if (angle < angleThreshold)
                        {
                            CmdPushPlayer(hit.collider.gameObject, toTarget.normalized);
                        }
                        else
                        {
                            Debug.Log("Target is outside of the push angle.");
                        }
                    }
                }
                else
                {
                    Debug.Log("Hit object is not a player or it is the same player.");
                }
            }
        }
    }

    [Client]
    void InteractWithObject()
    {
        if (heldObject == null)
        {
            Vector3 dirToTarget = Camera.main.transform.forward;
            _rayDir = dirToTarget * pickupRange;
            ray = new Ray(Camera.main.transform.position, dirToTarget);

            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, pickupRange))
            {
                if (hit.collider.gameObject.CompareTag("Interactable"))
                {
                    if (hit.collider.gameObject.TryGetComponent<Rigidbody>(out heldObjectRb))
                    {
                        heldObject = hit.collider.gameObject;
                        //GetComponent<PlayerMenuInteractions>().Tool = heldObject.GetComponent<InteractableBase>(); 
                        CmdPickUpObject(heldObject);
                    }
                }
            }
        }
        //else
        //{
        //    ThrowObject();
        //}
    }

    [Command]
    void CmdPushPlayer(GameObject targetPlayer, Vector3 pushDirection)
    {
        if (targetPlayer.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            Debug.Log("Applying force to " + targetPlayer.name);

            targetRb.AddForce(pushDirection * pushStrength, ForceMode.VelocityChange);
        }

        RpcPushPlayer(targetPlayer, pushDirection);
    }

    [ClientRpc]
    void RpcPushPlayer(GameObject targetPlayer, Vector3 pushDirection)
    {
        if (targetPlayer.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            // Debug log to confirm force is applied on clients
            Debug.Log("Pushed " + targetPlayer.name + " on client");

            // Apply the force to the player in the same direction as the server
            targetRb.AddForce(pushDirection * pushStrength, ForceMode.VelocityChange);
        }
    }

    // Draw the push range for visual debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pushRange);
    }

    private void LateUpdate()
    {
        if (state != LoadManager.State.Initialized) { return; }

        if (!isLocalPlayer) { Destroy(this); return; }

        CameraRotation();
    }

    //void TryPickUp()
    //{
    //    Vector3 dirToTarget = Camera.main.transform.forward;
    //    _rayDir = dirToTarget * pickupRange;
    //    ray = new Ray(Camera.main.transform.position, dirToTarget);

    //    RaycastHit hit;
    //    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, pickupRange))
    //    {
    //        if (hit.collider.gameObject.CompareTag("Interactable"))
    //        {
    //            heldObject = hit.collider.gameObject;
    //            heldObjectRb = heldObject.GetComponent<Rigidbody>();

    //            if (heldObjectRb != null)
    //            {
    //                CmdPickUpObject(heldObject);
    //            }
    //        }
    //    }
    //}

    void ThrowObject()
    {
        CmdThrowObject(heldObject, transform.forward * throwForce);
        heldObject = null;
    }

    [Command]
    void CmdPickUpObject(GameObject objectToPickUp)
    {
        //Assign client authority to the player who picked it up
        objectToPickUp.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
        InteractableBase interactable = objectToPickUp.GetComponent<InteractableBase>();
        //this.GetComponentInChildren<PlayerMountPoint>().transform.position = objectToPickUp.transform.position;
        //interactable.Take(_cameraRoot);
    }

    void InteractWithAuthority(GameObject go)
    {
        if (heldObject == go)
        {
            //this.GetComponentInChildren<PlayerMountPoint>().transform.position = go.transform.position;
            go.GetComponent<InteractableBase>().Take(this.gameObject);
        }
    }

    //void InteractWithObject(GameObject go)
    //{
    //    if (heldObject == go)
    //    {
    //        go.transform.SetParent(Camera.main.transform, true);
    //        //go.GetComponent<Rigidbody>().isKinematic = true; // Disable physics for the object while held
    //    }
    //}

    //[Command]
    [Client]
    void CmdThrowObject(GameObject objectToThrow, Vector3 force)
    {
        if (objectToThrow == null) return;

        Vector3 throwDirection = Camera.main.transform.forward;

        // Un-parent the object and enable physics
        objectToThrow.transform.SetParent(null);
        Rigidbody objectRb = objectToThrow.GetComponent<Rigidbody>();
        objectRb.isKinematic = false;

        // Apply the force in the camera's forward direction
        objectRb.AddForce(throwDirection * force.magnitude, ForceMode.Impulse);

        // Remove authority from the client, as the object is being thrown
        NetworkIdentity objectNetworkIdentity = objectToThrow.GetComponent<NetworkIdentity>();
        objectNetworkIdentity?.RemoveClientAuthority();  // Remove authority from the client

        // Sync the throw action across clients
        //RpcThrowObject(objectToThrow, throwDirection * force.magnitude);
    }

    //[ClientRpc]
    //void RpcThrowObject(GameObject objectToThrow, Vector3 force)
    //{
    //    // This ensures the throw is seen on all clients
    //    if (objectToThrow != null)
    //    {
    //        Rigidbody objectRb = objectToThrow.GetComponent<Rigidbody>();
    //        objectRb.isKinematic = false;
    //        objectRb.AddForce(force, ForceMode.Impulse);
    //    }
    //}

    [Client]
    private void CameraRotation()
    {
        if (!pim.playerProperties.isDead)
        {
            float baseMultiplier = 4.0f * lookSensitivityMultiplier;  // You can adjust this multiplier to increase sensitivity
                                                                      //utils.DebugOut($"lookSensitivityMultiplier: {lookSensitivityMultiplier}");
                                                                      //utils.DebugOut($"baseMultiplier: {baseMultiplier}");
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * baseMultiplier * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * baseMultiplier * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                //utils.DebugOut($"TargetPitch: {_cinemachineTargetPitch}");
                transform.Rotate(Vector3.up * _rotationVelocity);
                //utils.DebugOut($"RotationVelocity: {_rotationVelocity}");
            }
        }
    }

    [Client]
    private void Move()
    {
        // Target speed based on sprinting or walking
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        pim.settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.F_Speed_Modifier, _input.sprint ? 2f : 1f);
        //PersistentGroundedCollider.isTrigger = _input.sprint;
        //PersistentGrounded = _input.sprint ? PersistentGrounded : false;

        // No input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // Current horizontal speed of the player
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        // Calculate acceleration and deceleration
        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // Smooth transition between current and target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // Normalize the movement input direction
        inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // Update the movement direction based on player input
        if (_input.move != Vector2.zero)
        {
            Vector3 X = transform.right * _input.move.x;
            Vector3 Y = transform.forward * _input.move.y;
            //if (pim.settings.invertX == 1)
            //{
            //    X = -transform.right * _input.move.x;
            //}
            //if (pim.settings.invertY == 1)
            //{
            //    Y = -transform.forward * _input.move.y;
            //}
            inputDirection = X + Y;
        }

        dirVector = (inputDirection.normalized * (_speed * Time.deltaTime)) + (new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        // Move the player using the CharacterController
        _controller.Move(dirVector);

    }

    [Client]
    private void JumpAndGravity()
    {
        if (Gravity != -9.81f)
        {
            Gravity = -9.81f; // Apply standard gravity if it is incorrectly set
        }

        if (!PersistentGrounded)
        {
            if (LeftGrounded == RightGrounded)
            {
                if (LeftGrounded || RightGrounded)
                {
                    Grounded = true;
                }
                else
                {
                    Grounded = false;
                }
            }
            else
            {
                Grounded = true;
            }
        }

        pim.settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.B_Grounded, Grounded);

        if (Grounded)
        {
            if (AnticipateLandingCoroutine != null)
            {
                StopCoroutine(AnticipateLandingCoroutine);
                AnticipateLandingCoroutine = null;
            }
            // Reset vertical velocity when grounded
            _decentModifier = 0f;
            _fallTimeoutDelta = FallTimeout;

            // Stop downward velocity when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -20f; // Prevent any initial downward speed if grounded
            }

            // Handle Jumping
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // The square root formula to calculate the initial jump velocity
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -30f * Gravity);  // T_Jump force
                pim.settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.T_Jump);
            }

            // Handle jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            AnticipateLandingCoroutine ??= StartCoroutine(AnticipateLanding());

            
            // If the player is not grounded, apply gravity
            _input.jump = false; // Don't allow jumping while airborne

            // Reset jump timeout if the player is in the air
            _jumpTimeoutDelta = JumpTimeout;

            // Apply gravity when in the air (increases fall speed over time)
            // This just increases speed over time
            if (_rb.linearVelocity.y <= 0.1f)
            {
                //going down
                _decentModifier += Time.deltaTime * _decentMultiplier;
            }

            if (_verticalVelocity > _terminalVelocity)
            {
                _verticalVelocity += 5 * Gravity * _decentModifier * Time.deltaTime;  // Apply gravity continuously
            }

            // Fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }

    public Transform GetCameraTransform() => _followCamera.transform;

    public IEnumerator AnticipateLanding()
    {
        while (!Grounded)
        {
            if (Physics.Raycast(transform.position, Vector3.down, landingDistance, landingMask))
            {
                //transition from `!B_Grounded` to `Landing` animation state
                if (!Grounded)
                {
                    Grounded = true;
                }

                pim.settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.B_Grounded, Grounded);
            }

            yield return null;
        }
    }

    public void OnLeftFootTriggerEnter(OnTriggerDelegation delegation)
    {
        LeftGrounded = true;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }

    public void OnLeftFootTriggerStay(OnTriggerDelegation delegation)
    {
        LeftGrounded = true;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }

    public void OnLeftFootTriggerExit(OnTriggerDelegation delegation)
    {
        LeftGrounded = false;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }

        public void OnRightFootTriggerEnter(OnTriggerDelegation delegation)
    {
        RightGrounded = true;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }

    public void OnRightFootTriggerStay(OnTriggerDelegation delegation)
    {
        RightGrounded = true;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }

    public void OnRightFootTriggerExit(OnTriggerDelegation delegation)
    {
        RightGrounded = false;
        string layer = LayerMask.LayerToName(delegation.Other.gameObject.layer);
        switch (layer)
        {
            case "ToolTip":
                return;
            case "Invisible":
                return;
            default:
                break;
        }
    }
    public void OnPersistentGroundedTriggerEnter(OnTriggerDelegation delegation) => PersistentGrounded = true;

    public void OnPersistentGroundedTriggerStay(OnTriggerDelegation delegation) => PersistentGrounded = true;

    public void OnPersistentGroundedTriggerExit(OnTriggerDelegation delegation) => PersistentGrounded = false;
}
