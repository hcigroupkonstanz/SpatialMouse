using UnityEngine;

public class CubeManager : MonoBehaviour
{
    private int cubesOutside = 0; 
    private int cubesPlacedCorrectly = 0; 

    public VirtualMonitorInteraction virtualMonitorInteraction;
    public CubePlacementChecker cubePlacementChecker;

    void Update()
    {
        cubesOutside = virtualMonitorInteraction.GetCubesOutsideCount();
        cubesPlacedCorrectly = cubePlacementChecker.GetCorrectlyPlacedCount();
    }
    public bool CanPullCube()
    {
        return (cubesPlacedCorrectly == cubesOutside);
    }
}
