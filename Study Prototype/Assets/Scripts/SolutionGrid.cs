using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Valve.VR;

// This script manages the solution grid of cubes, loading their color configurations from a JSON file 
// and assigning corresponding materials to each cube based on the configuration.
public class SolutionGrid : MonoBehaviour
{
    public GameObject[] cubes;

    public Material redMaterial;
    public Material greenMaterial;
    public Material blueMaterial;
    public Material yellowMaterial;
    public Material pinkMaterial;
    public Material orangeMaterial;
    public Material noneMaterial;

    private CubeColorConfiguration cubeConfig;

    void Start()
    {
        LoadNextTask();
    }

    public void LoadNextTask()
    {
        LoadCubeConfiguration();
        AssignCubeMaterials();
    }

    public void LoadCubeConfiguration()
    {
        string filePath = SolutionManager.Instance.GetSolutionFilePath();

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            cubeConfig = JsonUtility.FromJson<CubeColorConfiguration>(jsonContent);
        }
        else
        {
            Debug.LogError("Cube color configuration file not found!");
        }
    }

    public void AssignCubeMaterials()
    {
        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] != null)
            {
                string slotName = $"Slot{i + 1}";
                string colorName = GetColorForSlot(slotName);

                Material assignedMaterial = GetMaterialFromColorName(colorName);

                if (assignedMaterial == null)
                {
                    cubes[i].SetActive(false);
                }
                else
                {
                    cubes[i].SetActive(true);
                    Renderer cubeRenderer = cubes[i].GetComponent<Renderer>();
                    if (cubeRenderer != null)
                    {
                        cubeRenderer.material = assignedMaterial;
                    }
                }
            }
        }
    }

    string GetColorForSlot(string slotName)
    {
        switch (slotName)
        {
            case "Slot1": return cubeConfig.Slot1;
            case "Slot2": return cubeConfig.Slot2;
            case "Slot4": return cubeConfig.Slot4;
            case "Slot5": return cubeConfig.Slot5;
            case "Slot7": return cubeConfig.Slot7;
            case "Slot8": return cubeConfig.Slot8;
            case "Slot10": return cubeConfig.Slot10;
            case "Slot11": return cubeConfig.Slot11;
            case "Slot13": return cubeConfig.Slot13;
            case "Slot14": return cubeConfig.Slot14;
            case "Slot16": return cubeConfig.Slot16;
            case "Slot17": return cubeConfig.Slot17;
            default: return "none";
        }
    }

    Material GetMaterialFromColorName(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red": return redMaterial;
            case "green": return greenMaterial;
            case "blue": return blueMaterial;
            case "yellow": return yellowMaterial;
            case "pink": return pinkMaterial;
            case "orange": return orangeMaterial;
            default: return null;
        }
    }
}

[System.Serializable]
public class CubeColorConfiguration
{
    public string Slot1;
    public string Slot2;
    public string Slot4;
    public string Slot5;
    public string Slot7;
    public string Slot8;
    public string Slot10;
    public string Slot11;
    public string Slot13;
    public string Slot14;
    public string Slot16;
    public string Slot17;
}
