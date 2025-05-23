using MenuEnums;
using UnityEngine;
using UnityServiceLocator;

public class HUDMenu : Menu
{
    [SerializeField] private Utility utils;
    [SerializeField] private ulong steamId;
    [SerializeField] private string lobbyIdString;
    private ulong? lobbyId;

    public override void TakeAction(ButtonParams action)
    {
    }

    protected override void OnAwake() => utils = ServiceLocator.For(this).Get<Utility>();//lobbyInfo = ServiceLocator.For(this).Get<LobbySaver>();

    protected override void OnStarting() => throw new System.NotImplementedException();

    //where the menus actually go to. 
    // Start is called once before the first execution of Update after the MonoBehaviour is created

}
