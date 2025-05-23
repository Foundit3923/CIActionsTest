using UnityEngine;
using UnityServiceLocator;
using TMPro;
using System.Collections.Generic;

public class Portal : Tooltip
{

    [SerializeField] public Portal targetPortal;

    [SerializeField] public TMP_Text toolTip;

    public bool cooldown;

    public bool enteringLevel;

    public float localFloorPos;

    BlackboardController blackboardController;

    Blackboard blackboard;

    BlackboardKey portalKey;

    public delegate void PlayerEnterLevelEvent();
    public static event PlayerEnterLevelEvent OnPlayerEnterLevel;

    public delegate void PlayerExitLevelEvent();
    public static event PlayerExitLevelEvent OnPlayerExitLevel;

    private void Awake()
    {
        enteringLevel = false;
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        BlackboardController.BlackboardKeyStrings key = default(BlackboardController.BlackboardKeyStrings);
        string name = transform.gameObject.name;
        if (transform.gameObject.tag == "ReceptionPortal")
        {
            key = BlackboardController.BlackboardKeyStrings.ReceptionPortal;
        }
        else if (transform.gameObject.tag == "LobbyPortal")
        {
            key = BlackboardController.BlackboardKeyStrings.LobbyPortal;
        }

        portalKey = blackboardController.GetKey(key);
        blackboard.SetValue(portalKey, this);
        cooldown = false;

    }
    public void Teleport(GameObject player)
    {
        //float newPlayerY = targetPortal.transform.parent.position.y;

        //player.transform.position = new Vector3(targetPortal.transform.position.x, newPlayerY, targetPortal.transform.position.z);

        float newPlayerY = targetPortal.transform.position.y + 0.5f;

        player.transform.position = new Vector3(targetPortal.transform.position.x, newPlayerY, targetPortal.transform.position.z);

        player.GetComponent<PlayerObjectController>().Grounded = false;

        Quaternion rotation = player.transform.rotation;

        player.transform.forward = -this.gameObject.transform.parent.right;
        //rotation = Quaternion.Euler(rotation.eulerAngles.x, targetPortal.transform.eulerAngles.y + 180, rotation.eulerAngles.z); ;

        //player.transform.rotation = rotation;

        if (transform.gameObject.tag == "LobbyPortal")
        {
            OnPlayerEnterLevel?.Invoke();
        }
        else if (transform.gameObject.tag == "ReceptionPortal")
        {
            OnPlayerExitLevel?.Invoke();
        }

        targetPortal.cooldown = true;

        //player.GetComponent<Transform>().rotation.SetEulerRotation(targetPortal.transform.eulerAngles.y + 180);

    }

    public override void DoOnTriggerEnter(Collider other)
    {

    }

    public override void DoOnTriggerStay(Collider other)
    {

    }

    public override void DoOnTriggerExit(Collider other) => cooldown = false;

    public override void PlayerInteraction(string button, GameObject player)
    {
        if (cooldown) { return; }

        if (player.transform.parent != null)
        {
            player = player.transform.root.gameObject;
        }

        Teleport(player);
    }
}
