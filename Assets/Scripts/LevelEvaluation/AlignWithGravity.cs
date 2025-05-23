using UnityEngine;

public class AlignWithGravity : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (this.transform.forward != Vector3.up)
        {
            this.transform.forward = Vector3.up;
        }
    }
}
