// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;

//assumes its all local
public class primitiveLine : MonoBehaviour {
  public Transform p1, p2;

  Vector3 lastPos1, lastPos2;

  void Awake() {
    lastPos1 = Vector3.zero;
    lastPos2 = Vector3.zero;
  }

  void Update() {
    if (lastPos1 != p1.localPosition || lastPos2 != p2.localPosition) {
      lastPos1 = p1.localPosition;
      lastPos2 = p2.localPosition;
      UpdateLine();
    }
  }

  void UpdateLine() {
    transform.position = Vector3.Lerp(p1.position, p2.position, .5f);
    float dist = Vector3.Distance(p1.localPosition, p2.localPosition);
    transform.localScale = new Vector3(.0025f, dist, 1);
    float rot = Mathf.Atan2(p1.transform.localPosition.x - p2.transform.localPosition.x, p2.transform.localPosition.y - p1.transform.localPosition.y) * Mathf.Rad2Deg;
    transform.localRotation = Quaternion.Euler(0, 0, rot);
  }
}
