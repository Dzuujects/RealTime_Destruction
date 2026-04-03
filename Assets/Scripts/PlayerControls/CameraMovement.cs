using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    // Camera movement parameters
    public float sens = 3f;
    public float slowSpeed = 2f;
    public float normalSpeed = 10f;
    public float fastSpeed = 50f;
    float moveSpeed;

    // Have the cursor hidden and locked to the center of the screen
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Call movement and rotation methods every frame for camera control
    void Update()
    { 
        Movement();
        Rotation();
    }

    // Handle camera rotation based on mouse input
    public void Rotation()
    {
        Vector3 mouseInput = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        transform.Rotate(mouseInput * sens);
        Vector3 eulerRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, 0);
    }


    // Handle camera movement based on keyboard input
    public void Movement()
    {
        // Check speed of player based on key inputs and move accordingly
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if(Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = fastSpeed;
        }
        else if(Input.GetKey(KeyCode.C))
        {
            moveSpeed = slowSpeed;
        }
        else
        {
            moveSpeed = normalSpeed;
        }
        transform.Translate(input * moveSpeed * Time.deltaTime);
        
        // Check for vertical movement inputs
        if(Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
    }
}
