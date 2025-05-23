using Mirror;
using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(NetworkIdentity))]
public class AuthorityRef : NetworkBehaviour
{
    NetworkIdentity identity;
    private void Awake()
    {
        DontDestroyOnLoad(this);
        ServiceLocator.Global.Register<AuthorityRef>(this);
        identity = gameObject.GetComponent<NetworkIdentity>();
    }
    public bool HasAuthority()
    {
        if (authority)
        {
            return true;
        }

        return false;
    }
}
