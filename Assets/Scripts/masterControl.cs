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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus;
using Unity.XR.Oculus;

public class masterControl : MonoBehaviour {

  public static masterControl instance;
  public UnityEngine.Audio.AudioMixer masterMixer;
  public static float versionNumber = .76f;

  public enum platform {
    Oculus,
    NoVR,
    Oculus_Deprecated
  };

  public platform currentPlatform = platform.Oculus;

  public Color tipColor = new Color(88 / 255f, 114 / 255f, 174 / 255f);

  public AudioSource backgroundAudio, metronomeClick;
  public GameObject exampleSetups;

  public UnityEngine.UI.Toggle muteEnvToggle;

  public float bpm = 120;
  public float curCycle = 0;

  public double measurePeriod = 4;

  public int curMic = 0;
  public string currentScene = "";

  public delegate void BeatUpdateEvent(float t);
  public BeatUpdateEvent beatUpdateEvent;

  public delegate void BeatResetEvent();
  public BeatResetEvent beatResetEvent;

  public bool showEnvironment = true;
  double _sampleDuration;
  double _measurePhase;

  public float glowVal = 1;

  public string SaveDir;

  public bool handlesEnabled = true;
  public bool jacksEnabled = true;

  public masterBusRecorder recorder;

  void Awake() {
    instance = this;
    _measurePhase = 0;
    _sampleDuration = 1.0 / AudioSettings.outputSampleRate;

    var configuration = AudioSettings.GetConfiguration();

    if (configuration.sampleRate == 48000)
    {
      Debug.Log("Unity sample rate is " + configuration.sampleRate);
    }
    else
    {
      Debug.LogWarning("Unity sample rate is " + configuration.sampleRate);
    }

    int bufferSize = 512;

    if(Application.platform == RuntimePlatform.WindowsEditor)
    {
      bufferSize = 512;
    } 
    else if (Application.platform == RuntimePlatform.Android)
    {
      bufferSize = 256;

      //OVRPlugin.systemDisplayFrequency = 72;

      Debug.Log("Current cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
      Debug.Log("Trying to set levels to 4");
      Debug.Log("TrySetCPULevel returned " + Performance.TrySetCPULevel(4));
      Debug.Log("TrySetGPULevel returned " + Performance.TrySetGPULevel(4));
      Debug.Log("New cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
      Debug.Log("Display refresh rate: " + Stats.AdaptivePerformance.RefreshRate);
      Unity.XR.Oculus.Utils.SetFoveationLevel(4);
      
    }

    Debug.Log("Buffer size is: " + configuration.dspBufferSize);
    //configuration.dspBufferSize = bufferSize;
    //AudioSettings.OnAudioConfigurationChanged += resetMasterClockDSPTime;
    //AudioSettings.Reset(configuration); // this fried the OnAudioFilterRead hook on the mastercontrol clock
    //AudioSettings.SetDSPBufferSize(bufferSize, 2);

    //Debug.Log("Buffer size is now set to: " + AudioSettings.GetConfiguration().dspBufferSize);

    //OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

    if (!PlayerPrefs.HasKey("glowVal")) PlayerPrefs.SetFloat("glowVal", 1);
    if (!PlayerPrefs.HasKey("envSound")) PlayerPrefs.SetInt("envSound", 1);

    glowVal = PlayerPrefs.GetFloat("glowVal");
    setGlowLevel(glowVal);

    if (PlayerPrefs.GetInt("envSound") == 0) {
      MuteBackgroundSFX(true);
      muteEnvToggle.isOn = true;
    }

    SaveDir = Application.persistentDataPath + Path.DirectorySeparatorChar + "SoundStage";
    ReadFileLocConfig();
    Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "Saves");
    Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "MySamples");

    beatUpdateEvent += beatUpdateEventLocal;
    beatResetEvent += beatResetEventLocal;

    setBPM(120);

    GetComponent<sampleManager>().Init();

   recorder = GetComponentInChildren<masterBusRecorder>();

  }

  //public void resetMasterClockDSPTime(bool wasChanged){
  //  if (wasChanged) resetClock();
  //}

  public void toggleInstrumentVolume(bool on) {
    masterMixer.SetFloat("instrumentVolume", on ? 0 : -18);
  }


  void ReadFileLocConfig() {
    if(File.Exists(Application.persistentDataPath + Path.DirectorySeparatorChar + "fileloc.cfg")) {
      string _txt = File.ReadAllText(Application.persistentDataPath + Path.DirectorySeparatorChar + "fileloc.cfg");
      if (_txt != @"x:/put/custom/dir/here" && _txt != "") {
        _txt = Path.GetFullPath(_txt);
        if (Directory.Exists(_txt)) SaveDir = _txt;
      }
    } 
  }

  public void toggleHandles(bool on) {
    handlesEnabled = on;
    handle[] handles = FindObjectsOfType<handle>();
    for (int i = 0; i < handles.Length; i++) {
      handles[i].toggleHandles(on);
    }
  }

  public void toggleJacks(bool on) {
    jacksEnabled = on;
    omniJack[] jacks = FindObjectsOfType<omniJack>();
    for (int i = 0; i < jacks.Length; i++) {
      jacks[i].GetComponent<Collider>().enabled = on;
    }

    omniPlug[] plugs = FindObjectsOfType<omniPlug>();
    for (int i = 0; i < plugs.Length; i++) {
      plugs[i].GetComponent<Collider>().enabled = on;
    }
  }

  drumDeviceInterface mostRecentDrum;
  public void newDrum(drumDeviceInterface d) {
    if (mostRecentDrum != null) mostRecentDrum.displayDrumsticks(false);
    mostRecentDrum = d;
    d.displayDrumsticks(true);

  }

  public void setGlowLevel(float t) {
    glowVal = t;
    PlayerPrefs.SetFloat("glowVal", glowVal);
  }

  public bool tooltipsOn = true;
  public bool toggleTooltips() {

    tooltipsOn = !tooltipsOn;

    touchpad[] pads = FindObjectsOfType<touchpad>();
    for (int i = 0; i < pads.Length; i++) {
      pads[i].buttonContainers[0].SetActive(tooltipsOn);
    }


    tooltips[] tips = FindObjectsOfType<tooltips>();
    for (int i = 0; i < tips.Length; i++) {
      tips[i].ShowTooltips(tooltipsOn);
    }
    return tooltipsOn;
  }

  public bool examplesOn = true;
  public bool toggleExamples() {
    examplesOn = !examplesOn;
    exampleSetups.SetActive(examplesOn);

    if (!examplesOn) {
      GameObject prevParent = GameObject.Find("exampleParent");
      if (prevParent != null) Destroy(prevParent);
    }
    return examplesOn;
  }

  public bool dialUsed = false;

  public void MicChange(int val) {
    curMic = val;
  }

  public void MuteBackgroundSFX(bool mute) {
    PlayerPrefs.SetInt("envSound", mute ? 0 : 1);
    if (mute) {
      backgroundAudio.volume = 0;
    } else backgroundAudio.volume = .02f;
  }

  void beatUpdateEventLocal(float t) { }
  void beatResetEventLocal() { }

  public void setBPM(float b) {
    bpm = Mathf.RoundToInt(b);
    measurePeriod = 480f / bpm;
    _measurePhase = curCycle * measurePeriod;
  }

  public void resetClock() {
    _measurePhase = 0;
    curCycle = 0;
    beatResetEvent();
  }

  bool beatUpdateRunning = true;
  public void toggleBeatUpdate(bool on) {
    beatUpdateRunning = on;
  }

  public GameObject tutorialsPrefab;
  public Transform patchAnchor;
  private void Start()
  {
    if(!PlayerPrefs.HasKey("showTutorialsOnStartup")){
      PlayerPrefs.SetInt("showTutorialsOnStartup", 1);
      PlayerPrefs.Save();
    }

    if(PlayerPrefs.GetInt("showTutorialsOnStartup") == 1){
      GameObject g = Instantiate(tutorialsPrefab, patchAnchor, false) as GameObject;

      //float height = Mathf.Clamp(Camera.main.transform.position.y, 1, 2);
      g.transform.position = new Vector3(0f, 1.3f, 0.75f); 
      g.transform.Rotate(0f, -180f, 0f);

      //g.GetComponent<tutorialsDeviceInterface>().forcePlay(); // not working

    }
  }

  int lastBeat = -1;
  void Update() {
    // metronome plays bound to screen updates! 
    // Prone to jitter and CPU hanging! 
    // Do not trust the metronome! Build your own one!
    // Other sequencers avoid Update calls, continue even if Update call stack hangs
    if (lastBeat != Mathf.FloorToInt(curCycle * 8f)) { 
      //metronomeClick.Play();
      lastBeat = Mathf.FloorToInt(curCycle * 8f);
    }


    if(OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch)){
      nextWireSetting();
    }

    if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
    {
      nextBinauralSetting();
    }

  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (!beatUpdateRunning) return;
    double dspTime = AudioSettings.dspTime;

    for (int i = 0; i < buffer.Length; i += channels) {
      beatUpdateEvent(curCycle);
      _measurePhase += _sampleDuration;
      if (_measurePhase > measurePeriod) _measurePhase -= measurePeriod;
      curCycle = (float)(_measurePhase / measurePeriod);
    }
  }

  public void openRecordings() {
//    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Samples" + Path.DirectorySeparatorChar + "Recordings" + Path.DirectorySeparatorChar);
  }

  public void openSavedScenes() {
//    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar);
  }

  public void openVideoTutorials() {
    Application.OpenURL("https://www.youtube.com/playlist?list=PL9oPBUaRjJEwjy7glYUvOMqw66QrtTxZD");
  }

  public string GetFileURL(string path) {
    return (new System.Uri(path)).AbsoluteUri;
  }

  public enum BinauralMode {
    None,
    Speaker,
    All
  };
  public BinauralMode BinauralSetting = BinauralMode.None;

  public void updateBinaural(int num) {
    if (BinauralSetting == (BinauralMode)num) {
      return;
    }
    BinauralSetting = (BinauralMode)num;

    speakerDeviceInterface[] standaloneSpeakers = FindObjectsOfType<speakerDeviceInterface>();
    for (int i = 0; i < standaloneSpeakers.Length; i++) {
      if (BinauralSetting == BinauralMode.None) standaloneSpeakers[i].audio.spatialize = false;
      else standaloneSpeakers[i].audio.spatialize = true;
    }
    embeddedSpeaker[] embeddedSpeakers = FindObjectsOfType<embeddedSpeaker>();
    for (int i = 0; i < embeddedSpeakers.Length; i++) {
      if (BinauralSetting == BinauralMode.All) embeddedSpeakers[i].audio.spatialize = true;
      else embeddedSpeakers[i].audio.spatialize = false;
    }
  }

  public enum WireMode {
    Curved,
    Straight,
    Invisible
  };

  public WireMode WireSetting = WireMode.Curved;
  public void updateWireSetting(int num) {
    if (WireSetting == (WireMode)num) {
      return;
    }
    WireSetting = (WireMode)num;

    omniPlug[] plugs = FindObjectsOfType<omniPlug>();
    for (int i = 0; i < plugs.Length; i++) {
      plugs[i].updateLineType(WireSetting);
    }
  }

  public void nextWireSetting()
  {
    updateWireSetting((WireSetting.GetHashCode() + 1) % 3); // modolo for sneaky wrapping
  }

  public void nextBinauralSetting()
  {
    updateBinaural((BinauralSetting.GetHashCode() + 1 ) % 3); 
  }
}
