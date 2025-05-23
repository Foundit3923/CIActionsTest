using UnityEngine;
using System;
//Don't think these are needed but we will find out.
//TODO: If it runs after 1/17/25 they are not needed
//using LanternHeadStates;
//using NuckelaveeStates;
//using NightGauntStates;
using System.Collections;
using System.Linq;
using UnityServiceLocator;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Text;
using System.IO;
using static IClientExpert;

public class EnemyInitializationReference : MonoBehaviour, IClientExpert
{
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
    private Utility utils;
    private BlackboardController blackboardController;
    private Blackboard blackboard;
    private List<string> navMeshAgentTypes;
    public Dictionary<string, GameObject> navMeshAgentDictionary;
    public Dictionary<string, List<Type>> classDictionary;
    BlackboardKey readyFlagKey;
    public LoadManager.State state;
    bool isPriority;

    private void Awake() => state = LoadManager.State.Awake;

    private void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        utils.DebugOut("InitManager Start");
        //We need this for the life of the game
        DontDestroyOnLoad(this.gameObject);
        navMeshAgentTypes = new();
        navMeshAgentDictionary = new();
        classDictionary = new();
        ServiceLocator.Global.Register<EnemyInitializationReference>(this);
        //Cycle through the list of nav mesh agents to get the name of each enemy type that would need a state machine
        //Store the name and id in a dictionary
        //make a list of namespaces for quick search
        //make a dictionary of namespaces and associated List<Type> to be passed to state machines for initialization.
        int settingsCount = NavMesh.GetSettingsCount(); //returns count of all agent types listed in Navigation Panel
        utils.DebugOut("InitManager GetSettings");
        for (int i = 0; i < settingsCount; i++)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
            string name = NavMesh.GetSettingsNameFromID(settings.agentTypeID);
            foreach (GameObject prefab in Manager.enemySpawnPrefabs)
            {
                EnemyProperties properties = prefab.GetComponent<EnemyProperties>();
                if (properties != null && properties.Name == name)
                {
                    properties.NavMeshAgentId = settings.agentTypeID;
                    navMeshAgentDictionary[name] = prefab;
                    string nameSpace = name + "States";
                    navMeshAgentTypes.Add(nameSpace);
                    classDictionary[nameSpace] = new List<Type>();
                }
            }
        }

        GetClassesInNamespace();
        blackboard = blackboardController.GetBlackboard();
        blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.NavMeshAgentDict), navMeshAgentDictionary); //useful for baking specific navMeshSurfaces
        blackboard.SetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.ClassDictionary), classDictionary);
        blackboardController.RegisterWithPriorityGroup(this);
        state = LoadManager.State.Started;
        utils.DebugOut("InitManager Started");
    }
    public void InitDependencies() => state = LoadManager.State.Initialized;

    //Get all classes who have namespaces in the previously made list of namespaces. Had to separate foreach.
    //Add classes to the appropriate namespace list in classDictionary
    public void GetClassesInNamespace()
    {
        utils.DebugOut("InitManager GetClassesInNamespace");
        List<Type> classes = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && navMeshAgentTypes.Contains(t.Namespace)).ToList();
        //.ForEach(x => classDictionary[x.Namespace].Add(x));

        foreach (Type t in classes)
        {
            classDictionary[t.Namespace].Add(t);
        }
    }

    public int GetInsistence(Blackboard blackboard)
    {
        utils.DebugOut("InitManager GetInsistence");
        return (int)ClientInsistenceLevel.DependencyData; //This should run until it is deregistered by LoadManager
    }

    public void Execute(Blackboard blackboard)
    {
        blackboard.AddAction(() =>
        {
            utils.DebugOut("InitManager Execute");
            int readyFlag = 0;
            readyFlagKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.ReadyFlag);

            if (state == LoadManager.State.Started)
            {
                readyFlag = 1;
            }

            if (blackboard.TryGetValue(readyFlagKey, out int globalReadyFlag))
            {
                blackboard.SetValue(readyFlagKey, readyFlag & globalReadyFlag);
            }
        });
    }

    public bool IsPriority(bool set)
    {
        isPriority = set;
        return set;
    }
}
