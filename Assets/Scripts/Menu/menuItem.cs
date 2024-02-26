// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class menuItem : manipObject {
  public DeviceType item = DeviceType.Oscillator;
  public Material menuMat;
  public Renderer glowRend;
  menuManager manager;
  Material glowMat;

  Texture tex;
  GameObject itemPrefab;
  GameObject g; // the instance of itemPrefab
  public Renderer symbol;
  TextMesh label;
  Material labelMat;

  bool disabled = false;

  

  //public enum deviceType {
  //  // Please obey ascending alphabetical order, will define the order to the menuItems
  //  AD,
  //  ADSR,
  //  Airhorn,
  //  Artefact,
  //  DC,
  //  Camera,
  //  Compressor,
  //  ControlCube,
  //  Delay,
  //  Drum,
  //  Filter,
  //  Freeverb,
  //  Gain,
  //  Glide,
  //  Keyboard,
  //  Looper,
  //  Maracas,
  //  Microphone,
  //  MIDIIN,
  //  MIDIOUT,
  //  Mixer,
  //  Multiple, // Only MultiMix and MultiSplit are shown in the menu palette, but they will be serialized and loaded as preconfigured Multiples on save/load
  //  MultiMix,
  //  MultiSplit,
  //  Noise,
  //  Oscillator,
  //  Polarizer,
  //  Quantizer,
  //  Recorder,
  //  Reverb,    
  //  SampleHold,
  //  Sampler,
  //  Scope,
  //  Sequencer,
  //  SequencerCV,
  //  Speaker,    
  //  Tapes,
  //  Timeline,
  //  TouchPad,
  //  Tutorials,
  //  VCA,
  //  XyloRoll,
  //  // this is a stopper, items below won't appear in menu; alternatively you can skip items in menuManager.loadMenu()
  //  Max,        
  //  TapeGroup,
  //  Pano
  //};

  //Color disabledColor;
  //Color normalColor;
  //Color selectColor;

  public void disable(bool on) {
    if (disabled == on) return;
    disabled = on;

    GetComponent<Collider>().enabled = !disabled;

  }

  public override void Awake() {
    base.Awake();
    //disabledColor = normalColor = selectColor = new Color(0.6f, 0.6f, 0.6f);
    
    label = GetComponentInChildren<TextMesh>();
    //labelMat = label.GetComponent<Renderer>().material;
    
    //labelMat.SetColor("_TintColor", normalColor);
    glowRend.gameObject.SetActive(false);
    glowMat = glowRend.material;
  }

  public GameObject Setup(DeviceType d) {
    item = d;
    tex = Resources.Load("Textures/" + item.ToString() + "Symbol") as Texture;
    if (tex != null) tex.mipMapBias = -1f; // shift mipmap by one level, improves clarity of menu symbols
    symbol.material.SetTexture("_BaseMap", tex);
    itemPrefab = Resources.Load("Prefabs/" + item.ToString()) as GameObject;
    label.text = item.ToString();
    // Please use the first letter of the original enum name for proper sorting in the menu!
    if (item == DeviceType.VCA) label.text = "VCA";
    if (item == DeviceType.Glide) label.text = "Glide";
    if (item == DeviceType.Gain) label.text = "Gain / Mute";
    if (item == DeviceType.MIDIIN) label.text = "MIDI In";
    if (item == DeviceType.MIDIOUT) label.text = "MIDI Out";
    if (item == DeviceType.Sequencer) label.text = "Sequencer (Old)";    
    if (item == DeviceType.SequencerCV) label.text = "Sequencer";
    if (item == DeviceType.Timeline) label.text = "Sequencer III";
    if (item == DeviceType.ControlCube) label.text = "ControlCube";
    if (item == DeviceType.Microphone) label.text = "Mic";
    if (item == DeviceType.SampleHold) label.text = "S&H";
    if (item == DeviceType.Reverb) label.text = "Reverb";
    //if (item == deviceType.Freeverb) label.text = "Reverb";
    if (item == DeviceType.DC) label.text = "DC";
    if (item == DeviceType.Polarizer) label.text = "Polarity";


    label.gameObject.SetActive(true);
    symbol.gameObject.SetActive(true);
    g = Instantiate(itemPrefab, transform.position, transform.rotation) as GameObject;
    g.transform.parent = transform;
    

    manager = transform.parent.parent.GetComponent<menuManager>();

    Vector3 size = Vector3.zero;
    Vector3 center = Vector3.zero;

    if (item == DeviceType.Sequencer) {
      for (int i = 0; i < 2; i++) {
        for (int i2 = 0; i2 < 4; i2++) {
          GameObject cube = Instantiate(g.GetComponent<sequencerDeviceInterface>().touchCubePrefab, transform.position, transform.rotation) as GameObject;
          cube.transform.parent = g.transform;
          cube.transform.Translate(Vector3.right * i2 * -.04f, Space.Self);
          cube.transform.Translate(Vector3.up * i * -.04f, Space.Self);
        }

        GameObject seq = Instantiate(g.GetComponent<sequencerDeviceInterface>().samplerPrefab, transform.position, transform.rotation) as GameObject;
        seq.transform.parent = g.transform;
        seq.transform.Translate(Vector3.right * .081f, Space.Self);
        seq.transform.Translate(Vector3.up * i * -.04f, Space.Self);
      }
      Destroy(g.transform.Find("stretchNode").gameObject);
    }

    if (item == DeviceType.SequencerCV)
    {
      for (int i = 0; i < 2; i++)
      {
        for (int i2 = 0; i2 < 4; i2++)
        {
          GameObject cube = Instantiate(g.GetComponent<sequencerCVDeviceInterface>().touchDialPrefab, transform.position, transform.rotation) as GameObject;
          cube.transform.parent = g.transform;
          cube.transform.Translate(Vector3.right * i2 * -.04f, Space.Self);
          cube.transform.Translate(Vector3.up * i * -.04f, Space.Self);
        }

        GameObject seq = Instantiate(g.GetComponent<sequencerCVDeviceInterface>().samplerPrefab, transform.position, transform.rotation) as GameObject;
        seq.transform.parent = g.transform;
        seq.transform.Translate(Vector3.right * .081f, Space.Self);
        seq.transform.Translate(Vector3.up * i * -.04f, Space.Self);
      }
      Destroy(g.transform.Find("stretchNode").gameObject);
    }

    if (item == DeviceType.Tapes) {
      GameObject tape = Instantiate(g.GetComponent<libraryDeviceInterface>().tapePrefab, transform, false) as GameObject;
      Destroy(g);
      g = tape;
    }

    if (item == DeviceType.Timeline) {
      GameObject tl = Instantiate(Resources.Load("Prefabs/timelineRep") as GameObject, transform, false) as GameObject;
      Destroy(g);
      g = tl;
    }

    MonoBehaviour[] m = g.GetComponentsInChildren<MonoBehaviour>();
    for (int i = 0; i < m.Length; i++) Destroy(m[i]);

    AudioSource[] audios = g.GetComponentsInChildren<AudioSource>();
    for (int i = 0; i < audios.Length; i++) Destroy(audios[i]);

    Rigidbody[] rig = g.GetComponentsInChildren<Rigidbody>();
    for (int i = 0; i < rig.Length; i++) Destroy(rig[i]);

    Renderer[] r = g.GetComponentsInChildren<Renderer>();
    for (int i = 0; i < r.Length; i++) {
      r[i].material = menuMat;
      if (r[i].bounds.size.sqrMagnitude > size.sqrMagnitude) {
        size = r[i].bounds.size;
        center = r[i].bounds.center;
      }
    }

    Collider[] c = g.GetComponentsInChildren<Collider>();
    for (int i = 0; i < c.Length; i++) Destroy(c[i]);
    tooltips t = GetComponentInChildren<tooltips>();
    if (t != null) Destroy(t.gameObject);

    g.tag = "Untagged";
    g.transform.localScale = g.transform.localScale / (size.magnitude * 20);
    g.transform.localPosition = g.transform.localPosition + Vector3.forward * .02f;

    if (item == DeviceType.Maracas) g.transform.localPosition = new Vector3(0, -.02f, .02f);

    if (item == DeviceType.Camera) {
      g.transform.localRotation = Quaternion.Euler(90, 0, 0);
      Camera[] cams = g.GetComponentsInChildren<Camera>();
      for (int i = 0; i < cams.Length; i++) Destroy(cams[i].gameObject);
      Destroy(g.transform.Find("screenFrame").gameObject);
    }

    if (item == DeviceType.Keyboard) {
      g.transform.localPosition = new Vector3(0.013f, 0, .026f);
      g.transform.localScale = Vector3.one * .08f;
      Destroy(g.transform.Find("KeyboardTimeline").gameObject);
    }

    if (item == DeviceType.XyloRoll) {
      g.transform.localPosition = new Vector3(.0239f, 0, .02f);
      g.transform.localScale = Vector3.one * .087f;
      g.transform.localRotation = Quaternion.Euler(34, 0, 0);
      Destroy(g.transform.Find("KeyboardTimeline").gameObject);
      Destroy(g.transform.Find("OscillatorBank").gameObject);
      Destroy(g.transform.Find("ADSR").gameObject);
    }

    if (item == DeviceType.MIDIOUT) {
      Destroy(g.transform.Find("CChandle").gameObject);
      Destroy(g.transform.Find("NOTEhandle").gameObject);
    }

    if (item == DeviceType.Airhorn) {
      g.transform.localPosition = new Vector3(-0.005f, -.018f, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 90, 0);
      g.transform.localScale = Vector3.one * .14f;
    }

    if (item == DeviceType.Tapes) {
      g.transform.localPosition = new Vector3(0, 0, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    if (item == DeviceType.Timeline) {
      g.transform.localPosition = new Vector3(0, 0, 0.02f);
      g.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    if (item == DeviceType.Filter) {
      //g.transform.localPosition = new Vector3(.015f, 0, .02f);
      //g.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }
    if (item == DeviceType.Scope)
    {
      //g.transform.localPosition = new Vector3(.015f, 0, .02f);
      //g.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }
    if (item == DeviceType.Multiple) {
      g.transform.localPosition = new Vector3(.0185f, 0, .02f);
    }
    if (item == DeviceType.MultiMix)
    {
      g.transform.localPosition = new Vector3(.0185f, 0, .02f);
    }
    if (item == DeviceType.MultiSplit)
    {
      g.transform.localPosition = new Vector3(.0185f, 0, .02f);
    }
    if (item == DeviceType.Sequencer) {
      g.transform.localScale = Vector3.one * .166f;
    }
    if (item == DeviceType.ControlCube) g.transform.localPosition = new Vector3(0, -.01f, .024f);
    if (item == DeviceType.Reverb) g.transform.localPosition = new Vector3(0, -0.0175f, .02f);
    if (item == DeviceType.Drum) {
      g.transform.localPosition = new Vector3(0, 0, .026f);
      g.transform.localRotation = Quaternion.Euler(40, 0, 0);
    }
    if (item == DeviceType.Mixer) {
      g.transform.localPosition = new Vector3(0.014f, 0, .02f);
      g.transform.localRotation = Quaternion.Euler(60, 0, 0);
    }

    g.SetActive(false);

    return g;
  }

  IEnumerator appearRoutine(bool on) {
    Vector3 destSize = Vector3.one;
    Vector3 startSize = Vector3.zero;
    glowRend.gameObject.SetActive(true);
    if (!on) {
      destSize = Vector3.zero;
      startSize = Vector3.one;
    }

    glowMat.SetColor("_EmissionColor", Color.white);
    float timer = 0;
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
      transform.localScale = Vector3.Lerp(startSize, destSize, timer);
      yield return null;
    }

    timer = 0;
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
      glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, timer));
      yield return null;
    }
    glowRend.gameObject.SetActive(false);
  }

  Coroutine appearCoroutine;
  public void Appear(bool on) {

    if (appearCoroutine != null) StopCoroutine(appearCoroutine);
    appearCoroutine = StartCoroutine(appearRoutine(on));
  }

  void createItem() {

    // Get the direction from the object to the camera
    Vector3 direction = Camera.main.transform.position - transform.position /*+ Vector3.down * 0.10f*/;

    // Generate a quaternion that looks towards the camera
    Quaternion lookRotation = Quaternion.LookRotation(direction);

    GameObject g = Instantiate(itemPrefab, transform.position /*+ new Vector3(-0f, 0f, -0.04f)*/, lookRotation) as GameObject;

    manipulator manip = manipulatorObj.GetComponent<manipulator>();

    // spawn directly into the hand if selecting by gaze
    if (manip != null && manip.wasGazeBased){
      g.transform.position = manip.transform.position + manip.transform.forward * 0.12f;
      manip.wasGazeBased = false; // treat that interaction as a physical one from now on, otherwise it would be handled in fine mode by default
    }

    if (item == DeviceType.Tapes)
    {
      g.transform.Translate(.1f, .02f, .15f, Space.Self);
      g.transform.Rotate(0, 180, 0, Space.Self);
    }

    if (item == DeviceType.ControlCube)
      g.transform.Translate(0f, 0f, -0.15f, Space.Self);

    if (item == DeviceType.XyloRoll)
    {
      g.transform.Rotate(90, 0, 0, Space.Self);
      g.transform.Translate(0.15f, 0f, -0.05f, Space.Self);
    }

    if (item == DeviceType.Drum)
      g.transform.Rotate(90, 0, 0, Space.Self);

    if (item == DeviceType.Keyboard)
      g.transform.Rotate(90, 0, 0, Space.Self);

    if (item == DeviceType.Mixer)
      g.transform.Rotate(90, 0, 0, Space.Self);


    //else if (item != deviceType.Filter && item != deviceType.Scope && item != deviceType.Airhorn && item != deviceType.ADSR) /*g.transform.Rotate(0, 180, 0, Space.Self);*/

    
    if (manip != null && manip.wasGazeBased) g.transform.parent = GameObject.Find("PatchAnchor").transform;  // Directly inject PatchAnchor as parent, since the normal grab and then place back routine is skipped when spawn by gaze
    manip.ForceGrab(g.GetComponentInChildren<handle>());

  }

  public override void setState(manipState state) {
    curState = state;

    if (disabled) return;

    if (curState == manipState.none) {
      label.gameObject.SetActive(true);
      symbol.gameObject.SetActive(true);
      g.SetActive(false);
    } else if (curState == manipState.selected) {
      //symbol.material.SetColor("_TintColor", normalColor);
      //labelMat.SetColor("_TintColor", normalColor);
      label.gameObject.SetActive(true);
      symbol.gameObject.SetActive(true);
      g.SetActive(true);
      manager.SelectAudio();
    } else if (curState == manipState.grabbed) {
      //symbol.material.SetColor("_TintColor", selectColor);
      label.gameObject.SetActive(true);
      symbol.gameObject.SetActive(true);
      g.SetActive(true);
      //labelMat.SetColor("_TintColor", selectColor);
      manager.GrabAudio();
      StartCoroutine(flash());
      createItem();
    }
  }

  IEnumerator flash() {
    float t = 0;
    glowRend.gameObject.SetActive(true);
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 3);
      glowMat.SetFloat("_EmissionGain", Mathf.Lerp(.6f, .2f, t));
      glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, t));
      yield return null;
    }
    glowRend.gameObject.SetActive(false);
  }

}


public enum DeviceCategory
{
  Various,
  Interface,
  Mixing,
  ModulationProcessor,
  ModulationGenerator,
  SoundProcessor,
  SampleGenerator,
  WaveGenerator,
  // this order defines the order in the menu
}

public class DeviceType
{
  private static readonly List<DeviceType> instances = new List<DeviceType>();

  public string Name { get; set; }
  public DeviceCategory Category { get; set; }
  public int Order { get; set; }

  public DeviceType() { }

  private DeviceType(string name, DeviceCategory category, int orderWithinCategory)
  {
    Name = name;
    Category = category;
    Order = orderWithinCategory;
    instances.Add(this);
  }

  // WaveGenerator
  public static readonly DeviceType Maracas = new DeviceType("Maracas", DeviceCategory.WaveGenerator, 3);
  public static readonly DeviceType Noise = new DeviceType("Noise", DeviceCategory.WaveGenerator, 2);
  public static readonly DeviceType Oscillator = new DeviceType("Oscillator", DeviceCategory.WaveGenerator, 1);

  // SampleGenerator
  public static readonly DeviceType Airhorn = new DeviceType("Airhorn", DeviceCategory.SampleGenerator, 5);
  public static readonly DeviceType Looper = new DeviceType("Looper", DeviceCategory.SampleGenerator, 4);
  public static readonly DeviceType Recorder = new DeviceType("Recorder", DeviceCategory.SampleGenerator, 3);
  public static readonly DeviceType Microphone = new DeviceType("Microphone", DeviceCategory.SampleGenerator, 2);
  public static readonly DeviceType Sampler = new DeviceType("Sampler", DeviceCategory.SampleGenerator, 1);

  // ModulationGenerator
  public static readonly DeviceType Sequencer = new DeviceType("Sequencer", DeviceCategory.ModulationGenerator, 6);
  public static readonly DeviceType Timeline = new DeviceType("Timeline", DeviceCategory.ModulationGenerator, 5);
  public static readonly DeviceType ADSR = new DeviceType("ADSR", DeviceCategory.ModulationGenerator, 4);
  public static readonly DeviceType SequencerCV = new DeviceType("SequencerCV", DeviceCategory.ModulationGenerator, 3);
  public static readonly DeviceType DC = new DeviceType("DC", DeviceCategory.ModulationGenerator, 2);
  public static readonly DeviceType AD = new DeviceType("AD", DeviceCategory.ModulationGenerator, 1);

  // SoundProcessor
  public static readonly DeviceType Reverb = new DeviceType("Reverb", DeviceCategory.SoundProcessor, 6);
  public static readonly DeviceType Artefact = new DeviceType("Artefact", DeviceCategory.SoundProcessor, 5);
  public static readonly DeviceType Compressor = new DeviceType("Compressor", DeviceCategory.SoundProcessor, 4);
  public static readonly DeviceType Filter = new DeviceType("Filter", DeviceCategory.SoundProcessor, 3);
  public static readonly DeviceType Delay = new DeviceType("Delay", DeviceCategory.SoundProcessor, 2);
  public static readonly DeviceType Freeverb = new DeviceType("Freeverb", DeviceCategory.SoundProcessor, 1);

  // ModulationProcessor
  public static readonly DeviceType Polarizer = new DeviceType("Polarizer", DeviceCategory.ModulationProcessor, 5);
  public static readonly DeviceType Glide = new DeviceType("Glide", DeviceCategory.ModulationProcessor, 4);
  public static readonly DeviceType SampleHold = new DeviceType("SampleHold", DeviceCategory.ModulationProcessor, 3);
  public static readonly DeviceType Quantizer = new DeviceType("Quantizer", DeviceCategory.ModulationProcessor, 2);
  public static readonly DeviceType VCA = new DeviceType("VCA", DeviceCategory.ModulationProcessor, 1);

  // Mixing
  public static readonly DeviceType Multiple = new DeviceType("Multiple", DeviceCategory.Mixing, 5);
  public static readonly DeviceType Mixer = new DeviceType("Mixer", DeviceCategory.Mixing, 4);
  public static readonly DeviceType Gain = new DeviceType("Gain", DeviceCategory.Mixing, 3);
  public static readonly DeviceType MultiMix = new DeviceType("MultiMix", DeviceCategory.Mixing, 2);
  public static readonly DeviceType MultiSplit = new DeviceType("MultiSplit", DeviceCategory.Mixing, 1);

  // Interface
  public static readonly DeviceType MIDIOUT = new DeviceType("MIDIOUT", DeviceCategory.Interface, 7);
  public static readonly DeviceType MIDIIN = new DeviceType("MIDIIN", DeviceCategory.Interface, 6);
  public static readonly DeviceType XyloRoll = new DeviceType("XyloRoll", DeviceCategory.Interface, 5);
  public static readonly DeviceType Drum = new DeviceType("Drum", DeviceCategory.Interface, 4);
  public static readonly DeviceType Keyboard = new DeviceType("Keyboard", DeviceCategory.Interface, 3);
  public static readonly DeviceType ControlCube = new DeviceType("ControlCube", DeviceCategory.Interface, 2);
  public static readonly DeviceType TouchPad = new DeviceType("TouchPad", DeviceCategory.Interface, 1);

  // Various
  public static readonly DeviceType Pano = new DeviceType("Pano", DeviceCategory.Various, 7);
  public static readonly DeviceType Tapes = new DeviceType("Tapes", DeviceCategory.Various, 6);
  public static readonly DeviceType Camera = new DeviceType("Camera", DeviceCategory.Various, 5);
  public static readonly DeviceType Tutorials = new DeviceType("Tutorials", DeviceCategory.Various, 4);
  public static readonly DeviceType Speaker = new DeviceType("Speaker", DeviceCategory.Various, 3);
  public static readonly DeviceType TapeGroup = new DeviceType("TapeGroup", DeviceCategory.Various, 2);
  public static readonly DeviceType Scope = new DeviceType("Scope", DeviceCategory.Various, 1);

  public static IEnumerable<DeviceType> GetAll(bool sortAlphabetically = false)
  {
    if (sortAlphabetically)
    {
      return instances.OrderBy(d => d.Name).ToList();
    }
    else
    {
      return instances.OrderBy(d => d.Category).ThenBy(d => d.Order).ToList();
    }
  }

  public static IEnumerable<DeviceType> GetAllByCategory(DeviceCategory category, bool sortAlphabetically = false)
  {
    var filteredDevices = instances.Where(d => d.Category == category);

    if (sortAlphabetically)
    {
      return filteredDevices.OrderBy(d => d.Name).ToList();
    }
    else
    {
      return filteredDevices.OrderBy(d => d.Order).ToList();
    }
  }

  public static implicit operator string(DeviceType deviceType) => deviceType.Name;

  public override string ToString() => Name;
}

