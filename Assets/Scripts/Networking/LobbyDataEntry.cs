using UnityEngine;
using Steamworks;
using TMPro;
using UnityServiceLocator;

public class LobbyDataEntry : MonoBehaviour
{
    public CSteamID lobbyID;
    public string lobbyName;
    public TMP_Text lobbyNameText;
    public SteamLobby steamLobby;
    private Utility utils;

    private void Awake() => utils = ServiceLocator.For(this).Get<Utility>();

    public void SetLobbyData()
    {
        if (lobbyName == "")
        {
            lobbyNameText.text = "Empty";
        }
        else
        {
            lobbyNameText.text = lobbyName;
        }
    }

    public void JoinLobby() => utils.ButtonInteraction(Utility.ButtonActions.Join, lobbyID);
}
