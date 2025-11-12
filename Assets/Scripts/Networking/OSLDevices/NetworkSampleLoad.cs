// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
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
                if (newValue.Length > 0)
                {
                    sampleLoaders[index].SetSample(sampleManager.GetFileName(newValue), sampleManager.CorrectPathSeparators(newValue));
                }
                break;
            case SyncList<string>.Operation.OP_INSERT:
                break;
            case SyncList<string>.Operation.OP_REMOVEAT:
                break;
            case SyncList<string>.Operation.OP_SET:
                //create tape
                if (newValue.Length > 0)
                {
                    if (sampleLoaders[index].hasTape() && sampleManager.CorrectPathSeparators(sampleLoaders[index].CurFile) == sampleManager.CorrectPathSeparators(newValue))
                    {
                        return;
                    }
                    sampleLoaders[index].SetSample(sampleManager.GetFileName(newValue), sampleManager.CorrectPathSeparators(newValue));
                }
                else if (newValue.Length <= 0 && sampleLoaders[index].hasTape())
                {
                    sampleLoaders[index].ForceEject(false);
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
            //Debug.Log($"Tape of index {index} got removed");
            if (isServer)
            {
                samplePaths[index] = "";
                if (sampleLoaders[index].hasTape())
                {
                    sampleLoaders[index].ForceEject(false);
                }
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
            if (samplePaths[index].Length > 0 && !sampleLoaders[index].hasTape())
            {
                //Debug.Log("Set tape on host");
                sampleLoaders[index].SetSample(sampleManager.GetFileName(path), sampleManager.CorrectPathSeparators(path));
            }
            else if (samplePaths[index].Length <= 0 && sampleLoaders[index].hasTape())
            {
                //Debug.Log("Remove tape on host");
                sampleLoaders[index].ForceEject(false);
            }
        }
    }
}
