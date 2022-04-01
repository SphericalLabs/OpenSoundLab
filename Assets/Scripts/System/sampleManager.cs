// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;

public class sampleManager : MonoBehaviour {
  public static sampleManager instance;

  public Dictionary<string, Dictionary<string, string>> sampleDictionary;

  List<string> customSamples = new List<string>();

  public void ClearCustomSamples() {
    for (int i = 0; i < customSamples.Count; i++) {
      samplerLoad[] samplers = FindObjectsOfType<samplerLoad>();
      for (int i2 = 0; i2 < samplers.Length; i2++) {
        if (samplers[i2].CurTapeLabel == customSamples[i]) {
          samplers[i2].ForceEject();
        }
      }

      sampleDictionary["Custom"].Remove(customSamples[i]);
    }

    PlayerPrefs.DeleteAll();
    customSamples.Clear();

    libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
    for (int i2 = 0; i2 < libs.Length; i2++) {
      if (libs[i2].curPrimary == "Custom") libs[i2].updateSecondaryPanels("Custom");

    }
  }

  public string parseFilename(string f) {

    if (f == "") return "";

        if (f.Substring(0, 3) == "APP") {
      f = f.Remove(0, 3);
      f = f.Insert(0, Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "samples");
    } else if (f.Substring(0, 3) == "DOC") {
      f = f.Remove(0, 3);
      f = f.Insert(0, masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Samples");
    }

    return f;
  }

  public void AddSample(string newsample) {
    if (sampleDictionary["Custom"].ContainsKey(Path.GetFileNameWithoutExtension(newsample))) return;

    if (!File.Exists(newsample)) {
      return;
    }

    customSamples.Add(Path.GetFileNameWithoutExtension(newsample));

    sampleDictionary["Custom"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

    libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
    for (int i2 = 0; i2 < libs.Length; i2++) {
      if (libs[i2].curPrimary == "Custom") libs[i2].updateSecondaryPanels("Custom");
    }

    if (!inStartup) {
      customSampleCount++;
      PlayerPrefs.SetInt("sampCount", customSampleCount);
      PlayerPrefs.SetString("samp" + customSampleCount, newsample);
    }
  }

  public void AddRecording(string newsample) {
    if (sampleDictionary["Recordings"].ContainsKey(Path.GetFileNameWithoutExtension(newsample))) {
      return;
    }

    sampleDictionary["Recordings"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

    libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
    for (int i2 = 0; i2 < libs.Length; i2++) {
      if (libs[i2].curPrimary == "Recordings") libs[i2].updateSecondaryPanels("Recordings");
    }
  }

  public void AddSession(string newsample)
  {
    if (sampleDictionary["Sessions"].ContainsKey(Path.GetFileNameWithoutExtension(newsample)))
    {
      return;
    }

    sampleDictionary["Sessions"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

    libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
    for (int i2 = 0; i2 < libs.Length; i2++)
    {
      if (libs[i2].curPrimary == "Sessions") libs[i2].updateSecondaryPanels("Sessions");
    }
  }

  int customSampleCount = 0;
  bool inStartup = false;
  void AddCustomSamples() {
    inStartup = true;

    if (!PlayerPrefs.HasKey("sampCount")) PlayerPrefs.SetInt("sampCount", 0);

    customSampleCount = PlayerPrefs.GetInt("sampCount");
    for (int i = 0; i < customSampleCount; i++) {

      AddSample(PlayerPrefs.GetString("samp" + (i + 1)));
    }
    inStartup = false;
  }

  void loadSampleDictionary(string dir, string pathtype) {
    if (Directory.Exists(dir)) {
      string[] subdirs = Directory.GetDirectories(dir);
      for (int i = 0; i < subdirs.Length; i++) {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string s = subdirs[i].Replace(dir + "\\", "");
#elif UNITY_ANDROID
                string s = subdirs[i].Replace(dir + "/", "");
#endif
                sampleDictionary[s] = new Dictionary<string, string>();

        for (int i2 = 0; i2 < 3; i2++) {
          string[] subdirFiles = Directory.GetFiles(subdirs[i], fileEndings[i2]);
          foreach (string d in subdirFiles) {
            sampleDictionary[s][Path.GetFileNameWithoutExtension(d)] = pathtype + Path.DirectorySeparatorChar + s + Path.DirectorySeparatorChar + Path.GetFileName(d);
          }
        }
      }

    } else {
      Debug.Log("NO SAMPLES FOLDER FOUND");
    }
  }

    string[] fileEndings = new string[] { "*.wav", "*.ogg", "*.mp3" };

  public void Init() {

#if UNITY_ANDROID
        //if Samples directory doesn't exist, extract default data...
        if (Directory.Exists(Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples") == false)
        {
            Directory.CreateDirectory(Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples");
            //copy tgz to directory where we can extract it
            WWW www = new WWW(Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Samples.tgz");
            while (!www.isDone) { }
            System.IO.File.WriteAllBytes(Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples.tgz", www.bytes);
            //extract it
            Utility_SharpZipCommands.ExtractTGZ(Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples.tgz", Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples");
            //delete tgz
            File.Delete(Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples.tgz");
        }
#endif

        instance = this;
    sampleDictionary = new Dictionary<string, Dictionary<string, string>>();

    string dir = Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "Samples";
    loadSampleDictionary(dir, "APP");


    dir = masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Samples";
    Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + "Custom");
    Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + "Recordings");
    Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + "Sessions");
    loadSampleDictionary(dir, "DOC");
    AddCustomSamples();

  }
}
