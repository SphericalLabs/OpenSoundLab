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
using UnityEngine.Events;
using System.Collections;

public class ControlCubeDeviceInterface : deviceInterface
{

    public Vector3 percent;
    public ControlCubeSingleSignalGenerator[] signals;
    public omniJack[] outputs;
    public cubeZone cubeManip;

    public UnityEvent onPercentChangedEvent;

    public override void Awake()
    {
        base.Awake();
        Setup(percent);
    }

    public void Setup(Vector3 p)
    {
        percent = p;
        cubeManip.updateLines(percent);
        updatePercent(percent);
        //toggleMute(true);
    }

    public override void hit(bool on, int ID = -1)
    {

    }

    void Update()
    {
    }

    public void updatePercent(Vector3 p, bool invokeChange = false)
    {
        percent = p;

        signals[0].value = (percent.x - .5f) * 2;
        signals[1].value = (percent.y - .5f) * 2;
        signals[2].value = (percent.z - .5f) * 2;

        if (invokeChange)
        {
            onPercentChangedEvent.Invoke();
        }
    }

    public override InstrumentData GetData()
    {
        ControlCubeData data = new ControlCubeData();

        data.deviceType = DeviceType.ControlCube;
        GetTransformData(data);

        data.jackOutID = new int[4];
        for (int i = 0; i < 3; i++) data.jackOutID[i] = outputs[i].transform.GetInstanceID();

        data.dimensionValues = new float[3];
        for (int i = 0; i < 3; i++) data.dimensionValues[i] = percent[i];

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ControlCubeData data = d as ControlCubeData;

        base.Load(data, copyMode);

        for (int i = 0; i < 3; i++) outputs[i].SetID(data.jackOutID[i], copyMode);
        for (int i = 0; i < 3; i++) percent[i] = data.dimensionValues[i];

        Setup(percent);
    }
}

public class ControlCubeData : InstrumentData
{
    public int[] jackOutID;
    public float[] dimensionValues;
}