// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class VideoPlayerDeviceInterface : componentInterface {

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

    void Awake() {
        //source = GetComponent<AudioSource>();
        vidUI = GetComponentInChildren<VideoPlayerUI>();
        vidPlayer = GetComponentInChildren<VideoPlayer>();
    }

    tooltips _tooltip;
    public void Autoplay(string file, tooltips _t) {
        videoSurface.material.SetColor("_TintColor", new Color32(0x9A, 0x9A, 0x9A, 0x80));
        _tooltip = _t;
        vidFilename = file;
        togglePlay();
        vidUI.updateControlQuad();
        vidUI.controlQuad.SetActive(false);
        vidUI.controlRends[0].material.SetFloat("_EmissionGain", 0f);
    }

    void Update() {
        if (loaded && playing) {
            //if (!movieTexture.isPlaying) {
            if (!vidPlayer.isPlaying) {
                endPlayback();
            }
        }
    }

    void endPlayback() {
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

    public void togglePlay() {
        playing = !playing;
        if (playing) {
            if (loaded) {
                vidQuad.SetActive(true);
                //movieTexture.Play();
                //source.Play();
                vidPlayer.Play();
                masterControl.instance.toggleInstrumentVolume(false);
            } else if (!loading) StartCoroutine(movieRoutine());
        } else if (loaded) {
            //movieTexture.Pause();
            vidPlayer.Pause();
            masterControl.instance.toggleInstrumentVolume(true);
        }
    }

    void OnDisable() {
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
    
    IEnumerator movieRoutine() {
        loading = true;
        //WWW www = new WWW("file:///" + Application.streamingAssetsPath + System.IO.Path.DirectorySeparatorChar + vidFilename);
        //movieTexture = www.movie;
        vidFilename = vidFilename.Replace(".ogv", ".mp4");
        vidPlayer.url = Application.streamingAssetsPath + "/" + vidFilename;
        vidPlayer.Prepare();

        //while (!movieTexture.isReadyToPlay) {
        while (!vidPlayer.isPrepared) {
                yield return new WaitForSeconds(.1f); ;
        }

        vidPlayer.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;
        vidPlayer.targetMaterialRenderer = vidQuad.GetComponent<Renderer>();
        vidPlayer.targetMaterialProperty = "_MainTex";
        //videoSurface.material.mainTexture = movieTexture;.GetComponent<Renderer>();
        //source.clip = movieTexture.audioClip;
        if (playing) {
            vidQuad.SetActive(true);

            //movieTexture.Play();
            //source.Play();
            vidPlayer.Play();
            masterControl.instance.toggleInstrumentVolume(false);
        } else {
            vidQuad.SetActive(false);
        }
        loading = false;
        loaded = true;
}
}
