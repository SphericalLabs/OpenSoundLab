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

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Mirror;

public class NetworkNoise : NetworkSyncListener
{
    private NoiseSignalGenerator noiseSignalGenerator;
    [SyncVar]//(hook = nameof(OnUpdateSeed))]
    private int syncSeed = 0; // select a specific noise pattern
    //private bool initialSeedSet = false;
    protected virtual void Awake()
    {
        noiseSignalGenerator = GetComponent<NoiseSignalGenerator>();
        var rateDial = GetComponent<NoiseDeviceInterface>().speedDial;
        rateDial.onPercentChangedEvent.AddListener(OnDragDial);
        rateDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }


    #region Mirror
    public override void OnStartServer()
    {
        Debug.Log($"{gameObject.name} initial noise seed {noiseSignalGenerator.GetSeed()}");
        syncSeed = noiseSignalGenerator.GetSeed();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }
    /* only needed when seed changed during lifetime
    private void OnUpdateSeed(int oldValue, int newValue)
    {
        Debug.Log($"{gameObject.name} update seed {newValue}");
        if (initialSeedSet)
        {
            noiseSignalGenerator.syncNoiseSignalGenerator(newValue, noiseSignalGenerator.NoiseStep);
        }
    }*/

    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdateSteps(noiseSignalGenerator.GetStep());
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
            RpcUpdateSteps(noiseSignalGenerator.GetStep());
        }
    }
    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync noise step {noiseSignalGenerator.GetStep()}");

        RpcUpdateSteps(noiseSignalGenerator.GetStep());

    }
    [ClientRpc]
    protected virtual void RpcUpdateSteps(int noiseStep)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old noiseStep: {noiseSignalGenerator.GetStep()}, new noiseStep {noiseStep}");
            noiseSignalGenerator.syncNoiseSignalGenerator(syncSeed, noiseStep);
            //initialSeedSet = true;
        }
    }

    #endregion

    #region onDial
    public void OnDragDial()
    {
        /*
        if (Time.frameCount % 8 == 0)
        {
            OnSync();
        }*/
    }

    public void OnStopDragDial()
    {
        OnSync();
    }
    #endregion
}
