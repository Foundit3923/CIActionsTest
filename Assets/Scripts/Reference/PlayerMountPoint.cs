using UnityEngine;

public class PlayerMountPoint : MonoBehaviour
{
    //idk, right hand or something
    [SerializeField] Transform mountPoint = null;

    public Transform MountPoint => mountPoint;

    private void Awake() => mountPoint = transform;
}
