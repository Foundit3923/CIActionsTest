using System;
using Steamworks;
using UnityEngine;
using UnityServiceLocator;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using Mirror;
using System.Text;
using System.IO;
using Unity.VisualScripting;
using System.Reflection;
using System.Collections;
using UnityEngine.Audio;

public class Utility : MonoBehaviour
{
    private Dictionary<string, List<Action>> eventsTriggeredDuringStartUp = new();
    private Menu menu;
    [SerializeField] private SteamLobby steamLobby;
    [SerializeField] public bool shouldDebug = true;
    [SerializeField] public DebugLevel SelectedDebugLevel;
    [SerializeField] public bool showOwnersInDebug;
    WindowFocusMonitor windowFocus;
    CursorExpert cursorExpert;
    EnemyInitializationReference enemyInitRef;
    private Scene currentScene;
    public string LocalSteamName;
    public string OfflineName = "Local|";
    private LogSender _logSender;
    private AuthorityRef _authorityRef;
    public LogSender logSender
    {
        get
        {
            if (_logSender != null)
            {
                return _logSender;
            }

            if (ServiceLocator.Global.TryGet<LogSender>(out _logSender))
            {
                return _logSender;
            }

            return null;
        }
    }
    public AudioMixer mixer;

    private bool CheckForMenu, applicationHasQuit;

    //Initialization and Blackboard
    [SerializeField] private BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey readyFlagKey;//, currentMenuKey;
    public LoadManager.State state;
    public bool isPriority, getBlackboardAndRegister;
    public bool sbReady = false;
    public bool isOnline = false;
    public bool DevMode = false;

    private StringBuilder sb;
    private string currentDir;
    private string logDir = "GrimmLogs\\";
    private string logFileName;
    private string logFilePath;
    public List<string> linesToWrite = new();
    private DebugLevel DefaultDebugLevel = DebugLevel.Surface;
    private bool DefaultServerOrigin = true;

    DifficultyManager difficultyManager;
    System.Random _rand;

    public PlayerInformationManager pim;

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

    public delegate void OnlineSceneEvent();
    public static event OnlineSceneEvent OnOnlineScene;

    public delegate void OfflineSceneEvent();
    public static event OfflineSceneEvent OnOfflineScene;

    public delegate void NonNullSceneEvent(Scene s, LoadSceneMode m);
    public static event NonNullSceneEvent OnNonNullScene;

    public delegate void NewMenuEvent();
    public static event NewMenuEvent OnNewMenu;

    public delegate void AudioMixerAvailableEvent();
    public static event AudioMixerAvailableEvent OnAudioMixerAvailable;

    public delegate void ReRegisterRequestEvent();
    public static event ReRegisterRequestEvent OnReRegisterRequest;

    public enum DebugLevel
    {
        Surface,
        Deep,
    }

    public enum ButtonActions
    {
        Host,
        LeaveLobby,
        InvitePlayer,
        StartPrivateLobby,
        StartPublicLobby,
        Join,
        GetLobbiesList
    }
    private void Update()
    {
        if (CheckForMenu)
        {
            DebugOut("Utility CheckForMenu", DebugLevel.Deep);
            //Checks by default
            if (currentScene.name != null)
            {
                if (ServiceLocator.ForSceneOf(currentScene).TryGet<Menu>(out menu))
                {
                    DebugOut("Menu acquired", DebugLevel.Deep);
                    EvalMenu(currentScene);
                }
            }
        }

        if (pim != null)
        {
            AudioSource source = pim.settings.audioSource;
            if (pim.settings.mixer != null && OnAudioMixerAvailable != null)
            {
                OnAudioMixerAvailable();
                OnAudioMixerAvailable -= ApplySettings;
            }
        }
    }

    private void Awake()
    {
        state = LoadManager.State.Awake;
        DebugOut("Utility Awake");
        LocalSteamName = OfflineName;
        if (!sbReady)
        {
            sb = new StringBuilder();
            currentDir = Directory.GetCurrentDirectory() + "\\";
            sb.Append(DateTime.Now.ToString("yyyy'_'MM'_'dd'T'HH'_'mm"));
            sb.Append("_");
            sb.Append("GrimmWorkLog.txt");
            logFileName = sb.ToString();
            string fullLogDir = currentDir + logDir;
            if (!Directory.Exists(fullLogDir))
            {
                Directory.CreateDirectory(fullLogDir);
            }

            logFilePath = currentDir + logDir + logFileName;
            //logFilePath = "E:\\Work\\TubbyBoyStudios\\Grimm\\Logs\\" + logFileName;
            sbReady = true;
            DebugOut(currentDir + logDir + logFileName);
        }

        DontDestroyOnLoad(this);
        ServiceLocator.Global.Register<Utility>(this);
        getBlackboardAndRegister = true;
        CheckForMenu = false;
        applicationHasQuit = false;

    }

    private void Start()
    {
        blackboard = blackboardController.GetBlackboard();
        DebugOut("Utility Start");
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnNonNullScene += GetMenuOnSceneLoad;
        OnAudioMixerAvailable += ApplySettings;
        state = LoadManager.State.Started;
        DebugOut("Utility Started");
        InvokeRepeating("WriteLogsToFile", 0f, 1f);
    }

    public void InitDependencies()
    {
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();
        state = LoadManager.State.Initialized;
        difficultyManager = ServiceLocator.For(this).Get<DifficultyManager>();
        _rand = new System.Random(difficultyManager.GameplaySettings.Seed);
    }

    public void ApplySettings()
    {
        DebugOut("Utility GetMenuOnSceneLoad");
        if (EvalInitState(state, () => ApplySettings())) { return; }

        PlayerInformationManager pim = GetComponent<PlayerInformationManager>();
        pim.ApplySettings();
    }

    private void OnDisable()
    {
        DebugOut("Utility OnDisable");
        GameCleanup();
    }

    void OnApplicationQuit()
    {
        DebugOut("Utility OnApplicationQuit");
        GameCleanup();
    }

    void OnDestroy()
    {
        DebugOut("Utility OnDestroy");
        GameCleanup();
    }

    private void GameCleanup()
    {
        DebugOut("Utility GameCleanup");
        if (!applicationHasQuit)
        {
            applicationHasQuit = true;
            SceneManager.sceneLoaded -= GetMenuOnSceneLoad;
            if (OnAudioMixerAvailable != null)
            {
                OnAudioMixerAvailable = null;
            }

            if (OnOfflineScene != null)
            {
                OnOfflineScene = null;
            }

            if (OnOnlineScene != null)
            {
                OnOnlineScene = null;
            }

            if (OnNonNullScene != null)
            {
                OnNonNullScene = null;
            }

            if (OnNewMenu != null)
            {
                OnNewMenu = null;
            }
        }
    }

    public void DebugOut<T>(T message, DebugLevel debugLevel = DebugLevel.Surface)
    {
        if (shouldDebug && (int)debugLevel <= (int)SelectedDebugLevel)
        {
            if (!sbReady)
            {
                sb = new StringBuilder();
                currentDir = Directory.GetCurrentDirectory() + "\\";
                sb.Append(DateTime.Now.ToString("yyyy'_'MM'_'dd'T'HH'_'mm"));
                sb.Append("_GrimmWorkLog.txt");
                logFileName = sb.ToString();
                string fullLogDir = currentDir + logDir;
                if (!Directory.Exists(fullLogDir))
                {
                    Directory.CreateDirectory(fullLogDir);
                }

                logFilePath = currentDir + logDir + logFileName;
                //logFilePath = "E:\\Work\\TubbyBoyStudios\\Grimm\\GrimmLogs\\" + logFileName;
                sbReady = true;
                DebugOut(currentDir + logDir + logFileName);
            }

            string debugLine = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffK") + "-" + message + System.Environment.NewLine;
            string owner = "";
            // if (Manager != null && Manager.AuthorityRef != null)
            // {
            //     if (Manager.AuthorityRef.HasAuthority())
            //     {
            //         //Server
            //         if (showOwnersInDebug)
            //         {
            //             owner = "SERVER-HOST|";
            //         }
            //         debugLine = owner + debugLine;
            //     }
            //     else
            //     {
            //         //Host
            //         if (showOwnersInDebug)
            //         {
            //             owner = LocalSteamName + "|";
            //         }
            //         debugLine = owner + debugLine;
            //     }
            // }
            // else
            //{
            //     //Client
            //     if (showOwnersInDebug)
            //     {
            //         owner = LocalSteamName + "|";
            //     }
            //     debugLine = owner + debugLine;
            // }
            if (Manager != null && Manager.AuthorityRef != null)
            {
                if (Manager.AuthorityRef.isClientOnly)
                {
                    //Client
                    if (showOwnersInDebug)
                    {
                        owner = LocalSteamName + "|";
                    }

                    debugLine = owner + debugLine;

                }
                else
                {
                    //Server
                    if (showOwnersInDebug)
                    {
                        owner = "SERVER-HOST|";
                    }

                    debugLine = owner + debugLine;
                }
            }
            else
            {
                //Client
                if (showOwnersInDebug)
                {
                    owner = LocalSteamName + "|";
                }

                debugLine = owner + debugLine;
            }

            //Check for network and client activity to decide where the log should go
            if (logSender != null && NetworkServer.active && NetworkClient.active)
            {
                logSender.Add(debugLine);
            }
            else
            {
                linesToWrite.Add(debugLine);
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log(owner + message);
            }
        }
    }

    private void WriteLogsToFile()
    {
        //Server should never directly write to a log file
        if (NetworkServer.active && NetworkClient.active)
        {
            ClientWriteLogsToFile();
        }
        else
        {
            //DebugOut("WriteLogsToFile");
            if (linesToWrite.Count > 0)
            {
                string[] lines = linesToWrite.ToArray();
                using (StreamWriter writer = new(logFilePath, true))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                        linesToWrite.Remove(lines[i]);
                    }
                }
            }
        }
    }

    [Client]
    private void ClientWriteLogsToFile()
    {
        //DebugOut("WriteLogsToFile");
        if (linesToWrite.Count > 0)
        {
            string[] lines = linesToWrite.ToArray();
            using (StreamWriter writer = new(logFilePath, true))
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    writer.WriteLine(lines[i]);
                    linesToWrite.Remove(lines[i]);
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugOut("Utility OnSceneLoaded");
        if (EvalInitState(state, () => OnSceneLoaded(scene, mode))) { return; }

        if (scene != null)
        {
            OnNonNullScene?.Invoke(scene, mode);
            if (Manager != null)
            {
                if (scene.name == Manager.onlineScene)
                {
                    isOnline = true;
                    OnOnlineScene?.Invoke();
                }
                else if (scene.name == Manager.offlineScene)
                {
                    isOnline = false;
                    OnOfflineScene?.Invoke();
                }
            }
        }
    }

    private void GetMenuOnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        DebugOut("Utility GetMenuOnSceneLoad");
        if (EvalInitState(state, () => GetMenuOnSceneLoad(scene, mode))) { return; }

        EvalMenu(scene);
    }

    private void EvalMenu(Scene scene)
    {
        currentScene = scene;
        if (currentScene.name != null)
        {
            if (ServiceLocator.Global.TryGet<Menu>(out Menu newMenu))
            {

                if (menu == null)
                {
                    CheckForMenu = false;
                    menu = newMenu;
                    OnNewMenu?.Invoke();
                    DebugOut("Menu acquired");
                }
                else if (!menu.Equals(newMenu))
                {
                    CheckForMenu = false;
                    menu = newMenu;
                    OnNewMenu?.Invoke();
                    DebugOut("Menu acquired");
                }
            }
            else
            {
                if (OnReRegisterRequest != null)
                {
                    CheckForMenu = true;
                    OnReRegisterRequest();
                }
            }

            DebugOut("SceneLoaded");
        }
    }

    public void ResetEventCatchupList()
    {
        DebugOut("Utility ResetEventCatchupList");
        eventsTriggeredDuringStartUp.Clear();
    }

    //public bool EvalInitState<T>(LoadManager.State state, Action<object[]> action, object[] args)
    public bool EvalInitState(LoadManager.State state, Action action, bool requiresAuthorization = false, bool setFirst = false)
    {
        Preconditions.CheckNotNull(action);
        if (requiresAuthorization)
        {
            if (state != LoadManager.State.Authorized)
            {
                var trace = new System.Diagnostics.StackTrace();
                MethodBase methodVar = trace.GetFrame(1).GetMethod();
                string callingClass = methodVar.DeclaringType.FullName;
                string caller = callingClass;
                if (!eventsTriggeredDuringStartUp.ContainsKey(caller))
                {
                    eventsTriggeredDuringStartUp[caller] = new();
                }

                if (setFirst)
                {
                    eventsTriggeredDuringStartUp[caller].Insert(0, delegate
                    {
                        action?.Invoke();
                    });
                }
                else
                {
                    eventsTriggeredDuringStartUp[caller].Add(delegate
                    {
                        action?.Invoke();
                    });
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        if (state != LoadManager.State.Initialized)
        {
            var trace = new System.Diagnostics.StackTrace();
            MethodBase methodVar = trace.GetFrame(1).GetMethod();
            string callingClass = methodVar.DeclaringType.FullName;
            string caller = callingClass;
            if (!eventsTriggeredDuringStartUp.ContainsKey(caller))
            {
                eventsTriggeredDuringStartUp[caller] = new();
            }

            if (setFirst)
            {
                eventsTriggeredDuringStartUp[caller].Insert(0, delegate
                {
                    action?.Invoke();
                });
            }
            else
            {
                eventsTriggeredDuringStartUp[caller].Add(delegate
                {
                    action?.Invoke();
                });
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void CatchupOnEventActions(bool executeAll = false)
    {
        DebugOut("Utility CatchupOnEventActions");
        List<string> keys = new();
        if (executeAll) { keys = eventsTriggeredDuringStartUp.Keys.ToList(); }
        else
        {
            var trace = new System.Diagnostics.StackTrace();
            MethodBase methodVar = trace.GetFrame(1).GetMethod();
            string callingClass = methodVar.DeclaringType.FullName;
            string key = callingClass;
            if (eventsTriggeredDuringStartUp.ContainsKey(key))
            {
                keys.Add(key);
            }
            else
            {
                DebugOut($"Utility CatchupOnEventActions: {key} has no actions to catch up on");
                return;
            }
        }

        foreach (string key in keys)
        {
            DebugOut($"Utility CatchupOnEventActions: {key}");
            List<Action> toDo = eventsTriggeredDuringStartUp[key].ToList();
            try
            {
                //Anything that stops the action from running should throw an error.
                //Catch and return with descriptive message
                for (int i = 0; i < toDo.Count; i++)
                {
                    toDo[i]();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to complete event catchup: Error: {ex}");
            }

            eventsTriggeredDuringStartUp[key].Clear();
        }
    }

    public Menu GetCurrentMenu()
    {
        DebugOut("Utility GetCurrentMenu");
        ServiceLocator.Global.TryGet<Menu>(out menu);
        return menu;
    }

    public void ButtonInteraction(ButtonActions action, CSteamID lobby = new())
    {
        DebugOut("Utility ButtonInteraction");
        switch (action)
        {
            case ButtonActions.Host:
                steamLobby.HostLobby();
                break;
            case ButtonActions.LeaveLobby:
                steamLobby.RequestLeaveLobby();
                break;
            case ButtonActions.InvitePlayer:
                //InviteToGame();
                steamLobby.OpenInviteDialog();
                break;
            //case ButtonActions.StartPublicLobby:
            //    sceneToLoad = "BuildScene";
            //    hostedMultiplayerLobby.SetPublic();
            //    //DebugOut($"Creating lobby with values from public lobby button clicks {sceneToLoad}, {hostedMultiplayerLobby.Data.ToString()}");
            //    await CreateLobby(1);
            //    break;
            //case ButtonActions.StartPrivateLobby:
            //    sceneToLoad = "BuildScene";
            //    hostedMultiplayerLobby.SetFriendsOnly();
            //    await CreateLobby(1);
            //    break;
            case ButtonActions.Join:
                steamLobby.JoinLobby(lobby);
                break;
            case ButtonActions.GetLobbiesList:
                LobbiesListManager listManager;
                if (menu.GetType().Name == "LobbySelection")
                {
                    LobbySelection lobbyMenu = (LobbySelection)menu;
                    lobbyMenu.GetListOfLobbies();
                }
                else
                {
                    DebugOut("Cannot find LobbySelection. Ensure it is active and try again.");
                }

                break;
            default:
                break;
        }
    }

    public void FindLocalPlayer()
    {
        //TODO: Redo this to be better
        //LocalPlayerObject = transform.root.gameObject;
        //LocalPlayerController = <PlayerNetworkCommunicator>();
    }

    public void SetGameLayerRecursive(GameObject go, int newLayer, int layerCondition = -1)
    {
        if (layerCondition == -1)
        {
            go.layer = newLayer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.layer = newLayer;

                Transform children = child.GetComponentInChildren<Transform>();
                if (children != null)
                {
                    SetGameLayerRecursive(child.gameObject, newLayer, layerCondition);
                }
            }
        }
        else
        {
            if (go.layer == layerCondition)
            {
                go.layer = newLayer;
            }

            foreach (Transform child in go.transform)
            {
                if (child.gameObject.layer == layerCondition)
                {
                    child.gameObject.layer = newLayer;
                }

                Transform children = child.GetComponentInChildren<Transform>();
                if (children != null)
                {
                    SetGameLayerRecursive(child.gameObject, newLayer, layerCondition);
                }
            }
        }
    }

    public void StoreWorldFloor(GameObject go) => blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.WorldFloor), go);

    public IEnumerator InstantiateWorldFloor(GameObject worldFloor)
    {
        GameObject go = Instantiate(worldFloor);
        DontDestroyOnLoad(go);
        StoreWorldFloor(go);
        yield return go;
    }

    public int GetRandomNumber(int low, int high)
    {
        if (EvalInitState(state, () => GetRandomNumber(low, high))) { return low - 1; }

        return _rand.Next(low, high);
    }

    /// <summary>
    /// Tries to find the nearest GameObject in the hierarchy with a PropRoot component
    /// </summary>
    /// <param name="service">The origin GameObject.</param>  
    /// <returns>The GameObject containing the PropRoot component. Return null if none found.</returns>
    public GameObject GetNearestPropRoot(GameObject go)
    {
        GameObject root = null;
        if (go.TryGetComponent<PropRoot>(out PropRoot propRoot))
        {
            return go;
        }

        Transform parent = go.transform;
        Transform current = null;
        while (root == null)
        {
            current = parent;
            if (current.parent != null)
            {
                parent = current.parent;
                if (parent.gameObject.TryGetComponent<PropRoot>(out propRoot))
                {
                    root = propRoot.gameObject;
                }
            }
            else
            {
                //No PropRoot in this hierarchy
                break;
            }
        }

        return root;
    }

    public GameObject GetRoomFromProp(GameObject prop)
    {
        GameObject propRoot = GetNearestPropRoot(prop);
        return propRoot.gameObject.transform.parent.gameObject;
    }
}
