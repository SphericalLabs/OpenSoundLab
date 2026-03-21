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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Events;

public class samplerLoad : MonoBehaviour
{
    const int BIQUAD_LOWPASS = 1;

    public GameObject tapePrefab;
    public GameObject loadingPrefab;
    public Transform deckOutline;
    public Transform deck;
    public clipPlayer[] players;
    public string CurTapeLabel;
    public string CurFile;

    public embeddedSpeaker miniSpeaker;

    Material deckMat;
    Color deckLight;

    [NonSerialized] public float[] clipSamples;
    public int generatedLayerCount = 3;
    public int clipReadFramesPerChunk = 16384;
    public int layerBuildFramesPerChunk = 8192;

    SampleLayerBank sampleLayerBank;

    public UnityEvent onLoadTapeEvents;
    public UnityEvent onUnloadTapeEvents;

    [DllImport("OSLNative")]
    static extern IntPtr Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels);

    [DllImport("OSLNative")]
    static extern void Biquad_free(IntPtr x);

    [DllImport("OSLNative")]
    static extern void Biquad_process(IntPtr x, int type, float frequency, float Q, float gain, float sampleRate,
        float[] input, float[] output, int n);

    void Awake()
    {
        if (players.Length == 0) players = GetComponents<clipPlayer>();
        deckMat = deckOutline.GetComponent<Renderer>().material;

        deckMat.SetFloat("_EmissionGain", .4f);
        deckLight = Color.white;
        deckMat.SetColor("_TintColor", Color.black);

        deckOutline.gameObject.SetActive(false);

        if (miniSpeaker != null) miniSpeaker.updateSecondary(false);
    }

    void Start()
    {
        deck.gameObject.layer = 14;
    }

    public bool hasTape()
    {
        return (currentTape != null);
    }

    tape currentTape;
    public void LoadTape(tape t, bool triggerEvent = true)
    {
        if (currentTape != null && currentTape != t)
        {
            ForceEject();
        }
        currentTape = t;
        CurTapeLabel = t.label;
        CurFile = t.filename;
        if (miniSpeaker != null) miniSpeaker.updateSecondary(true);
        LoadClip(t.filename);
        if (triggerEvent)
        {
            onLoadTapeEvents.Invoke();
        }
    }

    public void getTapeInfo(out string label, out string file)
    {
        if (currentTape != null)
        {
            label = CurTapeLabel;
            file = CurFile;
        }
        else
        {
            label = file = "";
        }
    }

    public void ForceEject(bool updateEvent = true)
    {
        if (currentTape != null)
        {
            tape temp = currentTape;
            UnloadTape(currentTape, updateEvent);
            temp.Eject();
        }
    }

    public void UnloadTape(tape t, bool updateEvent = true)
    {

        if (currentTape == t)
        {
            currentTape = null;
            if (_streamRoutine != null)
            {
                if (loaderObject != null) Destroy(loaderObject);
                StopCoroutine(_streamRoutine);
            }

            if (miniSpeaker != null) miniSpeaker.updateSecondary(false);

            for (int i = 0; i < players.Length; i++) players[i].UnloadClip();
            if (sampleLayerBank != null)
            {
                sampleLayerBank.Release();
                sampleLayerBank = null;
            }
            clipSamples = null;

            if (updateEvent)
            {
                onUnloadTapeEvents.Invoke();
            }
        }
    }

    public void LoadClip(string path)
    {

        string fullpath = sampleManager.instance.parseFilename(path);

        if (!File.Exists(fullpath))
        {
            return;
        }

        if (_streamRoutine != null)
        {
            if (loaderObject != null) Destroy(loaderObject);
            StopCoroutine(_streamRoutine);
        }

        _streamRoutine = StartCoroutine(streamRoutine(fullpath));
    }

    GameObject loaderObject;
    Coroutine _streamRoutine;
    IEnumerator streamRoutine(string fullpath)
    {
        AudioClip c = RuntimeAudioClipLoader.Manager.Load(fullpath, false, true, true);

        loaderObject = Instantiate(loadingPrefab, transform, false) as GameObject;
        loaderObject.transform.localPosition = new Vector3(-.05f, .013f, 0.061f);
        loaderObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        loaderObject.transform.localScale = Vector3.one * .1f;

        while (RuntimeAudioClipLoader.Manager.GetAudioClipLoadState(c) != AudioDataLoadState.Loaded)
        {
            yield return null;
        }

        for (int i = 0; i < players.Length; i++) players[i].UnloadClip();
        if (sampleLayerBank != null)
        {
            sampleLayerBank.Release();
            sampleLayerBank = null;
        }
        clipSamples = null;

        while (c.loadState != AudioDataLoadState.Loaded) yield return null;

        int channels = Mathf.Max(1, c.channels);
        int frames = Mathf.Max(1, c.samples);
        clipSamples = new float[frames * channels];
        yield return copyClipDataIncrementally(c, clipSamples, frames, channels);

        int requestedAdditionalLayers = Mathf.Clamp(generatedLayerCount, 0, SampleLayerBank.MaxAdditionalLayers);
        sampleLayerBank = null;
        yield return buildLayerBankIncrementally(clipSamples, channels, frames, c.frequency, requestedAdditionalLayers);

        if (loaderObject != null) Destroy(loaderObject);

        if (sampleLayerBank != null)
        {
            for (int i = 0; i < players.Length; i++) players[i].LoadSamples(sampleLayerBank);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < players.Length; i++) players[i].UnloadClip();
        if (sampleLayerBank != null)
        {
            sampleLayerBank.Release();
            sampleLayerBank = null;
        }
    }

    public string[] queuedSample = new string[] { "", "" };
    void OnEnable()
    {
        if (queuedSample[0] != "")
        {
            SetSample(queuedSample[0], queuedSample[1]);
            queuedSample[0] = "";
            queuedSample[1] = "";
        }

    }

    public void QueueSample(string s, string f)
    {
        if (gameObject.activeInHierarchy) SetSample(s, f);
        else
        {
            queuedSample[0] = s;
            queuedSample[1] = f;
        }
    }

    public void SetSample(string s, string f)
    {
        if (s == "") return;

        if (!File.Exists(sampleManager.instance.parseFilename(f)))
        {
            Debug.Log("File does't exist");
            return;
        }

        GameObject g = Instantiate(tapePrefab, Vector3.zero, Quaternion.identity) as GameObject;
        g.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        tape t = g.GetComponent<tape>();
        t.Setup(s, f);
        t.ForceLoad(deck);
    }

    bool flashing = false;
    public void flashDecklight(bool on)
    {
        if (flashing == on) return;

        flashing = on;
        if (flashing)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(flashRoutine());
        }
    }

    Coroutine _flashRoutine;

    IEnumerator copyClipDataIncrementally(AudioClip clip, float[] destination, int totalFrames, int channels)
    {
        int framesPerChunk = Mathf.Max(1024, clipReadFramesPerChunk);
        float[] chunk = new float[Mathf.Min(framesPerChunk, totalFrames) * channels];

        for (int offsetFrame = 0; offsetFrame < totalFrames; offsetFrame += framesPerChunk)
        {
            int framesThisChunk = Mathf.Min(framesPerChunk, totalFrames - offsetFrame);
            int samplesThisChunk = framesThisChunk * channels;
            if (chunk.Length != samplesThisChunk)
            {
                chunk = new float[samplesThisChunk];
            }

            clip.GetData(chunk, offsetFrame);
            System.Array.Copy(chunk, 0, destination, offsetFrame * channels, samplesThisChunk);
            yield return null;
        }
    }

    IEnumerator buildLayerBankIncrementally(float[] source, int channels, int sourceFrames, int sourceSampleRate,
        int additionalLayers)
    {
        List<float[]> layers = new List<float[]> { source };
        float[] currentLayer = source;
        int currentFrames = sourceFrames;
        int currentSampleRate = Mathf.Max(1, sourceSampleRate);

        for (int layerIndex = 0; layerIndex < additionalLayers; layerIndex++)
        {
            if (currentFrames < 32)
            {
                break;
            }

            float[] nextLayer = null;
            yield return buildDownsampledLayer(currentLayer, currentFrames, channels, currentSampleRate,
                value => nextLayer = value);
            if (nextLayer == null || nextLayer.Length == 0)
            {
                break;
            }

            layers.Add(nextLayer);
            currentLayer = nextLayer;
            currentFrames = Mathf.Max(1, nextLayer.Length / channels);
            currentSampleRate = Mathf.Max(1, currentSampleRate / 2);
        }

        sampleLayerBank = new SampleLayerBank(layers.ToArray(), channels);
    }

    IEnumerator buildDownsampledLayer(float[] source, int sourceFrames, int channels, int sourceSampleRate,
        System.Action<float[]> onComplete)
    {
        int framesPerChunk = Mathf.Max(1024, layerBuildFramesPerChunk);
        int destinationFrames = Mathf.Max(1, (sourceFrames + 1) / 2);
        float[] destination = new float[destinationFrames * channels];
        float[] filteredChunk = new float[Mathf.Min(framesPerChunk, sourceFrames) * channels];
        IntPtr lowpass = Biquad_new(BIQUAD_LOWPASS, Mathf.Max(80f, sourceSampleRate * 0.225f), 0.7071f, 0f,
            sourceSampleRate, channels);

        int writeFrame = 0;
        try
        {
            for (int offsetFrame = 0; offsetFrame < sourceFrames; offsetFrame += framesPerChunk)
            {
                int framesThisChunk = Mathf.Min(framesPerChunk, sourceFrames - offsetFrame);
                int samplesThisChunk = framesThisChunk * channels;
                if (filteredChunk.Length != samplesThisChunk)
                {
                    filteredChunk = new float[samplesThisChunk];
                }

                System.Array.Copy(source, offsetFrame * channels, filteredChunk, 0, samplesThisChunk);
                Biquad_process(lowpass, BIQUAD_LOWPASS, Mathf.Max(80f, sourceSampleRate * 0.225f), 0.7071f, 0f,
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

                yield return null;
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
            System.Array.Resize(ref destination, writeFrame * channels);
        }

        onComplete?.Invoke(destination);
    }

    IEnumerator flashRoutine()
    {
        deckOutline.gameObject.SetActive(true);
        while (flashing)
        {
            // Flash routine disabled until linked to modular phase.
            yield return null;
        }
        deckOutline.gameObject.SetActive(false);
    }


}
