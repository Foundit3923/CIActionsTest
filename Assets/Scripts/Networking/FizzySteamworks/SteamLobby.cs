using System;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

public class SteamLobby : MonoBehaviour
{
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequested;
    protected Callback<LobbyEnter_t> LobbyEntered;

    //Lobbies Callbacks
    protected Callback<LobbyMatchList_t> LobbyList;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdated;

    List<CSteamID> lobbyIDs = new();

    public ulong CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";
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

    //public TMP_Text LobbyNameText;

    public BlackboardController blackboardController;
    public Blackboard blackboard;
    public LoadManager.State state;
    public bool isPriority;
    [SerializeField] private Utility utils;

    //for player spawning and position. 
    public GameObject playerPrefab;
    public Vector3 spawnPosition;

    private void Awake()
    {
        state = LoadManager.State.Awake;
        DontDestroyOnLoad(this.gameObject);
        ServiceLocator.Global.Register<SteamLobby>(this);
        isPriority = false;
    }

    private void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        utils.DebugOut("SteamLobby Start");

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
        state = LoadManager.State.Started;
    }

    public void InitDependencies()
    {
        playerPrefab = Manager.playerPrefab;
        if (!SteamManager.Initialized) { utils.DebugOut("SteamManager Not Initialized"); throw new Exception("SteamManager Not Initialized"); }

        state = LoadManager.State.Initialized;
    }

    public void GetLobbiesList()
    {
        utils.DebugOut("SteamLobby GetLobbiesList");
        if (lobbyIDs.Count > 0) { lobbyIDs.Clear(); }

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(60);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnGetLobbyList(LobbyMatchList_t result)
    {
        utils.DebugOut("SteamLobby OnGetLobbyList");
        if (utils.EvalInitState(state, () => OnGetLobbyList(result))) { return; }

        LobbySelection menu = (LobbySelection)utils.GetCurrentMenu();
        if (menu.listOfLobbies.Count > 0)
        {
            menu.DestroyLobbies();
        }

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyId);
            SteamMatchmaking.RequestLobbyData(lobbyId);
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        utils.DebugOut("SteamLobby OnGetLobbyData");
        if (utils.EvalInitState(state, () => OnGetLobbyData(result))) { return; }

        if (SceneManager.GetActiveScene().name == "LobbyBrowser")
        {
            LobbySelection menu = (LobbySelection)utils.GetCurrentMenu();
            menu.DisplayLobbies(lobbyIDs, result);
        }
    }

    public void HostLobby()
    {
        utils.DebugOut("SteamLobby HostLobby");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, Manager.maxConnections);
        //startButton.SetActive(true);        
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        utils.DebugOut("SteamLobby OnLobbyCreated");
        if (utils.EvalInitState(state, () => OnLobbyCreated(callback))) { return; }

        if (callback.m_eResult != EResult.k_EResultOK) { return; }

        utils.DebugOut("Lobby created Successfully");

        Manager.StartHost();

        SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, HostAddressKey, SteamUser.GetSteamID().ToString());

        SteamMatchmaking.SetLobbyData((CSteamID)callback.m_ulSteamIDLobby, "name", SteamFriends.GetPersonaName().ToString() + "'s Lobby");
        //SpawnPlayer(playerPrefab, spawnPosition);
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        utils.DebugOut("SteamLobby OnJoinRequest");
        if (utils.EvalInitState(state, () => OnJoinRequest(callback))) { return; }

        utils.DebugOut("Request to join lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        utils.DebugOut("SteamLobby OnLobbyEntered");
        if (utils.EvalInitState(state, () => OnLobbyEntered(callback))) { return; }
        //Everyone
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        //SceneManager.LoadScene("World");
        //LobbyNameText.gameObject.SetActive(true);
        //LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name");

        //Clients
        if (NetworkServer.active) { return; }

        utils.DebugOut("SteamLobby OnLobbyEntered ClientsOnly");

        Manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        Manager.StartClient();
        //SpawnPlayer(playerPrefab, spawnPosition);
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        utils.DebugOut("SteamLobby JoinLobby");
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        if (memberCount > 0)
        {
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
                if (memberID != null && memberID != SteamUser.GetSteamID())
                {
                    SteamMatchmaking.JoinLobby(lobbyId);
                    return;
                }
            }

            utils.DebugOut("You're already in that lobby");
        }
        else
        {
            SteamMatchmaking.JoinLobby(lobbyId);
        }
    }

    //public void SpawnPlayer(GameObject playerPrefab, Vector3 spawnPosition)
    //{
    //    GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    //    NetworkServer.Spawn(newPlayer);
    //    Debug.Log($"Spawn player has ran! Spawned player at {spawnPosition}");
    //}

    public void OpenInviteDialog()
    {
        utils.DebugOut("SteamLobby OpenInviteDialog");
        SteamFriends.ActivateGameOverlayInviteDialog((CSteamID)CurrentLobbyID);
    }
    public void RequestLeaveLobby()
    {
        utils.DebugOut("SteamLobby RequestLeaveLobby");
        SteamMatchmaking.LeaveLobby((CSteamID)CurrentLobbyID);
    }
}