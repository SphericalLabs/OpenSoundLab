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
  float[] spectrum;

  int texW = 256*4;
  int texH = 256*1;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;
  Color32[] blackpixels;
  public Color32 onColor = new Color32(255, 255, 255, 255); 
  public Color32 offColor = new Color32(0, 0, 0, 255);
    
  public bool doSpectrum = true;
  public bool doClear1 = true;
  public bool doDraw = true;
  bool active = false;

  int drawY = 0;
  int drawX = 0;  
  int lastDrawX = 0;
  int lastDrawY = 0;
  public int skip = 1;
  float bandWidth;
  float maxLog;

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

    bandWidth = 24000f / texW;
    maxLog = Mathf.Log10(24000);

  }

  

  void GenerateTex() {

    // Please note: Ideally this would be ported to native code and OpenGL?
    // Fallback, decrease fps: 72, 36, 18
    // run multi-threaded? https://www.raywenderlich.com/7880445-unity-job-system-and-burst-compiler-getting-started

    if (doClear1)
    {
      Array.Copy(blackpixels, texpixels, blackpixels.Length);
    }
        
    if (doDraw)
    {
      // reset for this frame
      drawY = 0;
      drawX = 0;
      lastDrawX = 0;
      lastDrawY = 0;

      for (int freqBand = 1; freqBand < spectrum.Length; freqBand++) // skip 0, because of log negative?
      {
        drawX = Mathf.RoundToInt(Utils.map(Mathf.Log10(freqBand * bandWidth), 1f, maxLog, 0f, texW));
        if (drawX - lastDrawX <= skip) continue; // skip bands if too close together

        drawY = Mathf.RoundToInt(Mathf.Pow(spectrum[freqBand], 0.3f) * (texH-1));

        drawLine(texpixels, lastDrawX, lastDrawY, drawX, drawY, onColor);

        //p1 = p2;
        lastDrawX = drawX;
        lastDrawY = drawY;
      }

      tex.SetPixels32(texpixels);
      tex.Apply(); // apply takes time, from RAM to vRAM.
    }
  }

  // Bresenham's line algorithm, ported from http://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
  void drawLine(Color32[] pixels, int x0, int y0, int x1, int y1, Color color)
  {
    int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;  // move variables globally for speed
    int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
    int err = (dx > dy ? dx : -dy) / 2, e2;
    for (; ; )
    {
      pixels[y0 * texW + x0] = color;
      if (x0 == x1 && y0 == y1)
      {
        break;
      }
      e2 = err;
      if (e2 > -dx) { err -= dy; x0 += sx; }
      if (e2 < dy) { err += dx; y0 += sy; }
    }
  }

  public void toggleActive(bool on) {
    active = on;
    if (!active) {
      clearScreen();
      tex.SetPixels32(texpixels);
      tex.Apply(true);
    }
  }

  void clearScreen(){
    Array.Copy(blackpixels, texpixels, blackpixels.Length);
  }

  void Update() {
    //Stopwatch timer = new Stopwatch();
    //timer.Start();
    if (!active) return;

    if(doSpectrum) source.GetSpectrumData(spectrum, 0, fftWin);

    GenerateTex();
    //timer.Stop();
    //UnityEngine.Debug.Log(timer.ElapsedTicks);
  }
}
