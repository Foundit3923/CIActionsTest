using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    //mouse sensativity
    public float sensX;
    public float sensY;

    //direction player is facing
    public Transform orientation;

    //current rotation for the player
    public float xRotation;
    public float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        //get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        //to lock to rotation of the camera within the normal human range of view
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //rotate the camera and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

    }
}
