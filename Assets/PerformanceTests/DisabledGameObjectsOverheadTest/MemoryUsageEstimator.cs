using UnityEngine;
using UnityEngine.Profiling;

public class MemoryUsageEstimator : MonoBehaviour
{
    public GameObject targetObject;

    void Update()
    {
        if (targetObject != null)
        {
            long memoryUsage = CalculateMemoryUsage(targetObject);
            Debug.Log($"Estimated memory usage for {targetObject.name}: {memoryUsage / 1024f} KB");
        }
        else
        {
            Debug.LogError("Target object is not assigned.");
        }
    }

    long CalculateMemoryUsage(GameObject obj)
    {
        long totalMemory = 0;

        // Add memory used by the GameObject itself
        totalMemory += Profiler.GetRuntimeMemorySizeLong(obj);

        // Add memory used by all components
        foreach (Component component in obj.GetComponents<Component>())
        {
            totalMemory += Profiler.GetRuntimeMemorySizeLong(component);
        }

        // Add memory used by all child GameObjects
        foreach (Transform child in obj.transform)
        {
            totalMemory += CalculateMemoryUsage(child.gameObject);
        }

        return totalMemory;
    }
}
