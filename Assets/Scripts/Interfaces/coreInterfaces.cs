// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

public class componentInterface : MonoBehaviour {
  public bool useNativeIfAvailable = true;

  public virtual void hit(bool on, int ID = -1) {
  }

  public virtual void hit(bool on, int IDx, int IDy) {
  }

  public virtual void onSelect(bool on, int ID = -1) {
  }

  public virtual void onSelect(bool on, int IDx, int IDy) {
  }

  public virtual void onGrab(bool on, int ID) {

  }

  public virtual void onTimelineEvent(int track, bool on) {

  }
}

public class deviceInterface : componentInterface {
  public virtual void Awake() {

  }

  public virtual void OnMidiNote(int channel, bool on, int pitch) {

  }

  public virtual void OnMidiCC(int channel, int ID, int value) {

  }

  public virtual InstrumentData GetData() {
    return new InstrumentData();
  }

  public void GetTransformData(InstrumentData data) {
    data.position = transform.position;
    data.rotation = transform.rotation;
    data.scale = transform.localScale;
    data.ID = transform.GetInstanceID();
  }

  public virtual void Load(InstrumentData data) {
    transform.localPosition = data.position;
    transform.localRotation = data.rotation;
    transform.localScale = data.scale;
  }
}
