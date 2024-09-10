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

    void UpdateBinaural(){
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



    void UpdateWire(){
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



    void UpdateDisplay(){
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
