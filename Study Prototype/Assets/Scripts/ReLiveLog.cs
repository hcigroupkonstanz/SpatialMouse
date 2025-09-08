using System.Collections.Generic;
using UnityEngine;
using ReLive.Events;
using Valve.VR;

// This script logs input events from various devices (mouse, VR controller, and SpatialMouse)
// by sending event data to the ReLive event system and printing log messages to the console.


public class ReLiveLog : MonoBehaviour
{
    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Action_Boolean resetAction;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            LogEvent("Left Mouse Button");
        }

        if (grabAction.stateDown)
        {
            LogEvent("Controller Trigger");
        }

        if (Input.GetKeyDown(KeyCode.Mouse3))
        {
            LogEvent("SpatialMouse Trigger");
        }

        if (Input.GetKeyDown(KeyCode.Mouse4))
        {
            LogEvent("SpatialMouse Delete");
        }
    }

    public void LogEvent(string eventName)
    {
        ReliveEvent.Log(ReliveEventType.Inputs, new Dictionary<string, object>
        {
            { "Event", eventName }
        });

        Debug.Log($"Logged Event: {eventName}");
    }
}
