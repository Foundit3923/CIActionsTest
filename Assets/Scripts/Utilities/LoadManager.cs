using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using System.Collections.Generic;
using Mirror.FizzySteam;
using Unity.VisualScripting;
using Steamworks;
using Mirror;
using UnityEditor;
using Eflatun.SceneReference;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using static InventoryToolDirectory;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[RequireComponent(typeof(GameplaySettings))]
public class LoadManager : MonoBehaviour
{
    bool cleanupFlag, refsCreated, anchorsCreated, seedIsSet;
    [SerializeField] BlackboardController blackboardController;
    Blackboard blackboard;
    [SerializeField] private GameplaySettings gameplaySettings;
    [SerializeField] private Utility utils;
    [SerializeField] private LogSender clientSideRef;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] SceneReference gameplayScene;
    [SerializeField] SceneReference offlineScene;
    [SerializeField] Int32 seed = 0;
    [SerializeField] List<GameObject> AvailableMonsters = new();
    [SerializeField] List<GameObject> NetworkedObjects = new();
    [SerializeField] AnimationSerializer serializer;
    [SerializeField] string InternalDataFolderName;
    [SerializeField] string AnimationConditionsDictFilename;
    [SerializeField] GameObject WorldFloorPrefab;
    [SerializeField] AudioMixer mixer;
    [SerializeField] InputActionAsset inputActionAsset;
    [SerializeField] InventoryToolDirectory inventoryToolDirectory;

    private IEnumerator coroutine;

    private SteamLobby steamLobby;
    private FizzyNetworkManager netManager;
    private WindowFocusMonitor windowFocusMonitor;
    private CursorExpert cursorVisibilityExpert;
    private EnemyInitializationReference enemyInitializationReference;
    private LightManager lightManager;
    private DifficultyManager difficultyManager;
#if UNITY_EDITOR
    private List<(string, AnimatorController)> animationControllers = new();
#endif
    private AnimationConditions AnimationConditions = new();
    private _internal _internal;
    List<GameObject> anchors = new();
    List<object> processes = new();
    GameObject networkingAnchor;
    GameObject networkingManagerAnchor;
    GameObject utilitiesAnchor;
    GameObject consoleAnchor;
    GameObject commsAnchor;

    public delegate void AnchorInitializedEvent(string AnchorName);
    public static event AnchorInitializedEvent OnAnchorInitialized;

    private void Log(string message)
    {
        if (utils != null)
        {
            if (utils.state >= State.Started)
            {
                utils.DebugOut(message);
            }
            else
            {
                Debug.Log(message);
            }
        }
        else
        {
            Debug.LogError("utils is null");
            Debug.Log(message);
        }
    }

    private void Awake()
    {
        Log("LoadManager Awake");
        ServiceLocator.Global.Register<LoadManager>(this);
    }

    public enum State
    {
        Awake,
        Started,
        Initialized,
        Authorized
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (utils == null)
        {
            //utils = ServiceLocator.Global.Get<Utility>();
            utils = GameObject.FindGameObjectWithTag("Utilities").GetOrAddComponent<Utility>();
        }

        if (blackboardController == null)
        {
            blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        }

        Log($"LoadManager Start");

        DontDestroyOnLoad(this);
        cleanupFlag = true;
        refsCreated = false;
        anchorsCreated = false;
        seedIsSet = false;
        OnAnchorInitialized += EvalInitEvent;
#if UNITY_EDITOR
        foreach (GameObject obj in AvailableMonsters)
        {
            EnemyProperties props = obj.GetComponent<EnemyProperties>();
            animationControllers.Add((props.Name, props._animatorController));
        }
#endif

    }

    private void Init()
    {
        Log("LoadManager Blackboard Set");
        blackboard = blackboardController.GetBlackboard();
        if (WorldFloorPrefab != null)
        {
            StartCoroutine(utils.InstantiateWorldFloor(WorldFloorPrefab));
        }

        CheckForCmdLineArgs();
        SceneManager.LoadScene("StartUp");
    }

    private void Startup()
    {
        //Store the random seed if it was provided
        if (seed != 0 && !seedIsSet)
        {
            seedIsSet = true;
            blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.RandomSeed), seed);
        }
        //There are only a few things to find so this shouldn't be expensive
        //Create everything and pass dependencies in via constructor
        if (!anchorsCreated)
        {
            //Find anchor objects
            anchorsCreated = true;
            networkingAnchor = GameObject.FindGameObjectWithTag("Networking");
            networkingManagerAnchor = GameObject.FindGameObjectWithTag("NetworkingManager");
            Debug.LogError($"Utils GameObject Name: {utils.gameObject.name}");
            utilitiesAnchor = utils.gameObject;// GameObject.FindGameObjectWithTag("Utilities");
            consoleAnchor = GameObject.FindGameObjectWithTag("Console");
            commsAnchor = GameObject.FindGameObjectWithTag("Comms");
            commsAnchor.SetActive(false);
            OnAnchorInitialized("Anchors");
        }

        if (refsCreated)
        {
            //Set ready to 1, then & with all ready values. If all are ready it will still be 1 when the Value is checked
            //and the while loop will finish
            int ready = (int)State.Started;
            foreach (var obj in processes)
            {
                State state = (State)obj.GetType().GetField("state").GetValue(obj);
                ready &= (int)state;
            }

            if (ready == (int)State.Started)
            {
                //make sure this only happens once
                if (cleanupFlag)
                {
                    cleanupFlag = false;
                    processes.Add(utils);

                    //now we distribute refs
                    //Check each startup process for the appropriate method and then invoke for those that have it
                    processes.ForEach(p => { if (p.GetType().GetMember("InitDependencies").Length > 0) { p.GetType().GetMethod("InitDependencies").Invoke(p, null); } });
                    //processes.ForEach(p => { if (p.GetType().GetMember("SetUtils").Length > 0) { p.GetType().GetMethod("SetUtils").Invoke(p, new object[1] {_utils}); } });
                    //processes.ForEach(p => { if (p.GetType().GetMember("SetSteamLobby").Length > 0) { p.GetType().GetMethod("SetSteamLobby").Invoke(p, new object[1] { steamLobby }); } });

                    //Everything is ready
                    utils.DebugOut("Deregister all priority group experts");
                    blackboardController.DeregisterAllPriorityGroupExperts();
                    utils.DebugOut("Play Catchup");
                    utils.CatchupOnEventActions(true);
                    utils.DebugOut("Load Main Menu");
                    //Remove from don't destroy on load
                    SceneManager.MoveGameObjectToScene(this.gameObject, SceneManager.GetActiveScene());
                    if (blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.JoinRequestLobbyID), out ulong lobbyID))
                    {
                        if (lobbyID is <= 0 or default(ulong))
                        {
                            return;
                        }

                        if (lobbyID > 0)
                        {
                            steamLobby.JoinLobby((CSteamID)lobbyID);
                        }
                    }
                    else
                    {
                        Destroy(enemyInitializationReference);
                        SceneManager.LoadScene("MainMenu");
                    }
                }
            }
            else
            {
                //Try again on the next update
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Log($"LoadManager Update");
        Log($"LoadManager Scene: {SceneManager.GetActiveScene()}| Scene Name {SceneManager.GetActiveScene().name}");
        if (SceneManager.GetActiveScene() != null && SceneManager.GetActiveScene().name == "Init")
        {
            if (blackboardController.state == State.Started)
            {
                Init();
            }
            else
            {
                Log("Waiting for BlackboardController to finish Staring up");
            }
        }
        else if (SceneManager.GetActiveScene().name == "StartUp")
        {
            if (utils.state >= State.Started)
            {
                Startup();
            }
            else
            {
                Log("Waiting for Utils to finish Staring up");
            }
        }
    }

    private void CheckForCmdLineArgs()
    {
        //SteamApps.GetLaunchCommandLine(out var commandLine, 2048);
        // get your command line arguments
        var args = System.Environment.GetCommandLineArgs();

        // we really only care if we have 2 or more if we just want the lobbyid.
        if (args.Length >= 2)
        {
            // loop to the 2nd last one, because we are gonna do a + 1
            // the lobbyID is straight after +connect_lobby
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower() == "+connect_lobby")
                {
                    if (ulong.TryParse(args[i + 1], out ulong lobbyID))
                    {
                        if (lobbyID > 0)
                        {
                            // Store the lobbyID in the blackboard
                            blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.JoinRequestLobbyID), lobbyID);
                        }
                    }

                    break;
                }
            }
        }
    }

    private void EvalInitEvent(string anchor)
    {
        switch (anchor)
        {
            case "Anchors":
                InitializeInternal(utilitiesAnchor);
                break;
            case "Internal":
                InitializeNetworkingManager(networkingManagerAnchor);
                break;
            case "Networking":
                InitializeNetworkManager(networkingAnchor);
                break;
            case "Network":
                InitializeUtilities(utilitiesAnchor);
                break;
            case "Utilities":
                InitializeChatManager(consoleAnchor);
                break;
            case "Console":
                refsCreated = true;
                break;
            default:
                break;
        }
    }

    private void InitializeInternal(GameObject utilitiesAnchor)
    {
        _internal = utilitiesAnchor.GetOrAddComponent<_internal>();
        _internal.ProvideFilePaths(InternalDataFolderName, AnimationConditionsDictFilename);
        _internal.enabled = true;

        serializer = utilitiesAnchor.GetOrAddComponent<AnimationSerializer>();
        serializer.enabled = true;

        //ensure a copy of every
#if UNITY_EDITOR
        serializer.SerializeAnimatorControllers(animationControllers, _internal.AnimationConditionsDictFilepath);
#endif
        AnimationConditions = serializer.DeserializeAnimationControllers(_internal.AnimationConditionsDictFilepath);
        blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.AnimationConditions), AnimationConditions);

        OnAnchorInitialized("Internal");
    }

    private void InitializeNetworkingManager(GameObject networkingManagerAnchor)
    {
        FizzySteamworks steamworksTransport = networkingManagerAnchor.GetOrAddComponent<FizzySteamworks>();
        steamworksTransport.enabled = true;
        //processes.Add( steamworksTransport );
        netManager = networkingManagerAnchor.GetOrAddComponent<FizzyNetworkManager>();
        netManager.enabled = true;
        netManager.transport = steamworksTransport;
        netManager.playerPrefab = playerPrefab;
        netManager.offlineScene = offlineScene.Name;
        netManager.onlineScene = gameplayScene.Name;
        netManager._inventoryItemsPrefabs = netManager.AddComponent<InventoryToolDirectory>();
        netManager._inventoryItemsPrefabs.ToolDirectoryList = inventoryToolDirectory.ToolDirectoryList.ToList();
        foreach(InventoryToolEntry entry in inventoryToolDirectory.ToolDirectoryList)
        {
            netManager.spawnPrefabs.Add(entry.prefab);
        }

        netManager.LogManagerGO = NetworkedObjects[1];
        netManager.AuthorityRefGO = NetworkedObjects[3];
        netManager.enemySpawnPrefabs.AddRange(AvailableMonsters);
        netManager.spawnPrefabs.AddRange(AvailableMonsters);
        netManager.spawnPrefabs.AddRange(NetworkedObjects);
        netManager.chairPrefab = NetworkedObjects.Where(x => x.TryGetComponent<NetworkedChairRef>(out NetworkedChairRef reference) == true).FirstOrDefault();

        GameObject FireplaceAudioPrefab = NetworkedObjects[1];
        netManager.FireplaceAudio = FireplaceAudioPrefab;

        processes.Add(netManager);
        OnAnchorInitialized("Networking");
    }

    private void InitializeNetworkManager(GameObject networkingAnchor)
    {
        SteamManager steamManager = networkingAnchor.GetOrAddComponent<SteamManager>();
        steamManager.enabled = true;
        //processes.Add( steamManager );
        steamLobby = networkingAnchor.GetOrAddComponent<SteamLobby>();
        steamLobby.enabled = true;
        processes.Add(steamLobby);

        OnAnchorInitialized("Network");
    }

    private void InitializeUtilities(GameObject utilitiesAnchor)
    {
        PlayerInformationManager pim = utilitiesAnchor.GetOrAddComponent<PlayerInformationManager>();
        pim.enabled = true;
        pim.settings.mixer = mixer;
        pim.settings.InputActionAsset = inputActionAsset;
        utils.pim = pim;
        windowFocusMonitor = utilitiesAnchor.GetOrAddComponent<WindowFocusMonitor>();
        windowFocusMonitor.enabled = true;
        processes.Add(windowFocusMonitor);
        cursorVisibilityExpert = utilitiesAnchor.GetOrAddComponent<CursorExpert>();
        cursorVisibilityExpert.enabled = true;
        processes.Add(cursorVisibilityExpert);
        enemyInitializationReference = utilitiesAnchor.GetOrAddComponent<EnemyInitializationReference>();
        enemyInitializationReference.enabled = true;
        processes.Add(enemyInitializationReference);
        lightManager = utilitiesAnchor.GetOrAddComponent<LightManager>();
        lightManager.enabled = true;
        processes.Add(lightManager);

        difficultyManager = utilitiesAnchor.GetOrAddComponent<DifficultyManager>();

        difficultyManager.SetValues(gameplaySettings);
        difficultyManager.enabled = true;
        processes.Add(difficultyManager);

        OnAnchorInitialized("Utilities");
    }

    private void InitializeChatManager(GameObject consoleAnchor)
    {
        //processes.Add(chatManager);
        //processes.Add(utils);
        //processes.Add(blackboardController);
        OnAnchorInitialized("Console");
        commsAnchor.SetActive(true);
        DontDestroyOnLoad(commsAnchor);
    }
}
