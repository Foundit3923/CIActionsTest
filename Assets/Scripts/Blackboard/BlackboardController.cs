using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using System.IO;

public class BlackboardController : MonoBehaviour
{
    public enum BlackboardKeyStrings
    {
        //$"{transform.name}PlayerInformationManager"   //
        AllLights,                                      //List<Light>
        AnimationConditions,                            //AnimationConditions
        ClassDictionary,                                //Dictionary<string, List<Type>>
        Flicker,                                        //(int, float, float, float)
        JoinRequestLobbyID,                             //ulong
        Lobby,                                          //GameObject
        LobbyPortal,                                    //Portal
        MicList,                                        //List<Transform>
        Monsters,                                       //List<GameObject>
        MysticStoneEnemy,                               //GameObject
        MysticStoneLobby,                               //GameObject
        NavMeshAgentDict,                               //Dictionary<string, GameObject>
        PlayerLobbySpawn,                               //Transform
        Players,                                        //List<GameObject>
        PlayerSettingsAvailable,                        //Bool
        RandomSeed,                                     //Int32
        ReadyFlag,                                      //int
        Reception,                                      //GrimmGen.Room
        ReceptionPortal,                                   //Portal
        CursorVisibility,                               //Bool
        WindowFocus,                                    //Bool
        WorldFloor,                                     //GameObject
        LocalPLayer                                     //GameObject
    }

    [SerializeField] BlackboardData blackboardData;
    [SerializeField] Utility utils;
    private Blackboard blackboardSaveState;
    private Arbiter arbiterSaveState;
    private PriorityGroup priorityGroupSaveState;
    private Blackboard blackboard = new();
    private Arbiter arbiter = new();
    private PriorityGroup priorityGroup = new();
    public LoadManager.State state;
    private bool isBlackboardActive;
    private Dictionary<string, BlackboardKey> blackboardKeys = new();

    private void Awake()
    {
        utils.DebugOut("BlackboardController Awake");
        state = LoadManager.State.Awake;
        isBlackboardActive = false;
        DontDestroyOnLoad(this);
        ServiceLocator.Global.Register(this);
        blackboardData.SetValuesOnBlackboard(blackboard);
        blackboard.Debug();
        blackboardSaveState = null;
        arbiterSaveState = null;
        priorityGroupSaveState = null;
        SceneManager.sceneLoaded += EvalResetToSave;
        InitializeKeys();
        isBlackboardActive = true;
        state = LoadManager.State.Started;
        utils.DebugOut("BlackboardController Started");
    }

    private void InitializeKeys()
    {
        foreach (BlackboardKeyStrings key in Enum.GetValues(typeof(BlackboardKeyStrings)))
        {
            if (!blackboardKeys.ContainsKey(key.ToString()))
            {
                blackboardKeys.Add(key.ToString(), blackboard.GetOrRegisterKey(key.ToString()));
            }
        }
    }

    public BlackboardKey GetKey(BlackboardKeyStrings key)
    {
        Preconditions.CheckNotNull(key);
        return blackboardKeys[key.ToString()];
    }

    public BlackboardKey GetKey(string key)
    {
        Preconditions.CheckNotNull(key);
        if (blackboardKeys.ContainsKey(key))
        {
            return blackboardKeys[key];
        }
        else
        {
            BlackboardKey newKey = blackboard.GetOrRegisterKey(key);
            blackboardKeys[key] = newKey;
            return newKey;
        }
    }

    public void SaveState()
    {
        utils.DebugOut("BlackboardController SaveState");
        isBlackboardActive = false;
        blackboardSaveState ??= blackboard;
        arbiterSaveState ??= arbiter;
        priorityGroupSaveState ??= priorityGroup;
        isBlackboardActive = false;
    }

    private void EvalResetToSave(Scene scene, LoadSceneMode mode)
    {
        utils.DebugOut("BlackboardController EvalResetToSave");
        utils.DebugOut($"BlackboardController Scene: {scene.name}");
        if (scene.name == "MainMenu")
        {
            ResetToSave();
        }
    }

    private void ResetToSave()
    {
        utils.DebugOut("BlackboardController ResetToSave");
        isBlackboardActive = false;
        if (blackboardSaveState != null)
        {
            blackboard = blackboardSaveState;
        }

        if (arbiterSaveState != null)
        {
            arbiter = arbiterSaveState;
        }

        if (priorityGroupSaveState != null)
        {
            priorityGroup = priorityGroupSaveState;
        }

        isBlackboardActive = true;
    }

    public Blackboard GetBlackboard() => blackboard;

    public void RegisterExpert(IExpert expert) => arbiter.RegisterExpert(expert);
    public void DeregisterExpert(IExpert expert) => arbiter.DeregisterExpert(expert);
    public void RegisterWithPriorityGroup(IExpert expert) => priorityGroup.RegisterPriorityAction(expert);
    public void DeregisterWithPriorityGroup(IExpert expert) => priorityGroup.DeregisterPriorityAction(expert);

    public void DeregisterAllPriorityGroupExperts()
    {
        utils.DebugOut("BlackboardController DeregisterAllPriorityGroupExperts");
        List<IExpert> expertList = priorityGroup.GetExperts();
        foreach (IExpert expert in expertList)
        {
            priorityGroup.DeregisterPriorityAction(expert);
        }
    }

    public bool isPriorityGroupProcessingActions()
    {
        utils.DebugOut("BlackboardController isPriorityGroupProcessingActions");
        return priorityGroup.processingPriorityActions;
    }

    //TODO: Change to FixedUpdate for testing
    private void Update()
    {
        utils.DebugOut($"BlackboardController isBlackboardActive: {isBlackboardActive}", Utility.DebugLevel.Deep);
        if (isBlackboardActive)
        {
            utils.DebugOut($"BlackboardController isBlackboardActive: {isBlackboardActive}", Utility.DebugLevel.Deep);

            if (priorityGroup.HasPriorityActions(blackboard))
            {
                utils.DebugOut("BlackboardController HasPriorityActions", Utility.DebugLevel.Deep);
                foreach (var action in priorityGroup.BlackboardIteration(blackboard))
                {
                    action();
                }

                priorityGroup.processingPriorityActions = false;
            }
            // Execute all agreed actions from the current iteration 
            foreach (var action in arbiter.BlackboardIteration(blackboard))
            {
                action();
            }
        }
    }
}
