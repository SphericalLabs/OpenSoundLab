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

public class NetworkOscillator : NetworkSyncListener
{
    protected oscillatorSignalGenerator signalGenerator;
    protected oscillatorDeviceInterface oscillatorDeviceInterface;

    protected virtual void Awake()
    {
        signalGenerator = GetComponent<oscillatorSignalGenerator>();
        oscillatorDeviceInterface = GetComponent<oscillatorDeviceInterface>();
        oscillatorDeviceInterface.lfoSwitch.onSwitchChangedEvent.AddListener(OnLfoChange);

        var freqDial = oscillatorDeviceInterface.freqDial;
        freqDial.onPercentChangedEvent.AddListener(OnDragDial);
        freqDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }
    #region Mirror
    public void OnLfoChange()
    {
        StartCoroutine(WaitForLfoChanged());
    }

    IEnumerator WaitForLfoChanged()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log($"On Change Lfo {!oscillatorDeviceInterface.lfoSwitch.switchVal}");

        if (!oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            OnSync();
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCoroutine(WaitForLfoChanged());
    }

    protected override void OnSync()
    {
        base.OnSync();
        if (isServer)
        {
            RpcUpdatePhase(signalGenerator._phase);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdatePhase(signalGenerator._phase);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync phase {signalGenerator._phase}, lfo {signalGenerator.lfo}, frequency {signalGenerator.frequency}");
        RpcUpdatePhase(signalGenerator._phase);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double phase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old phase: {signalGenerator._phase}, new phase {phase}, lfo {signalGenerator.lfo}, frequency {signalGenerator.frequency}");
            signalGenerator._phase = phase;
        }
    }
    #endregion


    #region onDial
    public void OnDragDial()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        if (Time.frameCount % 8 == 0)
        {
            OnSync();
        }
    }

    public void OnStopDragDial()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        OnSync();
    }
    #endregion
}
