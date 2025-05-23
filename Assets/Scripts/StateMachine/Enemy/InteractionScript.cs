using UnityEngine;

public class InteractionScript : MonoBehaviour
{
    //interaction key 
    public KeyCode interactKey = KeyCode.E;
    public float interactArea = .8f;
    public Renderer interactionObject;
    public bool canInteract = false;
    private EnemyControllerStateMachine fsm;
    private Rigidbody rb;
    private Collider lastKnownCollision;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lastKnownCollision = other;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => rb = GetComponent<Rigidbody>();

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(interactKey) && canInteract)
        {
            Interact();
        }
    }

    void Interact()
    {
        //interactionObject.material.color = Color.black;
        Debug.Log("Player can interact with object: " + gameObject.name);
        Vector3 colliderPoint = lastKnownCollision.ClosestPoint(rb.transform.position);
        Vector3 collisionDirection = colliderPoint - rb.position;
        collisionDirection.Normalize();
        float dotProduct = Vector3.Dot(rb.transform.forward, collisionDirection);
        Debug.Log("Dot Product: " + dotProduct);

        if (dotProduct < -interactArea)
        {
            EnemyControllerContext _context = rb.GetComponent<EnemyControllerStateMachine>().getContext();
            _context.setInterrupted(true);
            _context.setInterruptTag("Player");
            _context.setDead(true);
        }
    }
}
