using System;
using UnityEngine;
using UnityServiceLocator;

public class TimeTickSystem : MonoBehaviour
{
    public class OnTickEventArgs : EventArgs
    {
        public int Tick;
    }

    public static event EventHandler<OnTickEventArgs> OnTick;
    public static event EventHandler<OnTickEventArgs> OnTick_2;
    public static event EventHandler<OnTickEventArgs> OnTick_5;
    public static event EventHandler<OnTickEventArgs> OnTick_12;

    private const float tickTimerMax = .1f;

    private int Tick;
    private float ShortTick;
    private float MedTick;
    private float LongTick;
    private float tickTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Tick = 0;
        ShortTick = 2f;
        MedTick = 5f;
        LongTick = 12f;
    }

    // Update is called once per frame
    void Update()
    {
        tickTimer += Time.deltaTime;
        while (tickTimer >= tickTimerMax)
        {
            tickTimer -= tickTimerMax;
            Tick++;

            OnTick?.Invoke(this, new OnTickEventArgs { Tick = Tick });

            if (Tick % ShortTick == 0)
            {
                OnTick_2?.Invoke(this, new OnTickEventArgs { Tick = Tick });
                if (Tick % LongTick == 0)
                {
                    OnTick_12?.Invoke(this, new OnTickEventArgs { Tick = Tick });
                }
            }

            if (Tick % MedTick == 0)
            {
                OnTick_5?.Invoke(this, new OnTickEventArgs { Tick = Tick });
            }
        }
    }
}
