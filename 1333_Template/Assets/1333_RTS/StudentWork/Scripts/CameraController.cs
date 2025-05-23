using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;

    public float minFOV = 15f;
    public float MaxFOV = 90f;
    public float zoomSpeed = 10f;

 
    void Update()
    {
        CameraPanning();
        CameraZooming();
        
    }

    void CameraPanning()
    {
        Vector3 pos = transform.position;

        if (Input.GetKey(KeyCode.W))
        {
            pos.z += panSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.z -= panSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            pos.x += panSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            pos.x -= panSpeed * Time.deltaTime;
        }
        transform.position = pos;
    }

    void CameraZooming()
    {
         float fov = Camera.main.fieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        fov = Mathf.Clamp(fov, minFOV, MaxFOV);
        Camera.main.fieldOfView = fov;
    }

    
}
