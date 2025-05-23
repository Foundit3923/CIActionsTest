using System.Text;
using System;
using UnityEngine;
using UnityServiceLocator;
using System.IO;
using static IClientExpert;

public class WindowFocusMonitor : MonoBehaviour, IClientExpert
{
    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey windowFocusKey, readyFlagKey;

    private Utility utils;

    public LoadManager.State state;

    private bool isPriority, isGameWindowFocused, focusChange, getBlackboardAndRegister;
    int readyFlag = 0;
    private int attempts = 0;

    StringBuilder sb;
    string logFileName;

    public int GetInsistence(Blackboard blackboard)
    {
        utils.DebugOut("WindowFocusManager GetInsistence", Utility.DebugLevel.Deep);
        int result = (int)ClientInsistenceLevel.None;
        if (focusChange)
        {
            result = (int)ClientInsistenceLevel.DependencyData + attempts; //Window focus is very important to know about
            attempts++;
        }

        return result;
    }

    public void Execute(Blackboard blackboard)
    {
        blackboard.AddAction(() =>
        {
            attempts = 0;

            utils.DebugOut("Record Focus Change", Utility.DebugLevel.Deep);
            focusChange = false;
            blackboard.SetValue(windowFocusKey, isGameWindowFocused);
        });
    }
    public bool IsPriority(bool set)
    {
        isPriority = set;
        return set;
    }

    void OnApplicationFocus(bool inFocus)
    {
        utils.DebugOut("WindowFocusManager OnApplicationFocus");
        if (state >= LoadManager.State.Started)
        {
            utils.DebugOut($"Record Focus Change {inFocus} vs {isGameWindowFocused}");
            if (inFocus != isGameWindowFocused)
            {
                focusChange = true;
            }

            isGameWindowFocused = inFocus;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        utils.DebugOut("WindowFocusManager Start");
        state = LoadManager.State.Awake;
        DontDestroyOnLoad(this);
        isGameWindowFocused = Application.isFocused;
        getBlackboardAndRegister = true;
        focusChange = false;
        ServiceLocator.Global.Register<WindowFocusMonitor>(this);
        blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        windowFocusKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.WindowFocus);
        blackboard.SetValue(windowFocusKey, Application.isFocused);
        blackboardController.RegisterExpert(this);
        state = LoadManager.State.Started;
        utils.DebugOut("WindowFocusManager Started");
    }
    public void InitDependencies() => state = LoadManager.State.Initialized;

    // Update is called once per frame
    void Update()
    {
    }
}
