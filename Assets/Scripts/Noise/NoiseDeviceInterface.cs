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

public class NoiseDeviceInterface : deviceInterface {

  int ID = 0;
  public dial speedDial;
  omniJack output;

  NoiseSignalGenerator gen;

  int texSize = 16;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;

  float blackFrequency = .85f;

  public override void Awake() {
    base.Awake();
    gen = GetComponent<NoiseSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
  }

  void Start() {
    tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
    texpixels = new Color32[texSize * texSize];

    GenerateRandomTex();
    tex.SetPixels32(texpixels);
    tex.Apply(true);

    texrend.material.mainTexture = tex;
    texrend.material.SetTexture(Shader.PropertyToID("_Illum"), tex);
    texrend.material.SetColor("_TintColor", Color.white);
    texrend.material.SetFloat("_EmissionGain", .4f);
  }

  void GenerateRandomTex() {
    for (int i = 0; i < texSize; i++) {
      for (int i2 = 0; i2 < texSize; i2++) {
        byte s = 255;
        if (Random.value < blackFrequency) s = 0;
        texpixels[i2 * texSize + i] = new Color32(s, s, s, 255);
      }
    }
  }

  void Update() {
    if (gen.updated || output.near == null) {
      gen.updated = false;
      GenerateRandomTex();
      tex.SetPixels32(texpixels);
      tex.Apply(true);
    }

    gen.updatePercent(speedDial.percent);
  }

  public override InstrumentData GetData() {
    NoiseData data = new NoiseData();
    data.deviceType = menuItem.deviceType.Noise;

    GetTransformData(data);

    data.dialState = speedDial.percent;
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    NoiseData data = d as NoiseData;
    base.Load(data);
    ID = data.ID;
    output.ID = data.jackOutID;
    speedDial.setPercent(data.dialState);
  }
}

public class NoiseData : InstrumentData {
  public float dialState;
  public int jackOutID;
}