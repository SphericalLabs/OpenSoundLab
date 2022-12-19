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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class uiPanelComponentInterface : componentInterface {
  public GameObject panelPrefab, loadButton, saveButton, cancelButton, diskPrefab;
  public pauseMenu rootMenu;
  public TextMesh currentText;
  public menuManager menuMgr;
  public Material previewMat;

  List<UIpanel> panels;
  List<string> filenames;
  List<Transform> panelNodes;

  public GameObject oldsavenote;

  int columnLength = 6;

  int curSelect = -1;

  bool initialized = false;
  void Start() {
    panels = new List<UIpanel>();
    panelNodes = new List<Transform>();
    filenames = new List<string>();
    loadButton.SetActive(false);
    saveButton.SetActive(false);

    spawnPanels();
    initialized = true;
  }

  void Update() {
    if (previewTransform != null) previewTransform.Rotate(0, Time.deltaTime * 30, 0, Space.Self);
  }

  public void refreshFiles(bool saving = false) {
    saveMode = saving;
    clearPanels();
    spawnPanels();
  }

  void clearPanels() {
    if (!initialized) return;
    if (previewTransform != null) {
      Destroy(previewTransform.gameObject);
      oldsavenote.SetActive(false);
    }
    loadButton.SetActive(false);
    saveButton.SetActive(false);
    panels.Clear();
    filenames.Clear();
    for (int i = 0; i < panelNodes.Count; i++) Destroy(panelNodes[i].gameObject);
    panelNodes.Clear();
  }

  bool saveMode = false;

  void spawnPanels() {
    string dir = masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Saves";
    Directory.CreateDirectory(dir);
    List<string> files = new List<string>(Directory.GetFiles(dir, "*.xml"));

    if (saveMode) files.Add("[new file]");

    int panelHeightCount = files.Count;

    if (files.Count == 0) currentText.text = "NO SAVED FILES";
    else if (masterControl.instance.currentScene == "") currentText.text = "CURRENT: NONE";
    else if (masterControl.instance.currentScene.Contains("Example")) currentText.text = "CURRENT: EXAMPLE";
    else currentText.text = "CURRENT";


    int nodeIndex = -1;
    int miniIndex = 0;
    bool resetMiniIndex = false;
    for (int i = 0; i < files.Count; i++) {
      filenames.Add(files[i]);
      string s = "+ [new file]";
      if (files[i] != "[new file]") s = Path.GetFileNameWithoutExtension(files[i]);
      float yMod = 0;
      if (miniIndex == 0 && !resetMiniIndex) {
        nodeIndex++;
        resetMiniIndex = true;
        GameObject g = new GameObject("panelColumn" + nodeIndex);
        panelNodes.Add(g.transform);
        g.transform.parent = transform;
        float xMod = 1;
        if (panelHeightCount < 6) yMod = -.02f * (6 - panelHeightCount);
        g.transform.localPosition = new Vector3(-.13f * (nodeIndex - xMod), .06f + yMod, 0);
        currentText.transform.localPosition = new Vector3(.19f, .08f + yMod, 0);
        g.transform.localRotation = Quaternion.identity;
      }

      if (files[i] == masterControl.instance.currentScene && !files[i].Contains("Example")) {
        panels.Add((Instantiate(panelPrefab, transform, false) as GameObject).GetComponent<UIpanel>());
        panels[i].transform.localPosition = new Vector3(0.08f, .082f + yMod, 0);
        panels[i].transform.parent = panelNodes[nodeIndex];
      } else {
        resetMiniIndex = false;
        panels.Add((Instantiate(panelPrefab, panelNodes[nodeIndex], false) as GameObject).GetComponent<UIpanel>());
        panels[i].transform.localPosition = Vector3.up * -.02f * miniIndex;
        miniIndex = (miniIndex + 1) % columnLength;
      }

      if (s.Length > 25) {
        s = s.Substring(0, 25);
        s += "...";
      }

      panels[i]._componentInterface = this;
      panels[i].buttonID = i;
      panels[i].setText(s);
    }
  }

  public void cancel() {
    clearPanels();
    transform.gameObject.SetActive(false);
  }
  public override void hit(bool on, int ID = -1) {
    if (!on) return;
    if (ID == -4) //save
    {
      rootMenu.saveFile(filenames[curSelect]);
      clearPanels();
      transform.gameObject.SetActive(false);
    } else if (ID == -2) //load
      {
      rootMenu.loadFile(filenames[curSelect]);
      clearPanels();
      transform.gameObject.SetActive(false);
    } else if (ID == -3) //cancel
      {
      rootMenu.cancelFileMenu();
      cancel();
    } else {
      if (saveMode) saveButton.SetActive(true);
      else loadButton.SetActive(true);
      curSelect = ID;
      if (filenames[curSelect] != "[new file]") {
        updatePreview();
      } else {
        if (previewTransform != null) {
          Destroy(previewTransform.gameObject);
          oldsavenote.SetActive(false);
        }
      }
      for (int i = 0; i < panels.Count; i++) {
        if (i != ID) panels[i].keyHit(false);
      }
    }
  }

  Transform previewTransform;
  void updatePreview() {
    if (previewTransform != null) Destroy(previewTransform.gameObject);

    previewTransform = (new GameObject("preview")).transform;
    previewTransform.SetParent(transform, false);
    previewTransform.localPosition = new Vector3(.11f, -.15f, -.04f);
    GameObject disk = Instantiate(diskPrefab, previewTransform, false) as GameObject;
    previewTransform.localScale = Vector3.one * .05f;
    disk.transform.localScale = new Vector3(100, 20, 100);
    disk.GetComponent<Renderer>().material = previewMat;
    bool uptodatesave = SaveLoadInterface.instance.PreviewLoad(filenames[curSelect], previewTransform);
    oldsavenote.SetActive(!uptodatesave);
  }
}

