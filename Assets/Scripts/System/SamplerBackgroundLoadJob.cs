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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using RuntimeAudioClipLoader;
using RuntimeAudioClipLoader.Internal;
using UnityEngine;

public enum SamplerLoadStage
{
    Idle,
    Decoding,
    BaseReady,
    PreparingLayers,
    Completed,
    Cancelled,
    Failed
}

public class SamplerBackgroundLoadJob
{
    const int BIQUAD_LOWPASS = 1;

    readonly object stateLock = new object();

    readonly string fullpath;
    readonly int maxAdditionalLayers;
    readonly int decodeFramesPerChunk;
    readonly int layerFramesPerChunk;
    readonly float wavetableSeconds;
    readonly float veryShortSeconds;
    readonly float shortSeconds;
    readonly float mediumSeconds;
    readonly int wavetableLayers;
    readonly int veryShortLayers;
    readonly int shortLayers;
    readonly int mediumLayers;
    readonly int longLayers;

    Thread workerThread;
    bool cancelRequested;
    bool completed;
    bool failed;
    bool rawReady;
    bool rawConsumed;
    bool layersReady;
    bool layersConsumed;
    float progress;
    SamplerLoadStage stage = SamplerLoadStage.Idle;
    Exception error;

    float[] rawSamples;
    int rawChannels;
    int rawFrames;
    int rawSampleRate;
    float[][] preparedLayers;

    public SamplerBackgroundLoadJob(string sourcePath, int requestedAdditionalLayers, int decodeChunkFrames,
        int layerChunkFrames, float wavetableThresholdSeconds, float extraShortThresholdSeconds,
        float shortThresholdSeconds, float mediumThresholdSeconds, int wavetableAdditionalLayers,
        int extraShortAdditionalLayers, int shortAdditionalLayers, int mediumAdditionalLayers,
        int longAdditionalLayers)
    {
        fullpath = sourcePath;
        maxAdditionalLayers = Mathf.Clamp(requestedAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
        decodeFramesPerChunk = Mathf.Max(1024, decodeChunkFrames);
        layerFramesPerChunk = Mathf.Max(1024, layerChunkFrames);
        wavetableSeconds = Mathf.Max(0.05f, wavetableThresholdSeconds);
        veryShortSeconds = Mathf.Max(wavetableSeconds, extraShortThresholdSeconds);
        shortSeconds = Mathf.Max(veryShortSeconds, shortThresholdSeconds);
        mediumSeconds = Mathf.Max(shortSeconds, mediumThresholdSeconds);
        wavetableLayers = Mathf.Clamp(wavetableAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
        veryShortLayers = Mathf.Clamp(extraShortAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
        shortLayers = Mathf.Clamp(shortAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
        mediumLayers = Mathf.Clamp(mediumAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
        longLayers = Mathf.Clamp(longAdditionalLayers, 0, SampleLayerBank.MaxAdditionalLayers);
    }

    [DllImport("OSLNative")]
    static extern IntPtr Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels);

    [DllImport("OSLNative")]
    static extern void Biquad_free(IntPtr x);

    [DllImport("OSLNative")]
    static extern void Biquad_process(IntPtr x, int type, float frequency, float Q, float gain, float sampleRate,
        float[] input, float[] output, int n);

    public float Progress
    {
        get
        {
            lock (stateLock)
            {
                return progress;
            }
        }
    }

    public SamplerLoadStage Stage
    {
        get
        {
            lock (stateLock)
            {
                return stage;
            }
        }
    }

    public bool Completed
    {
        get
        {
            lock (stateLock)
            {
                return completed;
            }
        }
    }

    public bool Failed
    {
        get
        {
            lock (stateLock)
            {
                return failed;
            }
        }
    }

    public Exception Error
    {
        get
        {
            lock (stateLock)
            {
                return error;
            }
        }
    }

    public void Start()
    {
        workerThread = new Thread(runLoad);
        workerThread.IsBackground = true;
        workerThread.Name = "SamplerBackgroundLoadJob";
        workerThread.Start();
    }

    public void Cancel()
    {
        lock (stateLock)
        {
            cancelRequested = true;
        }
    }

    public bool TryConsumeRaw(out float[] samples, out int channels, out int frames, out int sampleRate)
    {
        lock (stateLock)
        {
            if (!rawReady || rawConsumed || rawSamples == null)
            {
                samples = null;
                channels = 0;
                frames = 0;
                sampleRate = 0;
                return false;
            }

            rawConsumed = true;
            samples = rawSamples;
            channels = rawChannels;
            frames = rawFrames;
            sampleRate = rawSampleRate;
            return true;
        }
    }

    public bool TryConsumeLayers(out float[][] layers, out int channels)
    {
        lock (stateLock)
        {
            if (!layersReady || layersConsumed || preparedLayers == null)
            {
                layers = null;
                channels = 0;
                return false;
            }

            layersConsumed = true;
            layers = preparedLayers;
            channels = rawChannels;
            return true;
        }
    }

    void runLoad()
    {
        try
        {
            updateStage(SamplerLoadStage.Decoding);

            using (FileStream dataStream = File.OpenRead(fullpath))
            using (AudioDataReader reader = new AudioDataReader(dataStream,
                       AudioLoaderConfig.GetAudioFormat(fullpath), Manager.PreferredDecoder))
            {
                int channels = Math.Max(1, reader.WaveFormat.Channels);
                int samplesCount = (int)(reader.Length / Math.Max(1, reader.WaveFormat.BitsPerSample / 8));
                int frames = Math.Max(1, samplesCount / channels);
                int sampleRate = Math.Max(1, reader.WaveFormat.SampleRate);
                float durationSeconds = frames / (float)sampleRate;
                int plannedLayers = selectAdditionalLayerCount(durationSeconds);
                long totalWorkSamples = estimateTotalWorkSamples(samplesCount, channels, frames, plannedLayers);
                long completedSamples = 0;

                float[] samples = new float[samplesCount];
                int samplesPerChunk = Math.Max(channels * 512, decodeFramesPerChunk * channels);
                for (int offset = 0; offset < samplesCount; offset += samplesPerChunk)
                {
                    if (isCancelled())
                    {
                        markCancelled();
                        return;
                    }

                    int samplesThisChunk = Math.Min(samplesPerChunk, samplesCount - offset);
                    int samplesRead = reader.Read(samples, offset, samplesThisChunk);
                    if (samplesRead < samplesThisChunk)
                    {
                        Array.Clear(samples, offset + samplesRead, samplesThisChunk - samplesRead);
                    }

                    completedSamples += samplesThisChunk;
                    updateProgress(completedSamples, totalWorkSamples);
                }

                publishRaw(samples, channels, frames, sampleRate);

                if (plannedLayers <= 0)
                {
                    lock (stateLock)
                    {
                        progress = 1f;
                        stage = SamplerLoadStage.Completed;
                        completed = true;
                    }
                    return;
                }

                updateStage(SamplerLoadStage.PreparingLayers);
                float[][] layers = new float[plannedLayers + 1][];
                layers[0] = samples;
                float[] currentLayer = samples;
                int currentFrames = frames;
                int currentSampleRate = sampleRate;

                for (int layerIndex = 0; layerIndex < plannedLayers; layerIndex++)
                {
                    if (isCancelled())
                    {
                        markCancelled();
                        return;
                    }

                    float[] nextLayer = buildDownsampledLayer(currentLayer, currentFrames, channels, currentSampleRate,
                        ref completedSamples, totalWorkSamples);
                    if (isCancelled())
                    {
                        markCancelled();
                        return;
                    }
                    if (nextLayer == null || nextLayer.Length == 0)
                    {
                        Array.Resize(ref layers, layerIndex + 1);
                        break;
                    }

                    layers[layerIndex + 1] = nextLayer;
                    currentLayer = nextLayer;
                    currentFrames = Math.Max(1, nextLayer.Length / channels);
                    currentSampleRate = Math.Max(1, currentSampleRate / 2);
                }

                publishLayers(layers);
                lock (stateLock)
                {
                    progress = 1f;
                    stage = SamplerLoadStage.Completed;
                    completed = true;
                }
            }
        }
        catch (Exception ex)
        {
            lock (stateLock)
            {
                failed = true;
                error = ex;
                stage = SamplerLoadStage.Failed;
                completed = true;
            }
        }
    }

    float[] buildDownsampledLayer(float[] source, int sourceFrames, int channels, int sourceSampleRate,
        ref long completedSamples, long totalWorkSamples)
    {
        if (source == null || source.Length == 0 || sourceFrames < 32)
        {
            return null;
        }

        int destinationFrames = Math.Max(1, (sourceFrames + 1) / 2);
        float[] destination = new float[destinationFrames * channels];
        float[] filteredChunk = new float[Math.Min(source.Length, Math.Max(channels * 512, layerFramesPerChunk * channels))];
        IntPtr lowpass = Biquad_new(BIQUAD_LOWPASS, Math.Max(80f, sourceSampleRate * 0.225f), 0.7071f, 0f,
            sourceSampleRate, channels);

        int writeFrame = 0;
        try
        {
            for (int offsetFrame = 0; offsetFrame < sourceFrames; offsetFrame += layerFramesPerChunk)
            {
                if (isCancelled())
                {
                    return null;
                }

                int framesThisChunk = Math.Min(layerFramesPerChunk, sourceFrames - offsetFrame);
                int samplesThisChunk = framesThisChunk * channels;
                if (filteredChunk.Length != samplesThisChunk)
                {
                    filteredChunk = new float[samplesThisChunk];
                }

                Array.Copy(source, offsetFrame * channels, filteredChunk, 0, samplesThisChunk);
                Biquad_process(lowpass, BIQUAD_LOWPASS, Math.Max(80f, sourceSampleRate * 0.225f), 0.7071f, 0f,
                    sourceSampleRate, filteredChunk, filteredChunk, samplesThisChunk);

                int parity = offsetFrame & 1;
                for (int frame = parity; frame < framesThisChunk && writeFrame < destinationFrames; frame += 2)
                {
                    int readIndex = frame * channels;
                    int writeIndex = writeFrame * channels;
                    for (int channel = 0; channel < channels; channel++)
                    {
                        destination[writeIndex + channel] = filteredChunk[readIndex + channel];
                    }
                    writeFrame++;
                }

                completedSamples += samplesThisChunk;
                updateProgress(completedSamples, totalWorkSamples);
            }
        }
        finally
        {
            if (lowpass != IntPtr.Zero)
            {
                Biquad_free(lowpass);
            }
        }

        if (writeFrame * channels != destination.Length)
        {
            Array.Resize(ref destination, writeFrame * channels);
        }

        return destination;
    }

    int selectAdditionalLayerCount(float durationSeconds)
    {
        int policyLayers;
        if (durationSeconds < wavetableSeconds)
        {
            policyLayers = wavetableLayers;
        }
        else if (durationSeconds < veryShortSeconds)
        {
            policyLayers = veryShortLayers;
        }
        else if (durationSeconds < shortSeconds)
        {
            policyLayers = shortLayers;
        }
        else if (durationSeconds < mediumSeconds)
        {
            policyLayers = mediumLayers;
        }
        else
        {
            policyLayers = longLayers;
        }

        return Math.Min(policyLayers, maxAdditionalLayers);
    }

    long estimateTotalWorkSamples(int sourceSamples, int channels, int sourceFrames, int additionalLayers)
    {
        long total = sourceSamples;
        int frames = sourceFrames;
        for (int layerIndex = 0; layerIndex < additionalLayers; layerIndex++)
        {
            total += (long)frames * channels;
            frames = Math.Max(1, (frames + 1) / 2);
        }
        return Math.Max(1L, total);
    }

    bool isCancelled()
    {
        lock (stateLock)
        {
            return cancelRequested;
        }
    }

    void markCancelled()
    {
        lock (stateLock)
        {
            stage = SamplerLoadStage.Cancelled;
            completed = true;
        }
    }

    void publishRaw(float[] samples, int channels, int frames, int sampleRate)
    {
        lock (stateLock)
        {
            rawSamples = samples;
            rawChannels = channels;
            rawFrames = frames;
            rawSampleRate = sampleRate;
            rawReady = true;
            stage = SamplerLoadStage.BaseReady;
        }
    }

    void publishLayers(float[][] layers)
    {
        lock (stateLock)
        {
            preparedLayers = layers;
            layersReady = true;
        }
    }

    void updateStage(SamplerLoadStage nextStage)
    {
        lock (stateLock)
        {
            stage = nextStage;
        }
    }

    void updateProgress(long completedSamples, long totalWorkSamples)
    {
        lock (stateLock)
        {
            progress = Mathf.Clamp01((float)completedSamples / Math.Max(1L, totalWorkSamples));
        }
    }
}
