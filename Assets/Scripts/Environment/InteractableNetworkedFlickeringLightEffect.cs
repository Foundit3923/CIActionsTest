using UnityEngine;
using System.Collections.Generic;
using UnityServiceLocator;
using Mirror;

// License: CC0 Public Domain http://creativecommons.org/publicdomain/zero/1.0/

/// <summary>
/// Component which will flicker a linked light while active by changing its
/// intensity between the min and max values given. The flickering can be
/// sharp or smoothed depending on the Value of the smoothing parameter.
///
/// Just activate / deactivate this component as usual to pause / resume flicker
/// </summary>
public class InteractableNetworkedFlickeringLightEffect : InteractableBase
{
    [Tooltip("External light to flicker; you can leave this null if you attach script to a light")]
    public new Light light;
    [Tooltip("Minimum random light intensity")]
    public float minIntensity = 0f;
    [Tooltip("Maximum random light intensity")]
    public float maxIntensity = 1f;
    [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
    [Range(1, 50)]
    public int smoothing = 5;
    [Tooltip("How long should the current settings be used before asking for new settings?")]
    public float duration = 1f;
    private float remainingDuration;

    // Continuous average calculation via FIFO queue
    // Saves us iterating every time we update, we just change by the delta
    Queue<float> smoothQueue;
    private Queue<float> newQueue;
    private float lastSum = 0;
    public bool ShouldSpark = false;
    public bool ShouldFlicker = false;
    public bool isOn = true;

    //Blackboard integration
    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey flickerKey, allLightsKey;
    private (int, float, float, float) flickerTuple;
    private (int, float, float, float) defaultTuple;

    private System.Random _rand;

    /// <summary>
    /// Reset the randomness and start again. You usually don't need to call
    /// this, deactivating/reactivating is usually fine but if you want a strict
    /// restart you can do.
    /// </summary>
    public void Reset()
    {
        smoothQueue.Clear();
        lastSum = 0;
    }

    public override void OnStart() => Init();

    public void Init()
    {
        base.OnStart();
        _rand = new System.Random();
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        flickerKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Flicker);
        allLightsKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.AllLights);
        blackboard.SetListValue(allLightsKey, this.gameObject);
        smoothQueue = new Queue<float>(smoothing);
        // External or internal light?
        if (light == null)
        {
            light = GetComponent<Light>();
        }

        duration = _rand.Next(0, 100) / 100f;
        remainingDuration = duration;
        defaultTuple.Item1 = smoothing;
        defaultTuple.Item2 = duration;
        defaultTuple.Item3 = minIntensity;
        defaultTuple.Item4 = maxIntensity;
    }

    public override void OnUpdate()
    {
        if (light == null || !isOn)
            return;

        if (ShouldFlicker)
        {
            remainingDuration -= Time.deltaTime;

            if (remainingDuration <= 0f)
            {
                //request new flicker values
                if (blackboard.TryGetValue(flickerKey, out flickerTuple))
                {
                    newQueue = new Queue<float>(flickerTuple.Item1);
                    minIntensity = flickerTuple.Item3;
                    maxIntensity = flickerTuple.Item4;
                    if (flickerTuple.Item1 >= smoothQueue.Count)
                    {
                        foreach (float val in smoothQueue)
                        {
                            newQueue.Enqueue(val);
                        }
                    }
                    else
                    {
                        while (newQueue.Count <= flickerTuple.Item1)
                        {
                            newQueue.Enqueue(smoothQueue.Dequeue());
                        }
                    }

                    duration = flickerTuple.Item2;
                }
                else
                {
                    //reset to default values
                    newQueue = new Queue<float>(defaultTuple.Item1);
                    if (defaultTuple.Item1 >= smoothQueue.Count)
                    {
                        foreach (float val in smoothQueue)
                        {
                            newQueue.Enqueue(val);
                        }
                    }
                    else
                    {
                        while (newQueue.Count <= defaultTuple.Item1)
                        {
                            newQueue.Enqueue(smoothQueue.Dequeue());
                        }
                    }

                    duration = defaultTuple.Item2;
                }

                smoothQueue.Clear();
                smoothQueue = newQueue;
                remainingDuration = duration;
                int build = _rand.Next(0, 1);
                lastSum = build * lastSum;
                ShouldSpark = build == 0 ? true : false;
            }
        }

        if (!ShouldSpark)
        {
            // pop off an item if too big
            while (smoothQueue.Count >= smoothing)
            {
                lastSum -= smoothQueue.Dequeue();
            }

            // Generate random new item, calculate new average
            float newVal = Random.Range(minIntensity, maxIntensity);
            smoothQueue.Enqueue(newVal);
            lastSum += newVal;
        }
        else
        {
            //reset this so that the light returns to normal afterwards
            ShouldSpark = false;
            //trigger spark effect right before light intensity Value changes
        }
        // Calculate new smoothed average
        // if should spark this will eval to 0
        light.intensity = lastSum / (float)smoothQueue.Count;
    }

    public void TurnOff()
    {
        isOn = false;
        light.intensity = 0;
    }

    public void TurnOn(bool flicker = false)
    {        
        SetFlicker(flicker);
        isOn = true;
    }

    public void SetFlicker(bool flicker)
    {
        ShouldFlicker = flicker;
        if (!flicker)
        {
            minIntensity = 50;
            maxIntensity = 50;
            smoothing = 50;
            smoothQueue.Clear();
            smoothQueue = new Queue<float>(smoothing);
            lastSum = 0;
        }
        else
        {
            remainingDuration = 0;
        }
    }
    public override void CmdPlayerInteraction(PlayerMenuInteractions menuInteractions)
    {
    }
}
