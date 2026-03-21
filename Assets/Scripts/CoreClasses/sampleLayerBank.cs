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
using System.Runtime.InteropServices;
using UnityEngine;

public enum SamplerInterpolationMode
{
    None = 1,
    Linear = 2,
    WindowedSinc23 = 3,
    Hermite4 = 4,
    Lagrange4 = 5
}

public class SampleLayerBank
{
    public const int MaxLayers = 5;
    public const int MaxAdditionalLayers = MaxLayers - 1;

    public readonly float[][] layers;
    public readonly GCHandle[] handles;
    public readonly IntPtr[] pointers;
    public readonly int[] frameCounts;
    public readonly int channels;

    public int Count
    {
        get
        {
            return layers == null ? 0 : layers.Length;
        }
    }

    public SampleLayerBank(float[][] sourceLayers, int sourceChannels)
    {
        channels = Mathf.Max(1, sourceChannels);
        layers = sourceLayers ?? Array.Empty<float[]>();
        handles = new GCHandle[layers.Length];
        pointers = new IntPtr[layers.Length];
        frameCounts = new int[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            float[] layer = layers[i];
            if (layer == null || layer.Length == 0)
            {
                continue;
            }

            handles[i] = GCHandle.Alloc(layer, GCHandleType.Pinned);
            pointers[i] = handles[i].AddrOfPinnedObject();
            frameCounts[i] = layer.Length / channels;
        }
    }

    public void Release()
    {
        for (int i = 0; i < handles.Length; i++)
        {
            if (handles[i].IsAllocated)
            {
                handles[i].Free();
            }
        }
    }
}

public static class SampleInterpolationUtility
{
    public const int ZeroCrossingsPerAxis = 11;
    const float LayerSwitchThreshold = 1.25f;
    const float CutoffNormalized = 0.85f;
    const float WindowA0 = 0.35875f;
    const float WindowA1 = 0.48829f;
    const float WindowA2 = 0.14128f;
    const float WindowA3 = 0.01168f;

    public static int SelectLayerIndex(float playbackStep, int layerCount, bool useSampleLayers)
    {
        if (!useSampleLayers || layerCount <= 1)
        {
            return 0;
        }

        playbackStep = Mathf.Abs(playbackStep);
        int layer = 0;
        while (layer + 1 < layerCount && playbackStep > LayerSwitchThreshold)
        {
            playbackStep *= 0.5f;
            layer++;
        }
        return layer;
    }

    public static float ReadSample(SampleLayerBank bank, double framePosition, int channel, float playbackStep,
        SamplerInterpolationMode interpolationMode, bool useSampleLayers)
    {
        if (bank == null || bank.Count == 0)
        {
            return 0f;
        }

        int layerIndex = SelectLayerIndex(playbackStep, bank.Count, useSampleLayers);
        while (layerIndex > 0 && (bank.layers[layerIndex] == null || bank.frameCounts[layerIndex] <= 0))
        {
            layerIndex--;
        }

        float[] layer = bank.layers[layerIndex];
        if (layer == null || layer.Length == 0)
        {
            return 0f;
        }

        double scaledPosition = framePosition / (1 << layerIndex);
        return ReadLayerSample(layer, bank.frameCounts[layerIndex], bank.channels, scaledPosition, channel, interpolationMode);
    }

    static float ReadLayerSample(float[] layer, int frameCount, int channels, double framePosition, int channel,
        SamplerInterpolationMode interpolationMode)
    {
        if (frameCount <= 0)
        {
            return 0f;
        }

        channel = Mathf.Clamp(channel, 0, Mathf.Max(0, channels - 1));

        switch (interpolationMode)
        {
            case SamplerInterpolationMode.None:
                return ReadNearest(layer, frameCount, channels, framePosition, channel);
            case SamplerInterpolationMode.Linear:
                return ReadLinear(layer, frameCount, channels, framePosition, channel);
            case SamplerInterpolationMode.WindowedSinc23:
                return ReadWindowedSinc(layer, frameCount, channels, framePosition, channel);
            case SamplerInterpolationMode.Lagrange4:
                return ReadLagrange(layer, frameCount, channels, framePosition, channel);
            default:
                return ReadHermite(layer, frameCount, channels, framePosition, channel);
        }
    }

    static float ReadNearest(float[] layer, int frameCount, int channels, double framePosition, int channel)
    {
        int frame = Mathf.Clamp(Mathf.RoundToInt((float)framePosition), 0, frameCount - 1);
        return ReadFrame(layer, frameCount, channels, frame, channel);
    }

    static float ReadLinear(float[] layer, int frameCount, int channels, double framePosition, int channel)
    {
        int baseFrame = Mathf.FloorToInt((float)framePosition);
        float frac = Mathf.Clamp01((float)(framePosition - baseFrame));
        float a = ReadFrame(layer, frameCount, channels, baseFrame, channel);
        float b = ReadFrame(layer, frameCount, channels, baseFrame + 1, channel);
        return a + frac * (b - a);
    }

    static float ReadHermite(float[] layer, int frameCount, int channels, double framePosition, int channel)
    {
        int x1 = Mathf.FloorToInt((float)framePosition);
        float frac = (float)(framePosition - x1);
        float y0 = ReadFrame(layer, frameCount, channels, x1 - 1, channel);
        float y1 = ReadFrame(layer, frameCount, channels, x1, channel);
        float y2 = ReadFrame(layer, frameCount, channels, x1 + 1, channel);
        float y3 = ReadFrame(layer, frameCount, channels, x1 + 2, channel);

        float c0 = y1;
        float c1 = 0.5f * (y2 - y0);
        float c2 = y0 - 2.5f * y1 + 2f * y2 - 0.5f * y3;
        float c3 = 0.5f * (y3 - y0) + 1.5f * (y1 - y2);
        return ((c3 * frac + c2) * frac + c1) * frac + c0;
    }

    static float ReadLagrange(float[] layer, int frameCount, int channels, double framePosition, int channel)
    {
        int x1 = Mathf.FloorToInt((float)framePosition);
        float frac = (float)(framePosition - x1);
        float y0 = ReadFrame(layer, frameCount, channels, x1 - 1, channel);
        float y1 = ReadFrame(layer, frameCount, channels, x1, channel);
        float y2 = ReadFrame(layer, frameCount, channels, x1 + 1, channel);
        float y3 = ReadFrame(layer, frameCount, channels, x1 + 2, channel);

        float w0 = -frac * (frac - 1f) * (frac - 2f) / 6f;
        float w1 = (frac + 1f) * (frac - 1f) * (frac - 2f) * 0.5f;
        float w2 = -(frac + 1f) * frac * (frac - 2f) * 0.5f;
        float w3 = (frac + 1f) * frac * (frac - 1f) / 6f;
        return y0 * w0 + y1 * w1 + y2 * w2 + y3 * w3;
    }

    static float ReadWindowedSinc(float[] layer, int frameCount, int channels, double framePosition, int channel)
    {
        int center = Mathf.RoundToInt((float)framePosition);
        float frac = (float)(framePosition - center);
        float sum = 0f;

        for (int tap = -ZeroCrossingsPerAxis; tap <= ZeroCrossingsPerAxis; tap++)
        {
            int frame = center + tap;
            float sample = ReadFrame(layer, frameCount, channels, frame, channel);
            sum += sample * WindowedSincWeight(tap - frac);
        }

        return sum;
    }

    static float WindowedSincWeight(float distance)
    {
        float absDistance = Mathf.Abs(distance);
        if (absDistance > ZeroCrossingsPerAxis)
        {
            return 0f;
        }

        float sinc;
        if (absDistance < 1e-6f)
        {
            sinc = CutoffNormalized;
        }
        else
        {
            float x = CutoffNormalized * Mathf.PI * distance;
            sinc = Mathf.Sin(x) / x * CutoffNormalized;
        }

        float phase = (distance + ZeroCrossingsPerAxis) / (2f * ZeroCrossingsPerAxis);
        float window = WindowA0
            - WindowA1 * Mathf.Cos(2f * Mathf.PI * phase)
            + WindowA2 * Mathf.Cos(4f * Mathf.PI * phase)
            - WindowA3 * Mathf.Cos(6f * Mathf.PI * phase);
        return sinc * window;
    }

    static float ReadFrame(float[] layer, int frameCount, int channels, int frame, int channel)
    {
        frame = Mathf.Clamp(frame, 0, frameCount - 1);
        return layer[frame * channels + channel];
    }
}
