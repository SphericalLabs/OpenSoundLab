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
using System.Collections;

public class DistanceDeviceInterface : deviceInterface {
  public omniJack output;
  public dial radiusDial, linearityDial;

  Transform centerEye;

  public TextMesh valueDisplay;
  DistanceSignalGenerator signal;

  public float currentDistance;

  public override void Awake() {
    base.Awake();

    var rig = FindObjectOfType<OVRCameraRig>();      // or drag-assign in the Inspector
    if (rig == null)
    {
        Debug.LogError("OVRCameraRig not found!");
        enabled = false;
        return;
    }

    // 2. Cache the Center-Eye anchor (== head)
    centerEye = rig.centerEyeAnchor;                 // or rig.trackingSpace.Find("CenterEyeAnchor")

    signal = GetComponent<DistanceSignalGenerator>();

  }

    public float radius;

  void Update() {
    
    currentDistance = Vector3.Distance(transform.position, centerEye.position);

    radius = radiusDial.percent * 10f;
    valueDisplay.text = radius.ToString("F3");

  }

  public override InstrumentData GetData() {
    DistanceData data = new DistanceData();
    data.deviceType = DeviceType.Distance;
    GetTransformData(data);

    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    DistanceData data = d as DistanceData;
    base.Load(data, true);

    output.SetID(data.jackOutID, copyMode);

  }
}

public class DistanceData : InstrumentData {

  public float dial;

  public int jackOutID;
  public int jackInID;
}
