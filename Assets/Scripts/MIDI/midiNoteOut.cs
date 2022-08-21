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
using System.Runtime.InteropServices;

public class midiNoteOut : signalGenerator {
  public int ID;

  midiOutDeviceInterface _deviceinterface;

  signalGenerator incoming;
  public omniJack input;

  public GameObject statusObject;

  public Renderer[] glowRends;
  public TextMesh label;

  public bool noteOn = false;

  [DllImport("SoundStageNative")]
  public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

  public override void Awake() {
    base.Awake();
    input = GetComponentInChildren<omniJack>();
    _deviceinterface = GetComponentInParent<midiOutDeviceInterface>();
  }

  void Start() {
    glowRends[1].material.SetFloat("_EmissionGain", .3f);
    statusObject.SetActive(noteOn);
  }

  public void SetAppearance(string s, float h) {
    label.text = s;

    for (int i = 0; i < glowRends.Length; i++) glowRends[i].material.SetColor("_TintColor", Color.HSVToRGB(h, .5f, .7f));
  }

  void Update() {
    if (input.signal != incoming) incoming = input.signal;
    statusObject.SetActive(noteOn);
  }

  float lastBuffer = -1;
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (incoming == null) return;
    incoming.processBuffer(buffer, dspTime, channels);

    bool on = GetBinaryState(buffer, buffer.Length, channels, ref lastBuffer);

    if (on != noteOn) {
      noteOn = on;
      _deviceinterface.receiveMidiNote(ID, noteOn);
    }
  }
}
