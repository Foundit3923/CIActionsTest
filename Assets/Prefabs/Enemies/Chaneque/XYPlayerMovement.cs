using System.Runtime.CompilerServices;
using UnityEngine;

public class XYPlayerMovement : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    //movement 
    [Range(5, 50)] public float moveSpeed = 25; //this can be further adjusted and changed as needed based on input from Connor. 
    public GameObject cameraPosition;
    //ground check and to add drag for the player to slow down more effectively. 
    [Range(5, 20)] public float groundDrag; //do not think that this will ever need to be over 20, maybe for running but that should be factored in when calculating the movement then. 
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    //to check and control jump aspects of the player
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public bool readyToJump;
    public Chaneque monster;

    //orientation transform for player to move in  the direction that they are looking and based of the movement of the camera
    public Transform orientation;

    //to get the inputs from the controller (at least the keyboard inputs, controllers set up later if not possible with axis now)
    float horizontalInput;
    float verticalInput;

    //variable for the direction of movement
    Vector3 moveDirection;

    //variable for access to the rigibody or the think that actually moves the player
    Rigidbody playerRig;

    private void Start()
    {
        cameraPosition = GameObject.FindGameObjectWithTag("CameraPosition");
        //to declare the variable that will be used for the physics of movement
        playerRig = GetComponent<Rigidbody>();
        //to freeze rotation so the player is not faling on its face everytime it moves, this could be adjusted for later when drinks are added to the lobby and the player 
        //gets drinks and has to many, unfreeze rotation and allow the player to stumble and fall like a drunken mess. 
        playerRig.freezeRotation = true;
        if (monster != null)
        {
            monster = monster.GetComponent<Chaneque>();
        }
        else
        {
            Debug.LogError("Chaneque GameObject is not assigned.");
        }
    }
    private void Update()
    {
        //run the physics of the inputs every frame, I think I may move this later and put it into fixedUpdatte, if it does not run smoothly in here. 
        GetKeyboardInputs();
        SpeedControl();
        //check to see if the player is grounded and need drag applied
        grounded = Physics.Raycast(transform.position, Vector3.down, (playerHeight * 0.5f) + 0.2f, whatIsGround);

        //handle drag
        if (grounded)
        {
            playerRig.linearDamping = groundDrag;
        }
        else
        {
            playerRig.linearDamping = 0;
        }

        //when to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void FixedUpdate() => MovePlayer();
    private void GetKeyboardInputs()
    {
        //get the input from the axis, use raw as it is either a 0 or 1 Value. Later when adding controller schemes this may need to get changed or use it a different way, not sure yet????
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void MovePlayer()
    {
        if (monster.playerShrunk == true)
        {
            moveSpeed *= 2;
            monster.playerShrunk = false;
            Vector3 newPosition = cameraPosition.transform.position;
            newPosition.y -= 1f;
            cameraPosition.transform.position = newPosition;
        }
        //make the calculation of the movement for the player 
        moveDirection = (orientation.forward * verticalInput) + (orientation.right * horizontalInput);
        if (grounded)
        {
            //add the force of movement, normalize it first should make it less skating ring feeling
            playerRig.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
        }
        else if (!grounded)
        {
            playerRig.AddForce(moveDirection.normalized * moveSpeed * airMultiplier, ForceMode.Force);
        }
    }

    //to adjust the speed of the player and keep it within the bounds set
    private void SpeedControl()
    {
        Vector3 velocityNormalized = new(playerRig.linearVelocity.x, 0f, playerRig.linearVelocity.z);
        //this is to limit the velocity if needed
        if (velocityNormalized.magnitude > moveSpeed)
        {
            Vector3 velocityLimited = velocityNormalized.normalized * moveSpeed;
            playerRig.linearVelocity = new Vector3(velocityLimited.x, playerRig.linearVelocity.y, velocityLimited.z);
        }
    }

    private void Jump()
    {
        //resest the y velocity 
        playerRig.angularVelocity = new Vector3(playerRig.angularVelocity.x, 0f, playerRig.angularVelocity.z);

        //add the force to jump but use impulse to apply it only once
        playerRig.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump() => readyToJump = true;
}
