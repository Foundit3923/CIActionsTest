using UnityEngine;

public class PlayerInitialSettings : MonoBehaviour
{
    private Settings playerSettings;
    private const string GAMEFIRSTRUN = "Game's first run";
    private void Awake()
    {
        //bool isFirstRun = PlayerPrefs.GetInt(GAMEFIRSTRUN, 1) == 1;

        //if (isFirstRun)
        //{
        //    Debug.LogError("Game's First Run");
        //    PlayerPrefs.SetInt("GAMEFIRSTRUN", 0);
        //}
        //else
        //{
        //    Debug.LogError("This script ran after first run, it is not working correctly");
        //}

        //playerSettings = GetComponent<Settings>();
        //GraphicsSettings();
        //PlayerAudioSettings();
        //ControlsSettings();
        //GameSettings();
        //PlayerPrefs.Save();

    }

    private void GameSettings()
    {
        //text, look sensativity, invert x and y
        PlayerPrefs.SetString("Language", "English");
        PlayerPrefs.SetFloat("MouseSens", 0.5f);
        PlayerPrefs.SetInt("InvertX", 0);
        PlayerPrefs.SetInt("InvertY", 0);
    }
    private void ControlsSettings()
    {
        //controller settings
        //PlayerPrefs.
    }
    //to set the initial players settings of the game. 
    private void GraphicsSettings()
    {
        //brightness, fullscreen, vsync, pixelization
        PlayerPrefs.SetFloat("Brightness", 0);
        PlayerPrefs.SetInt("Fullscreen", 0);
        PlayerPrefs.SetInt("VsyncCount", 0);
        PlayerPrefs.SetFloat("PixelizationValue", 0);
    }

    private void PlayerAudioSettings()
    {
        //microphone, voice input, master volume
        PlayerPrefs.SetInt("Microphone", 0);
        PlayerPrefs.SetString("MicName", playerSettings.micDeviceName.value);
        PlayerPrefs.SetFloat("Volume", playerSettings.masterVolume.value);
    }
}
