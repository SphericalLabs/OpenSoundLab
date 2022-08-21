// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class cameraDeviceInterface : deviceInterface {

  public Camera rtCam;
  public GameObject realCam, previewCam;
  public Transform rtQuad, screenTrans;
  public GameObject recLight, recMessage;
  public button broadcastButton, previewButton;

  omniJack input;
  signalGenerator externalPulse;
  float[] lastPlaySig;

  bool activated = false;

  [DllImport("SoundStageNative")]
  public static extern int CountPulses(float[] buffer, int length, int channels, float[] lastSig);

  public override void Awake() {
    base.Awake();
    lastPlaySig = new float[] { 0, 0 };
    input = GetComponentInChildren<omniJack>();
    recLight.GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0, 152 / 255f, 69 / 255f));
    recLight.SetActive(false);
    recMessage.SetActive(false);
    camSetup();
  }

  void Start() {

    bool sky = masterControl.instance.showEnvironment;
    Camera[] cams = GetComponentsInChildren<Camera>();
    for (int i = 0; i < cams.Length; i++) {
      if (cams[i].GetComponent<Skybox>() != null) cams[i].GetComponent<Skybox>().enabled = sky;
    }
    realCam.GetComponent<Skybox>().enabled = sky;
    activated = true;

    GameObject.Find("Main Camera (ears)").GetComponent<AudioListener>().enabled = false; // turn off head ears while external camera is present, will only work for one cam setups
    gameObject.AddComponent(typeof(AudioListener));
  }

  void OnDestroy() {
    GameObject.Find("Main Camera (ears)").GetComponent<AudioListener>().enabled = true;
    if (screenTrans != null && activated) Destroy(screenTrans.gameObject);
  }

  void Update() {
    if (input.signal != externalPulse) externalPulse = input.signal;
    if (hits > 0) {
      if (hits % 2 != 0) toggleRealCam(!broadcasting);
      hits = 0;
    }
  }

  void OnDisable() {
    if (broadcasting) toggleRealCam(false);
  }

  bool preview = true;
  void togglePreview(bool on) {
    preview = on;
    previewCam.SetActive(on);
    screenTrans.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) toggleRealCam(on);
    if (ID == 1) togglePreview(on);
  }

  bool broadcasting = false;
  public void toggleRealCam(bool on) {
    if (broadcasting == on) return;
    broadcasting = on;
    if (on) {
      realCam.GetComponent<Skybox>().enabled = masterControl.instance.showEnvironment;
      cameraDeviceInterface[] cams = FindObjectsOfType<cameraDeviceInterface>();
      for (int i = 0; i < cams.Length; i++) {
        if (cams[i] != this) {
          cams[i].toggleRealCam(false);
          cams[i].broadcastButton.phantomHit(false);
        }
      }
    }

    broadcastButton.phantomHit(on);
    recLight.SetActive(on);
    recMessage.SetActive(on);
    realCam.SetActive(on);
  }

  bool curHiRes = false;
  public void updateResolution(bool hires) {
    if (curHiRes == hires) return;
    camSetup(hires);
  }

  void camSetup(bool hires = false) {
    curHiRes = hires;

    if (rtCam.targetTexture != null) {
      rtCam.targetTexture.Release();
    }
    int mult = hires ? 32 : 16;
    rtCam.targetTexture = new RenderTexture(4 * 16 * mult, 4 * 9 * mult, 16);
    rtQuad.GetComponent<Renderer>().material.mainTexture = rtCam.targetTexture;
  }



  int hits = 0;
  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (externalPulse == null) return;
    double dspTime = AudioSettings.dspTime;

    float[] playBuffer = new float[buffer.Length];
    externalPulse.processBuffer(playBuffer, dspTime, channels);

    hits += CountPulses(playBuffer, buffer.Length, channels, lastPlaySig);
  }


  public override InstrumentData GetData() {
    CameraData data = new CameraData();
    data.deviceType = menuItem.deviceType.Camera;
    GetTransformData(data);

    data.inputID = input.transform.GetInstanceID();

    data.screenPosition = screenTrans.localPosition;
    data.screenRotation = screenTrans.localRotation;
    data.screenScale = screenTrans.localScale;
    data.screenDisabled = !preview;
    return data;
  }

  public override void Load(InstrumentData d) {
    CameraData data = d as CameraData;
    base.Load(data);

    previewButton.startToggled = !data.screenDisabled;

    screenTrans.localPosition = data.screenPosition;
    screenTrans.localRotation = data.screenRotation;
    screenTrans.localScale = data.screenScale;
    input.ID = data.inputID;
  }
}

public class CameraData : InstrumentData {
  public Vector3 screenPosition;
  public Vector3 screenScale;
  public Quaternion screenRotation;
  public int inputID;
  public bool screenDisabled;
}
