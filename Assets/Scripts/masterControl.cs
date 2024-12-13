// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright ? 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright ? 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright ? 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright ? 2017 Apache 2.0 Google LLC SoundStage VR
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
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Net;
using System.Linq;
using Meta.XR.EnvironmentDepth;

public class masterControl : MonoBehaviour {

  public static masterControl instance;
  public UnityEngine.Audio.AudioMixer masterMixer;
  public static float versionNumber = -1f;

  // this enum will be cast as int, make sure that the scenes in the build dialogue have exactly the same order
    public enum Scenes {
        Base,
        Local,
        Relay
    };

  public enum platform {
    Oculus,
    NoVR,
    Oculus_Deprecated
  };

  public platform currentPlatform = platform.Oculus;

  public Color tipColor = new Color(88 / 255f, 114 / 255f, 174 / 255f);

  public AudioSource backgroundAudio, metronomeClick;

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
    public double MeasurePhase { get => _measurePhase; set => _measurePhase = value; }

    public float glowVal = 1;

    public string SaveDir;

    public bool handlesEnabled = true;
    public bool jacksEnabled = true;

    public masterBusRecorder recorder;
    public metronome metro;
    public GameObject CameraRig;
    public EnvironmentDepthManager depthManager;
    public manipulator leftManip, rightManip;

    void Awake() {

    if (!Application.isEditor && !Debug.isDebugBuild){ 
        Debug.unityLogger.logEnabled = false; 
    }

    DontDestroyOnLoad(this);
    DontDestroyOnLoad(CameraRig);

    instance = this;
    float f;
    bool success = float.TryParse(Application.version, out f);
    if (success) versionNumber = f;

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

    //int bufferSize = 512;

    //if(Application.platform == RuntimePlatform.WindowsEditor)
    //{
    //  bufferSize = 512;
    //} 
    //else if (Application.platform == RuntimePlatform.Android)
    //{
    //  bufferSize = 256;
    //}

    Debug.Log("Buffer size is: " + configuration.dspBufferSize);
    //configuration.dspBufferSize = bufferSize;
    //AudioSettings.OnAudioConfigurationChanged += resetMasterClockDSPTime;
    //AudioSettings.Reset(configuration); // this fried the OnAudioFilterRead hook on the mastercontrol clock
    //AudioSettings.SetDSPBufferSize(bufferSize, 2);

    //Debug.Log("Buffer size is now set to: " + AudioSettings.GetConfiguration().dspBufferSize);

    if (!PlayerPrefs.HasKey("glowVal")) PlayerPrefs.SetFloat("glowVal", 1);
    if (!PlayerPrefs.HasKey("envSound")) PlayerPrefs.SetInt("envSound", 1);

    glowVal = PlayerPrefs.GetFloat("glowVal");
    setGlowLevel(glowVal);

    if (PlayerPrefs.GetInt("envSound") == 0) {
      MuteBackgroundSFX(true);
      muteEnvToggle.isOn = true;
    }

    SaveDir = Application.persistentDataPath + Path.DirectorySeparatorChar + "OpenSoundLab";
    ReadFileLocConfig();
    //Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "MySamples");

    #if UNITY_ANDROID
      //if Saves doesn't exist, extract example data... 
      if (Directory.Exists(SaveDir + Path.DirectorySeparatorChar + "Saves") == false)
      {
        Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "Saves");
        //copy tgz to directory where we can extract it
        WWW www = new WWW(Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Examples.tgz");
        while (!www.isDone) { }
        System.IO.File.WriteAllBytes(SaveDir + Path.DirectorySeparatorChar + "Examples.tgz", www.bytes);
        //extract it
        Utility_SharpZipCommands.ExtractTGZ(SaveDir + Path.DirectorySeparatorChar + "Examples.tgz", SaveDir + Path.DirectorySeparatorChar + "Saves");
        //delete tgz
        File.Delete(SaveDir + Path.DirectorySeparatorChar + "Examples.tgz");
      }
    #endif

        beatUpdateEvent += beatUpdateEventLocal;
        beatResetEvent += beatResetEventLocal;

        GetComponent<sampleManager>().Init();

        recorder = GetComponentInChildren<masterBusRecorder>();

        depthManager = CameraRig.GetComponent<EnvironmentDepthManager>();
        depthIsSupported = EnvironmentDepthManager.IsSupported;

        SceneManager.activeSceneChanged += findMetronome;

    }

    bool depthIsSupported = false;

    public void findMetronome(Scene prev, Scene next)
    {
        if (next.buildIndex == (int)Scenes.Base) return;

        // todo: get rid of this hack
        metronome[] metronomes = GameObject.FindObjectsByType<metronome>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        metronome m = metronomes.FirstOrDefault();

        if (m != null)
        {
            metro = m;
            metro.bpmDial.onPercentChangedEventLocal.AddListener(metro.readBpmDialAndBroadcast);
        }
    }

    

    private void Start()
    {
        if (!PlayerPrefs.HasKey("showTutorialsOnStartup"))
        {
            PlayerPrefs.SetInt("showTutorialsOnStartup", 1);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.GetInt("showTutorialsOnStartup") == 1 && tutorialsPrefab != null)
        {
            
            GameObject g = Instantiate(tutorialsPrefab, GameObject.Find("PatchAnchor").transform, false) as GameObject;

            //float height = Mathf.Clamp(Camera.main.transform.position.y, 1, 2);
            g.transform.position = new Vector3(0f, 1.3f, 0.75f);
            g.transform.Rotate(0f, -180f, 0f);

            //g.GetComponent<tutorialsDeviceInterface>().forcePlay(); // not working

        }

        SceneManager.LoadScene((int)Scenes.Local);

    }


    int lastBeat = -1;

    void Update()
    {

        // metronome plays bound to screen updates! 
        // Prone to jitter and CPU hanging! 
        // Do not trust the metronome! Build your own one!
        // Other sequencers avoid Update calls, continue even if Update call stack hangs
        if (lastBeat != Mathf.FloorToInt(curCycle * 8f))
        {
            //metronomeClick.Play();
            lastBeat = Mathf.FloorToInt(curCycle * 8f);
        }



        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
        {
            nextWireSetting();
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
        {
            nextBinauralSetting();
        }

        leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

        if (leftStick.x > 0.5f && rightStick.x < -0.5f) // press both inwards
        {
            Camera.main.backgroundColor = Color.white;
        }

        if (leftStick.x < -0.5f && rightStick.x > 0.5f) // press both outward
        {
            Camera.main.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }
        

        if(depthIsSupported){ 

            if (leftStick.y > 0.5f && rightStick.y > 0.5f)
            {
                defaultOcclusionMode = OcclusionShadersMode.None;
                 depthManager.enabled = false;
            }

            if (leftStick.y < -0.5f && rightStick.y < -0.5f)
            {
                defaultOcclusionMode = OcclusionShadersMode.SoftOcclusion;
                depthManager.enabled = true;
            }

            if (leftManip == null) leftManip = GameObject.Find("LeftHandAnchor").GetComponentInChildren<manipulator>();
            if (rightManip == null) rightManip = GameObject.Find("RightHandAnchor").GetComponentInChildren<manipulator>();

            if (depthManager != null && OSLInput.getInstance().areBothSidesPressed() && !leftManip.isGrabbing() && !rightManip.isGrabbing())
            {
                depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
                depthManager.enabled = false;
            }
            else
            {
                depthManager.OcclusionShadersMode = defaultOcclusionMode;
                depthManager.enabled = true;
            }
        }


        if (metro != null)
        {
            if (metro.volumepercent != metro.volumeDial.percent)
            {
                metro.volumepercent = metro.volumeDial.percent;
                masterControl.instance.metronomeClick.volume = Mathf.Clamp01(metro.volumepercent - .1f);
            }
            //if (metro.bpmpercent != metro.bpmDial.percent) metro.readBpmDialAndBroadcast();
        }

    }

    OcclusionShadersMode defaultOcclusionMode = OcclusionShadersMode.SoftOcclusion;

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (!beatUpdateRunning) return;
        double dspTime = AudioSettings.dspTime;

        for (int i = 0; i < buffer.Length; i += channels)
        {
            beatUpdateEvent(curCycle);
            _measurePhase += _sampleDuration;
            if (_measurePhase > measurePeriod) _measurePhase -= measurePeriod;
            curCycle = (float)(_measurePhase / measurePeriod);
        }
    }

    //public void resetMasterClockDSPTime(bool wasChanged){
    //  if (wasChanged) resetClock();
    //}
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
    bpm = (float)MathF.Round(b, 1);
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

  Vector2 leftStick, rightStick;
  

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


  public UnityEvent onBinauralChangedEvent;
  public UnityEvent onWireChangedEvent;
  public UnityEvent onDisplayChangedEvent;


  public BinauralMode BinauralSetting = BinauralMode.Speaker;

  public void updateBinauralSetting(int num) {
    if (BinauralSetting == (BinauralMode)num) {
      return;
    }
    BinauralSetting = (BinauralMode)num;

    //speakerDeviceInterface[] standaloneSpeakers = FindObjectsOfType<speakerDeviceInterface>();
    //for (int i = 0; i < standaloneSpeakers.Length; i++) {
    //  if (BinauralSetting == BinauralMode.None) standaloneSpeakers[i].audio.spatialize = false;
    //  else standaloneSpeakers[i].audio.spatialize = true;
    //}
    embeddedSpeaker[] embeddedSpeakers = FindObjectsOfType<embeddedSpeaker>();
    for (int i = 0; i < embeddedSpeakers.Length; i++) {
      if (BinauralSetting == BinauralMode.All) embeddedSpeakers[i].audio.spatialize = true;
      else embeddedSpeakers[i].audio.spatialize = false;
    }

    onBinauralChangedEvent.Invoke();
  }



    public WireMode WireSetting = WireMode.Straight;
       

    public void updateWireSetting(int num)
    {
        // make straight in case for loading legacy with curved
        if ((WireMode)num == WireMode.Curved)
        {
            num = (int)WireMode.Straight;
        }

        WireSetting = (WireMode)num;

        omniPlug[] plugs = FindObjectsOfType<omniPlug>();
        for (int i = 0; i < plugs.Length; i++)
        {
            plugs[i].activateWireMode(WireSetting);
        }

        onWireChangedEvent.Invoke();
    }

    public void nextWireSetting()
    {
        // Get the total number of WireMode enum values
        int totalModes = System.Enum.GetNames(typeof(WireMode)).Length;

        // Get the current WireSetting as an integer
        int currentSetting = (int)WireSetting;

        // Calculate the next setting, ensuring it skips 0 and wraps around properly
        // Avoid curved for now until path points are synced
        int nextSetting = (currentSetting % (totalModes - 1)) + 1;

        // Update the WireSetting with the new value
        updateWireSetting(nextSetting);
    }


    public void nextBinauralSetting()
    {
        updateBinauralSetting((BinauralSetting.GetHashCode() + 1) % System.Enum.GetNames(typeof(BinauralMode)).Length);
    }


    public DisplayMode DisplaySetting = DisplayMode.All; // todo: implement display switching with actual consequences for the Renderers of the devices

    public void updateDisplaySetting(int num){
        DisplaySetting = (DisplayMode)num;
        onDisplayChangedEvent.Invoke();
    }
}

public enum WireMode
{
    Curved,
    Straight,
    Visualized,
    Invisible
};
public enum DisplayMode
{
    All,
    InputAndSpeaker,
    Speaker,
    Nothing
}
public enum BinauralMode
{
    Speaker,
    All
};