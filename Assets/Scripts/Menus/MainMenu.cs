using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using MenuEnums;
using UnityEngine.EventSystems;

namespace MenuEnums
{
    public static class MainMenuActions
    {
        public const MenuActions Start = MenuActions.Start;
        public const MenuActions Settings = MenuActions.Settings;
        public const MenuActions Credits = MenuActions.Credits;
        public const MenuActions Quit = MenuActions.Quit;
    }
}

public class MainMenu : Menu
{
    [SerializeField] private SteamLobby steamLobby;
    [SerializeField] private ulong steamId;
    [SerializeField] private string lobbyIdString;
    private Utility utils;
    private ulong? lobbyId;
    public GameObject lobbyListPanel;
    public TMP_Text lobbyNameText, playerCountText;
    //private List<MenuActions> MainMenuActions = new();

    protected override void OnAwake() => utils = ServiceLocator.For(this).Get<Utility>();

    //where the menus actually go to. 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void OnStarting()
    {
    }

    public override void TakeAction(ButtonParams arg)
    {
        MenuActions action = arg.baseMenuAction;
        switch (action)
        {
            case MenuActions.Start:
                SceneManager.LoadScene("LobbyBrowser");
                break;
            case MenuActions.Settings:
                SceneManager.LoadScene("SettingsMenu");
                break;
            case MenuActions.Credits:
                SceneManager.LoadScene("Credits");
                break;
            case MenuActions.Quit:
                Application.Quit();
                break;
            default:
                break;
        }
    }

    public void OnStartHover()
    {

    }
}
