using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
public class PropRoot : MonoBehaviour
{
    void Awake()
    {
        NetworkIdentity[] identities = GetComponentsInChildren<NetworkIdentity>();
        if (identities != null && identities.Length > 1)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
