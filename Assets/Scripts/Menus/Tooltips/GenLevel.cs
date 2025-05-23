using UnityEngine;
using UnityServiceLocator;
using TMPro;
using System.Collections.Generic;
using Mirror;

public class GenLevel : Tooltip
{
    [SerializeField] public GameObject GrimmGenPrefab;
    [SerializeField] public TMP_Text toolTip;
    [SerializeField] public GameObject ParentObject;

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

    public override void PlayerInteraction(string button, GameObject player)
    {
        if (ServiceLocator.Global.TryGet<GrimmGen>(out GrimmGen generator))
        {
            generator.Shutdown();
            UnityEngine.Object.Destroy(generator);
        }

        GameObject go = UnityEngine.Object.Instantiate(GrimmGenPrefab);
        go.SetActive(true);
        go.transform.SetParent(ParentObject.transform);
        NetworkServer.Spawn(go);
    }
}
