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
// You may not use this file except in compliance with the License.
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
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Net;
using System.Linq;
using UnityEngine.Networking;
using Meta.XR.EnvironmentDepth;

public class masterControl : MonoBehaviour
{

    public static masterControl instance;
    public UnityEngine.Audio.AudioMixer masterMixer;
    public static float versionNumber = -1f;

    // this enum will be cast as int, make sure that the scenes in the build dialogue have exactly the same order
    public enum Scenes
    {
        Base,
        Local,
        Relay
    };

    public enum platform
    {
        Oculus,
        NoVR,
        Oculus_Deprecated
    };

    public platform currentPlatform = platform.Oculus;

    public Color tipColor = new Color(88 / 255f, 114 / 255f, 174 / 255f);

    public AudioSource backgroundAudio, metronomeClick;

    public UnityEngine.UI.Toggle muteEnvToggle;

    public float bpm = 120;

    public int curMic = 0;
    public string currentScene = "";


    public bool showEnvironment = true;
    double _sampleDuration;

    public float glowVal = 1;

    public string SaveDir;

    public bool handlesEnabled = true;
    public bool jacksEnabled = true;
    [SerializeField] bool autoLoadLocalScene;

    public masterBusRecorder recorder;
    public GameObject CameraRig;
    public EnvironmentDepthManager depthManager;
    public manipulator leftManip, rightManip;
    public int hrtfSubjectIndex = 5;
    string[] hrtfSubjectIds = new string[0];
    string[] hrtfSubjectNames = new string[0];
    string currentHrtfSubjectId = "";
    string currentHrtfSubjectName = "";
    Type resonanceAudioType;

    void Awake()
    {

        if (!Application.isEditor && !Debug.isDebugBuild)
        {
            Debug.unityLogger.logEnabled = false;
        }

        CameraRig = GameObject.Find("OVRCameraRig-Variant");

        DontDestroyOnLoad(this);
        DontDestroyOnLoad(CameraRig);

        instance = this;

#if UNITY_EDITOR
        // ensure that you see every frame that is calculated, otherwise e.g. the wires couldn't reliably visualize short trigger signals
        int editorRefreshRate = getDisplayRefreshRate();
        QualitySettings.vSyncCount = 1; // sync to display refresh
        Application.targetFrameRate = editorRefreshRate > 0 ? editorRefreshRate : -1;
#endif


        float f;
        bool success = float.TryParse(Application.version, out f);
        if (success) versionNumber = f;

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

        if (PlayerPrefs.GetInt("envSound") == 0)
        {
            MuteBackgroundSFX(true);
            muteEnvToggle.isOn = true;
        }

        SaveDir = ResolveDefaultSaveDir();
        EnsureDirectoryExists(SaveDir);
        ReadFileLocConfig();
        EnsureDirectoryExists(SaveDir);
        Debug.Log($"masterControl: SaveDir resolved to {SaveDir}");

#if UNITY_ANDROID
        string savesPath = Path.Combine(SaveDir, "Saves");
        if (!Directory.Exists(savesPath))
        {
            Directory.CreateDirectory(savesPath);

            string archivePath = Path.Combine(SaveDir, "Examples.tgz");
            bool copySucceeded = TryCopyStreamingAsset("Examples.tgz", archivePath, out string copyError);

            if (!copySucceeded)
            {
                Debug.LogError($"masterControl: failed to stage Examples.tgz. {copyError}");
            }
            else
            {
                try
                {
                    Utility_SharpZipCommands.ExtractTGZ(archivePath, savesPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"masterControl: failed to extract Examples.tgz. {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    TryDeleteFile(archivePath);
                }
            }
        }
#endif

        GetComponent<sampleManager>().Init();

        recorder = GetComponentInChildren<masterBusRecorder>();

        depthManager = CameraRig.GetComponent<EnvironmentDepthManager>();
        depthIsSupported = EnvironmentDepthManager.IsSupported;

    }

    bool depthIsSupported = false;

    int getDisplayRefreshRate()
    {
#if UNITY_2021_2_OR_NEWER
        float refreshRate = (float)Screen.currentResolution.refreshRateRatio.value;
        if (refreshRate > 0f)
        {
            return Mathf.RoundToInt(refreshRate);
        }
#endif
        int refreshRateFallback = Screen.currentResolution.refreshRate;
        return refreshRateFallback > 0 ? refreshRateFallback : -1;
    }


    private void Start()
    {
        StartCoroutine(CompleteStartupAfterSamplesReady());
    }

    IEnumerator CompleteStartupAfterSamplesReady()
    {
        sampleManager manager = GetComponent<sampleManager>();
        while (manager != null && !manager.IsReady)
        {
            yield return null;
        }

        RunStartupSequence();
    }

    void RunStartupSequence()
    {
        if (_startupSequenceExecuted)
        {
            return;
        }

        _startupSequenceExecuted = true;

        updateBinauralSetting((int)BinauralMode.Speaker);
        updateWireSetting((int)WireMode.Visualized);
        refreshHrtfSubjectLabel();

        if (!PlayerPrefs.HasKey("showTutorialsOnStartup"))
        {
            PlayerPrefs.SetInt("showTutorialsOnStartup", 1);
            PlayerPrefs.Save();
        }

        if (autoLoadLocalScene)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex != (int)Scenes.Local)
            {
                SceneManager.LoadScene((int)Scenes.Local);
            }
        }
    }


    void Update()
    {
        // Local metronome visual click loop removed.
        // Devices now handle their own timing.



        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
        {
            previousHrtfSubject();
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
        {
            nextHrtfSubject();
        }

        refreshHrtfSubjectLabel();

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


        if (depthIsSupported)
        {

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

    }

    OcclusionShadersMode defaultOcclusionMode = OcclusionShadersMode.SoftOcclusion;

    public void toggleInstrumentVolume(bool on)
    {
        masterMixer.SetFloat("instrumentVolume", on ? 0 : -18);
    }


    public static string ResolveDefaultSaveDir()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Path.Combine("/sdcard", "Documents", "OpenSoundLab");
#else
        string documents = TryGetDocumentsPath();
        return Path.Combine(documents, "OpenSoundLab");
#endif
    }

    static void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }


#if UNITY_ANDROID
    static bool TryCopyStreamingAsset(string fileName, string destinationPath, out string error)
    {
        error = null;

#if UNITY_EDITOR
        string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(sourcePath))
        {
            error = $"Source asset not found at {sourcePath}.";
            return false;
        }

        try
        {
            File.Copy(sourcePath, destinationPath, true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
#else
        string sourceUri = string.Format("{0}/{1}", Application.streamingAssetsPath, fileName);

        using (UnityWebRequest request = UnityWebRequest.Get(sourceUri))
        {
            var downloadHandler = new DownloadHandlerFile(destinationPath)
            {
                removeFileOnAbort = true
            };
            request.downloadHandler = downloadHandler;

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
            {
                error = request.error;
                return false;
            }
#else
            if (request.isHttpError || request.isNetworkError)
            {
                error = request.error;
                return false;
            }
#endif
        }

        return true;
#endif
    }
#endif

    static void TryDeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"masterControl: unable to delete temporary file at {path}. {ex.Message}");
        }
    }


    static string TryGetDocumentsPath()
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        if (!string.IsNullOrEmpty(homePath))
        {
            string macDocuments = Path.Combine(homePath, "Documents");
            if (!Directory.Exists(macDocuments))
            {
                try
                {
                    Directory.CreateDirectory(macDocuments);
                }
                catch (Exception)
                {
                    // Ignore; fall back to other candidates below.
                }
            }

            if (Directory.Exists(macDocuments))
            {
                return macDocuments;
            }
        }
#endif

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrEmpty(documents))
        {
            if (!Directory.Exists(documents))
            {
                try
                {
                    Directory.CreateDirectory(documents);
                }
                catch (Exception)
                {
                    // Ignore and continue with fallbacks.
                }
            }

            if (Directory.Exists(documents))
            {
                return documents;
            }
        }

        string personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        if (!string.IsNullOrEmpty(personal))
        {
            return personal;
        }

        try
        {
            string parentPath = Directory.GetParent(Application.persistentDataPath)?.FullName;
            if (!string.IsNullOrEmpty(parentPath) && Directory.Exists(parentPath))
            {
                return parentPath;
            }
        }
        catch (Exception)
        {
            // ignore; fall back below
        }

        if (!string.IsNullOrEmpty(Application.persistentDataPath))
        {
            return Application.persistentDataPath;
        }

        return Environment.CurrentDirectory;
    }


    void ReadFileLocConfig()
    {
        if (File.Exists(Application.persistentDataPath + Path.DirectorySeparatorChar + "fileloc.cfg"))
        {
            string _txt = File.ReadAllText(Application.persistentDataPath + Path.DirectorySeparatorChar + "fileloc.cfg");
            if (_txt != @"x:/put/custom/dir/here" && _txt != "")
            {
                _txt = Path.GetFullPath(_txt);
                if (Directory.Exists(_txt)) SaveDir = _txt;
            }
        }
    }

    public void toggleHandles(bool on)
    {
        handlesEnabled = on;
        handle[] handles = FindObjectsOfType<handle>();
        for (int i = 0; i < handles.Length; i++)
        {
            handles[i].toggleHandles(on);
        }
    }

    public void toggleJacks(bool on)
    {
        jacksEnabled = on;
        omniJack[] jacks = FindObjectsOfType<omniJack>();
        for (int i = 0; i < jacks.Length; i++)
        {
            jacks[i].GetComponent<Collider>().enabled = on;
        }

        omniPlug[] plugs = FindObjectsOfType<omniPlug>();
        for (int i = 0; i < plugs.Length; i++)
        {
            plugs[i].GetComponent<Collider>().enabled = on;
        }
    }

    drumDeviceInterface mostRecentDrum;
    public void newDrum(drumDeviceInterface d)
    {
        if (mostRecentDrum != null) mostRecentDrum.displayDrumsticks(false);
        mostRecentDrum = d;
        d.displayDrumsticks(true);

    }

    public void setGlowLevel(float t)
    {
        glowVal = t;
        PlayerPrefs.SetFloat("glowVal", glowVal);
    }

    public bool tooltipsOn = true;
    public bool toggleTooltips()
    {

        tooltipsOn = !tooltipsOn;

        touchpad[] pads = FindObjectsOfType<touchpad>();
        for (int i = 0; i < pads.Length; i++)
        {
            pads[i].buttonContainers[0].SetActive(tooltipsOn);
        }


        tooltips[] tips = FindObjectsOfType<tooltips>();
        for (int i = 0; i < tips.Length; i++)
        {
            tips[i].ShowTooltips(tooltipsOn);
        }
        return tooltipsOn;
    }

    public bool examplesOn = true;
    public bool toggleExamples()
    {
        examplesOn = !examplesOn;

        if (!examplesOn)
        {
            GameObject prevParent = GameObject.Find("exampleParent");
            if (prevParent != null) Destroy(prevParent);
        }
        return examplesOn;
    }

    public bool dialUsed = false;

    public void MicChange(int val)
    {
        curMic = val;
    }

    public void MuteBackgroundSFX(bool mute)
    {
        PlayerPrefs.SetInt("envSound", mute ? 0 : 1);
        if (mute)
        {
            backgroundAudio.volume = 0;
        }
        else backgroundAudio.volume = .02f;
    }

    public void setBPM(float b)
    {
        bpm = (float)MathF.Round(b, 1);
    }

    bool _startupSequenceExecuted;

    public GameObject tutorialsPrefab;
    public Transform patchAnchor;

    Vector2 leftStick, rightStick;


    public void openRecordings()
    {
        //    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Samples" + Path.DirectorySeparatorChar + "Recordings" + Path.DirectorySeparatorChar);
    }

    public void openSavedScenes()
    {
        //    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar);
    }

    public void openVideoTutorials()
    {
        Application.OpenURL("https://www.youtube.com/playlist?list=PL9oPBUaRjJEwjy7glYUvOMqw66QrtTxZD");
    }

    public string GetFileURL(string path)
    {
        return (new System.Uri(path)).AbsoluteUri;
    }


    public UnityEvent onBinauralChangedEvent;
    public UnityEvent onWireChangedEvent;
    public UnityEvent onDisplayChangedEvent;
    public UnityEvent onHrtfChangedEvent = new UnityEvent();

    public void previousHrtfSubject()
    {
        selectHrtfSubject(-1);
    }

    public void nextHrtfSubject()
    {
        selectHrtfSubject(1);
    }

    public string getHrtfSubjectLabel()
    {
        refreshHrtfSubjectLabel();
        return currentHrtfSubjectName;
    }

    public string getHrtfSubjectId()
    {
        refreshHrtfSubjectLabel();
        return currentHrtfSubjectId;
    }

    void selectHrtfSubject(int offset)
    {
        if (!ensureHrtfSubjects())
        {
            Debug.LogWarning("masterControl: no HRTF subjects found for switching.", this);
            return;
        }

        int nextIndex = wrapIndex(hrtfSubjectIndex + offset, hrtfSubjectIds.Length);
        updateHrtfSubjectId(hrtfSubjectIds[nextIndex]);
    }

    public void updateHrtfSubjectId(string subjectId)
    {
        if (string.IsNullOrEmpty(subjectId) || !ensureHrtfSubjects())
        {
            return;
        }

        refreshHrtfSubjectLabel();
        bool changed = currentHrtfSubjectId != subjectId;
        int nextIndex = getHrtfSubjectIndex(subjectId);
        if (nextIndex < 0)
        {
            Debug.LogWarning("masterControl: unknown HRTF subject " + subjectId + ".", this);
            return;
        }

        hrtfSubjectIndex = nextIndex;
        refreshHrtfSubjectLabel();

        bool didApply = applyHrtfSubject(currentHrtfSubjectId);
        if (!didApply)
        {
            Debug.LogWarning("masterControl: selected HRTF subject " + currentHrtfSubjectId + " but the native renderer did not accept it.", this);
        }

        if (changed && onHrtfChangedEvent != null)
        {
            onHrtfChangedEvent.Invoke();
        }
    }

    int getHrtfSubjectIndex(string subjectId)
    {
        for (int i = 0; i < hrtfSubjectIds.Length; i++)
        {
            if (hrtfSubjectIds[i] == subjectId)
            {
                return i;
            }
        }

        return -1;
    }

    bool ensureHrtfSubjects()
    {
        if (hrtfSubjectIds.Length > 0 && hrtfSubjectIds.Length == hrtfSubjectNames.Length)
        {
            return true;
        }

        string[] nativeSubjectIds = invokeResonanceAudioStringArray("GetHrtfSubjectIds");
        string[] nativeSubjectNames = invokeResonanceAudioStringArray("GetHrtfSubjectNames");
        if (nativeSubjectIds.Length > 0 && nativeSubjectIds.Length == nativeSubjectNames.Length)
        {
            hrtfSubjectIds = nativeSubjectIds;
            hrtfSubjectNames = nativeSubjectNames;
            return true;
        }

        hrtfSubjectIds = new string[] {
            "Subject_002",
            "D1", "D2", "H3", "H4", "H5", "H6", "H7", "H8", "H9",
            "H10", "H11", "H12", "H13", "H14", "H15", "H16", "H17",
            "H18", "H19", "H20"
        };

        hrtfSubjectNames = new string[] {
            "Legacy SADIE Subject 002",
            "SADIE II D1", "SADIE II D2", "SADIE II H3", "SADIE II H4",
            "SADIE II H5", "SADIE II H6", "SADIE II H7", "SADIE II H8",
            "SADIE II H9", "SADIE II H10", "SADIE II H11", "SADIE II H12",
            "SADIE II H13", "SADIE II H14", "SADIE II H15", "SADIE II H16",
            "SADIE II H17", "SADIE II H18", "SADIE II H19", "SADIE II H20"
        };

        return true;
    }

    void refreshHrtfSubjectLabel()
    {
        if (!ensureHrtfSubjects())
        {
            currentHrtfSubjectId = "";
            currentHrtfSubjectName = "";
            return;
        }

        int count = Mathf.Min(hrtfSubjectIds.Length, hrtfSubjectNames.Length);
        if (count == 0)
        {
            currentHrtfSubjectId = "";
            currentHrtfSubjectName = "";
            return;
        }

        hrtfSubjectIndex = wrapIndex(hrtfSubjectIndex, count);
        currentHrtfSubjectId = hrtfSubjectIds[hrtfSubjectIndex];
        currentHrtfSubjectName = hrtfSubjectNames[hrtfSubjectIndex];
    }

    string[] invokeResonanceAudioStringArray(string methodName)
    {
        Type type = getResonanceAudioType();
        if (type == null)
        {
            return new string[0];
        }

        System.Reflection.MethodInfo method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null)
        {
            return new string[0];
        }

        try
        {
            return method.Invoke(null, null) as string[] ?? new string[0];
        }
        catch (Exception)
        {
            return new string[0];
        }
    }

    bool applyHrtfSubject(string subjectId)
    {
        if (string.IsNullOrEmpty(subjectId))
        {
            return false;
        }

        Type type = getResonanceAudioType();
        if (type == null)
        {
            return false;
        }

        System.Reflection.MethodInfo method = type.GetMethod("SetHrtfSubject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null)
        {
            return false;
        }

        try
        {
            object result = method.Invoke(null, new object[] { subjectId });
            return result is bool && (bool)result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    Type getResonanceAudioType()
    {
        if (resonanceAudioType != null)
        {
            return resonanceAudioType;
        }

        resonanceAudioType = Type.GetType("ResonanceAudio");
        if (resonanceAudioType != null)
        {
            return resonanceAudioType;
        }

        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            resonanceAudioType = assemblies[i].GetType("ResonanceAudio");
            if (resonanceAudioType != null)
            {
                return resonanceAudioType;
            }
        }

        return null;
    }

    int wrapIndex(int index, int count)
    {
        int wrappedIndex = index % count;
        return wrappedIndex < 0 ? wrappedIndex + count : wrappedIndex;
    }


    public BinauralMode BinauralSetting = BinauralMode.Speaker;

    public void updateBinauralSetting(int num)
    {
        BinauralMode nextSetting = BinauralMode.Speaker;
        bool changed = BinauralSetting != nextSetting;
        BinauralSetting = nextSetting;

        //speakerDeviceInterface[] standaloneSpeakers = FindObjectsOfType<speakerDeviceInterface>();
        //for (int i = 0; i < standaloneSpeakers.Length; i++) {
        //  if (BinauralSetting == BinauralMode.None) standaloneSpeakers[i].audio.spatialize = false;
        //  else standaloneSpeakers[i].audio.spatialize = true;
        //}
        embeddedSpeaker[] embeddedSpeakers = FindObjectsOfType<embeddedSpeaker>();
        for (int i = 0; i < embeddedSpeakers.Length; i++)
        {
            embeddedSpeakers[i].audio.spatialize = false;
        }

        if (changed)
        {
            onBinauralChangedEvent.Invoke();
        }
    }



    public WireMode WireSetting = WireMode.Visualized;


    public void updateWireSetting(int num)
    {
        WireMode nextSetting = WireMode.Visualized;
        bool changed = WireSetting != nextSetting;
        WireSetting = nextSetting;

        omniPlug[] plugs = FindObjectsOfType<omniPlug>();
        for (int i = 0; i < plugs.Length; i++)
        {
            plugs[i].activateWireMode(WireSetting);
        }

        if (changed)
        {
            onWireChangedEvent.Invoke();
        }
    }

    public void nextWireSetting()
    {
        updateWireSetting((int)WireMode.Visualized);
    }


    public void nextBinauralSetting()
    {
        updateBinauralSetting((int)BinauralMode.Speaker);
    }


    public DisplayMode DisplaySetting = DisplayMode.All; // todo: implement display switching with actual consequences for the Renderers of the devices

    public void updateDisplaySetting(int num)
    {
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
