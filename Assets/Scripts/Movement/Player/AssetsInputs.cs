using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class AssetsInputs : MonoBehaviour
{
    public InputActionAsset asset;

    [Header("Character input values")]
    public Vector2 move;
    public Vector2 look;
    public Vector2 pauseLook;
    public bool jump;
    public bool sprint;
    public bool crouch;
    public bool pause;
    public bool interact;
    public bool spawnObject;
    public bool interactWithTool;
    public bool dropTool;
    public bool spectateNext;
    public bool spectatePrevious;

    InputAction pauseAction;
    InputAction interactAction;
    InputAction spawnAction;
    InputAction interactWithToolAction;
    InputAction dropToolAction;
    InputAction spectateNextAction;
    InputAction spectatePreviousAction;


    [Header("Movement settings")]
    public bool analogMovement;

    [Header("Mouse cursor settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    [Header("Animation settings")]
    private AnimatorRef __animatorRef;
    private AnimatorRef _animatorRef
    {
        get
        {
            if (__animatorRef != null)
            {
                return __animatorRef;
            }

            return __animatorRef = _pim.settings.AnimatorRef;
        }
    }
    private PlayerInformationManager __pim;
    private PlayerInformationManager _pim
    {
        get
        {
            if (__pim != null)
            {
                return __pim;
            }

            return __pim = GetComponent<PlayerInformationManager>();
        }
    }

    public float walkSpeed = 1f;
    public float runSpeed = 2f;

#if ENABLE_INPUT_SYSTEM

    public void OnMove(InputValue value) => MoveInput(value.Get<Vector2>());

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value) => JumpInput(value.isPressed);

    public void OnSprint(InputValue value) => SprintInput(value.isPressed);

    public void OnCrouch(InputValue value) => CrouchInput(value.isPressed);

    private void Start()
    {
        pauseAction = asset.FindAction("Pause");
        interactAction = asset.FindAction("Interact");
        spawnAction = asset.FindAction("SpawnObject");
        interactWithToolAction = asset.FindAction("InteractWithTool");
        dropToolAction = asset.FindAction("DropTool");
        spectateNextAction = asset.FindAction("SpectateNext");
        spectatePreviousAction = asset.FindAction("SpectatePrevious");
    }

    [Client]
    private void Update()
    {
        // --- Input-based actions ---
        if (pauseAction.WasPressedThisFrame() && !pause)
        {
            pause = true;
        }
        else if (pause) { pause = false; }

        if (interactAction.WasPressedThisFrame() && !interact)
        {
            interact = true;
        }
        else if (interact) { interact = false; }

        if (spawnAction.WasPressedThisFrame() && !spawnObject)
        {
            spawnObject = true;
        }
        else if (spawnObject) { spawnObject = false; }

        if (interactWithToolAction.WasPressedThisFrame() && !interactWithTool)
        {
            interactWithTool = true;
        }
        else if (interactWithTool) { interactWithTool = false; }

        if (dropToolAction.WasPressedThisFrame() && !dropTool)
        {
            dropTool = true;
        }
        else if (dropTool) { dropTool = false; }

        if (spectateNextAction.WasPressedThisFrame() && !spectateNext)
        {
            spectateNext = true;
        }
        else if (spectateNext) { spectateNext = false; }

        if (spectatePreviousAction.WasPressedThisFrame() && !spectatePrevious)
        {
            spectatePrevious = true;
        }
        else if (spectatePrevious) { spectatePrevious = false; }

        // --- Animation movement logic ---
        if (_animatorRef.Animator != null)
        {
            float speedMultiplier = sprint ? runSpeed : walkSpeed;

            _animatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.F_Move_X, move.x * speedMultiplier);
            _animatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.F_Move_Y, move.y * speedMultiplier);
        }
    }
#endif

    public void MoveInput(Vector2 newMoveDirection) => move = newMoveDirection;

    public void LookInput(Vector2 newLookDirection) => look = newLookDirection;

    public void JumpInput(bool newJumpState) => jump = newJumpState;

    public void SprintInput(bool newSprintState) => sprint = newSprintState;

    public void CrouchInput(bool newCrouchState) => crouch = newCrouchState;

    public void SetCameraFromMouseConnection(bool state)
    {
        if (state)
        {
            if (pauseLook != null)
            {
                look = pauseLook;
            }

            cursorInputForLook = true;
        }
        else
        {
            cursorInputForLook = false;
            pauseLook = look;
            look = Vector2.zero;
        }
    }
}
