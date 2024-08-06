using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NetworkSyncEventManager : MonoBehaviour
{
    public static NetworkSyncEventManager Instance;
    public delegate void SyncHandler();
    public event SyncHandler SyncEvent;
    public event SyncHandler IntervalSyncEvent;

    [Header("Fixed Interval Sync")]
    private bool syncInFixedIntervals = false;
    private float syncTime = 60;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SubscribeToEvents();
        if (syncInFixedIntervals)
        {
            StartCoroutine(IntervalSync());
        }
    }

    private void SubscribeToEvents()
    {
        OVRManager.HMDAcquired += UpdateSync;
        OVRManager.HMDMounted += UpdateSync;
        OVRManager.VrFocusAcquired += UpdateSync;
        OVRManager.InputFocusAcquired += UpdateSync;
        OVRManager.TrackingAcquired += UpdateSync;
    }

    public void UpdateSync()
    {
        SyncEvent?.Invoke();
    }

    private IEnumerator IntervalSync()
    {
        yield return new WaitForSeconds(syncTime);
        IntervalSyncEvent?.Invoke();
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(NetworkSyncEventManager))]
public class MyScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector layout

        NetworkSyncEventManager myScript = (NetworkSyncEventManager)target;
        if (GUILayout.Button("Sync Now"))
        {
            myScript.UpdateSync();
        }
    }
}
#endif
