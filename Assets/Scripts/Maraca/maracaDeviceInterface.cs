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
using System.Runtime.InteropServices;

public class maracaDeviceInterface : deviceInterface {
  maracaSignalGenerator signal;
  maracaUI _maracaUI;
  omniJack jackOut;
  double _sampleDuration;

  [DllImport("OSLNative")]
  public static extern void MaracaProcessAudioBuffer(float[] buffer, float[] controlBuffer, int length, int channels, ref double _phase, double _sampleDuration);

  public override void Awake() {
    base.Awake();
    _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    signal = GetComponent<maracaSignalGenerator>();
    _maracaUI = GetComponentInChildren<maracaUI>();
    jackOut = GetComponentInChildren<omniJack>();
  }

  void Update() {
    signal.curShake = _maracaUI.shakeVal;
  }

  void OnDestroy() {
    if (_maracaUI.transform.parent != transform) Destroy(_maracaUI.gameObject);
  }

  double _phaseB = 0;
  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (jackOut.near != null || signal.curShake < .01f) return;

    double dspTime = AudioSettings.dspTime;
    float[] b = new float[buffer.Length];
    signal.processBuffer(b, dspTime, channels);

    MaracaProcessAudioBuffer(buffer, b, buffer.Length, channels, ref _phaseB, _sampleDuration);
  }

  public override InstrumentData GetData() {
    MaracaData data = new MaracaData();
    data.deviceType = DeviceType.Maracas;
    GetTransformData(data);
    data.jackOutID = jackOut.transform.GetInstanceID();
    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    MaracaData data = d as MaracaData;
    base.Load(data, true);
    jackOut.SetID(data.jackOutID, copyMode);
  }
}

public class MaracaData : InstrumentData {
  public int jackOutID;
}