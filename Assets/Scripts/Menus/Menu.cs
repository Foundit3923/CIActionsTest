using UnityEngine;
using UnityServiceLocator;
using MenuEnums;
using UnityEngine.Device;
namespace MenuEnums
{
    public enum MenuType
    {
        MainMenu,
        PauseMenu,
        LobbyBrowserMenu,
        SettingsMenu
    }
    public enum MenuActions
    {
        Return,
        Exit,
        Host,
        Join,
        Invite,
        Settings,
        Credits,
        Resume,
        Pause,
        Quit,
        ChatVolume,
        LeaveLobby,
        GetListOfLobbies,
        Refresh,
        Update,
        AudioPanel,
        GraphicsPanel,
        ControlsPanel,
        GamePanel,
        Save,
        Play,
        Stop,
        Record,
        Start,
        Reset
    }
}
public abstract class Menu : MonoBehaviour
{
    public CursorExpert cursorExpert;

    private void Awake()
    {
        ServiceLocator.Global.Register<Menu>(this, true);
        cursorExpert = ServiceLocator.Global.Get<CursorExpert>();
        OnAwake();
    }
    private void Start()
    {
        ServiceLocator.Global.Register<Menu>(this, true);
        Utility.OnReRegisterRequest += ReRegisterWithServiceLocator;
        if (cursorExpert == null) { cursorExpert = ServiceLocator.Global.Get<CursorExpert>(); }

        cursorExpert.SetMenuOnScreen(true);
        OnStarting();
    }

    protected abstract void OnStarting();
    protected abstract void OnAwake();

    public abstract void TakeAction(ButtonParams action);

    public void ToggleCursor(bool onScreen) => cursorExpert.SetMenuOnScreen(onScreen);

    private void OnDestroy()
    {
        if (ServiceLocator.Global.Get<Menu>().Equals(this))
        {
            cursorExpert.SetMenuOnScreen(false);
        }

        Utility.OnReRegisterRequest -= ReRegisterWithServiceLocator;
    }

    private void OnDisable()
    {
        if (ServiceLocator.Global.Get<Menu>().Equals(this))
        {
            cursorExpert.SetMenuOnScreen(false);
        }
        //cursorExpert.RequestSetCursorLock(true);

    }

    private void ReRegisterWithServiceLocator() => ServiceLocator.For(this).Register<Menu>(this, true);
}
