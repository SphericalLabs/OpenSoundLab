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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class VideoPlayerDeviceInterface : componentInterface {

    public string vidFilename;
    public Renderer videoSurface;
    public GameObject vidQuad;
    public GameObject controlQuad;
    AudioSource source;
    //UnityEngine.Video.VideoPlayer movieTexture;
    //VideoPlayerUI vidUI;
    VideoPlayer vidUI;
    //   public VideoClip videoClip;

    bool loading = false;
    bool loaded = false;
    public bool playing = false;

    void Awake() {
        //source = GetComponent<AudioSource>();
        //vidUI = GetComponentInChildren<VideoPlayerUI>();

        vidUI = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        vidUI.url = Application.streamingAssetsPath + "/" + vidFilename;

        vidUI.playOnAwake = false;
        vidUI.isLooping = false;
        vidUI.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;
        vidUI.targetMaterialRenderer = vidQuad.GetComponent<Renderer>();
        vidUI.targetMaterialProperty = "_MainTex";
    }

    tooltips _tooltip;
    public void Autoplay(string file, tooltips _t) {
        videoSurface.material.SetColor("_TintColor", new Color32(0x9A, 0x9A, 0x9A, 0x80));
        _tooltip = _t;
        vidFilename = file;
        togglePlay();
        //controlQuad.GetComponent.updateControlQuad();
        //controlQuad.SetActive(false);
        //vidUI.controlRends[0].material.SetFloat("_EmissionGain", 0f);
    }

    void Update() {
        if (loaded && playing) {
            //if (!movieTexture.isPlaying) {
            if (!vidUI.isPlaying) {
                endPlayback();
            }
        }
    }

    void endPlayback() {
        playing = false;
        vidUI.Stop();
        vidQuad.SetActive(false);
        //vidUI.Reset();
        vidUI.Stop();
        masterControl.instance.toggleInstrumentVolume(true);
        if (_tooltip != null) _tooltip.ToggleVideo(false);
    }

    public void togglePlay() {
        playing = !playing;
        if (playing) {
            if (loaded) {
                vidQuad.SetActive(true);
                vidUI.Play();
                //source.Play();
                masterControl.instance.toggleInstrumentVolume(false);
            } else if (!loading) StartCoroutine(movieRoutine());
        } else if (loaded) {
            vidUI.Pause();
            masterControl.instance.toggleInstrumentVolume(true);
        }
    }

    void OnDisable() {
        //   if (movieTexture != null) {
        endPlayback();
        //      videoSurface.material.mainTexture = null;
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
        vidUI.url = Application.streamingAssetsPath + "/" + vidFilename;
        vidUI.Prepare();
        //movieTexture = www.movie;
        //while (!movieTexture.isReadyToPlay)
        while (!vidUI.isPrepared)
        {
                yield return null;
        }
        loading = false;
        loaded = true;

        vidUI.targetMaterialRenderer = vidQuad.GetComponent<Renderer>();
        vidUI.targetMaterialProperty = "_MainTex";
        //videoSurface.material.mainTexture = movieTexture;.GetComponent<Renderer>();
        //source.clip = movieTexture.audioClip;
        if (playing)
        {
            vidQuad.SetActive(true);

            masterControl.instance.toggleInstrumentVolume(false);
            vidUI.Play();
            //source.Play();
        }
        else
        {
            vidQuad.SetActive(false);
        }
        loading = false;
        loaded = true;
}
}

