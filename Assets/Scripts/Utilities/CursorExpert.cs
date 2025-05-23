using System.Text;
using System;
using UnityEngine;
using UnityServiceLocator;
using System.IO;
using static IClientExpert;
using System.Net.Sockets;

public class CursorExpert : MonoBehaviour, IClientExpert
{
    private Utility utils;
    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey cursorVisibilityKey, windowFocusKey;

    public LoadManager.State state;

    private bool isPriority, isCursorVisble, isWindowFocused, isMenuOnScreen, saveVisibility, savedState;
    int readyFlag = 0;
    private int attempts = 0;

    StringBuilder sb;
    string logFileName;

    public int GetInsistence(Blackboard blackboard)
    {
        utils.DebugOut("CursorVisibility GetInsistence", Utility.DebugLevel.Deep);
        int result = (int)ClientInsistenceLevel.DependencyData + attempts; //Needs to be executed before normal operations but is not the most important
        attempts++;
        return result;
    }

    public void Execute(Blackboard blackboard)
    {
        blackboard.AddAction(() =>
        {
            utils.DebugOut("CursorVisibility Execute", Utility.DebugLevel.Deep);
            attempts = 0;
            //Non priority actions are executed after startup
            blackboard.SetValue(cursorVisibilityKey, isCursorVisble);

        });
    }
    public bool IsPriority(bool set)
    {
        utils.DebugOut("CursorVisibility IsPriority");
        isPriority = set;
        return set;
    }

    public void SetMenuOnScreen(bool set)
    {
        utils.DebugOut("CursorVisibility SetMenuOnScreen");
        isMenuOnScreen = set;
    }

    public bool RequestSetCursorVisiblity(bool visible)
    {
        utils.DebugOut("CursorVisibility RequestSetCursorVisiblity", Utility.DebugLevel.Deep);
        bool requestStatus = false;
        SetCursorVisibility(visible);
        //Decide if the request should be carried out
        return requestStatus;
    }

    private void SetCursorVisibility(bool visible)
    {
        utils.DebugOut("CursorVisibility SetCursorVisibility", Utility.DebugLevel.Deep);
        if (Cursor.visible != visible) { Cursor.visible = visible; }
    }

    public bool RequestSetCursorLock(bool locked)
    {
        utils.DebugOut("CursorVisibility RequestSetCursorLock", Utility.DebugLevel.Deep);
        bool requestStatus = false;
        SetCursorlock(locked == true ? CursorLockMode.Locked : CursorLockMode.None);
        SetCursorVisibility(locked);
        //Decide if the request should be carried out
        return requestStatus;
    }

    private void SetCursorlock(CursorLockMode locked)
    {
        utils.DebugOut("CursorVisibility SetCursorlock", Utility.DebugLevel.Deep);
        if (Cursor.lockState != locked) { Cursor.lockState = locked; }
    }

    public bool RequestSetCursorConfined(bool confined)
    {
        utils.DebugOut("CursorVisibility RequestSetCursorConfined", Utility.DebugLevel.Deep);
        bool requestStatus = false;
        SetCursorConfined(confined == true ? CursorLockMode.Confined : CursorLockMode.None);
        //Decide if the request should be carried out
        return requestStatus;
    }

    private void SetCursorConfined(CursorLockMode confined)
    {
        utils.DebugOut("CursorVisibility SetCursorlock", Utility.DebugLevel.Deep);
        if (Cursor.lockState != confined) { Cursor.lockState = confined; }
    }

    void Awake() => state = LoadManager.State.Awake;
    void Start()
    {
        utils = ServiceLocator.For(this).Get<Utility>();
        utils.DebugOut("CursorVisibility Start");
        DontDestroyOnLoad(this);
        isMenuOnScreen = false;
        isWindowFocused = false;
        isCursorVisble = false;
        ServiceLocator.Global.Register<CursorExpert>(this);
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        cursorVisibilityKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.CursorVisibility);
        windowFocusKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.WindowFocus);
        blackboardController.RegisterExpert(this);
        state = LoadManager.State.Started;
        InvokeRepeating("UpdateCursorVisibility", 0f, .2f);
        utils.DebugOut("CursorVisibility Started");
    }
    public void InitDependencies() => state = LoadManager.State.Initialized;

    private void UpdateCursorVisibility()
    {
        utils.DebugOut($"CursorVisibility Update", Utility.DebugLevel.Deep);
        //TODO: Manage locking cursor to game screen when moving in and out of focus
        if (blackboard != null)
        {
            if (blackboard.TryGetValue(windowFocusKey, out isWindowFocused))
            {
                if (isWindowFocused)
                {
                    if (Cursor.lockState != CursorLockMode.Confined)
                    {
                        RequestSetCursorConfined(true);
                    }
                }
                else
                {
                    if (Cursor.lockState == CursorLockMode.Confined)
                    {
                        RequestSetCursorConfined(false);
                    }
                }

                if (isMenuOnScreen)
                {
                    RequestSetCursorVisiblity(true);

                }
                else
                {
                    RequestSetCursorVisiblity(false);
                }
            }
        }
    }

    void Update()
    {

    }
}
