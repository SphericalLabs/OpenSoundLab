using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class NetworkSampleLoad : NetworkBehaviour
{
    public samplerLoad[] sampleLoaders;

    public readonly SyncList<string> samplePaths = new SyncList<string>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var sample in sampleLoaders)
        {
            if (sample.hasTape())
            {
                sample.getTapeInfo(out string labe, out string file);
                samplePaths.Add(file);
            }
            else
            {
                samplePaths.Add("");
            }
        }
    }

    private void Start()
    {
        //add sample on change callback event
        for (int i = 0; i < sampleLoaders.Length; i++)
        {
            int index = i;
            sampleLoaders[i].onLoadTapeEvents.AddListener(delegate { SetSamplePath(index); });
            sampleLoaders[i].onUnloadTapeEvents.AddListener(delegate { EndSamplePath(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            samplePaths.Callback += OnSampleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < sampleLoaders.Length; i++)
            {
                OnSampleUpdated(SyncList<string>.Operation.OP_ADD, i, "", samplePaths[i]);
            }
        }
    }

    void OnSampleUpdated(SyncList<string>.Operation op, int index, string oldValue, string newValue)
    {
        switch (op)
        {
            case SyncList<string>.Operation.OP_ADD:
                //create tape
                sampleLoaders[index].SetSample(newValue, newValue);
                break;
            case SyncList<string>.Operation.OP_INSERT:
                break;
            case SyncList<string>.Operation.OP_REMOVEAT:
                break;
            case SyncList<string>.Operation.OP_SET:
                //create tape
                if (!sampleLoaders[index].hasTape())
                {
                    sampleLoaders[index].SetSample(newValue, newValue);
                }
                break;
            case SyncList<string>.Operation.OP_CLEAR:
                break;
        }
    }

    public void SetSamplePath(int index)
    {
        if (index >= 0 && index < sampleLoaders.Length)
        {
            sampleLoaders[index].getTapeInfo(out string labe, out string file);

            Debug.Log($"Set Tape {index}: {file}");
            if (isServer)
            {
                samplePaths[index] = file;
            }
            else
            {
                CmdUpdateSamplePath(index, file);
            }
        }
    }

    public void EndSamplePath(int index)
    {
        if (index >= 0 && index < sampleLoaders.Length)
        {
            Debug.Log($"Tape of index {index} got removed");
            if (isServer)
            {
                samplePaths[index] = "";
            }
            else
            {
                CmdUpdateSamplePath(index, "");
            }
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdUpdateSamplePath(int index, string path)
    {
        if (samplePaths[index] != path)
        {
            samplePaths[index] = path;

            if (!sampleLoaders[index].hasTape())
            {
                sampleLoaders[index].SetSample(path, path);
            }
        }
    }
}
