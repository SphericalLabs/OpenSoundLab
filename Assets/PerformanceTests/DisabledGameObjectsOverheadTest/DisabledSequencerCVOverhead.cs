using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class DisabledSequencerCVOverhead : MonoBehaviour
{
    private const int objectCount = 100; // Number of prefabs to instantiate
    private GameObject[] gameObjects;
    public string prefabPath = "Prefabs/SequencerCV"; // Path to the prefab in Resources

    void Start()
    {
        gameObjects = new GameObject[objectCount];
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        if (prefab == null)
        {
            UnityEngine.Debug.LogError("Prefab not found at " + prefabPath);
            return;
        }

        // Instantiate and disable prefabs
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < objectCount; i++)
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false);
            gameObjects[i] = go;
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to instantiate and disable 100 prefabs: " + stopwatch.ElapsedMilliseconds + " ms");

        // Measure time to enable all prefabs
        stopwatch.Restart();
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(true);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to enable 100 prefabs: " + stopwatch.ElapsedMilliseconds + " ms");

        // Measure time to disable all prefabs
        stopwatch.Restart();
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(false);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to disable 100 prefabs: " + stopwatch.ElapsedMilliseconds + " ms");

        // Start frame time comparison
        StartCoroutine(MeasureFrameTime());
    }

    IEnumerator MeasureFrameTime()
    {
        // Measure frame time with all objects active
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(true);
        }

        yield return new WaitForSeconds(1f); // Let the system stabilize

        float totalFrameTimeActive = 0f;
        int frameCountActive = 0;

        for (int i = 0; i < 100; i++)
        {
            float frameTime = Time.deltaTime * 1000f; // Convert to milliseconds
            totalFrameTimeActive += frameTime;
            frameCountActive++;
            yield return null; // Wait for next frame
        }

        float averageFrameTimeActive = totalFrameTimeActive / frameCountActive;
        UnityEngine.Debug.Log("Average frame time with all objects active: " + averageFrameTimeActive + " ms");

        // Measure frame time with all root objects disabled
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(false);
        }

        yield return new WaitForSeconds(1f); // Let the system stabilize

        float totalFrameTimeDisabled = 0f;
        int frameCountDisabled = 0;

        for (int i = 0; i < 100; i++)
        {
            float frameTime = Time.deltaTime * 1000f; // Convert to milliseconds
            totalFrameTimeDisabled += frameTime;
            frameCountDisabled++;
            yield return null; // Wait for next frame
        }

        float averageFrameTimeDisabled = totalFrameTimeDisabled / frameCountDisabled;
        UnityEngine.Debug.Log("Average frame time with all root objects disabled: " + averageFrameTimeDisabled + " ms");
    }
}
