using System;
using System.Collections.Generic;
using EFOV;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityServiceLocator;
using static UnityEngine.Rendering.GPUSort;

public class Zombify : MonoBehaviour
{
    private TimeTickSystem timeTickSystem;
    private EnemyProperties enemyProperties;
    private FieldOfView fieldOfView;
    private NavMeshAgent agent;
    private EnemyControllerStateMachine stateMachine;
    private Animator animator;

    private BlackboardController blackboardController;
    private Blackboard blackboard;
    private Utility utils;
    private bool isZombie;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        utils = ServiceLocator.For(this).Get<Utility>();
        animator = this.GetComponent<Animator>();
        isZombie = false;
    }

    public void ConvertPlayerToZombie(GameObject enemy)
    {
        //Add EnemyProperties
        //Add StateMachine
        //Add TimeTickSystem
        //Add FieldOfView
        //Add NavMeshAgent
        if (!isZombie)
        {
            if (blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.NavMeshAgentDict), out Dictionary<string, GameObject> navMeshAgentDictionary))
            {
                GameObject zombieObject = navMeshAgentDictionary["Zombie"];
                if (zombieObject.TryGetComponent<EnemyProperties>(out enemyProperties))
                {
                    //Rotate player to face monster
                    Vector3 facingEnemy = -enemy.transform.forward;
                    if (this.transform.forward != facingEnemy)
                    {
                        this.transform.LookAt(enemy.transform.position);
                    }

                    //Set Animator parameters
                    animator.SetBool("L_Zombified", true);
                    animator.SetTrigger("Convert");

                    this.tag = "Zombie";
                    utils.SetGameLayerRecursive(this.gameObject, LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"));

                    //Disable unnecessary components
                    this.GetComponent<PlayerObjectController>().enabled = false;
                    this.GetComponent<AssetsInputs>().enabled = false;
                    this.GetComponent<PlayerInput>().enabled = false;
                    this.GetComponent<CharacterController>().enabled = false;
                    this.GetComponent<PlayerMenuInteractions>().enabled = false;
                    this.GetComponent<PlayerInformationManager>().enabled = false;
                    this.GetComponent<PlayerProperties>().enabled = false;
                    this.GetComponent<Settings>().enabled = false;
                    this.GetComponent<PlayerDetails>().enabled = false;
                    this.GetComponent<PlayerNetworkCommunicator>().enabled = false;

                    //Add required components
                    enemyProperties = CopyComponent(enemyProperties, this.gameObject);
                    //timeTickSystem = (TimeTickSystem)Activator.CreateInstance(typeof(TimeTickSystem));
                    timeTickSystem = this.AddComponent<TimeTickSystem>();
                    timeTickSystem.enabled = true;
                    //fieldOfView = (FieldOfView)Activator.CreateInstance(typeof(FieldOfView));
                    fieldOfView = this.AddComponent<FieldOfView>();
                    fieldOfView.enabled = true;
                    //agent = (NavMeshAgent)Activator.CreateInstance(typeof(NavMeshAgent));
                    agent = this.GetOrAddComponent<NavMeshAgent>();
                    agent.agentTypeID = enemyProperties.NavMeshAgentId;
                    agent.enabled = true;
                    Rigidbody rb = this.GetComponent<Rigidbody>();
                    stateMachine = this.AddComponent<EnemyControllerStateMachine>();
                    stateMachine.enabled = true;

                    //object[] args = new object[9] { rb, collider, renderer, enemyProperties.navmeshMask, enemyProperties.targetMask, enemyProperties.range, fieldOfView, enemyProperties, enemyProperties._animatorController };
                    stateMachine.Constructor(rb, enemyProperties.navmeshMask, enemyProperties.targetMask, enemyProperties.range, fieldOfView, enemyProperties);//, enemyProperties._animatorController);
                                                                                                                                                               //stateMachine = (EnemyControllerStateMachine)Activator.CreateInstance(typeof(EnemyControllerStateMachine),args);
                    if (!stateMachine.isActiveAndEnabled)
                    {
                        stateMachine.enabled = true;
                    }

                    if (stateMachine.didAwake)
                    {
                        Debug.Log("Chcek");
                    }
                }
                else
                {
                    utils.DebugOut("Unable to resolve EnemyProperties on Zombie object");
                }
            }
            else
            {
                utils.DebugOut("Unable to resolve NavMeshAgentDict");
            }
        }
    }

    public static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        var type = original.GetType();
        var copy = destination.AddComponent(type);
        var fields = type.GetFields();
        foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
        return copy as T;
    }
}
