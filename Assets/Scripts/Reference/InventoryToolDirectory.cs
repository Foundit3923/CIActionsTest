using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;
using static InventoryManager;
using System.Linq;
using static MountPointDirectory;

public class InventoryToolDirectory : MonoBehaviour
{
    [Serializable]
    public class InventoryToolEntry : ISerializable
    {
        public InventoryItems type;
        public GameObject prefab;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Type", this.type);
            info.AddValue("ToolPrefab", this.prefab);
        }
    }
    [SerializeField]
    public List<InventoryToolEntry> ToolDirectoryList = new();

    public InventoryToolEntry GetTool(InventoryItems type) => ToolDirectoryList.Where(d => d.type == type).ToList().FirstOrDefault();
}
