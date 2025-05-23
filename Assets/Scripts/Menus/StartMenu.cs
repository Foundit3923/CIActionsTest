using MenuEnums;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuEnums
{
    public static class StartMenuActions
    {
        public const MenuActions Start = MenuActions.Start;
        public const MenuActions Settings = MenuActions.Settings;
        public const MenuActions Credits = MenuActions.Credits;
        public const MenuActions Quit = MenuActions.Quit;

    }
}

public class StartMenu : Menu
{
    public void Awake()
    {
        //Cursor.visible = true;
    }

    public void OnDestroy()
    {
        //Cursor.visible = false;
    }

    protected override void OnStarting() => throw new System.NotImplementedException();

    protected override void OnAwake() => throw new System.NotImplementedException();

    public override void TakeAction(ButtonParams arg)
    {
        MenuActions action = arg.baseMenuAction;
        switch (action)
        {
            case MenuActions.Start:
                SceneManager.LoadScene("LobbyBrowser");
                break;
            case MenuActions.Settings:
                SceneManager.LoadScene("SettingsMenu");
                break;
            case MenuActions.Credits:
                SceneManager.LoadScene("Credits");
                break;
            case MenuActions.Quit:
                Application.Quit();
                break;
            default:
                break;
        }
    }
}
