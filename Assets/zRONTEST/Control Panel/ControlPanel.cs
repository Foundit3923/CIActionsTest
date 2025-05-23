using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using Mirror;
using UnityServiceLocator;
using System;

public class ControlPanel : MonoBehaviour
{
    public GameObject rebindRowPrefab;
    public Transform rowsParent;
    public Button saveButton;
    public InputActionAsset playerInputActions;

    private UnityEngine.InputSystem.PlayerInput playerInput;
    private Utility utils;
    private PlayerInformationManager pim;

    private InputActionMap playerActionMap;
    private Dictionary<string, string> currentBindings = new();
    private List<RebindUIRow> rows = new();

    private BlackboardController blackboardController;
    private Blackboard blackboard;

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
                return manager;

            if (FizzyNetworkManager.singleton != null)
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;

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
                    playerInput = localPlayer.gameObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                    playerInputActions = playerInput.actions;
                }
                //NetworkIdentity localPlayerId = NetworkClient.localPlayer;
                //blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Players, out List<GameObject> players);
                //foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
                //{
                //    if (player.gameObject.GetComponent<NetworkIdentity>().netId == localPlayerId.netId)
                //    {
                //        pim = player.gameObject.GetComponent<PlayerInformationManager>();
                //        playerInput = player.gameObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                //        playerInputActions = playerInput.actions;
                //    }
                //}
            }
            else
            {
                pim = utils.gameObject.GetComponent<PlayerInformationManager>();
                if (pim.settings.playerInput == null) { pim.settings.playerInput = playerInput; }

                if (pim.settings.InputActionAsset == null) { pim.settings.InputActionAsset = playerInputActions; }
            }
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    void Start()
    {
        try
        {
            if (playerInputActions == null || rebindRowPrefab == null || rowsParent == null)
            {
                Debug.LogError("ControlPanel is missing references.");
                return;
            }

            playerActionMap = playerInputActions.FindActionMap("Player");

            if (playerActionMap == null)
            {
                Debug.LogError("Could not find 'Player' Action Map.");
                return;
            }

            foreach (var action in playerActionMap.actions)
            {
                var binding = action.bindings[0];

                // Skip non-keyboard bindings
                if (action.bindings[0].effectivePath != "")
                {
                    if (!action.bindings[0].effectivePath.Contains("Keyboard") || action.bindings[0].isPartOfComposite)
                        continue;
                }
                else if (!action.bindings[0].groups.Contains("Keyboard") || action.bindings[0].isPartOfComposite)
                    continue;

                GameObject newRow = Instantiate(rebindRowPrefab, rowsParent);
                RebindUIRow row = newRow.GetComponent<RebindUIRow>();
                string bindingText = action.GetBindingDisplayString(binding);
                if (bindingText == "")
                {
                    var strings = action.bindings[0].path.Split('/', 2);
                    bindingText = strings[1];
                }
                else
                {
                    var strings = bindingText.Split('/', 2);
                    bindingText = strings[1];
                }

                row.Initialize(action, 0, bindingText, pim, StartRebindForAction);
                currentBindings[action.name + 0] = action.GetBindingDisplayString();
                row.UpdateBindingDisplay();
                rows.Add(row);
            }
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
            foreach (RebindUIRow row in rows)
            {
                row.UpdateBindingDisplay();
            }
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void ResetSettings()
    {
        try
        {
            pim.settings.ResetBindings();
            PullSettings(pim);
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    void StartRebindForAction(InputAction action, int bindingIndex)
    {
        try
        {
            action.Disable();
            action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(callback => OnRebindComplete(action, bindingIndex))
                .Start();
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    void OnRebindComplete(InputAction action, int bindingIndex)
    {
        try
        {
            action.Enable();
            pim.settings.ButtonsAndOverrides[action] = action.bindings[bindingIndex].overridePath;
            string newBinding = action.GetBindingDisplayString(bindingIndex);
            currentBindings[action.name + bindingIndex] = newBinding;

            foreach (Transform child in rowsParent)
            {
                RebindUIRow row = child.GetComponent<RebindUIRow>();
                if (row != null && row.GetInputAction() == action && row.GetBindingIndex() == bindingIndex)
                {
                    row.UpdateBindingDisplay();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    void OnSaveButtonClicked()
    {
        foreach (var binding in currentBindings)
        {
            string actionNameWithIndex = binding.Key;
            string newBinding = binding.Value;

            string actionName = actionNameWithIndex.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            InputAction action = playerActionMap.FindAction(actionName);

            if (action != null)
            {
                int index = int.Parse(actionNameWithIndex.Substring(action.name.Length));
                action.ApplyBindingOverride(index, newBinding);
            }
        }

        PlayerPrefs.SetString("PlayerBindings", "Bindings Saved!");
        PlayerPrefs.Save();

        Debug.Log("Bindings saved successfully.");
    }

    public void ApplyTempSettings(PlayerInformationManager pim, bool isOnline)
    {
        try
        {
            //Enforces temp bindings and saved bindings
            pim.settings.ApplyBindings();
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
}
