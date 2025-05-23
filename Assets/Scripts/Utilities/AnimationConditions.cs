using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AnimationConditions
{
    public AnimationConditions()
    {
    }
    public Dictionary<string, AnimationLayers> Monsters;
}

public sealed class AnimationLayers
{
    public AnimationLayers()
    {
    }
    public Dictionary<int, AnimationStates> Layers;
}

public sealed class AnimationStates
{
    public AnimationStates()
    {
    }
    public Dictionary<int, AnimationStateConditions> States;
}

public sealed class AnimationStateConditions
{
    public AnimationStateConditions()
    {
    }
    public Dictionary<string, List<AnimationStateConditionsValues>> Conditions;
}

public class AnimationStateConditionsValues
{
    public AnimationStateConditionsValues()
    {
    }
    public Tuple<string, float> Value
    {
        get; set;
    }
}

