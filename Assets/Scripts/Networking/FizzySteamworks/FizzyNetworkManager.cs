using UnityEngine;
using System.Collections.Generic;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using UnityServiceLocator;
using System;
using System.Text;
using System.IO;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;
using static InventoryManager;

public class FizzyNetworkManager : NetworkManager
{
    [SerializeField] private PlayerNetworkCommunicator gamePlayerPrefab;
    [SerializeField] public List<PlayerNetworkCommunicator> GamePlayers { get; } = new List<PlayerNetworkCommunicator>();
    [SerializeField] private SteamLobby steamLobby;
    [SerializeField] private Utility utils;

    //networking objects
    public InventoryToolDirectory _inventoryItemsPrefabs;
    [SerializeField] private List<Transform> chairSpawnPoints = new();
    public GameObject AuthorityRefGO;
    public AuthorityRef AuthorityRef;
    public GameObject LogManagerGO;
    //these need to be moved out of Network Manager
    public GameObject chairPrefab;
    public GameObject FireplaceAudio;

    public List<GameObject> enemySpawnPrefabs = new();
    public Dictionary<NetworkIdentity, GameObject> spawnedNetworkObjects = new();
    //This specific location is being used to test networked sounds (lobby fireplace right now.)
    private Vector3 locationToSpawn = new(-176, -28, 0);  //Can do this dynamically later.

    public bool hasAuthority = false;
    //public List<Vector3> spawnPositions = new List<Vector3> { new Vector3(-50.7f, 1.5f, 29.9f), new Vector3(-50.7f + 1f, 1.5f, 29.9f) };

    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey playersKey, spawnKey;
    public LoadManager.State state;
    public bool isPriority, getBlackboardAndRegister;

    StringBuilder sb;
    string logFileName;

    private void Update()
    {
    }

    private void Awake()
    {
        //_utils.DebugOut("FizzyNetworkManager Awake");
        state = LoadManager.State.Awake;
        DontDestroyOnLoad(this.gameObject);
        ServiceLocator.Global.Register<FizzyNetworkManager>(this);
    }
    private void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        LoadManager loadManager = ServiceLocator.For(this).Get<LoadManager>();
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        playersKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Players);
        spawnKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.PlayerLobbySpawn);
        gamePlayerPrefab = base.playerPrefab.GetComponent<PlayerNetworkCommunicator>();
        base.autoCreatePlayer = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        state = LoadManager.State.Started;
    }

    public void InitDependencies()
    {
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();
        state = LoadManager.State.Initialized;

    }

    [Server]
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (utils == null) { utils = ServiceLocator.For(this).Get<Utility>(); }

        if (utils.EvalInitState(state, () => OnSceneLoaded(scene, mode))) { return; }

        utils.DebugOut($"ClientSide Scene Loaded: {scene.name}");
        if (scene.name != null && scene.name == base.onlineScene)
        {
            //gamePlayerPrefab.CmdSpawnNetworkObjects();
            StartCoroutine(DelayedSpawnObjects());
        }
    }

    [Server]
    override public void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        utils.DebugOut("FizzyNetworkManager OnServerAddPlayer");
        if (SceneManager.GetActiveScene().name == base.onlineScene)
        {
            utils.DebugOut("FizzyNetworkManager OnServerAddPlayer: In Online Scene");

            Transform startPos = GetStartPosition();
            utils.DebugOut($"FizzyNetworkManager SpawnPosition: {startPos.ToCommaSeparatedString()}");
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);
            player.transform.position = startPos.position;
            player.transform.rotation = Quaternion.identity;

            PlayerNetworkCommunicator gamePlayerInstance = player.GetComponent<PlayerNetworkCommunicator>();
            gamePlayerInstance.ConnectionID = conn.connectionId;
            gamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            gamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)steamLobby.CurrentLobbyID, GamePlayers.Count);

            // instantiating a "Player" prefab gives it the name "Player(clone)"
            // => appending the connectionId is WAY more useful for debugging!
            gamePlayerInstance.CmdPlayerGameObjectNameUpdate($"{playerPrefab.name} [connId={conn.connectionId}]");
            NetworkServer.AddPlayerForConnection(conn, player);
            blackboard.SetListValue(BlackboardController.BlackboardKeyStrings.Players, player);
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (blackboard.TryGetValue(spawnKey, out Transform spawnTransform))
        {
            RegisterStartPosition(spawnTransform);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        utils.LocalSteamName = SteamFriends.GetPersonaName();
    }

    //Everything under this is for networked objects - Ron
    public void SpawnNetworkedPhysicsObjects()
    {
        SpawnObjectsAtPoints(chairPrefab, chairSpawnPoints);

        SpawnAudioSource(FireplaceAudio, locationToSpawn);


        //add additional spawn logic for other objects here later
    }

    private void SpawnObjectsAtPoints(GameObject prefab, List<Transform> spawnPoints)
    {
        if (!NetworkServer.active) return;
        utils.DebugOut("SpawnObjectsAtPoints");
        foreach (var spawnPoint in spawnPoints)
        {
            GameObject spawnedObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

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

            if (!spawnedNetworkObjects.ContainsKey(networkIdentity))
            {
                spawnedNetworkObjects.Add(networkIdentity, spawnedObject);
            }

            if (networkIdentity != null && networkIdentity.connectionToClient == null)
            {
                Debug.Log("Server owns the chair.");
            }
            else
            {
                Debug.LogError("NetworkIdentity is not properly set up.");
            }
        }
    }

    private void SpawnAudioSource(GameObject audioSourcePrefab, Vector3 locationToSpawn)
    {
        //if (isServer)

        GameObject audioSource = Instantiate(audioSourcePrefab, locationToSpawn, Quaternion.identity);

        NetworkServer.Spawn(audioSource);

    }

    private void PullSpawnPoints()
    {
        chairSpawnPoints.Clear();
        Debug.Log("Cleared chairSpawnPoints");

        GameObject[] chairSpawnPointObjects = GameObject.FindGameObjectsWithTag("ChairSpawnPoint");
        Debug.Log($"Found {chairSpawnPointObjects.Length} spawn point(s).");

        if (chairSpawnPointObjects.Length == 0)
        {
            Debug.LogWarning("No spawn points found with the tag 'ChairSpawnPoint'.");
        }

        foreach (var spawnPointObject in chairSpawnPointObjects)
        {
            if (spawnPointObject != null)
            {
                if (!chairSpawnPoints.Contains(spawnPointObject.transform))
                {
                    chairSpawnPoints.Add(spawnPointObject.transform);
                    Debug.Log($"Added spawn point at: {spawnPointObject.transform.position}");
                }
            }
            else
            {
                Debug.LogError("Found a null spawn point object.");
            }
        }

        Debug.Log($"Total spawn points: {chairSpawnPoints.Count}");
    }

    public IEnumerator DelayedSpawnObjects()
    {
        yield return new WaitForSeconds(0.5f);
        utils.DebugOut("DelayedSpawnObjects");
        //PullSpawnPoints();
        //SpawnNetworkedPhysicsObjects();
        try
        {
            AuthorityRefGO = Instantiate(AuthorityRefGO);
            AuthorityRef = AuthorityRefGO.GetComponent<AuthorityRef>();
            NetworkServer.Spawn(AuthorityRefGO);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
        }

        try
        {
            LogManagerGO = Instantiate(LogManagerGO);
            NetworkServer.Spawn(LogManagerGO);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
        }
    }
}
