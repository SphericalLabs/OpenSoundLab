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
using UnityEngine;

public class NetworkXHandles : NetworkBehaviour
{
    public xHandle[] xHandles;

    public readonly SyncList<float> xValues = new SyncList<float>();
    private float[] lastGrabedTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var handle in xHandles)
        {
            xValues.Add(handle.transform.localPosition.x);
        }
    }

    private void Start()
    {
        lastGrabedTimes = new float[xHandles.Length];

        //add dials on change callback event
        for (int i = 0; i < xHandles.Length; i++)
        {
            int index = i;
            xHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
            xHandles[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });

        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            xValues.Callback += OnHandleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < xValues.Count; i++)
            {
                OnHandleUpdated(SyncList<float>.Operation.OP_ADD, i, xHandles[i].transform.localPosition.x, xValues[i]);
            }
        }
    }

    void OnHandleUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                xHandles[index].updatePos(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (xHandles[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
                {
                    xHandles[index].updatePos(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateHandleValue(int index)
    {
        Debug.Log($"Update xHandle value of index: {index} to value: {xHandles[index].transform.localPosition.x}");
        if (isServer)
        {
            xValues[index] = xHandles[index].transform.localPosition.x;
        }
        else
        {
            CmdUpdateHandleValue(index, xHandles[index].transform.localPosition.x);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(int index, float value)
    {
        xValues[index] = value;
        xHandles[index].updatePos(value);
    }

    public void UpdateLastGrabedTime(int index)
    {
        if (index >= 0 && index < lastGrabedTimes.Length)
        {
            lastGrabedTimes[index] = Time.time;
        }
    }

    private bool IsEndGrabCooldownOver(int index)
    {
        if (lastGrabedTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}
