using System.IO;
using UnityEngine;

// This script manages solution tasks by ensuring a singleton instance,
// retrieving and loading solution files, advancing the solution counter,
// and deleting cubes from the scene when a task is finished.

public class SolutionManager : MonoBehaviour
{
    public static SolutionManager Instance { get; private set; }
    public CubePlacementChecker cubePlacementChecker;
    public SolutionGrid solutionGrid;

    private string[] tagsToDelete = { "green", "blue", "yellow", "red", "pink", "orange" };

    private int solutionNumber = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetSolutionFilePath()
    {
        return Path.Combine(Application.streamingAssetsPath, $"solution{solutionNumber}.json");
    }

    void DeleteCubesWithTags()
    {
        foreach (string tag in tagsToDelete)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject obj in objectsWithTag)
            {
                if (obj.CompareTag(tag))
                {
                    Destroy(obj);
                }
            }
        }
    }

    public void LoadNextSolution()
    {
        solutionNumber++;
        string filePath = GetSolutionFilePath();

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            Debug.Log($"Loaded solution content from: {filePath}");
            cubePlacementChecker.LoadNextTask();
            solutionGrid.LoadNextTask();

            DeleteCubesWithTags();

        }
        else
        {
            Debug.LogError($"Solution file not found: {filePath}");
        }
        
    }
}
