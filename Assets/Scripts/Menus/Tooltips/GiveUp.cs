using UnityEngine;
using UnityServiceLocator;
using TMPro;
using System.Collections.Generic;

public class GiveUp : Tooltip
{
    [SerializeField] public LevelEvaluator Scale;
    [SerializeField] public TMP_Text toolTip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void DoOnTriggerEnter(Collider other)
    {
        return;
    }

    public override void DoOnTriggerStay(Collider other)
    {
        return;
    }

    public override void DoOnTriggerExit(Collider other)
    {
        return;
    }

    public override void PlayerInteraction(string button, GameObject player) => Scale.PlayerInteraction();
}
