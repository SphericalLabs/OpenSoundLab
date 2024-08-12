using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Samples;
using System.IO;
using UnityEngine;

public class NetworkPlayerTape : NetworkBehaviour
{
    public GameObject handParent;
    [SyncVar(hook = nameof(OnSetHandSamplePath))]
    public string inHandSamplePath;
    [SyncVar(hook = nameof(OnChangeOffset))]
    private Vector3 offset;
    [SyncVar(hook = nameof(OnChangeRotationOffset))]
    private Quaternion rotationOffset;
    private tape tapeInHand;

    public GameObject tapePrefab;

    private void Start()
    {
        
    }

    public void OnSetHandSamplePath(string old, string newString)
    {
        if (old != newString)
        {
            SetSamplePath();
        }
    }


    public void OnChangeOffset(Vector3 old, Vector3 newValue)
    {
        if (tapeInHand != null && !isLocalPlayer)
        {
            tapeInHand.transform.localPosition = newValue;
        }
    }
    public void OnChangeRotationOffset(Quaternion old, Quaternion newValue)
    {
        if (tapeInHand != null && !isLocalPlayer)
        {
            tapeInHand.transform.localRotation = newValue;
            tapeInHand.transform.Rotate(-90, 0, 0, Space.Self);
        }
    }


    public void SetHandSamplePath(string path)
    {
        Debug.Log($"{gameObject.name} Set hand tape: {path}");
        if (isServer)
        {
            inHandSamplePath = path;
        }
        else
        {
            CmdUpdateSamplePath(path);
        }
    }

    [Command]
    public void CmdUpdateSamplePath(string path)
    {
        inHandSamplePath = path;
        SetSamplePath();
    }

    private void SetSamplePath()
    {
        if (isLocalPlayer)
        {
            return;
        }
        if (tapeInHand != null)
        {
            //destroy current tape
            Destroy(tapeInHand.gameObject);
        }
        if (inHandSamplePath.Length > 0)
        {
            //create new instance

            if (!File.Exists(sampleManager.instance.parseFilename(samplerLoad.CorrectPathSeparators(inHandSamplePath))))
            {
                Debug.Log("File does't exist");
                return;
            }

            GameObject g = Instantiate(tapePrefab, offset, rotationOffset);
            g.transform.Rotate(-90, 0, 0, Space.Self);
            tapeInHand = g.GetComponent<tape>();
            tapeInHand.Setup(samplerLoad.GetFileName(inHandSamplePath), samplerLoad.CorrectPathSeparators(inHandSamplePath));
            tapeInHand.TargetNetworkPlayerTape = this;
        }
    }
}
