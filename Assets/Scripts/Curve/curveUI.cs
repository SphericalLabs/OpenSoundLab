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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class curveUI : manipObject {
  public GameObject posIndicator;
  public override void Awake() {
    base.Awake();
    posIndicator.SetActive(false);
  }

  public override void selectUpdate(Transform t) {
    updatePosIndicator(t.position);
  }

  public override void grabUpdate(Transform t) {
    updatePosIndicator(t.position);
  }

  void updatePosIndicator(Vector3 worldpos) {
    Vector3 pos = transform.InverseTransformPoint(worldpos);
    pos.z = 0;
    pos.x = Mathf.Clamp(pos.x, -.5f, .5f);
    pos.y = Mathf.Clamp(pos.y, -.5f, .5f);
    posIndicator.transform.localPosition = pos;
  }

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      posIndicator.SetActive(false);
    } else if (curState == manipState.selected) {
      posIndicator.SetActive(true);
      if (selectObj != null) updatePosIndicator(selectObj.position);
    } else if (curState == manipState.grabbed) {
      posIndicator.SetActive(true);
      if (manipulatorObj != null) updatePosIndicator(manipulatorObj.position);
    }
  }
}
