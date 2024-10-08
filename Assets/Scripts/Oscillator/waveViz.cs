// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;



public class waveViz : MonoBehaviour
{

  public Renderer displayRenderer;

  RenderTexture offlineTexture;
  Material offlineMaterial;

  Texture2D onlineTexture;
  public Material onlineMaterial;

  public int waveWidth = 256;
  public int waveHeight = 64;
  public int heightPadding = 2;
  public int sampleStep = 512; // should not be higher than buffer size, otherwise looped data?
  public bool doTriggering = false;

  public bool active = false;

  FilterMode fm = FilterMode.Bilinear;
  int ani = 4;
  public int offset = 0;

  IntPtr ringBufferPtr;

  ///Writes n samples to the ring buffer.
  [DllImport("OSLNative")]
  static extern void RingBuffer_Write(float[] src, int n, IntPtr x);

  ///Writes samples to the ringbuffer with a specified stride. If the stride is 1, all samples are written to the ringbuffer. If stride < 1, some samples are skipped. If stride > 1, some samples are written more than once (=padded)f. No interpolation is performed. Returns the difference between old and new writeptr.
  [DllImport("OSLNative")]
  static extern int RingBuffer_WritePadded(float[] src, int n, float stride, IntPtr x);

  ///Reads n samples from the ring buffer
  [DllImport("OSLNative")]
  static extern void RingBuffer_Read(float[] dest, int n, int offset, IntPtr x);

  //Reads n samples with a specific stride
  [DllImport("OSLNative")]
  static extern void RingBuffer_ReadPadded(float[] dest, int n, int offset, float stride, IntPtr x);

  ///Reads n samples from the ring buffer and adds the values to the dest array.
  //static extern void RingBuffer_ReadAndAdd(float* dest, int n, int offset, struct RingBuffer *x);

  ///Resizes the buffer. This includes a memory re-allocation, so use with caution!
  //static extern void RingBuffer_Resize(int n, struct RingBuffer *x);
  [DllImport("OSLNative")]
  static extern IntPtr RingBuffer_New(int n);

  ///Frees all resources.
  [DllImport("OSLNative")]
  static extern void RingBuffer_Free(IntPtr x);

  [DllImport("OSLNative")]
  static extern void _fDeinterleave(float[] src, float[] dest, int n, int channels);

  [DllImport("OSLNative")]
  static extern void SetArrayToSingleValue(float[] a, int length, float val);

  float[] renderBuffer;
  float[] storageBuffer;
  float[] fullBuffer;

  void Awake()
  {
    //int longBufferLength = Mathf.RoundToInt(waveWidth * 4f);
    ringBufferPtr = RingBuffer_New(waveWidth*2);
    fullBuffer = new float[waveWidth*2];
    renderBuffer = new float[waveWidth];

    storageBuffer = new float[1];

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
    onlineMaterial.mainTexture = onlineTexture;

    if (onlineTexture.filterMode != fm) onlineTexture.filterMode = fm;
    if (onlineTexture.anisoLevel != ani) onlineTexture.anisoLevel = ani;
    if (onlineTexture.mipMapBias != -0.15f) onlineTexture.mipMapBias = -0.15f;

    // init black screen
    clearRenderTexture();

  }

  int tempIndex;
  public void storeBuffer(float[] buffer, int channels)
  {
    if (storageBuffer.Length != buffer.Length / channels / sampleStep) System.Array.Resize(ref storageBuffer, buffer.Length / channels / sampleStep);
    
    for (int i = 0; i < storageBuffer.Length; i++)
    {
      tempIndex = i * channels * sampleStep; // skip through the interleaved audio buffer
      if (tempIndex >= buffer.Length) break; // overshooting the buffer, finish
      storageBuffer[i] = buffer[tempIndex];
    }

    RingBuffer_Write(storageBuffer, storageBuffer.Length, ringBufferPtr);
  }

  void Update()
  {
    if (!active) return;
    if (!displayRenderer.isVisible) return;

    RenderGLToTexture(waveWidth, waveHeight, offlineMaterial);
    
  }

  bool lastWasNegative = false;
  bool foundZeroCrossing = false;

  int kk;

  void clearRenderTexture(){
    RenderTexture.active = offlineTexture;
    GL.Clear(false, true, Color.black);
    Graphics.CopyTexture(offlineTexture, onlineTexture);
  }

  void RenderGLToTexture(int width, int height, Material material)
  {

    if (doTriggering)
    {

      kk = 0;

      RingBuffer_Read(fullBuffer, fullBuffer.Length, 0, ringBufferPtr); // first scan on data, TODO: move trigger code to RingBuffer
      lastWasNegative = fullBuffer[fullBuffer.Length - 1 - kk] <= 0f; // take first sample
      foundZeroCrossing = false;
      kk++;

      while (kk < fullBuffer.Length - renderBuffer.Length)
      { // search for first 0-pass from right and then take as offset
        if ((fullBuffer[fullBuffer.Length - 1 - kk] < 0f && !lastWasNegative) /*(fullBuffer[fullBuffer.Length - 1 - kk] < 0f && lastWasPositive)*/) // triggers on rising zero crossing only
        {
          foundZeroCrossing = true;
          break;
        }
        lastWasNegative = fullBuffer[fullBuffer.Length - 1 - kk] <= 0f;
        kk++;
      }
      
      if (!foundZeroCrossing) return; // no new draw if nothing found

      Array.Copy(fullBuffer, fullBuffer.Length - 1 - renderBuffer.Length - kk, renderBuffer, 0, renderBuffer.Length);

    } else {
      RingBuffer_Read(renderBuffer, renderBuffer.Length, -renderBuffer.Length, ringBufferPtr);
    }

    
    // always re-set offline render here, in case another script had its finger on it in the meantime
    RenderTexture.active = offlineTexture;

    GL.Clear(false, true, Color.black);

    material.SetPass(0);
    GL.PushMatrix();
    GL.LoadPixelMatrix(0, width, height, 0);
    GL.Color(new Color(1, 1, 1, 1f));
    GL.Begin(GL.LINE_STRIP);

    for (int i = 0; i < renderBuffer.Length; i++)
    {
      //GL.Vertex3(i, height - (renderBuffer[i] + 1f) * 0.5f * height, 0); // 2px padding
      GL.Vertex3(i, Utils.map(renderBuffer[i], -1f, 1f, height - heightPadding, heightPadding), 0); // 2px padding
    }

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

    RingBuffer_Free(ringBufferPtr);
  }

  public void toggleActive(bool on)
  {
    if (active == on) return;
    active = on;
    if (offlineTexture || onlineTexture == null) return; // for Oscillator viz
    RenderTexture.active = offlineTexture;
    GL.Clear(false, true, Color.black);
    Graphics.CopyTexture(offlineTexture, onlineTexture);
  }

}
