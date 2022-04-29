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

public class loadingFeedback : MonoBehaviour {
  TextMesh txt;
  string[] txtFrames;
  // Use this for initialization
  void Awake() {
    txt = GetComponent<TextMesh>();
    txtFrames = new string[]
    {
            "  ",".","..","..."
    };
  }

  float timer = 0;
  int frame = 0;
  void Update() {
    timer = Mathf.Clamp01(timer + Time.deltaTime * 10);
    if (timer == 1) {
      timer = 0;
      frame = (frame + 1) % txtFrames.Length;
      txt.text = txtFrames[frame];
    }
  }
}
