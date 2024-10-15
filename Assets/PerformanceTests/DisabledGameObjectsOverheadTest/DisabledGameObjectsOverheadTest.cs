using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class DisabledGameObjectsOverheadTest : MonoBehaviour
{
    private const int objectCount = 5000;
    private GameObject[] gameObjects;

    void Start()
    {
        gameObjects = new GameObject[objectCount];

        // Create GameObjects with children and disable them
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < objectCount; i++)
        {
            GameObject go = new GameObject("TestObject" + i);
            for (int j = 0; j < 5; j++)
            {
                GameObject child = new GameObject("ChildObject" + j);
                child.transform.parent = go.transform;
            }
            go.SetActive(false);
            gameObjects[i] = go;
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to create and disable 5000 GameObjects with children: " + stopwatch.ElapsedMilliseconds + " ms");

        // Measure time to enable all GameObjects
        stopwatch.Restart();
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(true);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to enable 5000 GameObjects with children: " + stopwatch.ElapsedMilliseconds + " ms");

        // Measure time to disable all GameObjects
        stopwatch.Restart();
        for (int i = 0; i < objectCount; i++)
        {
            gameObjects[i].SetActive(false);
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to disable 5000 GameObjects with children: " + stopwatch.ElapsedMilliseconds + " ms");

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
