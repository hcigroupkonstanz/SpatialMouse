using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using ReLive.Events;

// This class checks if all cubes are correctly placed in their designated grid slots
// by verifying their positions and rotations against predefined tolerances.

public class CubePlacementChecker : MonoBehaviour
{
    private List<(string tag, Transform slotTransform)> correctPositions = new List<(string tag, Transform slotTransform)>();

    public float positionTolerance = 0.001f;
    public float rotationTolerance = 5.0f;

    public GameObject gridParent;

    public Material correctPlacementMaterial;
    public Material originalMaterial;
    public Material cubeInsideMaterial;

    public MeshRenderer next;
    public Material nextMaterial;
    public Material nextOriginalMaterial;

    public Text done;

    private CubeColorConfiguration cubeConfig;
    private bool isCheckingEnabled = true;

    public VirtualMonitorInteraction virtualMonitorInteraction;

    private int correctlyPlacedCount = 0;
    private HashSet<Transform> countedCorrectCubes = new HashSet<Transform>();

    public Material finishColor;

    private float modeSwitchTimer = 0f;
    private bool timerRunning = false;
    public UnifiedController unifiedController;

    private GameObject activeCube;

    void Start()
    {
        LoadNextTask();
    }

    public void LoadNextTask()
    {
        LoadCubeConfiguration();
        AssignCorrectPositions();
        virtualMonitorInteraction.ResetCubesOutside();
        correctlyPlacedCount = 0;
        countedCorrectCubes.Clear();
    }

    void Update()
    {
        HandleToggleInput();

        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            if (!unifiedController.isUsingSpatialMouse)
            {
                LogModeSwitchTime("VRController -> Mouse");
            }
            else
            {
                LogModeSwitchTime("VRMode -> MouseMode");
            }

        }

        if (AllCubesCorrectlyPlaced())
        {
            if (next.material != nextMaterial)
            {
                next.material = nextMaterial;
                done.color = Color.black;
                ApplyFinishColorToCubes();
            }
        }
        else
        {
            if (next.material != nextOriginalMaterial)
            {
                next.material = nextOriginalMaterial;
                done.color = new Color(0.5f, 0.5f, 0.5f);
                if(!isCheckingEnabled)
                {
                    ResetAllSlotMaterials();
                }
            }
        }
    }

    void HandleToggleInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            isCheckingEnabled = !isCheckingEnabled;
            if (!isCheckingEnabled)
            {
                ResetAllSlotMaterials();
                ResetNextMaterial();
                done.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }

    void ResetAllSlotMaterials()
    {
        foreach (var entry in correctPositions)
        {
            MeshRenderer slotRenderer = entry.slotTransform.GetComponent<MeshRenderer>();
            slotRenderer.material = originalMaterial;
        }
    }

    void ResetNextMaterial()
    {
        next.material = nextOriginalMaterial;
    }

    public void LoadCubeConfiguration()
    {
        string filePath = SolutionManager.Instance.GetSolutionFilePath();

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            cubeConfig = JsonUtility.FromJson<CubeColorConfiguration>(jsonContent);
        }
    }

    public void AssignCorrectPositions()
    {
        correctPositions.Clear();
        foreach (Transform slot in gridParent.transform)
        {
            string correctTag = "none";
            switch (slot.name)
            {
                case "Slot1": correctTag = cubeConfig.Slot1; break;
                case "Slot2": correctTag = cubeConfig.Slot2; break;
                case "Slot4": correctTag = cubeConfig.Slot4; break;
                case "Slot5": correctTag = cubeConfig.Slot5; break;
                case "Slot7": correctTag = cubeConfig.Slot7; break;
                case "Slot8": correctTag = cubeConfig.Slot8; break;
                case "Slot10": correctTag = cubeConfig.Slot10; break;
                case "Slot11": correctTag = cubeConfig.Slot11; break;
                case "Slot13": correctTag = cubeConfig.Slot13; break;
                case "Slot14": correctTag = cubeConfig.Slot14; break;
                case "Slot16": correctTag = cubeConfig.Slot16; break;
                case "Slot17": correctTag = cubeConfig.Slot17; break;
                default: correctTag = "none"; break;
            }

            correctPositions.Add((correctTag, slot));
        }
    }

    bool AllCubesCorrectlyPlaced()
    {
        bool allCorrect = true;

        foreach (var entry in correctPositions)
        {
            MeshRenderer slotRenderer = entry.slotTransform.GetComponent<MeshRenderer>();

            if (entry.tag == "none")
            {
                Collider[] colliders = Physics.OverlapSphere(entry.slotTransform.position, positionTolerance);
                bool hasCube = false;

                foreach (Collider collider in colliders)
                {
                    if (IsCubeTag(collider.tag))
                    {
                        hasCube = true;
                        break;
                    }
                }

                if (!hasCube)
                {
                    if (isCheckingEnabled && slotRenderer.material != originalMaterial)
                    {
                        slotRenderer.material = originalMaterial;
                    }
                }
                else
                {
                    if (isCheckingEnabled && slotRenderer.material != cubeInsideMaterial)
                    {
                        slotRenderer.material = cubeInsideMaterial;
                    }
                    allCorrect = false;
                }
            }
            else
            {
                Collider[] colliders = Physics.OverlapSphere(entry.slotTransform.position, positionTolerance);
                bool foundCorrectCube = false;
                bool foundWrongCube = false;

                foreach (Collider collider in colliders)
                {
                    if (collider.CompareTag(entry.tag))
                    {
                        if (IsCubeCorrectlyPlaced(collider.gameObject, entry.slotTransform.position, entry.slotTransform.rotation))
                        {
                            foundCorrectCube = true;

                            if (!countedCorrectCubes.Contains(entry.slotTransform))
                            {
                                countedCorrectCubes.Add(entry.slotTransform);
                                correctlyPlacedCount++;
                            }

                            if (isCheckingEnabled && slotRenderer.material != correctPlacementMaterial)
                            {
                                slotRenderer.material = correctPlacementMaterial;
                            }
                            break;
                        }
                    }
                    else if (IsCubeTag(collider.tag))
                    {
                        foundWrongCube = true;
                    }
                }

                if (!foundCorrectCube)
                {
                    if (foundWrongCube)
                    {
                        if (isCheckingEnabled && slotRenderer.material != cubeInsideMaterial)
                        {
                            slotRenderer.material = cubeInsideMaterial;
                        }
                    }
                    else
                    {
                        if (isCheckingEnabled && slotRenderer.material != originalMaterial)
                        {
                            slotRenderer.material = originalMaterial;
                        }
                    }

                    if (countedCorrectCubes.Contains(entry.slotTransform))
                    {
                        countedCorrectCubes.Remove(entry.slotTransform);
                        correctlyPlacedCount--;
                    }

                    allCorrect = false;
                }
            }
        }

        return allCorrect;
    }




    bool IsCubeTag(string tag)
    {
        return tag == "red" || tag == "blue" || tag == "green" ||
               tag == "yellow" || tag == "pink" || tag == "orange";
    }

    bool IsCubeCorrectlyPlaced(GameObject cube, Vector3 correctPosition, Quaternion correctRotation)
    {
        float distance = Vector3.Distance(cube.transform.position, correctPosition);
        Quaternion currentRotation = cube.transform.rotation;
        bool rotationIsCorrect = IsRotationWithinTolerance(currentRotation, correctRotation);
        return distance <= positionTolerance && rotationIsCorrect;
    }

    bool IsRotationWithinTolerance(Quaternion currentRotation, Quaternion correctRotation)
    {
        Quaternion[] validRotations = new Quaternion[]
        {
            correctRotation,
            correctRotation * Quaternion.Euler(0, 90, 0),
            correctRotation * Quaternion.Euler(0, 180, 0),
            correctRotation * Quaternion.Euler(0, 270, 0),
            correctRotation * Quaternion.Euler(90, 0, 0),
            correctRotation * Quaternion.Euler(90, 90, 0),
            correctRotation * Quaternion.Euler(90, 180, 0),
            correctRotation * Quaternion.Euler(90, 270, 0),
            correctRotation * Quaternion.Euler(180, 0, 0),
            correctRotation * Quaternion.Euler(180, 90, 0),
            correctRotation * Quaternion.Euler(180, 180, 0),
            correctRotation * Quaternion.Euler(180, 270, 0),
            correctRotation * Quaternion.Euler(270, 0, 0),
            correctRotation * Quaternion.Euler(270, 90, 0),
            correctRotation * Quaternion.Euler(270, 180, 0),
            correctRotation * Quaternion.Euler(270, 270, 0)
        };

        foreach (Quaternion validRotation in validRotations)
        {
            float angleDifference = Quaternion.Angle(currentRotation, validRotation);
            if (angleDifference <= rotationTolerance)
            {
                return true;
            }
        }

        return false;
    }

    public int GetCorrectlyPlacedCount()
    {
        return correctlyPlacedCount;
    }

    private void ApplyFinishColorToCubes()
    {

        foreach (var entry in correctPositions)
        {
            MeshRenderer slotRenderer = entry.slotTransform.GetComponent<MeshRenderer>();
            if (slotRenderer != null)
            {
                slotRenderer.material = finishColor;
            }
        }
    }

    public void LogDeviations()
    {
        foreach (var entry in correctPositions)
        {
            Collider[] colliders = Physics.OverlapSphere(entry.slotTransform.position, positionTolerance);
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag(entry.tag))
                {
                    GameObject cube = collider.gameObject;
                    float positionDeviation = Vector3.Distance(cube.transform.position, entry.slotTransform.position);

                    Quaternion currentRotation = cube.transform.rotation;
                    Quaternion expectedRotation = entry.slotTransform.rotation;

                    Quaternion[] validRotations = new Quaternion[]
                    {
                    expectedRotation,
                    expectedRotation * Quaternion.Euler(0, 90, 0),
                    expectedRotation * Quaternion.Euler(0, 180, 0),
                    expectedRotation * Quaternion.Euler(0, 270, 0),
                    expectedRotation * Quaternion.Euler(90, 0, 0),
                    expectedRotation * Quaternion.Euler(90, 90, 0),
                    expectedRotation * Quaternion.Euler(90, 180, 0),
                    expectedRotation * Quaternion.Euler(90, 270, 0),
                    expectedRotation * Quaternion.Euler(180, 0, 0),
                    expectedRotation * Quaternion.Euler(180, 90, 0),
                    expectedRotation * Quaternion.Euler(180, 180, 0),
                    expectedRotation * Quaternion.Euler(180, 270, 0),
                    expectedRotation * Quaternion.Euler(270, 0, 0),
                    expectedRotation * Quaternion.Euler(270, 90, 0),
                    expectedRotation * Quaternion.Euler(270, 180, 0),
                    expectedRotation * Quaternion.Euler(270, 270, 0)
                    };
                }
            }
        }
    }

    private void LogModeSwitchTime(string actionType)
    {
        if (timerRunning && activeCube != null)
        {
            float elapsedTime = Time.time - modeSwitchTimer;
            ReliveEvent.Log(ReliveEventType.MST, new Dictionary<string, object>
            {
                { "Direction", actionType },
                { "CubeColor", activeCube.name },
                { "MST", elapsedTime }
            });

            timerRunning = false;
            activeCube = null;
        }
    }

    private void StartModeSwitchTimer(GameObject cube)
    {
        modeSwitchTimer = Time.time;
        timerRunning = true;
        activeCube = cube;
    }

}