using System.Collections.Generic;
using System.Security.Principal;
using Mirror;
using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(NetworkIdentity))]
public class LogSender : NetworkBehaviour
{
    private Utility _utils;
    public Utility utils
    {
        get
        {
            if (_utils != null)
            {
                return _utils;
            }

            return _utils = ServiceLocator.Global.Get<Utility>();
        }
    }
    public readonly SyncList<string> syncLinesToStore = new();

    private void Awake()
    {
        ServiceLocator.Global.Register<LogSender>(this);
        DontDestroyOnLoad(this);
    }
    public void Add(string message) => syncLinesToStore.Add(message);

    public void AddRange(List<string> range) => syncLinesToStore.AddRange(range);

    public void Clear() => syncLinesToStore.Clear();

    //-------------------Command Test
    [Command]
    public void cmdTest() => utils.DebugOut("Test");

    //-------------------Client only 
    [Client]
    public override void OnStartClient()
    {
        base.OnStartClient();
        syncLinesToStore.AddRange(utils.linesToWrite);
    }
    [Client]
    public override void OnStopClient()
    {
        base.OnStopClient();
        utils.LocalSteamName = utils.OfflineName;
    }
}
