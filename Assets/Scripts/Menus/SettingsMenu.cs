using System;
using System.Collections.Generic;
using MenuEnums;
using Mirror;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityServiceLocator;

namespace MenuEnums
{
    public static class SettingsMenuActions
    {
        public const MenuActions AudioPanel = MenuActions.AudioPanel;
        public const MenuActions ControlsPanel = MenuActions.ControlsPanel;
        public const MenuActions GraphicsPanel = MenuActions.GraphicsPanel;
        public const MenuActions GamePanel = MenuActions.GamePanel;
        public const MenuActions Record = MenuActions.Record;
        public const MenuActions Play = MenuActions.Play;
        public const MenuActions Stop = MenuActions.Stop;
        public const MenuActions Return = MenuActions.Return;
        public const MenuActions Save = MenuActions.Save;
        public const MenuActions Reset = MenuActions.Reset;
    }
}

public class SettingsMenu : Menu
{
    private Utility utils;
    public AudioPanel audioPanel;
    public ControlPanel controlPanel;
    public GraphicsPanel graphicsPanel;
    public GamesPanel gamePanel;
    public MenuActions currentPanel;
    public PlayerInformationManager pim;
    public Slider volumeSlider;
    public Slider brightnessSlider;
    public Toggle fullscreenToggle;
    public Toggle vsyncDropdown;
    public Slider mouseSensSlider;
    public Toggle invertXToggle;
    public Toggle invertYToggle;
    public TMP_Dropdown micDropdown;
    public TMP_Dropdown languageDropdown;
    public Slider pixelizationSlider;

    string sceneName = "SettingsMenu";

    private BlackboardController blackboardController;
    private Blackboard blackboard;

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
        try
        {
            blackboardController = ServiceLocator.Global.Get<BlackboardController>();
            blackboard = blackboardController.GetBlackboard();
            utils = ServiceLocator.Global.Get<Utility>();
            if (SceneManager.GetActiveScene().name == Manager.onlineScene)
            {
                if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, out GameObject localPlayer))
                {
                    pim = localPlayer.gameObject.GetComponent<PlayerInformationManager>();
                }
                //NetworkIdentity localPlayerId = NetworkClient.localPlayer;
                //blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Players, out List<GameObject> players);
                //foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
                //{
                //    if (player.gameObject.GetComponent<NetworkIdentity>().netId == localPlayerId.netId)
                //    {
                //        pim = player.gameObject.GetComponent<PlayerInformationManager>();
                //    }
                //}
            }
            else if (SceneManager.GetActiveScene().name == sceneName)
            {
                pim = utils.gameObject.GetComponent<PlayerInformationManager>();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
    private void Update()
    {
        //Cursor.visible = true;
    }
    public override void TakeAction(ButtonParams arg)
    {
        try
        {
            MenuActions action = arg.baseMenuAction;
            switch (action)
            {
                case MenuActions.AudioPanel:
                    if (audioPanel.gameObject.activeSelf == true) return;

                    else
                    {
                        SetPanelActive(action);
                        currentPanel = action;
                    }

                    break;
                case MenuActions.ControlsPanel:
                    if (controlPanel.gameObject.activeSelf == true) return;

                    else
                    {
                        SetPanelActive(action);
                        currentPanel = action;
                    }

                    break;
                case MenuActions.GraphicsPanel:
                    if (graphicsPanel.gameObject.activeSelf == true) return;

                    else
                    {
                        SetPanelActive(action);
                        currentPanel = action;
                    }

                    break;
                case MenuActions.GamePanel:
                    if (gamePanel.gameObject.activeSelf == true) return;

                    else
                    {
                        SetPanelActive(action);
                        currentPanel = action;
                    }

                    break;
                case MenuActions.Record:
                    audioPanel.RecordTestClip();
                    break;
                case MenuActions.Play:
                    audioPanel.PlayTestClip();
                    break;
                case MenuActions.Stop:
                    audioPanel.StopTestClipPlayback();
                    break;
                case MenuActions.Return:
                    Cursor.visible = true;
                    if (SceneManager.GetActiveScene().name == sceneName)
                    {
                        SceneManager.UnloadSceneAsync(sceneName);
                        SceneManager.LoadScene("MainMenu");
                    }
                    else if (SceneManager.GetActiveScene().name == "World")
                    {
                        SceneManager.UnloadSceneAsync(sceneName);
                        SceneManager.LoadScene("PauseMenu", LoadSceneMode.Additive);
                    }

                    break;
                case MenuActions.Save:
                    pim.settings.SaveToDisk();
                    UpdateTempSettings();
                    break;
                case MenuActions.Reset:
                    ResetSettings(currentPanel);
                    break;
                default:
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    private void SetPanelActive(MenuActions action)
    {
        try
        {
            bool audio = false;
            bool control = false;
            bool graphics = false;
            bool game = false;
            if (action == MenuActions.AudioPanel) { audio = true; }

            if (action == MenuActions.ControlsPanel) { control = true; }

            if (action == MenuActions.GraphicsPanel) { graphics = true; }

            if (action == MenuActions.GamePanel) { game = true; }

            audioPanel.gameObject.SetActive(audio);
            controlPanel.gameObject.SetActive(control);
            graphicsPanel.gameObject.SetActive(graphics);
            gamePanel.gameObject.SetActive(game);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    private void ResetSettings(MenuActions action)
    {
        try
        {
            switch (action)
            {
                case MenuActions.AudioPanel:
                    audioPanel.ResetSettings();
                    break;
                case MenuActions.ControlsPanel:
                    controlPanel.ResetSettings();
                    break;
                case MenuActions.GraphicsPanel:
                    graphicsPanel.ResetSettings();
                    break;
                case MenuActions.GamePanel:
                    gamePanel.ResetSettings();
                    break;
                default:
                    break;
            }

            pim.settings.SaveToDisk();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    private void UpdateTempSettings()
    {
        try
        {
            audioPanel.ApplyTempSettings(pim, utils.isOnline);
            gamePanel.ApplyTempSettings(pim, utils.isOnline);
            graphicsPanel.ApplyTempSettings(pim, utils.isOnline);
            controlPanel.ApplyTempSettings(pim, utils.isOnline);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    protected override void OnStarting()
    {
        return;
    }

    protected override void OnAwake()
    {
        return;
    }
}
