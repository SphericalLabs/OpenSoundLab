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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class VideoPlayerDeviceInterface : componentInterface
{

    public string vidFilename;
    public Renderer videoSurface;
    public GameObject vidQuad;
    //AudioSource source;
    //MovieTexture movieTexture;
    VideoPlayer vidPlayer;
    VideoPlayerUI vidUI;

    bool loading = false;
    bool loaded = false;
    public bool playing = false;

    void Awake()
    {
        //source = GetComponent<AudioSource>();
        vidUI = GetComponentInChildren<VideoPlayerUI>();
        vidPlayer = GetComponentInChildren<VideoPlayer>();
    }

    tooltips _tooltip;
    public void Autoplay(string file, tooltips _t)
    {
        videoSurface.material.SetColor("_TintColor", new Color32(0x9A, 0x9A, 0x9A, 0x80));
        _tooltip = _t;
        vidFilename = file;
        togglePlay();
        vidUI.updateControlQuad();
        vidUI.controlQuad.SetActive(false);
        vidUI.controlRends[0].material.SetFloat("_EmissionGain", 0f);
    }

    void Update()
    {
        if (loaded && playing)
        {
            //if (!movieTexture.isPlaying) {
            if (!vidPlayer.isPlaying)
            {
                endPlayback();
            }
        }
    }

    void endPlayback()
    {
        playing = false;
        loading = false;
        loaded = false;
        //movieTexture.Stop();
        vidPlayer.Stop();
        vidQuad.SetActive(false);
        vidUI.Reset();
        masterControl.instance.toggleInstrumentVolume(true);
        if (_tooltip != null) _tooltip.ToggleVideo(false);
    }

    public void togglePlay()
    {
        playing = !playing;
        if (playing)
        {
            if (loaded)
            {
                vidQuad.SetActive(true);
                //movieTexture.Play();
                //source.Play();
                vidPlayer.Play();
                masterControl.instance.toggleInstrumentVolume(false);
            }
            else if (!loading) StartCoroutine(movieRoutine());
        }
        else if (loaded)
        {
            //movieTexture.Pause();
            vidPlayer.Pause();
            masterControl.instance.toggleInstrumentVolume(true);
        }
    }

    void OnDisable()
    {
        //   if (movieTexture != null) {
        endPlayback();
        //videoSurface.material.mainTexture = null;
        //      movieTexture = null;
        //    }
        loading = false;
        loaded = false;
        playing = false;
        masterControl.instance.toggleInstrumentVolume(true);
    }

    IEnumerator movieRoutine()
    {
        loading = true;
        //WWW www = new WWW("file:///" + Application.streamingAssetsPath + System.IO.Path.DirectorySeparatorChar + vidFilename);
        //movieTexture = www.movie;
        vidFilename = vidFilename.Replace(".ogv", ".mp4");
        vidPlayer.url = Application.streamingAssetsPath + "/" + vidFilename;
        vidPlayer.Prepare();

        //while (!movieTexture.isReadyToPlay) {
        while (!vidPlayer.isPrepared)
        {
            yield return new WaitForSeconds(.1f); ;
        }

        vidPlayer.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;
        vidPlayer.targetMaterialRenderer = vidQuad.GetComponent<Renderer>();
        vidPlayer.targetMaterialProperty = "_MainTex";
        //videoSurface.material.mainTexture = movieTexture;.GetComponent<Renderer>();
        //source.clip = movieTexture.audioClip;
        if (playing)
        {
            vidQuad.SetActive(true);

            //movieTexture.Play();
            //source.Play();
            vidPlayer.Play();
            masterControl.instance.toggleInstrumentVolume(false);
        }
        else
        {
            vidQuad.SetActive(false);
        }
        loading = false;
        loaded = true;
    }
}
