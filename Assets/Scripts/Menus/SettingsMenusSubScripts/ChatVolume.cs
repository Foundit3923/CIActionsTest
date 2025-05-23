using Mirror.BouncyCastle.Bcpg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Dissonance;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using System.Collections.Generic;

public class ChatVolume : MonoBehaviour
{
    public Slider chatVolumeSlider;
    public TMP_Text playerNameText;
    public DissonanceComms comms;
    public int playerCount;
    public UnityEngine.SceneManagement.Scene chatVolumeScreen;
    public GameObject textPrefab, sliderPrefab;
    public Canvas textCanvas, sliderCanvas;
    public int samplePlayerCount;
    public string roomToName;
    public DissonanceComms channel;
    public List<TMP_Text> playerNames; // Change List<Text> to List<TMP_Text>

    private void Start()
    {
        //add component, change the text and add the player names, start with no object attached
        //sample player count is only there for testing, rremove it once it has been tested and working
        //for(var  i = 0; i < comms.Players.Count; i++)
        for (int i = 0; i < samplePlayerCount; i++)
        {
            //VoicePlayerState player = comms.FindPlayer("Player ID");
            playerNameText.text = "Player: " + playerCount;
            playerCount++;
            playerNames.Add(playerNameText);
        }
        //var channel = comms.RoomChannels.Open(roomToName, amplitudeMultiplier: 0.5f);
    }
    private void Update() => PopulateScreen();//VolumeSlider();
    private void PopulateScreen()
    {
        if (chatVolumeScreen.name == "ChatVolume")
        {
            //for (int i = 0; i < playerCount; i++)
            Debug.Log("Current Scene: " + chatVolumeScreen.name);
            foreach (var player in playerNames)
            {
                GameObject playerName = Instantiate(textPrefab, textCanvas.transform);
                GameObject volumeSlider = Instantiate(sliderPrefab, sliderCanvas.transform);
            }
        }
    }
    void VolumeSlider() => comms.RemoteVoiceVolume = chatVolumeSlider.value;

}
