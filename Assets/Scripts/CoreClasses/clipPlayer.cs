// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
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

using System;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class clipPlayer : signalGenerator
{
    const int BIQUAD_LOWPASS = 1;
    const int BIQUAD_HIGHPASS = 2;

    public bool loaded = false;
    [NonSerialized] public float[] clipSamples;
    public int clipChannels = 2;
    [NonSerialized] public int[] sampleBounds = new int[] { 0, 0 };
    public double floatingBufferCount = 0;
    public int bufferCount = 0;
    public Vector2 trackBounds = new Vector2(0, 1);
    // Shared sampler defaults for A/B testing: set interpolation mode, sample-layer use, and post-filter strength here.
    public SamplerInterpolationMode interpolationMode = SamplerInterpolationMode.WindowedSinc23;
    public bool useSampleLayers = true;
    [Range(0f, 1f)] public float postFilterStrength = 0.5f;

    protected double _lastBuffer = 0;
    public double LastBuffer { get => _lastBuffer; set => _lastBuffer = value; }
    protected SampleLayerBank sampleLayerBank;
    protected int sampleLayerCount = 0;
    protected IntPtr[] sampleLayerPointers = new IntPtr[SampleLayerBank.MaxLayers];
    protected int[] sampleLayerFrameCounts = new int[SampleLayerBank.MaxLayers];
    protected readonly object clipStateLock = new object();
    IntPtr lowpassFilter = IntPtr.Zero;
    IntPtr dcBlocker = IntPtr.Zero;

    [DllImport("OSLNative")]
    static extern IntPtr Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels);

    [DllImport("OSLNative")]
    static extern void Biquad_free(IntPtr x);

    [DllImport("OSLNative")]
    static extern void Biquad_reset(IntPtr x);

    [DllImport("OSLNative")]
    static extern void Biquad_process(IntPtr x, int type, float frequency, float Q, float gain, float sampleRate,
        float[] input, float[] output, int n);

    public override void Awake()
    {
        base.Awake();
        ensureRuntimeState();
        lowpassFilter = Biquad_new(BIQUAD_LOWPASS, (float)_sampleRate * 0.45f, 0.7071f, 0f, (float)_sampleRate, 2);
        dcBlocker = Biquad_new(BIQUAD_HIGHPASS, 20f, 0.7071f, 0f, (float)_sampleRate, 2);
    }

    public void UnloadClip()
    {
        lock (clipStateLock)
        {
            ensureRuntimeState();
            loaded = false;
            clipSamples = null;
            sampleLayerBank = null;
            sampleLayerCount = 0;
            clearLayerPointers();
            resetPlaybackFilters();
            if (isActiveAndEnabled)
            {
                toggleWaveDisplay(false);
            }
        }
    }

    void OnDestroy()
    {
        freePlaybackFilters();
    }

    public void LoadSamples(SampleLayerBank bank, bool preservePlaybackState = false)
    {
        if (bank == null || bank.Count == 0)
        {
            return;
        }

        lock (clipStateLock)
        {
            ensureRuntimeState();
            sampleLayerBank = bank;
            clipChannels = bank.channels;
            clipSamples = bank.layers[0];
            sampleLayerCount = Mathf.Min(bank.Count, SampleLayerBank.MaxLayers);
            clearLayerPointers();
            for (int i = 0; i < sampleLayerCount; i++)
            {
                sampleLayerPointers[i] = bank.pointers[i];
                sampleLayerFrameCounts[i] = bank.frameCounts[i];
            }

            sampleBounds[0] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.x));
            sampleBounds[1] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.y));
            if (!preservePlaybackState || !loaded)
            {
                int safeStartFrame = Mathf.Min(sampleBounds[1], sampleBounds[0] + 1);
                floatingBufferCount = _lastBuffer = bufferCount = safeStartFrame;
                resetPlaybackFilters();
                toggleWaveDisplay(true);
                DrawClipTex();
            }
            else
            {
                double minFrame = Mathf.Min(sampleBounds[1], sampleBounds[0] + 1);
                double maxFrame = sampleBounds[1];
                _lastBuffer = Math.Max(minFrame, Math.Min(maxFrame, _lastBuffer));
                floatingBufferCount = Math.Max(minFrame, Math.Min(maxFrame, floatingBufferCount));
                bufferCount = Mathf.Clamp(bufferCount, (int)minFrame, sampleBounds[1]);
            }

            loaded = true;
        }
    }

    public virtual void toggleWaveDisplay(bool on)
    {
    }

    public virtual void DrawClipTex()
    {
    }

    protected float ReadLayeredSample(double framePosition, int channel, float playbackStep)
    {
        ensureRuntimeState();
        return SampleInterpolationUtility.ReadSample(sampleLayerBank, framePosition, channel, playbackStep,
            interpolationMode, useSampleLayers);
    }

    protected float EstimatePlaybackStep(float basePlaybackSpeed, float[] freqExpBuffer, bool hasFreqExp,
        float[] freqLinBuffer, bool hasFreqLin, int channels)
    {
        float maxStep = Mathf.Abs(basePlaybackSpeed);
        if (!hasFreqExp && !hasFreqLin)
        {
            return maxStep;
        }

        for (int i = 0; i < MAX_BUFFER_LENGTH && i < (freqExpBuffer != null ? freqExpBuffer.Length : int.MaxValue)
                && i < (freqLinBuffer != null ? freqLinBuffer.Length : int.MaxValue); i += channels)
        {
            float step = basePlaybackSpeed;
            if (hasFreqExp && freqExpBuffer != null)
            {
                step *= Mathf.Pow(2f, Mathf.Clamp(freqExpBuffer[i], -1f, 1f) * 10f);
            }

            if (hasFreqLin && freqLinBuffer != null)
            {
                step += freqLinBuffer[i] * 20f;
            }

            maxStep = Mathf.Max(maxStep, Mathf.Abs(step));
        }

        return maxStep;
    }

    protected void ApplyPlaybackFilters(float[] buffer, int channels, float playbackStep, bool specialMotion)
    {
        if (postFilterStrength <= 0f || buffer == null || buffer.Length == 0)
        {
            return;
        }

        float cutoff = calculatePostFilterCutoff(playbackStep, specialMotion);
        if (lowpassFilter != IntPtr.Zero && cutoff < (float)_sampleRate * 0.49f)
        {
            Biquad_process(lowpassFilter, BIQUAD_LOWPASS, cutoff, 0.7071f, 0f, (float)_sampleRate, buffer, buffer,
                buffer.Length);
        }

        if (specialMotion && dcBlocker != IntPtr.Zero)
        {
            Biquad_process(dcBlocker, BIQUAD_HIGHPASS, 20f, 0.7071f, 0f, (float)_sampleRate, buffer, buffer,
                buffer.Length);
        }
    }

    float calculatePostFilterCutoff(float playbackStep, bool specialMotion)
    {
        float absStep = Mathf.Max(0.0001f, Mathf.Abs(playbackStep));
        float cutoff = (float)_sampleRate * 0.45f / absStep;

        switch (interpolationMode)
        {
            case SamplerInterpolationMode.None:
                cutoff *= 0.55f;
                break;
            case SamplerInterpolationMode.Linear:
                cutoff *= 0.75f;
                break;
            case SamplerInterpolationMode.WindowedSinc23:
                cutoff *= 1.05f;
                break;
            case SamplerInterpolationMode.Lagrange4:
                cutoff *= 0.95f;
                break;
            default:
                cutoff *= 0.9f;
                break;
        }

        if (useSampleLayers && sampleLayerCount > 1)
        {
            cutoff *= 1.15f;
        }

        if (specialMotion)
        {
            cutoff *= 0.7f;
        }

        cutoff = Mathf.Lerp((float)_sampleRate * 0.49f, cutoff, Mathf.Clamp01(postFilterStrength));
        return Mathf.Clamp(cutoff, 80f, (float)_sampleRate * 0.49f);
    }

    protected void resetPlaybackFilters()
    {
        if (lowpassFilter != IntPtr.Zero)
        {
            Biquad_reset(lowpassFilter);
        }

        if (dcBlocker != IntPtr.Zero)
        {
            Biquad_reset(dcBlocker);
        }
    }

    void freePlaybackFilters()
    {
        if (lowpassFilter != IntPtr.Zero)
        {
            Biquad_free(lowpassFilter);
            lowpassFilter = IntPtr.Zero;
        }

        if (dcBlocker != IntPtr.Zero)
        {
            Biquad_free(dcBlocker);
            dcBlocker = IntPtr.Zero;
        }
    }

    void clearLayerPointers()
    {
        ensureRuntimeState();
        for (int i = 0; i < sampleLayerPointers.Length; i++)
        {
            sampleLayerPointers[i] = IntPtr.Zero;
            sampleLayerFrameCounts[i] = 0;
        }
    }

    protected void ensureRuntimeState()
    {
        if (sampleBounds == null || sampleBounds.Length < 2)
        {
            sampleBounds = new int[] { 0, 0 };
        }

        if (sampleLayerPointers == null || sampleLayerPointers.Length != SampleLayerBank.MaxLayers)
        {
            sampleLayerPointers = new IntPtr[SampleLayerBank.MaxLayers];
        }

        if (sampleLayerFrameCounts == null || sampleLayerFrameCounts.Length != SampleLayerBank.MaxLayers)
        {
            sampleLayerFrameCounts = new int[SampleLayerBank.MaxLayers];
        }
    }
}
