using System.Collections;
using Mirror;
using UnityEngine;
using UnityServiceLocator;

public class NetworkRotateOpenClose : NetworkBehaviour
{
    [SyncVar] GameObject owner = default(GameObject);
    [SyncVar] public bool isOpen;
    [SyncVar] public bool rotating;
    private float OpenAngle;
    private float CloseAngle;
    private float duration;
    private Tooltip tooltip;
    private Utility utils;

    public delegate void AuthorityGrantedEvent(GameObject go);
    public static event AuthorityGrantedEvent OnAuthorityGranted;

    public delegate void AuthorityRemovedEvent(GameObject go);
    public static event AuthorityRemovedEvent OnAuthorityRevoked;

    private void Awake() => init();

    void init()
    {
        isOpen = false;
        rotating = false;
        utils = ServiceLocator.Global.Get<Utility>();
    }

    [Command(requiresAuthority = false)]
    void CmdRegister(GameObject player) => GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);

    [Command(requiresAuthority = false)]
    void CmdSetOwner(GameObject owner) => this.owner = owner;

    [Client]
    void ToggleOpenClose(GameObject owner, Tooltip _tooltip, float openAngle, float closeAngle, float duration)
    {
        if (rotating) { return; }

        tooltip = _tooltip;
        utils.DebugOut("Calling Command: CmdToggleOpenClose");
        CmdToggleOpenClose(openAngle, closeAngle, duration);
    }

    [Command(requiresAuthority = false)]
    void CmdToggleOpenClose(float openAngle, float closeAngle, float _duration, NetworkConnectionToClient sender = null)
    {
        utils.DebugOut("DebugOut: Running CmdToggleOpenClose");
        OpenAngle = openAngle;
        CloseAngle = closeAngle;
        duration = _duration;

        float angle = OpenAngle;
        if (isOpen)
        {
            angle = CloseAngle;
        }

        StartCoroutine(RotateObject(this.gameObject, Vector3.right, angle, duration));
        return;
    }

    private IEnumerator RotateObject(GameObject objectToRotate, Vector3 axis, float angle, float duration)
    {
        if (rotating)
        {
            yield break;
        }

        rotating = true;

        Quaternion from = objectToRotate.transform.rotation;

        Quaternion to = objectToRotate.transform.rotation;

        to *= Quaternion.Euler(axis * angle);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            objectToRotate.transform.rotation = Quaternion.Slerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        objectToRotate.transform.rotation = to;
        isOpen = isOpen ? false : true;
        rotating = false;
    }

    public GameObject GetOwner(GameObject player) => owner;
    public void Register(GameObject player) => CmdRegister(player);
    public void Take(GameObject owner) => CmdSetOwner(owner);
    public void Release() => CmdSetOwner(null);
    public void Toggle(GameObject owner, Tooltip tooltip, float openAngle, float closeAngle, float duration) => ToggleOpenClose(owner, tooltip, openAngle, closeAngle, duration);

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        OnAuthorityGranted(this.gameObject);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        OnAuthorityRevoked(this.gameObject);
    }
}
