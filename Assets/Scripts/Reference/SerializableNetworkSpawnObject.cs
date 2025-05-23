using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;
using System.Linq;

public class SerializableNetworkSpawnObject : MonoBehaviour
{
    [Serializable]
    public class NetworkSpawnObject : ISerializable
    {
        public GameObject ObjectToSpawn;
        public GameObject Target;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ObjectToSpawn", this.ObjectToSpawn);
            info.AddValue("Target", this.Target);
        }
    }
    [SerializeField]
    public List<NetworkSpawnObject> NetworkSpawnObjectList = new();

    public NetworkSpawnObject GetTarget(GameObject ObjectToSpawn) => NetworkSpawnObjectList.Where(d => d.ObjectToSpawn == ObjectToSpawn).ToList().FirstOrDefault();
}
