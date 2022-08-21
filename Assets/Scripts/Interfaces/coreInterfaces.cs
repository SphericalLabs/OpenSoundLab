// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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
