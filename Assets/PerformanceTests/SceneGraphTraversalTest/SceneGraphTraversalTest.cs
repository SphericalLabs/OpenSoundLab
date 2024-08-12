using UnityEngine;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

public class SceneGraphTraversalTest : MonoBehaviour
{
    int iterations = 100000;
    int childCount = 500;

    void Start()
    {
        GenerateHierarchy();
        RunBenchmarks();
    }

    void GenerateHierarchy()
    {
        for (int i = 0; i < childCount; i++)
        {
            GameObject child = new GameObject($"Child_{i}");
            child.transform.SetParent(transform);

            if (i % 2 == 0) child.AddComponent<BoxCollider>();
            if (i % 3 == 0) child.AddComponent<SphereCollider>();
            if (i % 5 == 0) child.AddComponent<Rigidbody>();

            if (i % 4 == 0)
            {
                GameObject nestedChild1 = new GameObject($"NestedChild1_{i}");
                nestedChild1.transform.SetParent(child.transform);
                nestedChild1.AddComponent<MeshCollider>();

                GameObject nestedChild2 = new GameObject($"NestedChild2_{i}");
                nestedChild2.transform.SetParent(nestedChild1.transform);
                nestedChild2.AddComponent<CapsuleCollider>();
            }
        }
    }

    void RunBenchmarks()
    {
        BenchmarkTransformAccess();
        BenchmarkGameObjectAccess();
        BenchmarkGetComponent();
        BenchmarkGetComponentInChildren();
        BenchmarkGetComponents();
        BenchmarkGetComponentsInChildren();
        BenchmarkFind();
    }

    void BenchmarkTransformAccess()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            Transform t = transform;
        }
        sw.Stop();
        UnityEngine.Debug.Log($"Transform access: {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkGameObjectAccess()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            GameObject go = gameObject;
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GameObject access: {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkGetComponent()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            Collider col = GetComponent<Collider>();
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GetComponent<>(): {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkGetComponentInChildren()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            Collider col = GetComponentInChildren<Collider>();
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GetComponentInChildren<>(): {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkGetComponents()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            Collider[] cols = GetComponents<Collider>();
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GetComponents<>(): {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkGetComponentsInChildren()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            Collider[] cols = GetComponentsInChildren<Collider>();
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GetComponentsInChildren<>(): {sw.ElapsedMilliseconds}ms");
    }

    void BenchmarkFind()
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            GameObject go = GameObject.Find("Child_0");
        }
        sw.Stop();
        UnityEngine.Debug.Log($"GameObject.Find(): {sw.ElapsedMilliseconds}ms");
    }
}