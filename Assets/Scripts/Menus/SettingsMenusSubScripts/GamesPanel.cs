using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;
using UnityServiceLocator;

public class GamesPanel : MonoBehaviour
{
    private Utility utils;
    public PlayerInformationManager pim;
    public List<string> languages;
    public TMP_Dropdown languageDropdown;
    private PlayerObjectController playerController;
    public Slider mouseSense;
    public Toggle xToggle, yToggle;
    public Text xToggleText, yToggleText;
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
            if (utils.isOnline)
            {
                if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, out GameObject localPlayer))
                {
                    pim = localPlayer.gameObject.GetComponent<PlayerInformationManager>();
                    playerController = localPlayer.gameObject.GetComponent<PlayerObjectController>();
                }
                //NetworkIdentity localPlayerId = NetworkClient.localPlayer;
                //blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Players, out List<GameObject> players);
                //foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
                //{
                //    if (player.gameObject.GetComponent<NetworkIdentity>().netId == localPlayerId.netId)
                //    {
                //        pim = player.gameObject.GetComponent<PlayerInformationManager>();
                //        playerController = player.gameObject.GetComponent<PlayerObjectController>();
                //    }
                //}
            }
            else
            {
                pim = utils.gameObject.GetComponent<PlayerInformationManager>();
            }
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
    public void Start()
    {
        try
        {
            PullSettings(pim);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void PullSettings(PlayerInformationManager _pim)
    {
        try
        {
            languageDropdown.AddOptions(languages);
            string language = _pim.settings.language.value;
            languageDropdown.value = languages.IndexOf(_pim.settings.language.value);
            languageDropdown.onValueChanged.AddListener(delegate { SetLanguage(_pim); });
            mouseSense.value = _pim.settings.mouseSensitivity.value;
            xToggle.isOn = _pim.settings.invertX.value;
            yToggle.isOn = _pim.settings.invertY.value;
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void ResetSettings()
    {
        try
        {
            pim.settings.language.ResetProperty();
            pim.settings.mouseSensitivity.ResetProperty();
            pim.settings.invertX.ResetProperty();
            pim.settings.invertY.ResetProperty();
            PullSettings(pim);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void SetLanguage(PlayerInformationManager _pim)
    {
        try
        {
            _pim.settings.language.value = languages[languageDropdown.value];
            //Set localization here
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void MouseSensitivity()
    {
        try
        {
            pim.settings.mouseSensitivity.value = mouseSense.value;
            if (utils.isOnline)
            {
                playerController.lookSensitivityMultiplier = mouseSense.value * pim.settings.sensitivityMultiplier;
            }
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    //to invert the axis of the player
    public void InvertAxisX()
    {
        try
        {
            pim.settings.invertX.value = xToggle.isOn;
            xToggleText.text = xToggle.isOn ? "On" : "Off";
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void InvertAxisY()
    {
        try
        {
            pim.settings.invertY.value = yToggle.isOn;
            yToggleText.text = yToggle.isOn ? "On" : "Off";
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
    public void ApplyTempSettings(PlayerInformationManager pim, bool isOnline)
    {
        try
        {
            //set localization here with pim.language
            if (isOnline)
            {
                pim.gameObject.GetComponent<PlayerObjectController>().lookSensitivityMultiplier = pim.settings.mouseSensitivity.value * pim.settings.sensitivityMultiplier;
            }
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
}
