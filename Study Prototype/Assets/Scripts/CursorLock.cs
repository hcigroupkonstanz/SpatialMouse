using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script handles mouse input, allowing for mouse locking and calculating mouse movement 
// based on sensitivity settings.
public class CursorLock : MonoBehaviour
{
    public float sensitivity = 1.0f;
    private Vector3 previousMousePosition;
    private bool isMouseLocked = false;

    void Start()
    {
        previousMousePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            isMouseLocked = !isMouseLocked;
            Cursor.lockState = isMouseLocked ? CursorLockMode.Locked : CursorLockMode.None;

            if (isMouseLocked)
            {
                previousMousePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                Cursor.SetCursor(null, previousMousePosition, CursorMode.Auto);
            }
        }

        if (isMouseLocked)
        {
            Vector3 mouseDelta = Input.mousePosition - previousMousePosition;
            Vector3 monitorSpaceDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0) * sensitivity;

            Cursor.lockState = CursorLockMode.None;
            Cursor.lockState = CursorLockMode.Locked;

            previousMousePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Cursor.SetCursor(null, previousMousePosition, CursorMode.Auto);
        }
    }
}
