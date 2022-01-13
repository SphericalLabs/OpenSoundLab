// Copyright 2017 Google LLC
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
using System.Diagnostics;
using System;

public class spectrumDisplay : MonoBehaviour {
  public AudioSource source;
  public FFTWindow fftWin = FFTWindow.BlackmanHarris;

  int texW = 256*4;
  int texH = 256;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;
  Color32[] blackpixels;
  public Color32 onColor = new Color32(255, 255, 255, 255); 
  public Color32 offColor = new Color32(0, 0, 0, 255);

  bool active = false;

  public bool doSpectrum = true;
  public bool doClear1 = true;
  public bool doClear2 = false;
  public bool doDraw = true;

  float[] spectrum;

  void Start() {
    spectrum = new float[texW];

    tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
    texpixels = new Color32[texW * texH];
    blackpixels = new Color32[texW * texH];

    for (int i = 0; i < blackpixels.Length; i++) blackpixels[i] = offColor;
    tex.SetPixels32(blackpixels);
    tex.Apply(true);

    texrend.material.mainTexture = tex;
    texrend.material.SetTexture(Shader.PropertyToID("_MainTex"), tex);

  }

  const float spectrumMult = 5;
  public int skip = 15;

  void GenerateTex() {

    // Please note: Ideally this would be ported to native code and OpenGL?
    // Fallback, decrease fps: 72, 36, 18

    if (doClear1)
    {
      Array.Copy(blackpixels, texpixels, blackpixels.Length);
    }

    if(doClear2){
      //fill background
      for (int i = 0; i < texpixels.Length; i++)
      {
        texpixels[i] = offColor; // takes a lot of time, speed up with optimised copy from backbuffer?
      }
    }

    // run multi-threaded? https://www.raywenderlich.com/7880445-unity-job-system-and-burst-compiler-getting-started
    if (doDraw)
    {
      // draw spectrum
      int bandHeight = 0;
      int bandX = 0;
      Vector2 p1 = new Vector2(0f, 0f);
      Vector2 p2 = new Vector2(0f, 0f);
      int lastBandX = 0;
      for (int freqBand = 0; freqBand < spectrum.Length; freqBand++)
      {
        bandX = Mathf.RoundToInt(Mathf.Pow((float)freqBand / spectrum.Length, 0.5f) * spectrum.Length); // skip bands if to close together?, do uv trick to avoid non-linearity?
        if (bandX - lastBandX <= skip) continue;
        bandHeight = Mathf.RoundToInt(Mathf.Pow(spectrum[freqBand], 0.3f) * texH);

        p2.x = bandX;
        p2.y = bandHeight;
        drawLine(texpixels, p1, p2, onColor); 
        p1 = p2;
        lastBandX = bandX;
      }
      tex.SetPixels32(texpixels);
      tex.Apply(); // apply takes time, from RAM to vRAM.
    }
  }

  public void drawLine(Color32[] pixels, Vector2 p1, Vector2 p2, Color col)
  {
    Vector2 t = p1;
    float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
    float ctr = 0;

    while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
    {
      t = Vector2.Lerp(p1, p2, ctr);
      ctr += frac;
      //tex.SetPixel((int)t.x, (int)t.y, col);
      pixels[(int)t.y * tex.width + (int)t.x] = col;
    }
  }

  public void toggleActive(bool on) {
    active = on;
    if (!active) {
      for (int i = 0; i < texpixels.Length; i++) texpixels[i] = new Color32(255, 0, 0, 255);
      tex.SetPixels32(texpixels);
      tex.Apply(true);
    }
  }

  void Update() {
    Stopwatch timer = new Stopwatch();
    timer.Start();
    if (!active) return;

    if(doSpectrum) source.GetSpectrumData(spectrum, 0, fftWin);

    GenerateTex();
    timer.Stop();
    UnityEngine.Debug.Log(timer.ElapsedTicks);

    
  }
}
