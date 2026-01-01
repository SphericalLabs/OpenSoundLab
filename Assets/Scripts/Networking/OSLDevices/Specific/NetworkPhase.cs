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
// you may not use this file except in compliance with the License.
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
using Mirror;

public class NetworkPhase : NetworkSyncListener
{
    private phaseDeviceInterface phaseInterface;

    private void Awake()
    {
        phaseInterface = GetComponent<phaseDeviceInterface>();
    }

    private void Start()
    {
        GetComponent<NetworkDials>().dialValues.Callback += OnBpmDialUpdated;

        if (phaseInterface.playButton != null) phaseInterface.playButton.onStartGrabEvents.AddListener(OnButtonPress);
        if (phaseInterface.rewindButton != null) phaseInterface.rewindButton.onStartGrabEvents.AddListener(OnButtonPress);
    }

    private void OnDestroy()
    {
        if (phaseInterface.playButton != null) phaseInterface.playButton.onStartGrabEvents.RemoveListener(OnButtonPress);
        if (phaseInterface.rewindButton != null) phaseInterface.rewindButton.onStartGrabEvents.RemoveListener(OnButtonPress);
    }

    private void OnButtonPress()
    {
        NetworkSyncEventManager.Instance.UpdateSync();
    }

    void OnBpmDialUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
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
            RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, phaseInterface.isRunning);
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
            RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, phaseInterface.isRunning);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, phaseInterface.isRunning);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double measurePhase, bool running)
    {
        if (isClient && !isServer)
        {
            phaseInterface.phaseSignal._measurePhase = measurePhase;
            phaseInterface.resetSignal._measurePhase = measurePhase;
            phaseInterface.isRunning = running;

            // Sync visual button state
            if (phaseInterface.playButton != null && phaseInterface.playButton.isHit != running)
            {
                phaseInterface.playButton.phantomHit(running);
            }
        }
    }
    #endregion
}
