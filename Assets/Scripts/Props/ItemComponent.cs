using Mirror;
using UnityEngine;

public abstract class ItemComponent : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnOwnerChanged))] GameObject owner;

    PlayerMountPoint mountpoint;

    public delegate void AuthorityGrantedEvent(GameObject go);
    public static event AuthorityGrantedEvent OnAuthorityGranted;

    public delegate void AuthorityRemovedEvent(GameObject go);
    public static event AuthorityRemovedEvent OnAuthorityRevoked;

    void Update()
    {
        CmdUpdatePos();
        OnUpdate();
    }

    public abstract void OnUpdate();

    [Command(requiresAuthority = false)]
    void CmdUpdatePos()
    {
        if (owner != null)
        {
            this.transform.position = mountpoint.MountPoint.transform.position;
            this.transform.rotation = mountpoint.MountPoint.transform.rotation;
        }
    }

    [Command]
    void CmdSetOwner(GameObject owner)
    {
        //check viability etc yadayada no cheating
        this.owner = owner;
        if (!this.GetComponent<Rigidbody>().isKinematic) { this.GetComponent<Rigidbody>().isKinematic = true; }
    }

    void OnOwnerChanged(GameObject old, GameObject @new) => mountpoint = owner?.GetComponentInChildren<PlayerMountPoint>();

    public void Take(GameObject owner) => CmdSetOwner(owner);
    public void Release() => CmdSetOwner(null);

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        OnAuthorityGranted?.Invoke(this.gameObject);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        OnAuthorityRevoked?.Invoke(this.gameObject);
    }
}

