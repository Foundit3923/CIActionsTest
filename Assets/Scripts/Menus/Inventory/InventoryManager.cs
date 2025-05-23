using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Mirror.Examples.Common;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;
using static MountPointDirectory;

public class InventoryManager : NetworkBehaviour
{
    //---------- Depenedencies
    private PlayerInformationManager __pim;
    private PlayerInformationManager _pim
    {
        get
        {
            if (__pim != null)
            {
                return __pim;
            }

            return __pim = GetComponent<PlayerInformationManager>();
        }
        set => __pim = value;
    }
    private PlayerNetworkCommunicator __communicator;
    private PlayerNetworkCommunicator _communicator
    {
        get
        {
            if (__communicator != null)
            {
                return __communicator;
            }

            if (TryGetComponent<PlayerNetworkCommunicator>(out PlayerNetworkCommunicator communicator))
            {
                return __communicator = communicator;
            }

            return null;
        }
        set => __communicator = value;
    }
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
    private Utility _utils;
    private Utility utils
    {
        get
        {
            if (_utils != null)
            {
                return _utils;
            }

            return _utils = ServiceLocator.Global.Get<Utility>();
        }
        set => _utils = value;
    }

    //---------- Enums
    public enum InventoryItems
    {
        None,
        Flashlight
    }

    public enum Handedness
    {
        Left,
        Right,
        Both
    }

    //---------- Fields
    private int _slotCount = 2;
    private InventoryItems[] _inventory;
    private InventoryItems __selectedToolType;
    private InventoryItems _selectedToolType
    {
        get => __selectedToolType;
        set
        {
            if (__selectedToolType != value)
            {
                OnNewInventoryItemSelected?.Invoke(__selectedToolType, value);
            }

            __selectedToolType = value;
        }
    }

    [SyncVar(hook = nameof(OnSelectedToolChanged))] //Can only be spawned GameObjects with a NetworkIdentity
    private GameObject _selectedTool;

    private int __selectedIndex;
    private int _selectedIndex
    {
        get => __selectedIndex;
        set
        {
            if (__selectedIndex != value)
            {
                OnNewSelectedIndex?.Invoke(__selectedIndex, value);
            }

            __selectedIndex = value;
        }
    }
    private Dictionary<InventoryItems, int> _inventoryItemsAnimations = new()
    {
        { InventoryItems.None, 0 },
        { InventoryItems.Flashlight, 3 },
    };
    
    private Dictionary<InventoryItems, MountPointTargetType> _inventoryToolsMountPoints = new()
    {
        { InventoryItems.None, MountPointTargetType.None },
        { InventoryItems.Flashlight, MountPointTargetType.Hand },
    };

    //---------- Events
    public delegate void NewInventoryItemSelectedEvent(InventoryItems old, InventoryItems @new);
    public static event NewInventoryItemSelectedEvent OnNewInventoryItemSelected;

    public delegate void NewSelectedIndexEvent(int old, int @new);
    public static event NewSelectedIndexEvent OnNewSelectedIndex;

    //---------- MonoBehaviour lifecycle
    private void Awake() 
    {
        _inventory = new InventoryItems[_slotCount];
        _selectedIndex = 0;

    }

    private void OnEnable()
    {
        OnNewInventoryItemSelected += OnInventoryItemSelected;
        OnNewSelectedIndex += OnIndexSelected;
    }

    private void OnDisable()
    {
        OnNewInventoryItemSelected -= OnInventoryItemSelected;
        OnNewSelectedIndex -= OnIndexSelected;
    }

    //---------- Setters
    public void AddToolToInventory(InventoryItems tool)
    {
        if (CanAddToolToInventory())
        {
            if (_inventory[0] == InventoryItems.None)
            {
                _inventory[0] = tool;

            }
            else if (_inventory[1] == InventoryItems.None)
            {
                _inventory[1] = tool;
            }

            _selectedToolType = _inventory[_selectedIndex];
        }
    }
    public void RemoveToolFromInventory()
    {
        //Always removes the selected tool
        _inventory[_selectedIndex] = InventoryItems.None;

        _selectedToolType = _inventory[_selectedIndex];
    }
    public void RemoveSpecificToolFromInventory(InventoryItems tool)
    {
        //Removes the first instance of the specified tool
        if (_inventory[0] == tool)
        {
            _inventory[0] = InventoryItems.None;

        }
        else if (_inventory[1] == tool)
        {
            _inventory[1] = InventoryItems.None;
        }
    }
    public void SetSelectedInventoryItem(InventoryItems item)
    {
        if (CanAddToolToInventory()) 
        {
            if (_inventory[0] == InventoryItems.None)
            {
                _inventory[0] = item;
            }
            else
            {
                _inventory[1] = item;
            }
        }
    }
    public void StoreCurrentToolReference(GameObject toolReference) => _selectedTool = toolReference;
    public void SetSelectedIndex(int index) => _selectedIndex = index;

    //---------- Getters
    public InventoryItems[] Inventory => _inventory;
    public GameObject GetCurrentToolReference() => _selectedTool;
    public InventoryItems GetCurrentToolType() => _selectedToolType;

    //---------- Event Callbacks
    private void OnInventoryItemSelected(InventoryItems old, InventoryItems @new)
    {
        if (isLocalPlayer)
        {
            if (_inventoryItemsAnimations[old] != _inventoryItemsAnimations[@new])
            {
                ApplyRightArmStateAnimation(_inventoryItemsAnimations[@new]);
                if (@new != InventoryItems.None)
                {
                    RequestSpawnItem(@new);
                }
                else if (old != @new)
                {
                    //only release the tool if the old tool was not None
                    ReleaseTool();
                }
            }
        }
    }
    private void OnIndexSelected(int old, int @new)
        //Inventory index has been selected manually
        => _selectedToolType = _inventory[@new];
    private void OnSelectedToolChanged(GameObject old, GameObject @new)
    {
        if (isLocalPlayer)
        {
            if (@new != null)
            {
                //start the process of attaching it to the player
                InteractableBase.OnAuthorityGranted += InteractWithAuthority;
                _communicator.CmdAssignClientAuthority(@new);
            }
            else
            {
                old.GetComponent<InteractableBase>().Release();
                _communicator.CmdRemoveClientAuthority(old);
            }
        }
    }

    private void InteractWithAuthority(GameObject go)
    {
        if (go == _selectedTool)
        {
            InteractableBase.OnAuthorityGranted -= InteractWithAuthority;
            _selectedTool.GetComponent<InteractableBase>().Take(this.gameObject);
        }
    }
    

    //----------- Evaluation
    public bool CanAddToolToInventory() => AvailableSlots() < _slotCount ? true : false;
    public int AvailableSlots() => _inventory.Where(x => x != InventoryItems.None).Count();

    //---------- Animation
    private void ApplyRightArmStateAnimation(int armState)
        //default handedness
        => _pim.settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.I_Arms_State, armState);

    //---------- Actions
    //Triggers OnNewIndexSelected -> OnNewInventoryItemSelected -> 
    public void MoveToNextItem() => _selectedIndex = _selectedIndex == 0 ? 1 : 0;
    [Server]
    private void RequestSpawnItem(InventoryItems tool) => _communicator.CmdSpawnInventoryTool(tool);
    private void ReleaseTool() => _selectedTool.GetComponent<InteractableBase>().Release();

    //---------- Icons
    public Image GetToolIcon(InventoryItems tool)
    {
        Image icon = null;
        if (_selectedTool != null)
        {
            icon = _selectedTool.GetComponent<ToolProperties>().Icon;
        }

        return icon;
    }
}
