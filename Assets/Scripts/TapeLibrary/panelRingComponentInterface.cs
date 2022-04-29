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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class panelRingComponentInterface : componentInterface {

  public GameObject panelPrefab;

  public bool secondary = false;
  public libraryDeviceInterface _deviceInterface;
  public List<libraryPanel> panels = new List<libraryPanel>();

  int panelMax = 24;
  float panelRadius = 1;
  public List<string> labels;

  int curSelect = -1;

  void Awake() {
    if (secondary) panelMax = 30;
    else panelMax = 20;
    _deviceInterface = GetComponentInParent<libraryDeviceInterface>();
  }

  void addPanel(bool on) {
    if (!on) {
      if (panelMax < 6) return;
      panelMax--;
      Destroy(panels[0].gameObject);
      panels.RemoveAt(0);
    } else {
      panelMax++;

      //create one
      GameObject g = Instantiate(panelPrefab, transform, false) as GameObject;
      int newID = panels[0].buttonID - 1;
      panels.Insert(0, g.GetComponent<libraryPanel>());

      if (newID >= 0) {
        panels.First().Setup(transform.parent, panelRadius, newID, labels[newID], secondary);
      } else {
        panels.First().Setup(transform.parent, panelRadius, newID, "", secondary);
        panels.First().SetActive(false);
      }
    }

    int tempOffset = Mathf.Clamp(labels.Count / 2, 0, panelMax / 3);
    for (int i = 0; i < panelMax; i++) {
      int id = panels[i].buttonID;
      Quaternion q = Quaternion.Euler(180f / panelMax * (id - panelMax / 2f) + 90f / panelMax, 0, 0);
      panels[i].transform.localPosition = q * Vector3.forward * panelRadius;
      panels[i].transform.localRotation = q;

    }
  }

  public void loadPanels(float pR) {
    transform.localRotation = Quaternion.identity;

    panelRadius = pR;
    for (int i = 0; i < panelMax; i++) {
      GameObject g = Instantiate(panelPrefab, transform, false) as GameObject;
      Quaternion q = Quaternion.Euler(180f / panelMax * (i - panelMax / 2) + 90f / panelMax, 0, 0);
      g.transform.localPosition = q * Vector3.forward * panelRadius;
      g.transform.localRotation = q;
      panels.Add(g.GetComponent<libraryPanel>());

      if (i < labels.Count) {
        panels.Last().Setup(transform.parent, panelRadius, i, labels[i], secondary);
      } else {
        panels.Last().Setup(transform.parent, panelRadius, i, "", secondary);
        panels.Last().SetActive(false);
      }
    }

    float offset = Mathf.Clamp(labels.Count, 0, panelMax) / (float)panelMax;
    transform.localRotation = Quaternion.Euler(90 * (1 - offset), 0, 0);
  }

  void addElement(bool on, string s) {
    if (on) {
      labels.Add(s);
      refreshPanels();
    } else if (labels.Count > 0) {
      labels.Remove(s);
      int shift = 0;
      if (panels.Last().buttonID > labels.Count - 1 && panels[0].buttonID >= 0) shift = -1;
      refreshPanels(shift);
    }
  }

  void resetPanels(int newID = 0) {
    curSelect = -1;
    transform.localRotation = Quaternion.identity;
    for (int i = 0; i < panelMax; i++) {
      Vector3 pos;
      Quaternion rot;
      Quaternion q = Quaternion.Euler(180f / panelMax * (i - panelMax / 2) + 90f / panelMax, 0, 0);
      panels[i].transform.localPosition = q * Vector3.forward * panelRadius;
      panels[i].transform.localRotation = q;

      if (i < labels.Count) panels[i].Setup(transform.parent, panelRadius, i, labels[i], secondary, false);
      else panels[i].Setup(transform.parent, panelRadius, i, "", secondary, false);

      bool temp = requestNewID(panels[i], panels[i].buttonID, 0, out pos, out rot);
      panels[i].SetActive(temp);

    }

    float offset = Mathf.Clamp(labels.Count, 0, panelMax) / (float)panelMax;
    transform.localRotation = Quaternion.Euler(90 * (1 - offset), 0, 0);
  }

  void refreshPanels(int shift = 0) {

    for (int i = 0; i < panelMax; i++) {
      Vector3 pos;
      Quaternion rot;
      bool temp = requestNewID(panels[i], panels[i].buttonID + shift, 0, out pos, out rot);
      panels[i].SetActive(temp);
    }
  }

  public void updatePanels(List<string> l) {
    labels = l;
    resetPanels();
  }

  public bool requestNewID(libraryPanel l, int oldID, int dir, out Vector3 pos, out Quaternion rot) {
    int newID = oldID;
    if (dir > 0) {
      panels.RemoveAt(panels.Count - 1);
      newID = panels.First().buttonID - dir;
      panels.Insert(0, l);
    } else if (dir < 0) {
      panels.RemoveAt(0);
      newID = panels.Last().buttonID - dir;
      panels.Add(l);
    }

    rot = Quaternion.Euler(180f / panelMax * (newID - panelMax / 2) + 90f / panelMax, 0, 0);
    pos = rot * Vector3.forward * panelRadius;

    _deviceInterface.spinLocks[0] = newID < -panelMax / 2; ;
    _deviceInterface.spinLocks[1] = newID > labels.Count + panelMax / 2; ;

    if (newID < 0 || newID >= labels.Count) {
      l.setNewID(newID, "");
      return false;
    } else {
      l.setNewID(newID, labels[newID], newID == curSelect);
      return true;
    }
  }

  public override void hit(bool on, int ID = -1) {
    if (!on) return;

    curSelect = ID;
    for (int i = 0; i < panels.Count; i++) {
      if (panels[i].buttonID != ID) panels[i].keyHit(false);
      else _deviceInterface.panelEvent(ID, secondary, i);
    }
  }
}
