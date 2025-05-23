using TMPro;
using UnityEngine;

public class BuyTool : Tooltip
{
    [SerializeField] public TMP_Text toolTip;

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
    public override void PlayerInteraction(string button, GameObject player) => player.GetComponent<PlayerInformationManager>().playerProperties.AddToolToInventory(InventoryManager.InventoryItems.Flashlight);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
