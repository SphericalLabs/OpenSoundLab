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
using System.Collections.Generic;

public class tapeGroupDeviceInterface : deviceInterface {
  public GameObject tapePrefab;
  public Transform tapeHolder;
  public TextMesh label;
  int columns = 6;

  string samplegroup = "";

  Vector2 offset = new Vector2(0, -.03f);
  public void Setup(string s) {
    int count = 0;
    label.text = samplegroup = s;
    foreach (KeyValuePair<string, string> entry in sampleManager.instance.sampleDictionary[s]) {
      GameObject g = Instantiate(tapePrefab, Vector3.zero, Quaternion.identity) as GameObject;
      g.transform.parent = tapeHolder.transform;
      g.transform.localRotation = Quaternion.Euler(-90, 0, 0);
      int xMult = count % columns;
      int yMult = count / columns;
      g.transform.localPosition = new Vector3(offset.x - .115f * xMult, offset.y + -yMult * .035f, 0.02f);
      g.GetComponent<tape>().Setup(entry.Key, entry.Value);
      count++;
    }
  }

  public override InstrumentData GetData() {
    TapeGroupData data = new TapeGroupData();
    data.deviceType = menuItem.deviceType.TapeGroup;
    GetTransformData(data);

    data.samplegroup = samplegroup;

    return data;
  }

  public override void Load(InstrumentData d) {
    TapeGroupData data = d as TapeGroupData;
    base.Load(data);

    Setup(data.samplegroup);
  }
}
public class TapeGroupData : InstrumentData {
  public string samplegroup;
}