using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityServiceLocator;

public class LogReceiver : NetworkBehaviour
{
    private LogSender _sender;
    private LogSender sender
    {
        get
        {
            if (_sender != null)
            {
                return _sender;
            }

            if (ServiceLocator.Global.TryGet<LogSender>(out _sender))
            {
                return _sender;
            }

            return null;
        }
    }
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
    public readonly SyncList<string> syncLinesToWrite = new();

    private void Awake()
    {
        ServiceLocator.Global.Register<LogReceiver>(this);
        DontDestroyOnLoad(this);
    }

    //----------------------Client Only

    public override void OnStartClient()
    {
        base.OnStartClient();
        syncLinesToWrite.OnAdd += OnItemAdded;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        syncLinesToWrite.OnAdd -= OnItemAdded;
    }

    private void OnItemAdded(int index)
    {
        if (isClient)
        {
            utils.linesToWrite.Add(syncLinesToWrite[index]);
        }
        //if (isServer)
        //{
        //    syncLinesToWrite.Clear();
        //}
    }

    ////----------------------ClientRpc

    //[ClientRpc]
    //private void StoreLogsLocally()
    //{
    //    utils.linesToWrite.AddRange(syncLinesToWrite.ToArray());
    //}

    //----------------------Server Only Functions

    [Server]
    private void Start() => StartCoroutine();
    [Server]
    private void StartCoroutine() => InvokeRepeating("SendLogs", 0, 1);
    [Server]
    private void SendLogs()
    {
        syncLinesToWrite.AddRange(sender.syncLinesToStore.ToArray());
        sender.Clear();

    }
    [Server]
    public void Add(string message) => syncLinesToWrite.Add(message);
    [Server]
    public void AddRange(List<string> range) => syncLinesToWrite.AddRange(range);
    [Server]
    public void Clear() => syncLinesToWrite.Clear();
}
