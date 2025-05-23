using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MenuEnums;

namespace MenuEnums
{
    public static class PauseMenuActions
    {
        public const MenuActions Return = MenuActions.Resume;
        public const MenuActions Save = MenuActions.Quit;
        public const MenuActions Settings = MenuActions.Settings;
    }
}

public class PauseMenu : Menu
{
    public Button resumeButton, settingsButton, quitButton;

    private void Awake()
    {
        //Cursor.visible = true;
    }
    protected override void OnStarting()
    {
        return;
    }

    protected override void OnAwake()
    {
        return;
    }

    public override void TakeAction(ButtonParams arg)
    {
        MenuActions action = arg.baseMenuAction;
        switch (action)
        {
            case MenuActions.Resume:
                SceneManager.UnloadSceneAsync("PauseMenu");
                break;
            case MenuActions.Settings:
                SceneManager.LoadSceneAsync("SettingsMenu", LoadSceneMode.Additive);
                SceneManager.UnloadSceneAsync("PauseMenu");
                break;
            case MenuActions.Quit:
                SceneManager.LoadScene("MainMenu");
                break;
        }
    }
    private void OnDestroy()
    {
        //Cursor.visible = false;
    }
}
