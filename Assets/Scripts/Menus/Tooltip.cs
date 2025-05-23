using System.Collections.Generic;
using Mirror;
using TriggerDelegator;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;

public abstract class Tooltip : MonoBehaviour
{
    public List<GameObject> playersInRange = new();

    [SerializeField] public bool isFreeFloating = false;
    [SerializeField] public bool runAsCommand = false;
    public GameObject PropRoot;

    public Utility utils;

    private void Awake()
    {
        utils = ServiceLocator.Global.Get<Utility>();

        PropRoot = null;
        Transform parent = transform;
        Transform current = null;
        while (PropRoot == null)
        {
            current = parent;
            parent = current.parent;
            if (parent == null)
            {
                PropRoot = this.transform.parent.gameObject;
                break;
            }

            if (parent.gameObject.TryGetComponent<PropRoot>(out PropRoot propRoot))
            {
                PropRoot = propRoot.gameObject;
            }
        }

        NetworkIdentity[] identities = PropRoot.GetComponentsInChildren<NetworkIdentity>();
        if (identities != null && identities.Length == 0)
        {
            this.gameObject.AddComponent<NetworkIdentity>();
        }

        this.gameObject.layer = LayerMask.NameToLayer("Invisible");
        foreach (Transform child in transform)
        {
            child.gameObject.layer = this.gameObject.layer;
        }

        this.gameObject.GetComponent<Collider>().enabled = true;
        this.gameObject.GetComponent<Collider>().isTrigger = true;
    }

    public void OnProximityTriggerEnter(OnTriggerDelegation delegation)
    {
        if (delegation.Other.tag != "Player") { return; }

        if (playersInRange.Contains(delegation.Other.gameObject)) { return; }

        playersInRange.Add(delegation.Other.gameObject);
        delegation.Other.gameObject.GetComponentInChildren<PlayerMenuInteractions>().RegisterToolTip(this);
        DoOnTriggerEnter(delegation.Other);
    }

    public void OnProximityTriggerStay(OnTriggerDelegation delegation)
    {
        if (delegation.Other.tag != "Player") { return; }

        if (!playersInRange.Contains(delegation.Other.gameObject)) { return; }

        if (isFreeFloating)
        {
            if (delegation.Other.gameObject.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity) && identity.isLocalPlayer)
            {
                this.transform.forward = (this.transform.position - delegation.Other.GetComponent<PlayerObjectController>().GetCameraTransform().position).normalized;
            }
        }

        DoOnTriggerStay(delegation.Other);
    }

    public void OnProximityTriggerExit(OnTriggerDelegation delegation)
    {
        if (delegation.Other.tag != "Player") { return; }

        if (!playersInRange.Contains(delegation.Other.gameObject)) { return; }

        playersInRange.Remove(delegation.Other.gameObject);
        delegation.Other.gameObject.GetComponentInChildren<PlayerMenuInteractions>().DeregisterToolTip(this);
        DoOnTriggerExit(delegation.Other);
    }

    public abstract void DoOnTriggerEnter(Collider other);
    public abstract void DoOnTriggerStay(Collider other);
    public abstract void DoOnTriggerExit(Collider other);

    public abstract void PlayerInteraction(string button, GameObject player);

}
