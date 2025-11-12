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

public class NetworkMetronome : NetworkSyncListener
{
    [SerializeField] private button startButton;
    [SerializeField] private button rewindButton;
    [SerializeField] private metronome metro;

    private void Start()
    {
        startButton.onStartGrabEvents.AddListener(OnButtonPress);
        rewindButton.onStartGrabEvents.AddListener(OnButtonPress);

        GetComponent<NetworkDials>().dialValues.Callback += OnBpmDialUpdated;

    }

    void OnBpmDialUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                // careful, this is hardwiring index 0.
                if (index == 0) metro.readBpmDialAndBroadcast();
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    private void OnDestroy()
    {
        startButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
        rewindButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
    }

    private void OnButtonPress()
    {
        NetworkSyncEventManager.Instance.UpdateSync();
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
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
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
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
    }

    [ClientRpc]
    protected virtual void RpcUpdate_measurePhase(double measurePhase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old _measurePhase: {masterControl.instance.MeasurePhase}, new _measurePhase {measurePhase}");

            masterControl.instance.MeasurePhase = measurePhase;
        }
    }
    #endregion
}
