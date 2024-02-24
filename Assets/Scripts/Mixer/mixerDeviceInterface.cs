// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
using System.Collections.Generic;
using System.Linq;

public class mixerDeviceInterface : deviceInterface {

  mixer signal;
  public GameObject mixerPrefab;
  public Transform stretchSlider, speaker, output, lengthSlider;

  public List<fader> faderList = new List<fader>();

  int count = 0;

  float faderLength = 0;
  float prevFaderLength = 0;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<mixer>();

    float xVal = stretchSlider.localPosition.x;
    count = Mathf.FloorToInt((xVal + .075f) / -.04f) + 1;
    updateMixerCount();
  }

  void updateMixerCount() {
    int cur = signal.incoming.Count;
    if (count > cur) {
      for (int i = 0; i < count - cur; i++) {
        signalGenerator s = (Instantiate(mixerPrefab, transform, false) as GameObject).GetComponent<signalGenerator>();
        faderList.Add(s as fader);
        s.transform.localPosition = new Vector3(-.03f - .04f * signal.incoming.Count, 0, 0);
        signal.incoming.Add(s);

        float fL = 1 + faderLength * 4f;
        faderList.Last().updateFaderLength(fL);
        Vector3 pos = faderList.Last().transform.localPosition;
        pos.z = -.12f * fL + .12f;
        faderList.Last().transform.localPosition = pos;
      }
    } else // count < cur
       {
      for (int i = 0; i < cur - count; i++) {
        signalGenerator s = signal.incoming.Last();
        faderList.RemoveAt(signal.incoming.Count - 1);
        signal.incoming.RemoveAt(signal.incoming.Count - 1);
        Destroy(s.gameObject);
      }
    }
  }

  void Update() {
    float xVal = stretchSlider.localPosition.x;
    speaker.localPosition = new Vector3(xVal - .0125f, 0, .11f);
    output.localPosition = new Vector3(xVal - .0125f, 0, .14f);

    count = Mathf.FloorToInt((xVal + .075f) / -.04f) + 1;
    if (count != signal.incoming.Count) updateMixerCount();


    faderLength = lengthSlider.localPosition.x;
    if (faderLength != prevFaderLength) {
      prevFaderLength = faderLength;
      float fL = 1 + faderLength * 4f;
      for (int i = 0; i < faderList.Count; i++) {
        faderList[i].updateFaderLength(fL);
        Vector3 pos = faderList[i].transform.localPosition;
        pos.z = -.12f * fL + .12f;
        faderList[i].transform.localPosition = pos;
      }
    }
  }

  public override InstrumentData GetData() {
    MixerData data = new MixerData();
    data.deviceType = DeviceType.Mixer;
    data.jackOutID = output.GetInstanceID();

    GetTransformData(data);

    data.jackInID = new int[count * 2];
    data.sliders = new float[count];

    for (int i = 0; i < count; i++) {
      data.sliders[i] = signal.incoming[i].GetComponent<fader>().fadeSlider.percent;
      data.jackInID[2 * i] = signal.incoming[i].GetComponent<fader>().inputA.transform.GetInstanceID();
      data.jackInID[2 * i + 1] = signal.incoming[i].GetComponent<fader>().inputB.transform.GetInstanceID();
    }

    data.height = lengthSlider.localPosition.x;

    return data;
  }

  public override void Load(InstrumentData d) {
    MixerData data = d as MixerData;
    base.Load(data);
    output.GetComponent<omniJack>().ID = data.jackOutID;

    count = data.sliders.Length;
    Vector3 pos = stretchSlider.localPosition;
    pos.x = (count - 1) * -.04f - .076f;
    stretchSlider.localPosition = pos;
    updateMixerCount();

    pos = lengthSlider.localPosition;
    pos.x = data.height;
    lengthSlider.localPosition = pos;

    for (int i = 0; i < count; i++) {
      signal.incoming[i].GetComponent<fader>().fadeSlider.setPercent(data.sliders[i]);
      signal.incoming[i].GetComponent<fader>().inputA.ID = data.jackInID[2 * i];
      signal.incoming[i].GetComponent<fader>().inputB.ID = data.jackInID[2 * i + 1];
    }
  }
}

public class MixerData : InstrumentData {
  public int[] jackInID;
  public float[] sliders;
  public int jackOutID;
  public float height;
}
