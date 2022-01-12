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

public class spectrumDisplay : MonoBehaviour {
  public AudioSource source;
  int texW = 2048;
  int texH = 64;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;
  Color32 onColor = new Color32(255, 255, 255, 255); // add transparency?
  Color32 offColor = new Color32(0, 0, 0, 255);


  bool active = false;

  float[] spectrum;

  void Start() {
    spectrum = new float[texW];

    tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
    texpixels = new Color32[texW * texH];

    for (int i = 0; i < texpixels.Length; i++) texpixels[i] = new Color32(0, 255, 0, 100);
    tex.SetPixels32(texpixels);
    tex.Apply(true);

    texrend.material.mainTexture = tex;
    texrend.material.SetTexture(Shader.PropertyToID("_MainTex"), tex);
    texrend.material.SetColor("_EmissionColor", Color.HSVToRGB(10 / 400f, 98 / 255f, 1f));
    texrend.material.SetFloat("_EmissionGain", .4f);
  }

  const float spectrumMult = 5;

  void GenerateTex() {

    // fill background
    for (int i = 0; i < texpixels.Length; i++) 
    {
      texpixels[i] = offColor;
    }
    tex.SetPixels32(texpixels);
    tex.Apply();

    // draw spectrum
    int bandHeight = 0;
    int bandX = 0;
    for (int freqBand = 0; freqBand < spectrum.Length; freqBand++)
    {
      //bandY = Mathf.RoundToInt(Mathf.Pow(spectrum[freqBand], 0.5f) * texH);

      bandX = Mathf.RoundToInt( Mathf.Pow((float) freqBand / spectrum.Length, 0.5f) * spectrum.Length);

      bandHeight = Mathf.RoundToInt(Mathf.Pow(spectrum[freqBand], 0.3f) * texH);

      for (int bandY = 0; bandY < bandHeight; bandY ++){
        tex.SetPixel(
          Mathf.RoundToInt(bandX),
          Mathf.RoundToInt(bandY),
          onColor);
      }
    }
    tex.Apply();

    //for (int x = 0; x < texW; x++)
    //{
    //  for (int y = 0; y < texH; y++)
    //  {
    //    texpixels[y * texW + x] = (spectrum[x] * spectrumMult * texH >= y) ? onColor : offColor; // how to make this logarithmic?
    //  }
    //}


    //for (int i = 1; i < spectrum.Length - 1; i++)
    //{
    //  Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0)                        , new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
    //  Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2)         , new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
    //  Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1)         , new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
    //  Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3)   , new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
    //}
  }

  //public void drawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
  //{
  //  Vector2 t = p1;
  //  float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
  //  float ctr = 0;

  //  while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
  //  {
  //    t = Vector2.Lerp(p1, p2, ctr);
  //    ctr += frac;
  //    tex.SetPixel((int)t.x, (int)t.y, col);
  //  }
  //}

  

  public void toggleActive(bool on) {
    active = on;
    if (!active) {
      for (int i = 0; i < texpixels.Length; i++) texpixels[i] = new Color32(255, 0, 0, 255);
      tex.SetPixels32(texpixels);
      tex.Apply(true);
    }
  }

  void Update() {
    if (!active) return;

    source.GetSpectrumData(spectrum, 0, FFTWindow.Triangle);
    GenerateTex();
    
    //tex.Apply(true);
  }
}
