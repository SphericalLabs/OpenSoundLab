// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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

public class scrubQuad : MonoBehaviour {
  Dictionary<manipulator, scrubber> manips = new Dictionary<manipulator, scrubber>();
  public Transform scrubBar;
  public Transform scrubIndicator;
  clipPlayerComplex player;

  scrubber scrubberActive = null;
  scrubber scrubberCandidate = null;
  Vector2 scrubrange = new Vector2(.2f, -.2f);
  class scrubber {
    public Transform trans { get; set; }
    public bool trigger { get; set; }
    public bool colliding { get; set; }

    public scrubber(Transform t, bool g, bool c) {
      trans = t;
      trigger = g;
      colliding = c;
    }
  }

  void Awake() {
    player = transform.parent.GetComponent<clipPlayerComplex>();
    Color c = Color.HSVToRGB(298 / 400f, 84 / 255f, .01f);
    scrubIndicator.GetComponent<Renderer>().material.SetColor("_TintColor", c);
  }

  void OnCollisionEnter(Collision coll) {
    return; // workaround for turning off broken scrubbing
    manipulator m = coll.transform.GetComponent<manipulator>();
    if (m == null) return;
    if (manips.ContainsKey(m)) manips[m].colliding = true;
    else manips[m] = new scrubber(m.transform, m.triggerDown, true);
    m.hapticPulse();
    {
      scrubberCandidate = manips[m];
      scrubIndicator.gameObject.SetActive(true);
    }
  }


  void OnCollisionExit(Collision coll) {
    return; // workaround for turning off broken scrubbing
    manipulator m = coll.transform.GetComponent<manipulator>();
    if (m == null) return;
    if (manips.ContainsKey(m)) {
      manips[m].colliding = false;
      if (scrubberCandidate == manips[m]) {
        scrubberCandidate = null;
        scrubIndicator.gameObject.SetActive(false);
      }
    }
  }


  void updateScrubbers(manipulator m) {
    if (manips[m].trigger && manips[m].colliding && m.emptyGrab) {
      scrubberCandidate = scrubberActive = manips[m];
      player.grabScrub(true);
    } else {
      player.grabScrub(false);
    }
  }

  void Update() {
    foreach (manipulator m in manips.Keys) {
      if (m == null) manips.Remove(m);
      else {
        if (manips[m].trigger != m.triggerDown) {
          manips[m].trigger = m.triggerDown;
          updateScrubbers(m);
        }
      }
    }

    if (scrubberCandidate != null) {
      Vector3 pos = transform.parent.InverseTransformPoint(scrubberCandidate.trans.position);
      Vector3 posB = scrubIndicator.localPosition;
      posB.x = Mathf.Clamp(pos.x, .2f - .4f * player.trackBounds.y, .2f - .4f * player.trackBounds.x);
      scrubIndicator.localPosition = posB;
    }

    if (scrubberActive != null) {
      Vector3 pos = transform.parent.InverseTransformPoint(scrubberActive.trans.position);
      Vector3 posB = scrubBar.localPosition;
      posB.x = Mathf.Clamp(pos.x, .2f - .4f * player.trackBounds.y, .2f - .4f * player.trackBounds.x);
      scrubBar.localPosition = posB;
    }
  }
}
