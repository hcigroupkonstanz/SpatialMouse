using ReLive.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

// This script allows for controlling the cubes using either a VR controller or the Spatial Mouse, 
// handling interactions such as grabbing, releasing, and moving objects based on user input.
public class UnifiedController : MonoBehaviour
{
    public GameObject VRController;
    public GameObject SpatialMouse;
    public GameObject monitor;
    public LineRenderer lineRenderer;
    public Material rayOne;
    public Material rayTwo;

    private const float Speed = 2f;

    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Action_Boolean resetAction;
    public SteamVR_Action_Vector2 scrollAction;

    private bool isGrabbing = false;
    private GameObject selectedObject = null;
    private RaycastHit _hit;
    public bool isUsingSpatialMouse = false;

    public VirtualMonitorInteraction virtualMonitorInteraction;
    private float modeSwitchTimer = 0f;
    private bool isModeSwitchTimerRunning = false;

    public ReLiveLog reLiveLog;

    public void StartModeSwitchTimer(GameObject cube)
    {
        
        modeSwitchTimer = Time.time;
        isModeSwitchTimerRunning = true;
        Debug.Log($"Mode switch timer started for cube: {cube.name}");
        
    }

    public void StopModeSwitchTimer(GameObject cube, string action)
    {
        if (isModeSwitchTimerRunning)
        {
            float elapsedTime = Time.time - modeSwitchTimer;
            ReliveEvent.Log(ReliveEventType.MST, new Dictionary<string, object>
            {
                { "Direction", action },
                { "CubeColor", cube.name },
                { "MST", elapsedTime }
            });

            isModeSwitchTimerRunning = false;
            Debug.Log($"Mode switch timer stopped for cube: {cube.name}, elapsed time: {elapsedTime} seconds");
        }
    }

    // Initializes the LineRenderer component and sets its properties.
    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.material = rayOne;
        lineRenderer.startWidth = 0.007f;
        lineRenderer.endWidth = 0.007f;
    }

    // Main update loop that toggles control schemes and handles input.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            isUsingSpatialMouse = !isUsingSpatialMouse;
        }

        if (isUsingSpatialMouse)
        {
            HandleSpatialMouse();
        }
        else
        {
            HandleVRController();
        }

        HandleLineRenderer();
    }

    // Manages interactions when using the VR controller, including object selection and grabbing.
    private void HandleVRController()
    {
        Vector3 trackerPosition = VRController.transform.position;
        Vector3 trackerForward = VRController.transform.forward;

        Quaternion rotation = Quaternion.AngleAxis(-45, -VRController.transform.right);
        trackerForward = rotation * trackerForward;

        if (!isGrabbing)
        {
            if (Physics.Raycast(trackerPosition, trackerForward, out _hit, 25))
            {
                GameObject hitObject = _hit.collider.gameObject;
                if (IsValidTag(hitObject.tag))
                {
                    if (resetAction.GetStateDown(SteamVR_Input_Sources.Any))
                    {
                        virtualMonitorInteraction.DecrementCubesOutside();
                        Destroy(hitObject);
                        selectedObject = null;
                        reLiveLog.LogEvent("Controller Delete");
                    }
                    if (resetAction.GetStateUp(SteamVR_Input_Sources.Any))
                    {
                        virtualMonitorInteraction.DecrementCubesOutside();
                        Destroy(hitObject);
                        selectedObject = null;
                    }

                    selectedObject = hitObject;
                }
            }
        }

        if (grabAction.state && selectedObject != null && !isGrabbing)
        {
            isGrabbing = true;
            lineRenderer.material = rayTwo;
            selectedObject.transform.SetParent(VRController.transform);

            StopModeSwitchTimer(selectedObject, "Mouse -> VRController");
        }

        if (isGrabbing && selectedObject != null)
        {
            if (!grabAction.state)
            {
                isGrabbing = false;
                lineRenderer.material = rayOne;
                selectedObject.transform.SetParent(null);
            }
        }

        if (selectedObject != null)
        {
            //HandleObjectInteraction(selectedObject);
        }

        if (!isGrabbing && !grabAction.state)
        {
            selectedObject = null;
        }
    }

    // Manages interactions when using the Spatial Mouse, similar to the VR controller handling.
    private void HandleSpatialMouse()
    {
        Vector3 trackerPosition = SpatialMouse.transform.position;
        Vector3 trackerForward = SpatialMouse.transform.right;

        if (!isGrabbing)
        {
            if (Physics.Raycast(trackerPosition, trackerForward, out _hit, 25))
            {
                GameObject hitObject = _hit.collider.gameObject;
                if (IsValidTag(hitObject.tag))
                {
                    if (Input.GetKey(KeyCode.Mouse4))
                    {
                        Destroy(hitObject);
                        virtualMonitorInteraction.DecrementCubesOutside();
                    }

                    selectedObject = hitObject;
                }
            }
        }

        if (Input.GetKey(KeyCode.Mouse3) && selectedObject != null && !isGrabbing)
        {
            isGrabbing = true;
            lineRenderer.material = rayTwo;
            selectedObject.transform.SetParent(SpatialMouse.transform);

            StopModeSwitchTimer(selectedObject, "MouseMode -> VRMode");
        }

        if (isGrabbing && selectedObject != null)
        {
            if (!Input.GetKey(KeyCode.Mouse3))
            {
                isGrabbing = false;
                lineRenderer.material = rayOne;
                selectedObject.transform.SetParent(null);
            }
        }

        if (selectedObject != null)
        {
            //HandleObjectInteraction(selectedObject);
        }

        if (!isGrabbing && !Input.GetKey(KeyCode.Mouse3))
        {
            selectedObject = null;
        }
    }



    // Controls the appearance and positioning of the LineRenderer for visual feedback.
    private void HandleLineRenderer()
    {
        
        Vector3 trackerPosition = isUsingSpatialMouse ? SpatialMouse.transform.position : VRController.transform.position;
        Vector3 trackerForward;

        if (isUsingSpatialMouse)
        {
            trackerForward = SpatialMouse.transform.right;
            // Toggle ray visibility based on middle mouse button (Mouse2)
            lineRenderer.enabled = Input.GetKey(KeyCode.Mouse2);
        }
        else
        {
            lineRenderer.enabled = true;
            Quaternion rotation = Quaternion.AngleAxis(-45, -VRController.transform.right);
            trackerForward = rotation * VRController.transform.forward;
        }

        lineRenderer.SetPosition(0, trackerPosition);
        lineRenderer.SetPosition(1, trackerPosition + trackerForward * 25);
    }

    // Handles moving the selected object based on user input from the VR controller or Spatial Mouse.
    private void HandleObjectInteraction(GameObject cube)
    {
        if (!isUsingSpatialMouse)
        {
            Vector2 thumbstickInput = scrollAction.GetAxis(SteamVR_Input_Sources.Any);
            float speedMultiplier = Mathf.Abs(thumbstickInput.y);
            Vector3 trackerForwardd = VRController.transform.forward;
            Quaternion rotation = Quaternion.AngleAxis(-45, -VRController.transform.right);
            trackerForwardd = rotation * VRController.transform.forward;

            if (thumbstickInput.y > 0f)
            {
                cube.transform.position += trackerForwardd * Speed * speedMultiplier * Time.deltaTime;
            }
            else if (thumbstickInput.y < 0f)
            {
                cube.transform.position -= trackerForwardd * Speed * speedMultiplier * Time.deltaTime;
            }
        }
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll > 0f)
            {
                cube.transform.position += SpatialMouse.transform.right * Speed * Time.deltaTime;
            }
            else if (scroll < 0f)
            {
                cube.transform.position -= SpatialMouse.transform.right * Speed * Time.deltaTime;
            }
        }
    }

    // Validates if the object's tag is one of the allowed colors for interaction.
    private bool IsValidTag(string tag)
    {
        return tag == "pink" || tag == "yellow" || tag == "green" || tag == "orange" || tag == "blue" || tag == "red";
    }
}
