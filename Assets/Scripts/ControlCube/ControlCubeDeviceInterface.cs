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

public class ControlCubeDeviceInterface : deviceInterface {

  public Vector3 percent;
  public ControlCubeSingleSignalGenerator[] signals;
  public omniJack[] outputs;
  public cubeZone cubeManip;

  public override void Awake() {
    base.Awake();
    Setup(percent);
  }

  void Setup(Vector3 p) {
    percent = p;
    cubeManip.updateLines(percent); 
    updatePercent(percent);
    //toggleMute(true);
  }

  public override void hit(bool on, int ID = -1) {

  }

  void Update() {
  }

  public void updatePercent(Vector3 p) {
    percent = p;

    signals[0].value = (percent.x - .5f) * 2;
    signals[1].value = (percent.y - .5f) * 2;
    signals[2].value = (percent.z - .5f) * 2;

  }

  public override InstrumentData GetData() {
    ControlCubeData data = new ControlCubeData();

    data.deviceType = menuItem.deviceType.ControlCube;
    GetTransformData(data);

    data.jackOutID = new int[4];
    for (int i = 0; i < 3; i++) data.jackOutID[i] = outputs[i].transform.GetInstanceID();

    data.dimensionValues = new float[3];
    for (int i = 0; i < 3; i++) data.dimensionValues[i] = percent[i];

    return data;
  }

  public override void Load(InstrumentData d) {
    ControlCubeData data = d as ControlCubeData;

    base.Load(data);

    for (int i = 0; i < 3; i++) outputs[i].ID = data.jackOutID[i];
    for (int i = 0; i < 3; i++) percent[i] = data.dimensionValues[i];
        
    Setup(percent);
  }
}

public class ControlCubeData : InstrumentData {
  public int[] jackOutID;
  public float[] dimensionValues;
}