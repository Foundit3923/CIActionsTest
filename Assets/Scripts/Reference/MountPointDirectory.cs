using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;
using System.Linq;

public class MountPointDirectory : MonoBehaviour
{
    public enum MountPointTargetType
    {
        Hand,
        HeldItem,
        None,
        Grip
    }
    [Serializable]
    public class MountPointEntry : ISerializable
    {
        public MountPointTargetType type;
        public PlayerMountPoint target;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Type", this.type);
            info.AddValue("Target", this.target);
        }
    }
    [SerializeField]
    List<MountPointEntry> TargetDirectoryList = new();

    public MountPointEntry GetTarget(MountPointTargetType type) => TargetDirectoryList.Where(d => d.type == type).ToList().FirstOrDefault();
}
