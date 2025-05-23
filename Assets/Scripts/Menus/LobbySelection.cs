using System.Collections.Generic;
using System.Linq;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;
using UnityServiceLocator;
using MenuEnums;

namespace MenuEnums
{
    public static class LobbyBrowserMenuActions
    {
        public const MenuActions Host = MenuActions.Host;
        public const MenuActions GetListOfLobbies = MenuActions.GetListOfLobbies;
        public const MenuActions Return = MenuActions.Return;
        public const MenuActions Update = MenuActions.Update;
    }
}
public class LobbySelection : Menu
{
    private Utility utils;
    private SteamLobby steamLobby;
    [SerializeField] public GameObject lobbyDataItemPrefab;
    [SerializeField] public GameObject lobbyListContent;

    LoadManager.State state;

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            if (FizzyNetworkManager.singleton != null)
            {
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
            }

            return ServiceLocator.For(this).Get<FizzyNetworkManager>();
        }
    }
    private void Awake()
    {
        //Cursor.visible = true;
    }

    public List<GameObject> listOfLobbies = new();
    protected override void OnAwake()
    {
        state = LoadManager.State.Awake;
        utils = ServiceLocator.For(this).Get<Utility>();
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();

    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "LobbyBrowser" && state < LoadManager.State.Initialized)
        {
            InvokeRepeating("GetListOfLobbies", 0f, 30f);
            state = LoadManager.State.Initialized;
        }
    }

    protected override void OnStarting()
    {
        //ToggleCursor(true);
        utils = ServiceLocator.For(this).Get<Utility>();
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();
    }

    public void DestroyLobbies()
    {
        foreach (GameObject item in listOfLobbies)
        {
            Destroy(item);
        }

        listOfLobbies.Clear();
    }

    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        for (int i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID == result.m_ulSteamIDLobby)
            {

                GameObject createdItem = Instantiate(lobbyDataItemPrefab);

                createdItem.GetComponent<LobbyDataEntry>().lobbyID = (CSteamID)lobbyIDs[i].m_SteamID;

                createdItem.GetComponent<LobbyDataEntry>().lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");

                createdItem.GetComponent<LobbyDataEntry>().SetLobbyData();

                createdItem.transform.SetParent(lobbyListContent.transform);
                createdItem.transform.localScale = Vector3.one;
                bool dupes = listOfLobbies.Where(x => x.GetComponent<LobbyDataEntry>().lobbyName == createdItem.GetComponent<LobbyDataEntry>().lobbyName).ToList().Count > 0;
                if (dupes)
                {
                    Destroy(createdItem);
                }
                else
                {
                    listOfLobbies.Add(createdItem);
                }
            }
        }
    }

    public override void TakeAction(ButtonParams arg)
    {
        MenuActions action = arg.baseMenuAction;
        switch (action)
        {
            case MenuActions.Host:
                utils.ButtonInteraction(Utility.ButtonActions.Host);
                break;
            case MenuActions.GetListOfLobbies:
                steamLobby.GetLobbiesList();
                break;
            case MenuActions.Return:
                SceneManager.LoadScene(Manager.offlineScene);
                break;
            case MenuActions.Update:
                GetListOfLobbies();
                break;
            default:
                break;
        }
    }

    public void GetListOfLobbies() => steamLobby.GetLobbiesList();

    private void OnDestroy()
    {
        //Cursor.visible = false;
    }
}
