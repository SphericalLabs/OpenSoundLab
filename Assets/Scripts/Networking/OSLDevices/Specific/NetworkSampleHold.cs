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

using Mirror;
using UnityEngine;

public class NetworkSampleHold : NetworkSyncListener
{
    SampleHoldSignalGenerator sampleHoldSignalGenerator;

    int lastObservedRevision = -1;
    int lastReceivedRevision = -1;
    bool pendingSync;

    protected virtual void Awake()
    {
        sampleHoldSignalGenerator = GetComponent<SampleHoldSignalGenerator>();
    }

    void Update()
    {
        if (!isServer || sampleHoldSignalGenerator == null) return;

        int revision = sampleHoldSignalGenerator.getHoldRevision();
        if (revision != lastObservedRevision)
        {
            lastObservedRevision = revision;
            pendingSync = true;
        }

        if (!pendingSync) return;

        sendState();
    }

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
        base.OnSync();
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
        if (!isServer) return;
        sendState();
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        sendState();
    }

    [ClientRpc]
    protected void RpcUpdateState(float currentHoldValue, bool triggerHigh, int revision)
    {
        if (!isClient || isServer) return;
        if (revision <= lastReceivedRevision) return;
        if (sampleHoldSignalGenerator == null) return;

        lastReceivedRevision = revision;
        sampleHoldSignalGenerator.applyNetworkState(currentHoldValue, triggerHigh, revision);
    }

    void sendState()
    {
        if (sampleHoldSignalGenerator == null) return;
        sampleHoldSignalGenerator.captureNetworkState(out float currentHoldValue, out bool triggerHigh, out int revision);
        RpcUpdateState(currentHoldValue, triggerHigh, revision);
        pendingSync = false;
    }
}
