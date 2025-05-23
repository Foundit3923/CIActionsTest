using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class WillTransition : AnimationStateMonitor
{
    bool isSequenceComplete = false;
    public override void OnEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        isSequenceComplete = EvalConditions(animator, stateInfo, layerIndex);
        if (isSequenceComplete)
        {
            base.CallEvent(animator, isSequenceComplete);
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Proc event if sequence is complete OR if the sequence state has changed
        bool sequenceState = EvalConditions(animator, stateInfo, layerIndex);
        if (sequenceState || sequenceState != isSequenceComplete)
        {
            isSequenceComplete = sequenceState;
            base.CallEvent(animator, isSequenceComplete);
        }
    }

    public bool EvalConditions(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        bool result = true;
        string monsterName = EnemyProperties.Name;
        int parameterCount = animator.parameterCount;
        //skip to the correct monster
        if (AnimationConditions.Monsters[monsterName].Layers[layerIndex].States.ContainsKey(stateInfo.shortNameHash))
        {
            //Compare conditions
            AnimationStateConditions conditions = AnimationConditions.Monsters[monsterName].Layers[layerIndex].States[stateInfo.shortNameHash];
            if (conditions.Conditions != null && conditions.Conditions.Count > 0)
            {
                if (conditions.Conditions.ContainsKey("State"))
                {
                    int stateParam = animator.GetInteger("State");
                    List<AnimationStateConditionsValues> conditionInfoList = conditions.Conditions["State"];
                    foreach (AnimationStateConditionsValues condition in conditionInfoList)
                    {
                        if (!result)
                        {
                            break;
                        }

                        switch (condition.Value.Item1)
                        {
                            case "If":
                                if (stateParam > 0) { result = false; }

                                break;
                            case "IfNot":
                                if (stateParam! > 0) { result = false; }

                                break;
                            case "Greater":
                                if (stateParam > condition.Value.Item2) { result = false; }

                                break;
                            case "Less":
                                if (stateParam < condition.Value.Item2) { result = false; }

                                break;
                            case "Equals":
                                if (stateParam == condition.Value.Item2) { result = false; }

                                break;
                            case "NotEqual":
                                if (stateParam != condition.Value.Item2) { result = false; }

                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else
            {
                //No conditions. Will automatically move to next state so sequence is NOT complete
                result = false;
            }
        }

        return result;
    }
}
