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
//using System.Collections;
using System.Collections.Generic;
//using System.Runtime.InteropServices;

public class waveViz : MonoBehaviour {

  public Renderer displayRenderer;

  RenderTexture offlineTexture;
  Material offlineMaterial;

  Texture2D onlineTexture;
  public Material onlineMaterial;

  int waveWidth = 512;
  int waveHeight = 64;
  public int period = 512;

  //int curWaveW = 0;
  //int lastWaveH = 0;
  
  public FilterMode fm = FilterMode.Bilinear;
  public int ani = 4;

  public float[] lastBuffer; // will be written from audio thread

  //List<float> bufferDrawList;
  
  void Awake() {
    
  offlineMaterial = new Material(Shader.Find("GUI/Text Shader"));
    offlineMaterial.hideFlags = HideFlags.HideAndDontSave;
    offlineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
    // get a RenderTexture and set it as target for offline rendering
    offlineTexture = new RenderTexture(waveWidth, waveHeight, 24);
    offlineTexture.useMipMap = true;
    
    // these are the ones that you will actually see
    onlineTexture = new Texture2D(waveWidth, waveHeight, TextureFormat.RGBA32, true);
    onlineMaterial = Instantiate(onlineMaterial);
    displayRenderer.material = onlineMaterial;
    onlineMaterial.SetTexture(Shader.PropertyToID("_MainTex"), onlineTexture);

  }
  

  void Start() {
    onlineMaterial.mainTexture = onlineTexture;
  }
  
  public void storeBuffer(float[] buffer){
    
    if(lastBuffer == null) lastBuffer = new float[0]; 
    if (lastBuffer.Length != buffer.Length) System.Array.Resize(ref lastBuffer, buffer.Length);
    System.Array.Copy(buffer, lastBuffer, buffer.Length);
    
  }

  void Update() {

    RenderGLToTexture(waveWidth, waveHeight, offlineMaterial);

    onlineTexture.filterMode = fm;
    onlineTexture.anisoLevel = ani;

    //waverend.material.mainTextureOffset = new Vector2((float)curWaveW / waveWidth, 0);
  }

  void RenderGLToTexture(int width, int height, Material material)
  {

    // always re-set offline render here, in case another script had its finger on it in the meantime
    RenderTexture.active = offlineTexture;

    GL.Clear(false, true, Color.black);

    material.SetPass(0);
    GL.PushMatrix();
    GL.LoadPixelMatrix(0, width, height, 0);
    GL.Color(new Color(1, 1, 1, 1f));
    GL.Begin(GL.LINE_STRIP);

    for(int i = 0; i < lastBuffer.Length; i++){
      GL.Vertex3(i, (1f - (lastBuffer[i] + 1f) * 0.5f) * waveHeight, 0);
    }
    

    //drawY = 0;
    //drawX = 0;
    //lastDrawX = 0;
    //lastDrawY = 0;

    //for (int freqBand = 0; freqBand < spectrum.Length; freqBand++) // skip 0, because of log negative?
    //{
    //  drawX = Mathf.RoundToInt(Utils.map(Mathf.Log10(freqBand * bandWidth), leftOffset, maxLog, 0f, width));
    //  if (drawX - lastDrawX < skip)
    //  {
    //    continue; // skip bands if too close together
    //  }

    //  drawY = height - Mathf.RoundToInt(Mathf.Pow(spectrum[freqBand], 0.3f) * height);

    //  GL.Vertex3(drawX, drawY, 0);

    //  //p1 = p2;
    //  lastDrawX = drawX;
    //  lastDrawY = drawY;
    //}

    GL.End();
    GL.PopMatrix();

    // blit/copy the offlineTexture into the onlineTexture (on GPU)
    Graphics.CopyTexture(offlineTexture, onlineTexture);

  }

  public void OnDestroy()
  {
    // cleanup manually, since the GC isn't managing the GPU
    if (offlineTexture != null)
    {
      offlineTexture.Release();
      Destroy(offlineTexture);
    }
  }

}
