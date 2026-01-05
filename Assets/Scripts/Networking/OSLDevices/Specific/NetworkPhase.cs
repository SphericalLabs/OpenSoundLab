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
    private bool resetQueued;

    private void Awake()
    {
        phaseInterface = GetComponent<phaseDeviceInterface>();
    }

    private void Start()
    {
        base.Start();

        NetworkDials networkDials = GetComponent<NetworkDials>();
        if (networkDials != null)
        {
            networkDials.dialValues.Callback += OnBpmDialUpdated;
        }

        if (phaseInterface != null && phaseInterface.playButton != null)
        {
            phaseInterface.playButton.onToggleChangedEvent.AddListener(OnPlayToggleChanged);
        }
        if (phaseInterface != null && phaseInterface.rewindButton != null)
        {
            phaseInterface.rewindButton.onToggleChangedEvent.AddListener(OnRewindToggleChanged);
        }
    }

    private void OnDestroy()
    {
        if (phaseInterface != null && phaseInterface.playButton != null)
        {
            phaseInterface.playButton.onToggleChangedEvent.RemoveListener(OnPlayToggleChanged);
        }
        if (phaseInterface != null && phaseInterface.rewindButton != null)
        {
            phaseInterface.rewindButton.onToggleChangedEvent.RemoveListener(OnRewindToggleChanged);
        }
    }

    private void OnPlayToggleChanged()
    {
        if (phaseInterface == null || phaseInterface.playButton == null) return;
        if (!phaseInterface.playButton.isHit) return;
        requestSync(false);
    }

    private void OnRewindToggleChanged()
    {
        if (phaseInterface == null || phaseInterface.rewindButton == null) return;
        if (!phaseInterface.rewindButton.isHit) return;
        requestSync(true);
    }

    void requestSync(bool reset)
    {
        if (!isServer)
        {
            CmdRequestSync(reset);
            return;
        }

        if (reset) resetQueued = true;
        if (NetworkSyncEventManager.Instance != null) NetworkSyncEventManager.Instance.UpdateSync();
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
            CmdRequestSync(false);
        }
    }

    protected override void OnSync()
    {
        if (phaseInterface == null || phaseInterface.phaseSignal == null) return;
        if (isServer)
        {
            bool sendReset = resetQueued;
            resetQueued = false;
            RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, sendReset);
        }
        else
        {
            CmdRequestSync(false);
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer && phaseInterface != null && phaseInterface.phaseSignal != null)
        {
            RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, false);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync(bool requestReset)
    {
        if (phaseInterface == null || phaseInterface.phaseSignal == null) return;
        if (requestReset)
        {
            phaseInterface.ApplyNetworkReset();
        }
        RpcUpdatePhase(phaseInterface.phaseSignal._measurePhase, requestReset);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double measurePhase, bool triggerReset)
    {
        if (isClient && !isServer)
        {
            if (phaseInterface == null) return;
            if (triggerReset)
            {
                phaseInterface.ApplyNetworkReset();
            }
            else
            {
                if (phaseInterface.phaseSignal != null) phaseInterface.phaseSignal._measurePhase = measurePhase;
                if (phaseInterface.resetSignal != null) phaseInterface.resetSignal._measurePhase = measurePhase;
            }
        }
    }
    #endregion
}
