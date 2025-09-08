using UnityEngine;
using TMPro;
using Oculus.Interaction.PoseDetection;
using System.Collections;
using System.Collections.Generic;
using ReLive.Events;
using ReLive.Entities;

// This script manages the interaction with the virtual monitor,
// allowing the user to drag and drop the colored cubes and interact with the buttons on the monitor.
public class VirtualMonitorInteraction : MonoBehaviour
{
    public GameObject mouse;
    public MeshRenderer mouse1;
    public MeshRenderer mouse2;
    public MeshRenderer mouse3;
    public GameObject monitor;

    public GameObject pink;
    public GameObject yellow;
    public GameObject green;
    public GameObject orange;
    public GameObject blue;
    public GameObject red;

    public GameObject front;
    public GameObject right;
    public GameObject back;
    public GameObject left;
    public GameObject top;
    public GameObject bottom;

    public GameObject done;
    public MeshRenderer doneMat;

    public MeshRenderer screen;

    public Material screenMat;
    public Material selected;

    public Material frontP;
    public Material rightP;
    public Material backP;
    public Material leftP;
    public Material topP;
    public Material bottomP;

    public MeshRenderer F;
    public MeshRenderer R;
    public MeshRenderer Ba;
    public MeshRenderer L;
    public MeshRenderer T;
    public MeshRenderer Bo;

    public Material notClicked;
    public Material clicked;

    public Material nextMaterial;

    private float sensitivity = 0.001f;
    private float lockedSensitivity = 7f;
    private Vector3 previousMousePosition;
    private bool isDraggingPink = false;
    private bool isDraggingYellow = false;
    private bool isDraggingGreen = false;
    private bool isDraggingOrange = false;
    private bool isDraggingBlue = false;
    private bool isDraggingRed = false;
    private bool isDragging = false;
    private Vector3 cubeOffset;
    private bool boxOut = false;

    private string last = "none";
    private bool isMouseLocked = true;

    private int cubesOutside = 0;
    public CubeManager cubeManager; 
    public CubePlacementChecker placementChecker;

    public GameObject frameFront;
    public GameObject frameRight;
    public GameObject frameBack;
    public GameObject frameLeft;
    public GameObject frameTop;
    public GameObject frameBottom;

    public GameObject screenframeFront;
    public GameObject screenframeRight;
    public GameObject screenframeBack;
    public GameObject screenframeLeft;
    public GameObject screenframeTop;
    public GameObject screenframeBottom;

    private float startTime;
    private bool isTimerRunning = false;
    private List<float> taskCompletionTimes = new List<float>();

    private int taskCounter = 0;
    private float pausedTime = 0f;

    private GameObject lastDraggedCube = null;



    void Start()
    {
        ShowFrame(frameFront);
        ShowScreenFrame(screenframeFront);
        previousMousePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartTask();


    }

    public void StartTask()
    {
        if (!isTimerRunning)
        {
            startTime = Time.time;
            isTimerRunning = true;
            Debug.Log("Task gestartet!");
            taskCounter++;
        }
    }

    public void PauseTask()
    {
        if (isTimerRunning)
        {
            pausedTime = Time.time - startTime; 
            isTimerRunning = false;
        }
        else
        {
            startTime = Time.time - pausedTime; 
            isTimerRunning = true;
            Debug.Log($"Task wird fortgesetzt! Fortlaufende Zeit: {pausedTime} Sekunden");
            pausedTime = 0f; 

        }
    }

    public void CompletedTask()
    {
        if (isTimerRunning)
        {
            float completionTime = Time.time - startTime;
            taskCompletionTimes.Add(completionTime);
            isTimerRunning = false;
            Debug.Log($"Task abgeschlossen! Dauer: {completionTime} Sekunden");
            ReliveEvent.Log(ReliveEventType.TCT, new Dictionary<string, object>
            {
                { "TaskCounter", taskCounter },
                { "TCT", completionTime }
            });
            isTimerRunning = false;
            StartTask();
        }
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.N))
        {
            SolutionManager.Instance.LoadNextSolution();
            CompletedTask();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            PauseTask();
        }

        // Toggle cursor locking with the 'L' key
        if (Input.GetKeyDown(KeyCode.L))
        {
            isMouseLocked = !isMouseLocked;
            if (isMouseLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (isMouseLocked)
        {
            // Use Input.GetAxis to track mouse movement while the cursor is locked
            float mouseX = Input.GetAxis("Mouse X") * sensitivity * lockedSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * lockedSensitivity;

            Vector3 monitorSpaceDelta = new Vector3(mouseX, mouseY, 0);

            mouse.transform.localPosition += monitor.transform.TransformDirection(monitorSpaceDelta);
        }
        else
        {
            // Calculate mouse delta using the previous mouse position
            Vector3 mouseDelta = Input.mousePosition - previousMousePosition;

            Vector3 monitorSpaceDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0) * sensitivity;

            mouse.transform.localPosition += monitor.transform.TransformDirection(monitorSpaceDelta);

            previousMousePosition = Input.mousePosition;
        }

        if (!isDragging)
        {
            Vector3 localPos = mouse.transform.localPosition;
            localPos.x = Mathf.Clamp(localPos.x, -monitor.transform.localScale.x / 1.9f, monitor.transform.localScale.x / 1.9f);
            localPos.y = Mathf.Clamp(localPos.y, -monitor.transform.localScale.y / 1.9f, monitor.transform.localScale.y / 1.9f);
            mouse.transform.localPosition = localPos;
        }

        if (isDragging)
        {
            if (!IsMouseInsideMonitor())
            {
                boxOut = true;

                if (cubeManager.CanPullCube())
                {
                    IncrementCubesOutside();
                    // Handle releasing of cubes based on the last interacted color
                    if (last == "pink")
                    {
                        pink.transform.parent = null;
                        StartCoroutine(ScaleOverTime(pink.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        pink.tag = "pink";
                    }
                    else if (last == "yellow")
                    {
                        yellow.transform.parent = null;
                        StartCoroutine(ScaleOverTime(yellow.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        yellow.tag = "yellow";
                    }
                    else if (last == "green")
                    {
                        green.transform.parent = null;
                        StartCoroutine(ScaleOverTime(green.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        green.tag = "green";
                    }
                    else if (last == "orange")
                    {
                        orange.transform.parent = null;
                        StartCoroutine(ScaleOverTime(orange.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        orange.tag = "orange";
                    }
                    else if (last == "blue")
                    {
                        blue.transform.parent = null;
                        StartCoroutine(ScaleOverTime(blue.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        blue.tag = "blue";
                    }
                    else if (last == "red")
                    {
                        red.transform.parent = null;
                        StartCoroutine(ScaleOverTime(red.transform, new Vector3(0.15f, 0.15f, 0.15f), 0.5f));
                        red.tag = "red";
                    }

                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            mouse1.material = clicked;
            mouse2.material = clicked;
            mouse3.material = clicked;

            float dPink = Vector3.Distance(mouse.transform.position, pink.transform.position);
            float dYellow = Vector3.Distance(mouse.transform.position, yellow.transform.position);
            float dGreen = Vector3.Distance(mouse.transform.position, green.transform.position);
            float dOrange = Vector3.Distance(mouse.transform.position, orange.transform.position);
            float dBlue = Vector3.Distance(mouse.transform.position, blue.transform.position);
            float dRed = Vector3.Distance(mouse.transform.position, red.transform.position);

            float dFront = Vector3.Distance(mouse.transform.position, front.transform.position);
            float dRight = Vector3.Distance(mouse.transform.position, right.transform.position);
            float dBack = Vector3.Distance(mouse.transform.position, back.transform.position);
            float dLeft = Vector3.Distance(mouse.transform.position, left.transform.position);
            float dTop = Vector3.Distance(mouse.transform.position, top.transform.position);
            float dBottom = Vector3.Distance(mouse.transform.position, bottom.transform.position);
            float dDone = Vector3.Distance(mouse.transform.position, done.transform.position);

            if (cubeManager.CanPullCube())
            {
                if (dPink < 0.04f)
                {
                    isDragging = true;
                    isDraggingPink = true;
                    cubeOffset = pink.transform.position - mouse.transform.position;
                    last = "pink";
                }
                else if (dYellow < 0.04f)
                {
                    isDragging = true;
                    isDraggingYellow = true;
                    cubeOffset = yellow.transform.position - mouse.transform.position;
                    last = "yellow";
                }
                else if (dGreen < 0.04f)
                {
                    isDragging = true;
                    isDraggingGreen = true;
                    cubeOffset = green.transform.position - mouse.transform.position;
                    last = "green";
                }
                else if (dOrange < 0.04f)
                {
                    isDragging = true;
                    isDraggingOrange = true;
                    cubeOffset = orange.transform.position - mouse.transform.position;
                    last = "orange";
                }
                else if (dBlue < 0.04f)
                {
                    isDragging = true;
                    isDraggingBlue = true;
                    cubeOffset = blue.transform.position - mouse.transform.position;
                    last = "blue";
                }
                else if (dRed < 0.04f)
                {
                    isDragging = true;
                    isDraggingRed = true;
                    cubeOffset = red.transform.position - mouse.transform.position;
                    last = "red";
                }

            }

            if (dFront < 0.03f)
            {
                ShowFrame(frameFront);
                ShowScreenFrame(screenframeFront);

                if (screen.material != front)
                {
                    screen.material = frontP;
                }
                if (F.material != selected)
                {
                    F.material = selected;
                }
                if (R.material != screenMat)
                {
                    R.material = screenMat;
                }
                if (Ba.material != screenMat)
                {
                    Ba.material = screenMat;
                }
                if (L.material != screenMat)
                {
                    L.material = screenMat;
                }
                if (T.material != screenMat)
                {
                    T.material = screenMat;
                }
                if (Bo.material != screenMat)
                {
                    Bo.material = screenMat;
                }
            }
            else if (dRight < 0.03f)
            {
                ShowFrame(frameRight);
                ShowScreenFrame(screenframeRight);
                if (screen.material != right)
                {
                    screen.material = rightP;
                }
                if (F.material != screenMat)
                {
                    F.material = screenMat;
                }
                if (R.material != selected)
                {
                    R.material = selected;
                }
                if (Ba.material != screenMat)
                {
                    Ba.material = screenMat;
                }
                if (L.material != screenMat)
                {
                    L.material = screenMat;
                }
                if (T.material != screenMat)
                {
                    T.material = screenMat;
                }
                if (Bo.material != screenMat)
                {
                    Bo.material = screenMat;
                }
            }
            else if (dBack < 0.03f)
            {
                ShowFrame(frameBack);
                ShowScreenFrame(screenframeBack);
                if (screen.material != back)
                {
                    screen.material = backP;
                }
                if (F.material != screenMat)
                {
                    F.material = screenMat;
                }
                if (R.material != screenMat)
                {
                    R.material = screenMat;
                }
                if (Ba.material != selected)
                {
                    Ba.material = selected;
                }
                if (L.material != screenMat)
                {
                    L.material = screenMat;
                }
                if (T.material != screenMat)
                {
                    T.material = screenMat;
                }
                if (Bo.material != screenMat)
                {
                    Bo.material = screenMat;
                }
            }
            else if (dLeft < 0.03f)
            {
                ShowFrame(frameLeft);
                ShowScreenFrame(screenframeLeft);
                if (screen.material != left)
                {
                    screen.material = leftP;
                }
                if (F.material != screenMat)
                {
                    F.material = screenMat;
                }
                if (R.material != screenMat)
                {
                    R.material = screenMat;
                }
                if (Ba.material != screenMat)
                {
                    Ba.material = screenMat;
                }
                if (L.material != selected)
                {
                    L.material = selected;
                }
                if (T.material != screenMat)
                {
                    T.material = screenMat;
                }
                if (Bo.material != screenMat)
                {
                    Bo.material = screenMat;
                }
            }
            else if (dTop < 0.03f)
            {

                ShowFrame(frameTop);
                ShowScreenFrame(screenframeTop);
                if (screen.material != top)
                {
                    screen.material = topP;
                }
                if (F.material != screenMat)
                {
                    F.material = screenMat;
                }
                if (R.material != screenMat)
                {
                    R.material = screenMat;
                }
                if (Ba.material != screenMat)
                {
                    Ba.material = screenMat;
                }
                if (L.material != screenMat)
                {
                    L.material = screenMat;
                }
                if (T.material != selected)
                {
                    T.material = selected;
                }
                if (Bo.material != screenMat)
                {
                    Bo.material = screenMat;
                }
            }
            else if (dBottom < 0.03f)
            {
                ShowFrame(frameBottom);
                ShowScreenFrame(screenframeBottom);
                if (screen.material != bottom)
                {
                    screen.material = bottomP;
                }
                if (F.material != screenMat)
                {
                    F.material = screenMat;
                }
                if (R.material != screenMat)
                {
                    R.material = screenMat;
                }
                if (Ba.material != screenMat)
                {
                    Ba.material = screenMat;
                }
                if (L.material != screenMat)
                {
                    L.material = screenMat;
                }
                if (T.material != screenMat)
                {
                    T.material = screenMat;
                }
                if (Bo.material != selected)
                {
                    Bo.material = selected;
                }
            }
            else if (dDone < 0.04f)
            {
                if (doneMat.sharedMaterial.name == nextMaterial.name)
                {
                    placementChecker.LogDeviations();
                    SolutionManager.Instance.LoadNextSolution();
                    CompletedTask();
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mouse1.material = notClicked;
            mouse2.material = notClicked;
            mouse3.material = notClicked;

            isDragging = isDraggingPink = isDraggingYellow = isDraggingGreen = isDraggingOrange = isDraggingBlue = isDraggingRed = false;

            if (!IsMouseInsideMonitor() || boxOut)
            {
                if (last == "pink") lastDraggedCube = pink;
                else if (last == "yellow") lastDraggedCube = yellow;
                else if (last == "green") lastDraggedCube = green;
                else if (last == "orange") lastDraggedCube = orange;
                else if (last == "blue") lastDraggedCube = blue;
                else if (last == "red") lastDraggedCube = red;

                Debug.Log($"Timer started for cube: {lastDraggedCube.name}");

                FindObjectOfType<UnifiedController>().StartModeSwitchTimer(lastDraggedCube);
                

                boxOut = false;
                Vector3 currentPosition = mouse.transform.localPosition;
                mouse.transform.localPosition = new Vector3(0, 0, currentPosition.z);
                Vector3 localPositionPink = new Vector3(0.3729f, -0.294f, 0.43f);
                Vector3 localPositionYellow = new Vector3(0.3729f, 0f, 0.432f);
                Vector3 localPositionGreen = new Vector3(0.3729f, 0.294f, 0.434f);
                Vector3 localPositionOrange = new Vector3(0.2259f, -0.294f, 0.431f);
                Vector3 localPositionBlue = new Vector3(0.2259f, 0f, 0.433f);
                Vector3 localPositionRed = new Vector3(0.2259f, 0.294f, 0.435f);
                Vector3 localScale = new Vector3(0.05f, 0.1f, 2f);
                Quaternion localRotation = Quaternion.identity;

                if (last == "pink")
                {
                    GameObject newPinkCube = Instantiate(pink, monitor.transform);
                    //pink.AddComponent<TracedGameObject>();
                    newPinkCube.transform.localPosition = localPositionPink;
                    newPinkCube.transform.localRotation = localRotation;
                    newPinkCube.transform.localScale = localScale;
                    pink = newPinkCube;
                    pink.tag = "Test";
                }
                else if (last == "yellow")
                {
                    GameObject newYellowCube = Instantiate(yellow, monitor.transform);
                    //yellow.AddComponent<TracedGameObject>();
                    newYellowCube.transform.localPosition = localPositionYellow;
                    newYellowCube.transform.localRotation = localRotation;
                    newYellowCube.transform.localScale = localScale;
                    yellow = newYellowCube;
                    yellow.tag = "Test";
                }
                else if (last == "green")
                {
                    GameObject newGreenCube = Instantiate(green, monitor.transform);
                    //green.AddComponent<TracedGameObject>();
                    newGreenCube.transform.localPosition = localPositionGreen;
                    newGreenCube.transform.localRotation = localRotation;
                    newGreenCube.transform.localScale = localScale;
                    green = newGreenCube;
                    green.tag = "Test";
                }
                else if (last == "orange")
                {
                    GameObject newOrangeCube = Instantiate(orange, monitor.transform);
                    //orange.AddComponent<TracedGameObject>();
                    newOrangeCube.transform.localPosition = localPositionOrange;
                    newOrangeCube.transform.localRotation = localRotation;
                    newOrangeCube.transform.localScale = localScale;
                    orange = newOrangeCube;
                    orange.tag = "Test";
                }
                else if (last == "blue")
                {
                    GameObject newBlueCube = Instantiate(blue, monitor.transform);
                    //blue.AddComponent<TracedGameObject>();
                    newBlueCube.transform.localPosition = localPositionBlue;
                    newBlueCube.transform.localRotation = localRotation;
                    newBlueCube.transform.localScale = localScale;
                    blue = newBlueCube;
                    blue.tag = "Test";
                }
                else if (last == "red")
                {
                    GameObject newRedCube = Instantiate(red, monitor.transform);
                    //red.AddComponent<TracedGameObject>();
                    newRedCube.transform.localPosition = localPositionRed;
                    newRedCube.transform.localRotation = localRotation;
                    newRedCube.transform.localScale = localScale;
                    red = newRedCube;
                    red.tag = "Test";
                }
            }
        }

        if (isDraggingPink)
        {
            UpdateCubePosition(pink.transform);
        }
        else if (isDraggingYellow)
        {
            UpdateCubePosition(yellow.transform);
        }
        else if (isDraggingGreen)
        {
            UpdateCubePosition(green.transform);
        }
        else if (isDraggingOrange)
        {
            UpdateCubePosition(orange.transform);
        }
        else if (isDraggingBlue)
        {
            UpdateCubePosition(blue.transform);
        }
        else if (isDraggingRed)
        {
            UpdateCubePosition(red.transform);
        }
    }

    public int GetCubesOutsideCount()
    {
        return cubesOutside;
    }

    public void IncrementCubesOutside()
    {
        cubesOutside++;
    }
    public void ResetCubesOutside()
    {
        cubesOutside = 0;
    }

    public void DecrementCubesOutside()
    {
        if (cubesOutside > 0)
            cubesOutside--;
    }
    private void DeactivateAllFrames()
    {
        frameFront.SetActive(false);
        frameRight.SetActive(false);
        frameBack.SetActive(false);
        frameLeft.SetActive(false);
        frameBottom.SetActive(false);
        frameTop.SetActive(false);
    }

    private void DeactivateAllScreenFrames()
    {
        screenframeFront.SetActive(false);
        screenframeRight.SetActive(false);
        screenframeBack.SetActive(false);
        screenframeLeft.SetActive(false);
        screenframeBottom.SetActive(false);
        screenframeTop.SetActive(false);
    }

    private void ShowFrame(GameObject frameToShow)
    {
        DeactivateAllFrames(); 
        frameToShow.SetActive(true);
    }

    private void ShowScreenFrame(GameObject frameToShow)
    {
        DeactivateAllScreenFrames();
        frameToShow.SetActive(true);
    }

    private bool IsMouseInsideMonitor()
    {
        Vector3 mousePosition = mouse.transform.localPosition;
        Vector3 monitorSize = monitor.transform.localScale;

        if (Mathf.Abs(mousePosition.x) <= monitorSize.x / 1.8f && Mathf.Abs(mousePosition.y) <= monitorSize.y / 1.8f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void UpdateCubePosition(Transform cubeTransform)
    {
        Vector3 newPosition = mouse.transform.position + cubeOffset;
        cubeTransform.position = newPosition;
    }

    IEnumerator ScaleOverTime(Transform target, Vector3 toScale, float duration)
    {
        Vector3 originalScale = target.localScale;
        float currentTime = 0.0f;

        while (currentTime <= duration)
        {
            target.localScale = Vector3.Lerp(originalScale, toScale, currentTime / duration);
            currentTime += Time.deltaTime;
            yield return null;
        }
        target.localScale = toScale;
    }
}
