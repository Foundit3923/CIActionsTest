using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityServiceLocator;

public class RebindUIRow : MonoBehaviour
{
    public TMP_Text actionNameText;
    public TMP_Text bindingText;
    public Button rebindButton;

    private PlayerInformationManager _pim;

    private InputAction inputAction;
    private int bindingIndex;

    private Utility _utils;
    public Utility utils
    {
        get
        {
            if (_utils != null)
            {
                return _utils;
            }

            return _utils = ServiceLocator.Global.Get<Utility>();
        }
    }

    public void Initialize(InputAction action, int index, string text, PlayerInformationManager pim, System.Action<InputAction, int> onRebindClicked)
    {
        inputAction = action;
        bindingIndex = index;
        _pim = pim;

        actionNameText.text = action.name;
        bindingText.text = text;

        rebindButton.onClick.RemoveAllListeners();
        rebindButton.onClick.AddListener(() => onRebindClicked?.Invoke(inputAction, bindingIndex));
    }

    public void UpdateBindingDisplay()
    {
        if (inputAction != null)
        {
            utils.DebugOut("UpdateBindingDisplay");
            bindingText.text = inputAction.GetBindingDisplayString(bindingIndex);
        }
    }

    public InputAction GetInputAction() => inputAction;
    public int GetBindingIndex() => bindingIndex;
}
