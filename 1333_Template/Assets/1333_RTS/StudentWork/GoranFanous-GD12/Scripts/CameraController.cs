using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Speed at which the camera moves when panning
    public float panSpeed = 20f;

    // Thickness of the screen border (used for edge scrolling, not currently used)
    public float panBorderThickness = 10f;

    // Minimum field of view (zoom in limit)
    public float minFOV = 15f;

    // Maximum field of view (zoom out limit)
    public float MaxFOV = 90f;

    // Speed at which the camera zooms in and out
    public float zoomSpeed = 10f;

    void Update()
    {
        // Check for panning and zooming input every frame
        CameraPanning();
        CameraZooming();
    }

    // Handles camera movement using WASD keys
    void CameraPanning()
    {
        // Get current camera position
        Vector3 currentCameraPosition = transform.position;

        // Move camera forward (along Z axis)
        if (Input.GetKey(KeyCode.W))
        {
            currentCameraPosition.z += panSpeed * Time.deltaTime;
        }

        // Move camera backward
        if (Input.GetKey(KeyCode.S))
        {
            currentCameraPosition.z -= panSpeed * Time.deltaTime;
        }

        // Move camera right (along X axis)
        if (Input.GetKey(KeyCode.D))
        {
            currentCameraPosition.x += panSpeed * Time.deltaTime;
        }

        // Move camera left
        if (Input.GetKey(KeyCode.A))
        {
            currentCameraPosition.x -= panSpeed * Time.deltaTime;
        }

        // Apply updated position to the camera
        transform.position = currentCameraPosition;
    }

    // Handles zooming in and out using the mouse scroll wheel
    void CameraZooming()
    {
        // Get current field of view from the main camera
        float currentFieldOfView = Camera.main.fieldOfView;

        // Adjust the field of view based on scroll input
        currentFieldOfView += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        // Clamp the field of view to stay within defined min and max limits
        currentFieldOfView = Mathf.Clamp(currentFieldOfView, minFOV, MaxFOV);

        // Apply the new field of view to the camera
        Camera.main.fieldOfView = currentFieldOfView;
    }

}
