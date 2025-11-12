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
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class NetworkMasterControl : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnWireSettingChanged))]
    public WireMode WireSetting;

    [SyncVar(hook = nameof(OnDisplaySettingChanged))]
    public DisplayMode DisplaySetting;

    [SyncVar(hook = nameof(OnBinauralSettingChanged))]
    public BinauralMode BinauralSetting;


    void OnWireSettingChanged(WireMode oldValue, WireMode newValue)
    {
        //if(IsWireCooldownOver()){
        masterControl.instance.updateWireSetting((int)newValue);
        //}
    }

    void OnDisplaySettingChanged(DisplayMode oldValue, DisplayMode newValue)
    {
        //if (IsDisplayCooldownOver())
        //{
        masterControl.instance.updateDisplaySetting((int)newValue);
        //}
    }

    void OnBinauralSettingChanged(BinauralMode oldValue, BinauralMode newValue)
    {
        //if (IsBinauralCooldownOver())
        //{
        masterControl.instance.updateBinauralSetting((int)newValue);
        //}
    }

    void Start()
    {
        masterControl.instance.onBinauralChangedEvent.AddListener(UpdateBinaural);
        //masterControl.instance.onBinauralChangedEvent.AddListener(delegate{ lastBinauralTime = Time.time; });

        masterControl.instance.onWireChangedEvent.AddListener(UpdateWire);
        //masterControl.instance.onWireChangedEvent.AddListener(delegate { lastWireTime = Time.time; });

        masterControl.instance.onDisplayChangedEvent.AddListener(UpdateDisplay);
        //masterControl.instance.onDisplayChangedEvent.AddListener(delegate { lastDisplayTime = Time.time; });
    }

    private void OnDestroy()
    {
        masterControl.instance.onBinauralChangedEvent.RemoveListener(UpdateBinaural);
        masterControl.instance.onWireChangedEvent.RemoveListener(UpdateWire);
        masterControl.instance.onDisplayChangedEvent.RemoveListener(UpdateDisplay);
    }

    public override void OnStartClient()
    {
        // Process initial SyncList payload
        // after it had already been received, but no hook was triggered automatically on first init
        OnDisplaySettingChanged(masterControl.instance.DisplaySetting, DisplaySetting);
        OnBinauralSettingChanged(masterControl.instance.BinauralSetting, BinauralSetting);
        OnWireSettingChanged(masterControl.instance.WireSetting, WireSetting);
    }


    void Update()
    {

    }

    void UpdateBinaural()
    {
        Debug.Log($"Update BinauralSetting: {masterControl.instance.BinauralSetting}");
        if (isServer)
        {
            BinauralSetting = masterControl.instance.BinauralSetting;
        }
        else
        {
            CmdBinauralUpdate(masterControl.instance.BinauralSetting);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdBinauralUpdate(BinauralMode mode)
    {
        BinauralSetting = mode;
        masterControl.instance.updateBinauralSetting((int)mode);
    }



    void UpdateWire()
    {
        Debug.Log($"Update WireSetting: {masterControl.instance.WireSetting}");
        if (isServer)
        {
            WireSetting = masterControl.instance.WireSetting;
        }
        else
        {
            CmdWireUpdate(masterControl.instance.WireSetting);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdWireUpdate(WireMode mode)
    {
        WireSetting = mode;
        masterControl.instance.updateWireSetting((int)mode);
    }



    void UpdateDisplay()
    {
        Debug.Log($"Update DisplaySetting: {masterControl.instance.DisplaySetting}");
        if (isServer)
        {
            DisplaySetting = masterControl.instance.DisplaySetting;
        }
        else
        {
            CmdDisplayUpdate(masterControl.instance.DisplaySetting);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDisplayUpdate(DisplayMode mode)
    {
        DisplaySetting = mode;
        masterControl.instance.updateDisplaySetting((int)mode);
    }

    //// cooldown time measurements
    //float lastBinauralTime = 0f;
    //float lastWireTime = 0f;
    //float lastDisplayTime = 0f;

    //private bool IsBinauralCooldownOver()
    //{
    //    return true;

    //    if (lastBinauralTime + 0.5f < Time.time)
    //    {
    //        return true;
    //    }
    //    return false;
    //}

    //private bool IsWireCooldownOver()
    //{
    //    return true;

    //    if (lastWireTime + 0.5f < Time.time)
    //    {
    //        return true;
    //    }
    //    return false;
    //}

    //private bool IsDisplayCooldownOver()
    //{
    //    return true;

    //    if (lastDisplayTime + 0.5f < Time.time)
    //    {
    //        return true;
    //    }
    //    return false;
    //}
}
