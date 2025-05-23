using System;
using UnityEngine;
using UnityServiceLocator;

public abstract class AnimationStateMonitor : StateMachineBehaviour
{
    public delegate void StateTransitionEval(GameObject go, bool x);
    public static event StateTransitionEval OnStateTransitionEval;

    BlackboardController blackboardController;
    Blackboard blackboard;

    public AnimationConditions AnimationConditions;

    public EnemyProperties EnemyProperties;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (blackboardController == null)
        {
            blackboardController = ServiceLocator.Global.Get<BlackboardController>();
            blackboard = blackboardController.GetBlackboard();
            if (blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.AnimationConditions), out AnimationConditions))
            {

            }

            EnemyProperties = animator.gameObject.GetComponent<EnemyProperties>();
        }

        OnEnter(animator, stateInfo, layerIndex);
    }

    public abstract void OnEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);

    public void CallEvent(Animator animator, bool isSequenceComplete) => animator.SetBool("willTransition", isSequenceComplete);
}
