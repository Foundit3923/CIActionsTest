using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using MenuEnums;
using static IClientExpert;
using Mirror;

public class PlayerMenuInteractions : NetworkBehaviour
{
    [SerializeField] private InputAction move;
    [SerializeField] private InputAction attack;
    private PlayerInformationManager __pim;
    private PlayerInformationManager _pim
    {
        get
        {
            if (__pim != null)
            {
                return __pim;
            }

            if (TryGetComponent<PlayerInformationManager>(out PlayerInformationManager pim))
            {
                return __pim = pim;
            }

            return null;
        }
    }
    public InteractableBase Tool => _pim?.playerProperties.GetCurrentTool().GetComponent<InteractableBase>();
    private PlayerInput playerInput;
    private Utility utils;
    private Menu currentMenu;
    private Scene currentContext;
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

    private Tooltip ToolTip;
    private List<Tooltip> AvailableTooltips = new();
    private bool ToolTipAvailable;
    private AssetsInputs _input;

    private PlayerCameraController _spectatorController
    {
        get
        {
            if(TryGetComponent<PlayerCameraController>(out PlayerCameraController spectatorController))
            {
                return spectatorController;
            }

            return null;
        }
    }

    //Blackboard references
    private BlackboardController blackboardController;
    private Blackboard blackboard;
    //public bool isPaused;
    private int attempts = 0;

    private float delta = 0f;

    public delegate void PlayerInteractWithToolEvent(PlayerMenuInteractions e);
    public static event PlayerInteractWithToolEvent OnPlayerInteractWithTool;

    void OnEnable()
    {
        // Utility.OnNewMenu += UpdateMenuAndContext;
    }
    private void Awake()
    {

        //DontDestroyOnLoad(this.transform.root.gameObject);
        //pauseMenu = transform.root.gameObject.GetComponentInChildren<HUD.OldPauseMenu>();
        playerInput = GetComponent<PlayerInput>();
        //isPaused = false;
        currentContext = SceneManager.GetActiveScene();
        Utility.OnNewMenu += UpdateMenuAndContext;
        _input = GetComponent<AssetsInputs>();

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        utils = ServiceLocator.For(this).Get<Utility>();
        move = playerInput.actions.FindAction("Move");
        attack = playerInput.actions.FindAction("Attack");
        delta = .2f;
        //InvokeRepeating("EvaluateTooltips", 0f, 0.2f);
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            EvalInput();
        }
    }

    void FixedUpdate()
    {
        delta -= Time.deltaTime;
        if (delta <= 0)
        {
            delta = .2f;
            EvaluateTooltips();
        }
    }

    private void UpdateMenuAndContext()
    {
        currentMenu = utils.GetCurrentMenu();
        currentContext = SceneManager.GetActiveScene();
    }

    void EvalInput()
    {
        if (currentContext.name == Manager.onlineScene)
        {
            utils.DebugOut($"Context: {Manager.onlineScene}", Utility.DebugLevel.Deep);

            PauseMenu();
            ToolInteraction();
            if (ToolTipAvailable)
            {
                utils.DebugOut("ToolTip Available", Utility.DebugLevel.Deep);
                TooltipAction();
            }
            else
            {
                utils.DebugOut("Tooltip Not Available", Utility.DebugLevel.Deep);
                OnlineInteractionButton();
            }

            SpectatorMode();

            return;
        }

        if (currentContext.name == Manager.offlineScene)
        {
            return;
        }
    }

    private void SpectatorMode()
    {
        if (_spectatorController != null)
        {
            // Only allow input to cycle through players while spectating
            if (_pim.playerProperties.isDead && _pim.playerProperties.isSpectating)
            {
                if (_input.spectateNext)
                {
                    _spectatorController.SpectateNext();
                }
                else if (_input.spectatePrevious)
                {
                    _spectatorController.SpectatePrevious();
                }
            }
        }
    }

    private void ToolInteraction()
    {
        if (!_pim.playerProperties.isDead)
        {
            if (_input.interactWithTool)
            {
                utils.DebugOut("Interact with tool");
                if (Tool != null)
                {
                    Tool.CmdPlayerInteraction(this);
                }
            }

            if (_input.dropTool)
            {
                utils.DebugOut("Drop tool");
                if (Tool != null)
                {
                    _pim.playerProperties.RemoveToolFromInventory();
                }
            }
        }
    }

    void OnlineInteractionButton()
    {
        utils.DebugOut("OnlineInteractionButton");
        if (_input.spawnObject)
        {
            utils.DebugOut("Spawn Object");
            ServiceLocator.Global.TryGet<LogSender>(out LogSender sender);
            if (sender != null)
            {
                try
                {
                    sender.cmdTest();
                }
                catch (System.Exception e)
                {
                    utils.DebugOut("CmdTest Exception: " + e);
                }
            }
            // gameObject.GetComponent<PlayerNetworkCommunicator>().CmdSpawnNetworkObjects();
        }
    }

    void PauseMenu()
    {
        if (_input.pause)
        {
            string pauseSceneName = "PauseMenu";
            string settingsSceneName = "SettingsMenu";
            utils.DebugOut("From player controller.");
            Scene pauseScene = SceneManager.GetSceneByName(pauseSceneName);
            Scene settingsScene = SceneManager.GetSceneByName(settingsSceneName);
            if (settingsScene.IsValid())
            {
                SceneManager.UnloadSceneAsync(settingsScene);
            }

            if (!pauseScene.IsValid())
            {
                SceneManager.LoadScene(pauseSceneName, LoadSceneMode.Additive);
            }
            else
            {
                SceneManager.UnloadSceneAsync(pauseScene);
            }
        }
    }

    void TooltipAction()
    {
        if (_input.interact)
        {
            utils.DebugOut("Interact Pressed");
            if (ToolTip.runAsCommand)
            {
                utils.DebugOut("Interacting with Tooltip via Command");
                CmdTooltipAction(ToolTip.GetComponent<NetworkIdentity>(), this.gameObject);
            }
            else
            {
                utils.DebugOut("Interacting with Tooltip");
                ToolTip.PlayerInteraction("Interact", this.gameObject);
            }
        }
    }

    [Command]
    void CmdTooltipAction(NetworkIdentity ToolTip, GameObject self) => ToolTip.GetComponent<Tooltip>().PlayerInteraction("Interact", self);

    public void RegisterToolTip(Tooltip toolTipObject)
    {
        if (!AvailableTooltips.Contains(toolTipObject))
        {
            AvailableTooltips.Add(toolTipObject);
        }
    }

    public void DeregisterToolTip(Tooltip toolTipObject)
    {
        if (ToolTip == toolTipObject)
        {
            SetToolTipToLayer("Invisible");
            AvailableTooltips.Remove(toolTipObject);
            EvaluateTooltips();
        }
        else
        {
            if (AvailableTooltips.Contains(toolTipObject))
            {
                AvailableTooltips.Remove(toolTipObject);
            }
        }
    }

    private void SetToolTipToLayer(string layerName)
    {
        LayerMask mask = LayerMask.NameToLayer(layerName);
        Transform[] children = ToolTip.gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in children)
        {
            child.gameObject.layer = mask;
        }
    }

    private void EvaluateTooltips()
    {
        if (AvailableTooltips.Count > 0)
        {
            float closestDist = float.PositiveInfinity;
            Tooltip closestTooltip = default(Tooltip);
            if (ToolTip != null)
            {
                closestDist = Vector3.Distance(transform.position, ToolTip.transform.position);
                closestTooltip = ToolTip;
            }

            foreach (Tooltip tooltip in AvailableTooltips)
            {
                if (Vector3.Distance(transform.position, tooltip.transform.position) < closestDist)
                {
                    closestTooltip = tooltip;
                }
            }

            if (closestTooltip != ToolTip && ToolTip != null)
            {
                //Moving to another tooltip. Make current invisible before switching
                SetToolTipToLayer("Invisible");
            }

            ToolTip = closestTooltip;
            SetToolTipToLayer("ToolTip");
            ToolTipAvailable = true;
        }
        else
        {
            ToolTipAvailable = false;
        }
    }
}
