using UnityEngine;
using Mirror;

public class ServerPhysicsOnly : NetworkBehaviour
{
    void Start()
    {
        if (!isServer)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }
}
