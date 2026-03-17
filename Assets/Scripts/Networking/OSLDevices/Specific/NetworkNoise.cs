// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
using Mirror;
using UnityEngine;

public class NetworkNoise : NetworkSyncListener
{
    private NoiseSignalGenerator noiseSignalGenerator;
    private int lastReceivedRevision = -1;
    private int stateRevision = 0;

    protected virtual void Awake()
    {
        noiseSignalGenerator = GetComponent<NoiseSignalGenerator>();
        var rateDial = GetComponent<NoiseDeviceInterface>().speedDial;
        rateDial.onPercentChangedEvent.AddListener(OnDragDial);
        rateDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }


    #region Mirror
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }

    protected override void OnSync()
    {
        if (isServer)
        {
            sendState();
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
            sendState();
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync noise step {noiseSignalGenerator.GetStep()}");
        sendState();
    }

    [ClientRpc]
    protected virtual void RpcUpdateNoiseState(int revision, int seed, int noiseStep, float currentSample, int currentCounter, float ratePercent)
    {
        if (isClient && !isServer)
        {
            if (revision <= lastReceivedRevision) return;
            lastReceivedRevision = revision;
            Debug.Log($"{gameObject.name} old noiseStep: {noiseSignalGenerator.GetStep()}, new noiseStep {noiseStep}");
            noiseSignalGenerator.applyNetworkState(seed, noiseStep, currentSample, currentCounter, ratePercent);
        }
    }

    #endregion

    #region onDial
    public void OnDragDial()
    {
        if (!isServer) return;
        sendState();
    }

    public void OnStopDragDial()
    {
        if (isServer)
        {
            sendState();
        }
        else
        {
            StartCoroutine(requestStateAfterFrame());
        }
    }
    #endregion

    void sendState()
    {
        if (noiseSignalGenerator == null) return;
        noiseSignalGenerator.captureNetworkState(out int seed, out int step, out float currentSample, out int currentCounter, out float ratePercent);
        stateRevision++;
        RpcUpdateNoiseState(stateRevision, seed, step, currentSample, currentCounter, ratePercent);
    }

    IEnumerator requestStateAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        CmdRequestSync();
    }
}
