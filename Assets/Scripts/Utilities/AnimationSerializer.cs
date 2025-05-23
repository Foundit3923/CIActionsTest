using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityServiceLocator;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

public class AnimationSerializer : MonoBehaviour
{
    private Utility utils;
    private AnimationConditions animationConditions;
    public AnimationConditions AnimationConditions
    {
        get
        {
            if (animationConditions == null)
            {
                animationConditions = new AnimationConditions();
                return animationConditions;
            }

            return animationConditions;
        }
        set => animationConditions = value;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (utils == null)
        {
            Init();
        }
    }

    private void Init() => utils = ServiceLocator.Global.Get<Utility>();

#if UNITY_EDITOR
    public void SerializeAnimatorControllers(List<(string, AnimatorController)> animationControllers, string filepath)
    {
        if (utils == null)
        {
            Init();
        }

        foreach ((string, AnimatorController) monsterController in animationControllers)
        {
            AnimatorController controller = monsterController.Item2 as AnimatorController;
            AnimationLayers monsterLayers = new()
            {
                Layers = new()
            };
            for (int layer = 0; layer < controller.layers.Length; layer++)
            {
                AnimationStates stateConditions = new()
                {
                    States = new()
                };
                for (int state = 0; state < controller.layers[layer].stateMachine.states.Length; state++)
                {
                    AnimationStateConditions conditions = new()
                    {
                        Conditions = new()
                    };
                    for (int transition = 0; transition < controller.layers[layer].stateMachine.states[state].state.transitions.Length; transition++)
                    {
                        for (int condition = 0; condition < controller.layers[layer].stateMachine.states[state].state.transitions[transition].conditions.Length; condition++)
                        {
                            string conditionString = controller.layers[layer].stateMachine.states[state].state.transitions[transition].conditions[condition].parameter.ToString();
                            float conditionThreshold = controller.layers[layer].stateMachine.states[state].state.transitions[transition].conditions[condition].threshold;
                            string conditionMode = controller.layers[layer].stateMachine.states[state].state.transitions[transition].conditions[condition].mode.ToString();
                            if (conditions.Conditions.ContainsKey(conditionString))
                            {
                                AnimationStateConditionsValues value = new()
                                {
                                    Value = Tuple.Create(conditionMode, conditionThreshold)
                                };
                                conditions.Conditions[conditionString].Add(value);
                            }
                            else
                            {
                                AnimationStateConditionsValues value = new()
                                {
                                    Value = Tuple.Create(conditionMode, conditionThreshold)
                                };
                                List<AnimationStateConditionsValues> newList = new() { value };
                                conditions.Conditions.Add(conditionString, newList);
                            }
                        }
                    }

                    int key = controller.layers[layer].stateMachine.states[state].state.nameHash;
                    if (!stateConditions.States.ContainsKey(key))
                    {
                        stateConditions.States.Add(key, conditions);
                    }
                }

                for (int stateMachines = 0; stateMachines < controller.layers[layer].stateMachine.stateMachines.Length; stateMachines++)
                {
                    for (int state = 0; state < controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states.Length; state++)
                    {
                        AnimationStateConditions conditions = new()
                        {
                            Conditions = new()
                        };
                        for (int transition = 0; transition < controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.transitions.Length; transition++)
                        {
                            for (int condition = 0; condition < controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.transitions[transition].conditions.Length; condition++)
                            {
                                string conditionString = controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.transitions[transition].conditions[condition].parameter.ToString();
                                float conditionThreshold = controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.transitions[transition].conditions[condition].threshold;
                                string conditionMode = controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.transitions[transition].conditions[condition].mode.ToString();
                                if (conditions.Conditions.ContainsKey(conditionString))
                                {
                                    AnimationStateConditionsValues value = new()
                                    {
                                        Value = Tuple.Create(conditionMode, conditionThreshold)
                                    };
                                    conditions.Conditions[conditionString].Add(value);
                                }
                                else
                                {
                                    AnimationStateConditionsValues value = new()
                                    {
                                        Value = Tuple.Create(conditionMode, conditionThreshold)
                                    };
                                    List<AnimationStateConditionsValues> newList = new() { value };
                                    conditions.Conditions.Add(conditionString, newList);
                                }
                            }
                        }

                        int key = controller.layers[layer].stateMachine.stateMachines[stateMachines].stateMachine.states[state].state.nameHash;
                        if (!stateConditions.States.ContainsKey(key))
                        {
                            stateConditions.States.Add(key, conditions);
                        }
                    }
                }

                monsterLayers.Layers.Add(layer, stateConditions);
            }

            if (AnimationConditions.Monsters == null)
            {
                AnimationConditions.Monsters = new();
            }

            AnimationConditions.Monsters.Add(monsterController.Item1, monsterLayers);
        }

        string serializedDict = Newtonsoft.Json.JsonConvert.SerializeObject(AnimationConditions, Newtonsoft.Json.Formatting.Indented);

        if (!File.Exists(filepath))
        {
            File.CreateText(filepath).Dispose();
        }

        using (StreamWriter file = new(filepath, false))
        {
            JsonSerializer serializer = new();
            serializer.Serialize(file, animationConditions);
        }
    }
#endif

    public AnimationConditions DeserializeAnimationControllers(string filepath)
    {
        if (utils == null)
        {
            Init();
        }

        using (StreamReader file = File.OpenText(filepath))
        {
            JsonSerializer serializer = new();
            AnimationConditions = (AnimationConditions)serializer.Deserialize(file, typeof(AnimationConditions));
        }

        return AnimationConditions;
    }
}
