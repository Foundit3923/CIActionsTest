using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

public class TargetDirectory : MonoBehaviour
{
    public enum TargetType
    {
        Camera,
        Sjena,
        RightArmRoot,
        RightArmMid,
        RightArmTip,
        LeftArmRoot,
        LeftArmMid,
        LeftArmTip,
        RightArm,
        LeftArm,
        RightIndex1,
        RightIndex2,
        RightIndex3,
        RightIndexEnd,
        RightMiddle1,
        RightMiddle2,
        RightMiddle3,
        RightMiddleEnd,
        RightRing1,
        RightRing2,
        RightRing3,
        RightRingEnd,
        RightPinky1,
        RightPinky2,
        RightPinky3,
        RightPinkyEnd,
        RightThumb1,
        RightThumb2,
        RightThumb3,
        RightThumbEnd
    }
    [Serializable]
    public class Entry : ISerializable
    {
        public TargetType type;
        public Target target;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Type", this.type);
            info.AddValue("Target", this.target);
        }
    }
    [SerializeField]
    List<Entry> TargetDirectoryList= new();

    public Entry GetTarget(TargetType type)
    {
        Entry result = TargetDirectoryList.Where(d => d.type == type).FirstOrDefault();
        return result;
    }
}
