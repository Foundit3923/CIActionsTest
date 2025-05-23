using System.Collections;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class RotateOpenClose : Tooltip
{
    [SerializeField] public GameObject door;
    [SerializeField] public float OpenAngle;
    [SerializeField] public float CloseAngle;
    [SerializeField] public float duration;
    [SerializeField] public TMP_Text toolTipText;
    [SerializeField] public string OpenText;
    [SerializeField] public string CloseText;
    public NetworkRotateOpenClose networkDoor;
    private GameObject playerWithAuthority;

    private void Start()
    {
        //networkDoor = PropRoot.GetComponentInChildren<NetworkRotateOpenClose>();
        //door = networkDoor.gameObject;
    }

    private void Update()
    {
        if (PropRoot == null)
        {
            utils.GetNearestPropRoot(this.gameObject);
        }

        if (networkDoor == null)
        {
            networkDoor = PropRoot.GetComponentInChildren<NetworkRotateOpenClose>();
        }

        if (networkDoor != null && networkDoor.isOpen)
        {
            toolTipText.text = CloseText;
        }
        else
        {
            toolTipText.text = OpenText;
        }
    }
    public override void DoOnTriggerEnter(Collider other)
    {
        return;
    }

    public override void DoOnTriggerExit(Collider other)
    {
        return;
    }

    public override void DoOnTriggerStay(Collider other)
    {
        return;
    }

    public override void PlayerInteraction(string button, GameObject player)
    {
        if (networkDoor != null)
        {
            utils.DebugOut("Calling networkDoor.Toggle");
            networkDoor.Toggle(player, this, OpenAngle, CloseAngle, duration);
            //playerWithAuthority = player;
            //networkDoor.Register(player);
            //player.GetComponent<PlayerNetworkCommunicator>().CmdAssignClientAuthority(networkDoor.gameObject);
            //networkDoor.GetComponent<NetworkIdentity>().AssignClientAuthority(player.GetComponent<NetworkIdentity>().connectionToClient);
        }
        else
        {
            if (PropRoot != null)
            {
                utils.GetNearestPropRoot(this.gameObject);
            }

            networkDoor = PropRoot.GetComponentInChildren<NetworkRotateOpenClose>();
            if (networkDoor != null)
            {
                utils.DebugOut("Calling networkDoor.Toggle");
                networkDoor.Toggle(player, this, OpenAngle, CloseAngle, duration);
            }
            //door = networkDoor.gameObject;

            //if (door.TryGetComponent<NetworkRotateOpenClose>(out networkDoor))
            //{

            //}
        }
    }

    //void InteractWithAuthority(GameObject go)
    //{
    //    if (networkDoor.gameObject == go && playerWithAuthority != null)
    //    {
    //        networkDoor.Take(playerWithAuthority);
    //        networkDoor.Toggle(playerWithAuthority, this, OpenAngle, CloseAngle, duration);
    //    }
    //}

    //void EndInteraction(GameObject go)
    //{
    //    if (networkDoor.gameObject == go)
    //    {
    //        playerWithAuthority = null;
    //        networkDoor.Release();
    //    }
    //}
}
