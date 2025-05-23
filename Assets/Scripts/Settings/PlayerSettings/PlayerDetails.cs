using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

public class PlayerDetails : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSteamNameChanged))]
    public string steamName;

    public TextMeshProUGUI nameTagText;

    private Camera playerCamera;

    private void Awake()
    {
        if (isLocalPlayer)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnSteamNameChanged(string oldName, string newName)
    {
        if (nameTagText != null)
        {
            nameTagText.text = newName;
        }
        Debug.Log($"{oldName} has changed their name to {newName}");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isLocalPlayer)
        {
            // Set steam name when the local player is initialized
            steamName = SteamFriends.GetPersonaName();
            CmdSetSteamName(steamName);
        }
    }

    [Command]
    void CmdSetSteamName(string name)
    {
        steamName = name;
    }

    void Update()
    {
        if (nameTagText != null && isLocalPlayer)
        {
            playerCamera = Camera.main;  // This is the local player's camera

            nameTagText.transform.LookAt(playerCamera.transform);
        }      

        if (nameTagText != null && Camera.main != null)
        {
            // Make the canvas (not the text itself) look at the camera
            nameTagText.transform.rotation = Quaternion.LookRotation(nameTagText.transform.position - Camera.main.transform.position);
        }
        
    }
}
