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
using System.Runtime.InteropServices;
public class clipPlayer : signalGenerator {

  public bool loaded = false;
  public float[] clipSamples;
  public int clipChannels = 2;
  public int[] sampleBounds = new int[] { 0, 0 };
  public float floatingBufferCount = 0;
  public int bufferCount = 0;
  public Vector2 trackBounds = new Vector2(0, 1);

  public void UnloadClip() {
    loaded = false;
    toggleWaveDisplay(false);
  }

  public GCHandle m_ClipHandle;

  public void LoadSamples(float[] samples, GCHandle _cliphandle, int channels) {

    m_ClipHandle = _cliphandle;
    clipChannels = channels;
    clipSamples = samples;
    sampleBounds[0] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.x));
    sampleBounds[1] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.y));
    floatingBufferCount = bufferCount = sampleBounds[0];

    toggleWaveDisplay(true);
    DrawClipTex();
    loaded = true;
  }

  public virtual void toggleWaveDisplay(bool on) {
  }

  public virtual void DrawClipTex() {
  }
}
