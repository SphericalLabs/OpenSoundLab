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

using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkXYHandles : NetworkBehaviour
{
    public xyHandle[] xyHandles;

    public readonly SyncList<Vector2> xyValues = new SyncList<Vector2>();
    private float[] lastGrabedTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var handle in xyHandles)
        {
            xyValues.Add(new Vector2(handle.transform.localPosition.x, handle.transform.localPosition.y));
        }
    }

    private void Awake()
    {
        lastGrabedTimes = new float[xyHandles.Length];

        //add dials on change callback event
        for (int i = 0; i < xyHandles.Length; i++)
        {
            int index = i;
            xyHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
            xyHandles[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });

        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            xyValues.Callback += OnHandleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < xyValues.Count; i++)
            {
                Vector2 vector = new Vector2(xyHandles[i].transform.localPosition.x, xyHandles[i].transform.localPosition.y);
                OnHandleUpdated(SyncList<Vector2>.Operation.OP_ADD, i, vector, xyValues[i]);
            }
        }
    }

    void OnHandleUpdated(SyncList<Vector2>.Operation op, int index, Vector2 oldValue, Vector2 newValue)
    {
        switch (op)
        {
            case SyncList<Vector2>.Operation.OP_ADD:
                xyHandles[index].updatePos(newValue);
                break;
            case SyncList<Vector2>.Operation.OP_INSERT:
                break;
            case SyncList<Vector2>.Operation.OP_REMOVEAT:
                break;
            case SyncList<Vector2>.Operation.OP_SET:
                if (xyHandles[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
                {
                    xyHandles[index].updatePos(newValue);
                }
                break;
            case SyncList<Vector2>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateHandleValue(int index)
    {
        Debug.Log($"Update xHandle value of index: {index} to value: {xyHandles[index].transform.localPosition.x}/{xyHandles[index].transform.localPosition.y}");
        if (isServer)
        {
            xyValues[index] = new Vector2(xyHandles[index].transform.localPosition.x, xyHandles[index].transform.localPosition.y);
        }
        else
        {
            CmdUpdateHandleValue(index, new Vector2(xyHandles[index].transform.localPosition.x, xyHandles[index].transform.localPosition.y));
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(int index, Vector2 value)
    {
        xyValues[index] = value;
        xyHandles[index].updatePos(value);
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
