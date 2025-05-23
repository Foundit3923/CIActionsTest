using Mirror;
using UnityEngine;

public class VerifyAuthority : NetworkBehaviour
{
    public delegate void NewAuthorityGrantedEvent(GameObject go);
    public static event NewAuthorityGrantedEvent OnAuthorityGranted;

    Rigidbody rb;
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        OnAuthorityGranted(this.gameObject);
        rb = this.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        this.GetComponent<Rigidbody>().isKinematic = true;
        rb.useGravity = true;
    }
}
