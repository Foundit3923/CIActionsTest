using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityServiceLocator;
using TMPro;

public class FizzyLobbyController : MonoBehaviour
{    //UI Elements
    //public TMP_Text LobbyNameText;

    //Player Data View
    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    //Other data
    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    private List<PlayerListItem> PlayerListItems = new();
    public PlayerNetworkCommunicator LocalPlayerController;
    private Utility utils;
    public string sceneName = "World";
    Vector3 spawnPosition = new(-48.5255966f, -0.306308746f, 7.43708611f);
    [SerializeField] private FizzyNetworkManager manager;
    [SerializeField] private SteamLobby steamLobby;

    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
        }
    }

    private void Awake()
    {
        //Manager = FizzyNetworkManager.Instance;  //ServiceLocator.Global.Get<FizzyNetworkManager>();
        ServiceLocator.Global.Register(this);
        LocalPlayerController = GetComponent<PlayerNetworkCommunicator>();
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();
        utils = ServiceLocator.For(this).Get<Utility>();
        utils.DebugOut("FizzyLobbyController Awake");
    }

    private void Start()
    {

    }

    public void UpdateLobbyName()
    {
        utils.DebugOut("FizzyLobbyControllerUpdateLobbyName");
        CurrentLobbyID = steamLobby.CurrentLobbyID;
        //LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");

    }

    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated) { CreateHostPlayerItem(); }

        if (PlayerListItems.Count < Manager.GamePlayers.Count) { CreateClientPlayerItem(); }

        if (PlayerListItems.Count > Manager.GamePlayers.Count) { RemovePlayerItem(); }

        if (PlayerListItems.Count == Manager.GamePlayers.Count) { UpdatePlayerItem(); }
    }

    public void FindLocalPlayer()
    {
        utils.DebugOut("FizzyLobbyController FindLocalPlayer");
        //TODO: Redo this to be better
        LocalPlayerObject = transform.root.gameObject;
        LocalPlayerController = LocalPlayerObject.GetComponentInChildren<PlayerNetworkCommunicator>();
    }

    public void StartGame(string SceneName)
    {
        utils.DebugOut("FizzyLobbyController StartGame");
        SceneName = sceneName;
        CanStartGame(sceneName);
    }
    public void CreateHostPlayerItem()
    {
        utils.DebugOut("FizzyLobbyController CreateHostPlayerItem");
        foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
        {
            GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

            NewPlayerItemScript.PlayerName = player.PlayerName;
            NewPlayerItemScript.ConnectionID = player.ConnectionID;
            NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
            NewPlayerItemScript.SetPlayerValues();

            NewPlayerItem.transform.SetParent(PlayerListViewContent.transform);
            NewPlayerItem.transform.localScale = Vector3.one;

            PlayerListItems.Add(NewPlayerItemScript);
        }

        PlayerItemCreated = true;
    }

    public void CreateClientPlayerItem()
    {
        utils.DebugOut("FizzyLobbyController CreateClientPlayerItem");
        foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
        {
            if (!PlayerListItems.Any(b => b.ConnectionID == player.ConnectionID))
            {
                GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
                PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

                NewPlayerItemScript.PlayerName = player.PlayerName;
                NewPlayerItemScript.ConnectionID = player.ConnectionID;
                NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
                NewPlayerItemScript.SetPlayerValues();

                NewPlayerItem.transform.SetParent(PlayerListViewContent.transform);
                NewPlayerItem.transform.localScale = Vector3.one;

                PlayerListItems.Add(NewPlayerItemScript);
            }
        }

        PlayerItemCreated = true;
    }

    public void UpdatePlayerItem()
    {
        utils.DebugOut("FizzyLobbyController UpdatePlayerItem");
        foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
        {
            foreach (PlayerListItem PlayerListItemScript in PlayerListItems)
            {
                if (PlayerListItemScript.ConnectionID == player.ConnectionID)
                {
                    PlayerListItemScript.PlayerName = player.PlayerName;
                    PlayerListItemScript.SetPlayerValues();
                }
            }
        }
    }

    public void RemovePlayerItem()
    {
        utils.DebugOut("FizzyLobbyController RemovePlayerItem");
        List<PlayerListItem> playerListItemToRemove = new();

        foreach (PlayerListItem playerListItem in PlayerListItems)
        {
            if (!Manager.GamePlayers.Any(b => b.ConnectionID == playerListItem.ConnectionID))
            {
                playerListItemToRemove.Add(playerListItem);
            }
        }

        if (playerListItemToRemove.Count > 0)
        {
            foreach (PlayerListItem playerListItem_ToRemove in playerListItemToRemove)
            {
                GameObject ObjectToRemove = playerListItem_ToRemove.gameObject;
                PlayerListItems.Remove(playerListItem_ToRemove);
                Destroy(ObjectToRemove);
                ObjectToRemove = null;
            }
        }
    }

    public void CanStartGame(string sceneName)
    {
        utils.DebugOut("FizzyLobbyController CanStartGame");
        LocalPlayerController.CmdCanStartGame(sceneName);
    }
}
