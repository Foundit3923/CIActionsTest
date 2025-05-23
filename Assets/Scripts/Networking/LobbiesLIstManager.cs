using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityServiceLocator;

public class LobbiesListManager : MonoBehaviour
{

    public GameObject lobbyDataItemPrefab;
    public GameObject lobbyListContent;

    public GameObject lobbiesButton, hostButton;

    public List<GameObject> listOfLobbies = new();

    private SteamLobby steamLobby;

    private void Awake()
    {
        ServiceLocator.ForSceneOf(this).Register<LobbiesListManager>(this);
        steamLobby = ServiceLocator.For(this).Get<SteamLobby>();
        InvokeRepeating("GetListOfLobbies", 0f, 30f);
    }

    public void DestroyLobbies()
    {
        foreach (GameObject item in listOfLobbies)
        {
            Destroy(item);
        }

        listOfLobbies.Clear();
    }

    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        for (int i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID == result.m_ulSteamIDLobby)
            {

                GameObject createdItem = Instantiate(lobbyDataItemPrefab);

                createdItem.GetComponent<LobbyDataEntry>().lobbyID = (CSteamID)lobbyIDs[i].m_SteamID;

                createdItem.GetComponent<LobbyDataEntry>().lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");

                createdItem.GetComponent<LobbyDataEntry>().SetLobbyData();

                createdItem.transform.SetParent(lobbyListContent.transform);
                createdItem.transform.localScale = Vector3.one;
                bool dupes = listOfLobbies.Where(x => x.GetComponent<LobbyDataEntry>().lobbyName == createdItem.GetComponent<LobbyDataEntry>().lobbyName).ToList().Count > 0;
                if (dupes)
                {
                    Destroy(createdItem);
                }
                else
                {
                    listOfLobbies.Add(createdItem);
                }
            }
        }
    }

    public void GetListOfLobbies() => steamLobby.GetLobbiesList();

}
