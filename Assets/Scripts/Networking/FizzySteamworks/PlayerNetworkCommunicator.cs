using Mirror;
using UnityEngine;
using Steamworks;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using System.Collections;
using Unity.VisualScripting;
using System.Linq;

public class PlayerNetworkCommunicator : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerGameObjectNameUpdate))] public string GameObjectName;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;

    private Utility utils;
    private FizzyLobbyController lobbyController;
    private IEnumerator coroutine;

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

            return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
        }
    }

    private void Awake() => state = LoadManager.State.Awake;

    private void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        coroutine = GetLobbyController(2.0f);
        StartCoroutine(coroutine);
        //lobbyController = ServiceLocator.For(this).Get<FizzyLobbyController>();
        //state = LoadManager.State.Initialized;
        utils.DebugOut("PlayerNetworkCommunicator Start");
        //utils.CatchupOnEventActions();
    }

    private IEnumerator GetLobbyController(float waitTime)
    {
        while (lobbyController == null)
        {
            if (ServiceLocator.For(this).TryGet<FizzyLobbyController>(out lobbyController))
            {
                state = LoadManager.State.Initialized;
                utils.CatchupOnEventActions();
                break;
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    public override void OnStartAuthority()
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => OnStartAuthority())) { return; }

        utils.DebugOut("PlayerNetworkCommunicator OnStartAuthority");
        state = LoadManager.State.Authorized;
        //if (state != LoadManager.State.Initialized)
        //{
        //    _utils = ServiceLocator.For(this).Get<Utility>();
        //    lobbyController = ServiceLocator.For(this).Get<FizzyLobbyController>();
        //    state = LoadManager.State.Initialized;
        //}
        string name = SteamFriends.GetPersonaName().ToString();
        CmdSetPlayerName(name);
        CmdPlayerGameObjectNameUpdate("LocalGamePlayer");
        lobbyController.FindLocalPlayer();
        lobbyController.UpdateLobbyName();
    }
    public override void OnStartServer()
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => OnStartServer())) { return; }

        if (!Manager.GamePlayers.Contains(this))
        {
            Manager.GamePlayers.Add(this);
        }

        lobbyController.UpdateLobbyName();
        lobbyController.UpdatePlayerList();
    }

    public override void OnStartClient()
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => OnStartClient())) { return; }

        utils.DebugOut("PlayerNetworkCommunicator OnStartClient");
        //if (state != LoadManager.State.Initialized)
        //{
        //    _utils = ServiceLocator.For(this).Get<Utility>();
        //    lobbyController = ServiceLocator.For(this).Get<FizzyLobbyController>();
        //    state = LoadManager.State.Initialized;
        //}
        if (!Manager.GamePlayers.Contains(this))
        {
            Manager.GamePlayers.Add(this);
        }

        lobbyController.UpdateLobbyName();
        lobbyController.UpdatePlayerList();
    }
    public override void OnStopClient()
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => OnStopClient())) { return; }

        utils.DebugOut("PlayerNetworkCommunicator OnStopClient");
        Manager.GamePlayers.Remove(this);
        lobbyController.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => CmdSetPlayerName(PlayerName), true)) { return; }

        utils.DebugOut("PlayerNetworkCommunicator CmdSetPlayerName");
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => PlayerNameUpdate(OldValue, NewValue))) { return; }

        utils.DebugOut("PlayerNetworkCommunicator PlayerNameUpdate");
        if (isServer)
        {
            utils.DebugOut("PlayerNetworkCommunicator PlayerNameUpdate: this is server");
            this.PlayerName = NewValue;
        }

        if (isClient)
        {
            utils.DebugOut("PlayerNetworkCommunicator PlayerNameUpdate: this is client");
            lobbyController.UpdatePlayerList();
        }
    }

    [Command]
    public void CmdPlayerGameObjectNameUpdate(string GameObjectName)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => CmdPlayerGameObjectNameUpdate(GameObjectName), true)) { return; }

        utils.DebugOut("PlayerNetworkCommunicator CmdPlayerGameObjectNameUpdate");
        this.PlayerGameObjectNameUpdate(this.GameObjectName, GameObjectName);
    }

    public void PlayerGameObjectNameUpdate(string OldValue, string NewValue)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => PlayerGameObjectNameUpdate(OldValue, NewValue))) { return; }

        utils.DebugOut("PlayerNetworkCommunicator PlayerGameObjectNameUpdate");
        if (isClient)
        {
            utils.DebugOut("PlayerNetworkCommunicator PlayerGameObjectNameUpdate: this is client");
            this.GameObjectName = NewValue;
            this.gameObject.name = this.GameObjectName;
        }
    }

    [Command]
    public void CmdCanStartGame(string sceneName)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => CmdCanStartGame(sceneName), true)) { return; }

        utils.DebugOut("PlayerNetworkCommunicator CmdCanStartGame");
        StartGame(sceneName);
    }

    public void StartGame(string sceneName)
    {
        utils.DebugOut("PlayerNetworkCommunicator StartGame");
        NetworkServer.isLoadingScene = true;
        SceneManager.LoadScene(sceneName);

        //blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        //blackboard = blackboardController.GetBlackboard();
        //spawnerKey = blackboard.GetOrRegisterKey("PlayerLobbySpawn");
        //blackboard.SetValue(spawnerKey, spawnerPoint);
        //NetworkManager.startPositions = new List<Transform> { spawnerPoint };
        //foreach (var position in Manager.spawnPositions)
        //{
        //    GameObject spawnPoint = new GameObject("SpawnPoint");
        //    spawnPoint.transform.position = (Vector3)position;
        //    NetworkManager.startPositions.Add(spawnPoint.transform);
        //    Debug.Log($"{Manager.spawnPositions} spawn positions");
        //}
    }

    [Command]
    public void CmdSpawnNetworkObjects()
        //utils.DebugOut($"CmdSpawnNetworkObjects");
        => StartCoroutine(SpawnObject());

    private IEnumerator SpawnObject()
    {
        GameObject spawnedObject = Instantiate(Manager.chairPrefab, gameObject.transform.position + (gameObject.transform.forward * 5), gameObject.transform.rotation);

        // Spawn the object on the network (server-side).
        NetworkServer.Spawn(spawnedObject);
        // Ensure the server owns the object when it spawns.
        NetworkIdentity networkIdentity = spawnedObject.GetComponent<NetworkIdentity>();
        networkIdentity.visibility = Visibility.ForceShown;
        utils.DebugOut($"Identity: {networkIdentity.assetId}, Observers: {networkIdentity.observers.ToCommaSeparatedString()}");
        if (networkIdentity.observers.Count > 0)
        {
            utils.DebugOut($"observer name: {networkIdentity.observers[0].identity.gameObject.name}");
        }

        if (!Manager.spawnedNetworkObjects.ContainsKey(networkIdentity))
        {
            Manager.spawnedNetworkObjects.Add(networkIdentity, spawnedObject);
        }

        if (networkIdentity != null && networkIdentity.connectionToClient == null)
        {
            Debug.Log("Server owns the chair.");
        }
        else
        {
            Debug.LogError("NetworkIdentity is not properly set up.");
        }

        yield return null;
    }

    [Command]
    public void CmdSpawnInventoryTool(InventoryManager.InventoryItems tool)
    //utils.DebugOut($"CmdSpawnNetworkObjects");
    => StartCoroutine(SpawnTool(tool));

    private IEnumerator SpawnTool(InventoryManager.InventoryItems tool)
    {
        GameObject objectToSpawn = Manager._inventoryItemsPrefabs.ToolDirectoryList.Where(x => x.type == tool).FirstOrDefault().prefab;
        GameObject spawnedTool = Instantiate(objectToSpawn, new Vector3(0, -200, 0), Quaternion.identity);

        // Spawn the object on the network (server-side).
        NetworkServer.Spawn(spawnedTool);

        GetComponent<PlayerInformationManager>().playerProperties.StoreCurrentToolReference(spawnedTool);
        // Ensure the server owns the object when it spawns.
        NetworkIdentity networkIdentity = spawnedTool.GetComponent<NetworkIdentity>();
        networkIdentity.visibility = Visibility.ForceShown;
        utils.DebugOut($"Identity: {networkIdentity.assetId}, Observers: {networkIdentity.observers.ToCommaSeparatedString()}");
        if (networkIdentity.observers.Count > 0)
        {
            utils.DebugOut($"observer name: {networkIdentity.observers[0].identity.gameObject.name}");
        }

        if (!Manager.spawnedNetworkObjects.ContainsKey(networkIdentity))
        {
            Manager.spawnedNetworkObjects.Add(networkIdentity, spawnedTool);
        }

        if (networkIdentity != null && networkIdentity.connectionToClient == null)
        {
            Debug.Log("Server owns the chair.");
        }
        else
        {
            Debug.LogError("NetworkIdentity is not properly set up.");
        }

        yield return null;
    }

    [Command]
    public void CmdReleaseInventoryTool() => StartCoroutine(ReleaseTool());

    private IEnumerator ReleaseTool()
    {
        GetComponent<PlayerInformationManager>().playerProperties.StoreCurrentToolReference(null);
        yield return null;
    }

    [Command]
    public void CmdAssignClientAuthority(GameObject target) => target.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);

    [Command]
    public void CmdRemoveClientAuthority(GameObject target) => target.GetComponent<NetworkIdentity>().RemoveClientAuthority();
}
