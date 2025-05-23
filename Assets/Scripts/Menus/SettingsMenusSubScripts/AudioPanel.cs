using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Diagnostics.Contracts;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityServiceLocator;
using NUnit.Framework.Interfaces;
using Mirror;
using UnityEngine.Rendering;
using System.Linq;
using System;

public class AudioPanel : MonoBehaviour
{
    //for the audio panel options
    private Utility utils;
    public PlayerInformationManager pim;
    public List<string> microphones = new();
    public TMP_Dropdown microphoneDropdown;
    public Slider volumeSlider, inputSlider;
    private string selectedMic;
    public AudioMixer mixer;
    private AudioSource audioSource;
    public float minThreshold;
    //public TMP_Text countdownText;
    //public int countdownMax = 10;
    //public int countdown = 0;
    public string volumeMixerName = "MasterVolume";
    public TMP_Text micTestBtn;
    public Button record;
    public Button play;
    public Button stop;
    public AudioClip testClip;
    public bool shouldPlay;
    public Color red = new(200, 0, 0, 255);
    public Color green = new(47, 197, 36, 255);
    public Color gray = new(104, 96, 96, 255);
    string sceneName = "SettingsMenu";

    private BlackboardController blackboardController;
    private Blackboard blackboard;

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            if (FizzyNetworkManager.singleton != null)
            {
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
            }

            return ServiceLocator.For(this).Get<FizzyNetworkManager>();
        }
    }

    private void Awake()
    {
        try
        {
            blackboardController = ServiceLocator.Global.Get<BlackboardController>();
            blackboard = blackboardController.GetBlackboard();
            utils = ServiceLocator.Global.Get<Utility>();
            if (SceneManager.GetActiveScene().name == Manager.onlineScene)
            {
                //blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, out GameObject player);
                if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, out GameObject localPlayer))
                {
                    pim = localPlayer.gameObject.GetComponent<PlayerInformationManager>();
                }
                //NetworkIdentity localPlayerId = NetworkClient.localPlayer;
                //blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Players, out List<GameObject> players);
                //foreach (PlayerNetworkCommunicator player in Manager.GamePlayers)
                //{
                //    if (player.gameObject.GetComponent<NetworkIdentity>().netId == localPlayerId.netId)
                //    {
                //        pim = player.gameObject.GetComponent<PlayerInformationManager>();
                //    }
                //}
            }
            else if (SceneManager.GetActiveScene().name == sceneName)
            {
                pim = utils.gameObject.GetComponent<PlayerInformationManager>();
            }

            microphoneDropdown.ClearOptions();
            microphones = Microphone.devices.ToList();
            microphoneDropdown.AddOptions(microphones);
            int index = microphones.IndexOf(pim.settings.micDeviceName.value);
            if (index == -1)
            {
                index = 0;
                pim.settings.micDeviceIndex.value = 0;
                pim.settings.micDeviceName.value = microphoneDropdown.options[index].text;
            }

            microphoneDropdown.value = index;
            microphoneDropdown.onValueChanged.AddListener(delegate { SelectedMicrophone(); });
            shouldPlay = false;
            stop.image.color = red;
            play.gameObject.SetActive(false);
            play.image.color = green;
            play.gameObject.SetActive(false);
            record.image.color = gray;
            record.gameObject.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
    private void Start()
    {
        try
        {
            PullSettings(pim);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void PullSettings(PlayerInformationManager _pim)
    {
        try
        {
            audioSource = _pim.settings.audioSource;
            microphoneDropdown.value = _pim.settings.micDeviceIndex.value;
            volumeSlider.value = _pim.settings.masterVolume.value;
            inputSlider.value = _pim.settings.playerMicVolume.value;
            _pim.settings.volumeGroupName.value = volumeMixerName;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void ResetSettings()
    {
        try
        {
            pim.settings.micDeviceIndex.ResetProperty();
            pim.settings.masterVolume.ResetProperty();
            pim.settings.playerMicVolume.ResetProperty();
            pim.settings.volumeGroupName.ResetProperty();
            PullSettings(pim);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    void SelectedMicrophone()
    {
        try
        {
            pim.settings.micDeviceName.value = microphoneDropdown.options[microphoneDropdown.value].text;
            pim.settings.micDeviceIndex.value = microphoneDropdown.value;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
    public void SetTempVolume()
    {
        try
        {
            if (volumeSlider.value <= 0)
            {
                volumeSlider.value = 0.01f;
            }

            float level = Mathf.Log10(volumeSlider.value) * 20;
            pim.settings.masterVolume.value = volumeSlider.value;
            mixer.SetFloat(pim.settings.volumeGroupName.value, level);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    private void Update()
    {
        try
        {
            if (Microphone.IsRecording(pim.settings.micDeviceName.value))
            {
                record.image.color = red;
            }
            else
            {
                record.image.color = gray;
                if (testClip != null)
                {
                    play.gameObject.SetActive(true);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void RecordTestClip()
    {
        try
        {
            if (pim.settings.micDeviceName == null) return;
            testClip = null;
            if (Microphone.IsRecording(pim.settings.micDeviceName.value))
            {
                Microphone.End(pim.settings.micDeviceName.value);
            }

            StartCoroutine(waitForSecs(10));
            testClip = Microphone.Start(pim.settings.micDeviceName.value, true, 10, 44100);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    IEnumerator waitForSecs(int n)
    {
        yield return new WaitForSeconds(n);
        Microphone.End(pim.settings.micDeviceName.value);
    }

    public void StopTestClipPlayback()
    {
        try
        {
            stop.gameObject.SetActive(false);
            audioSource.Stop();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void PlayTestClip()
    {
        try
        {
            play.gameObject.SetActive(false);
            stop.gameObject.SetActive(true);
            audioSource.PlayOneShot(testClip);
            play.gameObject.SetActive(true);
            stop.gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void OnMicSliderValueChange()
    {
        try
        {
            float inputValue = inputSlider.value;
            if (inputValue < minThreshold)
            {
                audioSource.volume = 0f;
                pim.settings.playerMicVolume.value = audioSource.volume;
            }
            else
            {
                audioSource.volume = inputValue;
                pim.settings.playerMicVolume.value = audioSource.volume;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void ApplyTempSettings(PlayerInformationManager pim, bool isOnline)
    {
        try
        {
            float level = Mathf.Log10(pim.settings.masterVolume.value) * 20;
            mixer.SetFloat(pim.settings.volumeGroupName.value, pim.settings.masterVolume.value);
            pim.settings.audioSource.volume = pim.settings.playerMicVolume.value;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
}
