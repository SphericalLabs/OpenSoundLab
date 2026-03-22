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
using System.IO;
using UnityEngine.Events;

public class samplerLoad : MonoBehaviour
{
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
    public float wavetableSampleSeconds = 1f;
    public int wavetableAdditionalLayers = 4;
    public float extraShortSampleSeconds = 2.5f;
    public float shortSampleSeconds = 5f;
    public float mediumSampleSeconds = 20f;
    public int extraShortAdditionalLayers = 3;
    public int shortAdditionalLayers = 2;
    public int mediumAdditionalLayers = 1;
    public int longAdditionalLayers = 0;
    [NonSerialized] public float loadingProgress = 0f;
    [NonSerialized] public SamplerLoadStage loadingStage = SamplerLoadStage.Idle;

    SampleLayerBank sampleLayerBank;
    SamplerBackgroundLoadJob activeLoadJob;
    int activeLoadRequestId = 0;

    public UnityEvent onLoadTapeEvents;
    public UnityEvent onUnloadTapeEvents;

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
            cancelActiveLoad();

            if (miniSpeaker != null) miniSpeaker.updateSecondary(false);

            clearLoadedSampleState();

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

        cancelActiveLoad();
        clearLoadedSampleState();

        activeLoadRequestId++;
        int requestId = activeLoadRequestId;
        activeLoadJob = new SamplerBackgroundLoadJob(fullpath, generatedLayerCount, clipReadFramesPerChunk,
            layerBuildFramesPerChunk, wavetableSampleSeconds, extraShortSampleSeconds, shortSampleSeconds,
            mediumSampleSeconds, wavetableAdditionalLayers, extraShortAdditionalLayers, shortAdditionalLayers,
            mediumAdditionalLayers, longAdditionalLayers);
        activeLoadJob.Start();
        _streamRoutine = StartCoroutine(streamRoutine(activeLoadJob, requestId));
    }

    GameObject loaderObject;
    Coroutine _streamRoutine;
    IEnumerator streamRoutine(SamplerBackgroundLoadJob job, int requestId)
    {
        loaderObject = Instantiate(loadingPrefab, transform, false) as GameObject;
        loaderObject.transform.localPosition = new Vector3(-.05f, .013f, 0.061f);
        loaderObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        loaderObject.transform.localScale = Vector3.one * .1f;

        bool baseBankPublished = false;
        while (requestId == activeLoadRequestId && activeLoadJob == job)
        {
            loadingProgress = job.Progress;
            loadingStage = job.Stage;

            if (job.TryConsumeRaw(out float[] rawSamples, out int rawChannels, out int rawFrames, out int rawSampleRate))
            {
                clipSamples = rawSamples;
                applySampleBank(new float[][] { rawSamples }, rawChannels, false);
                baseBankPublished = true;
            }

            if (job.TryConsumeLayers(out float[][] layers, out int layerChannels))
            {
                clipSamples = layers[0];
                applySampleBank(layers, layerChannels, baseBankPublished);
            }

            if (job.Completed)
            {
                if (job.Failed && job.Error != null)
                {
                    Debug.LogException(job.Error, this);
                }
                break;
            }

            yield return null;
        }

        if (requestId == activeLoadRequestId)
        {
            activeLoadJob = null;
            loadingProgress = job.Progress;
            loadingStage = job.Stage;
        }

        if (loaderObject != null)
        {
            Destroy(loaderObject);
            loaderObject = null;
        }

        _streamRoutine = null;
    }

    void OnDestroy()
    {
        cancelActiveLoad();
        clearLoadedSampleState();
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

    void cancelActiveLoad()
    {
        activeLoadRequestId++;

        if (activeLoadJob != null)
        {
            activeLoadJob.Cancel();
            activeLoadJob = null;
        }

        if (_streamRoutine != null)
        {
            StopCoroutine(_streamRoutine);
            _streamRoutine = null;
        }

        if (loaderObject != null)
        {
            Destroy(loaderObject);
            loaderObject = null;
        }

        loadingProgress = 0f;
        loadingStage = SamplerLoadStage.Idle;
    }

    void clearLoadedSampleState()
    {
        for (int i = 0; i < players.Length; i++) players[i].UnloadClip();

        if (sampleLayerBank != null)
        {
            sampleLayerBank.Release();
            sampleLayerBank = null;
        }

        clipSamples = null;
    }

    void applySampleBank(float[][] layers, int channels, bool preservePlaybackState)
    {
        if (layers == null || layers.Length == 0 || layers[0] == null || layers[0].Length == 0)
        {
            return;
        }

        SampleLayerBank oldBank = sampleLayerBank;
        sampleLayerBank = new SampleLayerBank(layers, channels);
        for (int i = 0; i < players.Length; i++)
        {
            players[i].LoadSamples(sampleLayerBank, preservePlaybackState);
        }

        if (oldBank != null && oldBank != sampleLayerBank)
        {
            oldBank.Release();
        }
    }

}
