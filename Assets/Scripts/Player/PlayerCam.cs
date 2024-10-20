using System.Collections;

using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;



public class PlayerCam : MonoBehaviour

{
    public float sensX;
    public float sensY;
    public Transform orientation;
    public float mouseSensitivity = 100f;

    public Transform playerBody;
    public Transform cameraPosition;

    float xRotation;
    float yRotation;

    private void Start()

    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }
    
    private void LateUpdate()

    {
        transform.position = cameraPosition.position;

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        

    }

}