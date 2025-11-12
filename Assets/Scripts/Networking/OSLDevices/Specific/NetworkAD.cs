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
using UnityEngine;
using Mirror;

public class NetworkAD : NetworkSyncListener
{
    protected ADSignalGenerator adSignalGenerator;
    protected ADDeviceInterface adDeviceInterface;
    protected virtual void Awake()
    {
        adSignalGenerator = GetComponent<ADSignalGenerator>();
        adDeviceInterface = GetComponent<ADDeviceInterface>();
        adDeviceInterface.attackDial.onEndGrabEvents.AddListener(OnStopDragDial);
        adDeviceInterface.releaseDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }
    #region Mirror


    public override void OnStartClient()
    {
        if (!isServer)
        {
            CmdRequestSync();
        }
    }

    protected override void OnSync()
    {
        base.OnSync();
        if (isServer)
        {
            RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync glidedVal {adSignalGenerator.GlidedVal}");
        RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
    }

    [ClientRpc]
    protected virtual void RpcUpdateGlidedVal(bool isRunning, int stage, int counter, float glidedVal)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old glidedVal: {adSignalGenerator.GlidedVal}, new phase {glidedVal}");
            adSignalGenerator.IsRunning = isRunning;
            adSignalGenerator.Stage = stage;
            adSignalGenerator.Counter = counter;
            adSignalGenerator.GlidedVal = glidedVal;
        }
    }
    #endregion

    #region onDial
    public void OnStopDragDial()
    {
        OnSync();
    }
    #endregion
}
