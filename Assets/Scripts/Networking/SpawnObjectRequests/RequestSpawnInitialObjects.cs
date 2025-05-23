using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityServiceLocator;

public class RequestSpawnInitialObjects : MonoBehaviour
{
    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
        }
    }

    private Utility utils;

    private SerializableNetworkSpawnObject objectsToSpawn;
    SerializableNetworkSpawnObject ObjectsToSpawn
    {
        get
        {
            if (objectsToSpawn != null)
            {
                return objectsToSpawn;
            }

            return objectsToSpawn = GetComponent<SerializableNetworkSpawnObject>();
        }
    }

    bool wakeup = false;
    bool finished = false;

    private void Update()
    {
        if (!finished)
        {
            //if (!NetworkServer.activeHost)
            //{
            //    finished = true;
            //}

            if (NetworkServer.active)
            {
                wakeup = true;
            }

            if (wakeup)
            {
                finished = true;
                wakeup = false;
                Startup();
            }
        }
    }

    [Server]
    private void Startup()
    {
        if (ObjectsToSpawn.NetworkSpawnObjectList != null)
        {
            //Get utils scripts
            utils = ServiceLocator.For(this).Get<Utility>();

            //Register all objects to spawn with Server
            foreach (SerializableNetworkSpawnObject.NetworkSpawnObject toSpawn in ObjectsToSpawn.NetworkSpawnObjectList)
            {
                if (!Manager.spawnPrefabs.Contains(toSpawn.ObjectToSpawn))
                {
                    Manager.spawnPrefabs.Add(toSpawn.ObjectToSpawn);
                }
            }

            InitializeAndSpawn();
        }
    }

    [Server]
    private void InitializeAndSpawn()
    {
        for (int i = 0; i < ObjectsToSpawn.NetworkSpawnObjectList.Count; i++)
        {
            if (ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent != null)
            {
                //in hierarchy
                if (ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent.TryGetComponent<NetworkIdentity>(out NetworkIdentity parentIdentity))
                {
                    ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent.gameObject.RemoveComponent<PropRoot>();
                    ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent.gameObject.RemoveComponent<NetworkIdentity>();
                }
            }
            else
            {
                //top level
                if (ObjectsToSpawn.NetworkSpawnObjectList[i].Target.TryGetComponent<NetworkIdentity>(out NetworkIdentity TargetIdentity))
                {
                    ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent.gameObject.RemoveComponent<NetworkIdentity>();
                }
            }

            try
            {
                GameObject obj = Instantiate(ObjectsToSpawn.NetworkSpawnObjectList[i].ObjectToSpawn, ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.position, ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.rotation, ObjectsToSpawn.NetworkSpawnObjectList[i].Target.transform.parent);
                NetworkServer.Spawn(obj);
            }
            catch (Exception e)
            {
                utils.DebugOut("Exception: " + e);
                utils.DebugOut(new System.Diagnostics.StackTrace());
            }
        }
    }
}
