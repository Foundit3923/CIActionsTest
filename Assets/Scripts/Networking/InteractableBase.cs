using Mirror;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnOwnerChanged))] public GameObject owner;

    public PlayerMountPoint mountpoint;

    public delegate void AuthorityGrantedEvent(GameObject go);
    public static event AuthorityGrantedEvent OnAuthorityGranted;

    public delegate void AuthorityRemovedEvent(GameObject go);
    public static event AuthorityRemovedEvent OnAuthorityRevoked;

    private void Start() => OnStart();

    void Update()
    {
        if (owner != null)
        {
            CmdUpdatePos();
        }

        OnUpdate();
    }

    public abstract void OnUpdate();
    public virtual void OnStart() { }

    [Command(requiresAuthority = false)]
    void CmdUpdatePos()
    {
        if (mountpoint != null)
        {
            this.transform.position = mountpoint.MountPoint.transform.position;
            this.transform.rotation = mountpoint.MountPoint.transform.rotation;
        }
    }

    [Command]
    void CmdSetOwner(GameObject owner)
    {
        //check viability etc yadayada no cheating
        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.excludeLayers = owner == null ? (LayerMask)0 : (LayerMask)LayerMask.GetMask("Player");
        rb.useGravity = owner == null ? true : false;
        rb.isKinematic = owner == null ? false : true;

        this.owner = owner;
    }

    public virtual void OnOwnerChanged(GameObject old, GameObject @new) => mountpoint = @new?.GetComponent<MountPointDirectory>().GetTarget(MountPointDirectory.MountPointTargetType.HeldItem).target;

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

    [Command]
    public virtual void CmdPlayerInteraction(PlayerMenuInteractions menuInteractions)
    {
    }
}

