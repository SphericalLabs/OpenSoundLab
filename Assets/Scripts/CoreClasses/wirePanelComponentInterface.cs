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

public class wirePanelComponentInterface : componentInterface {
  public pauseMenu rootMenu;
  public menuManager menuMgr;
  public slider glowSlider;

  public UIpanel[] panels;
  int curSelect = 0;

  public uiPanelSinglePress handlepanel, jackpanel, midipanel;

  Color colorGreen = Color.HSVToRGB(.4f, 230f / 255, 118f / 255);
  Color colorRed = Color.HSVToRGB(0f, 230f / 255, 118f / 255);

  void Start() {
    //midipanel.newColor(colorGreen);
    //jackpanel.newColor(colorGreen);
    //handlepanel.newColor(colorGreen);

    glowSlider.setPercent(masterControl.instance.glowVal);
    for (int i = 0; i < panels.Length; i++) {
      panels[i].keyHit(i == curSelect);
    }

    if (PlayerPrefs.GetInt("midiOut") == 1) {
      string s = "DISABLE MIDI OUT";
      midipanel.label.text = s;
      midipanel.newColor(Color.HSVToRGB(0f, 230f / 255, 118f / 255));
    }

  }

  void Update() {
    if (glowSlider.percent != masterControl.instance.glowVal) {
      masterControl.instance.setGlowLevel(glowSlider.percent);
    }
  }

  public override void hit(bool on, int ID = -1) {
    if (!on) return;
    if (ID == -2) //okay
    {
      rootMenu.cancelFileMenu(); //not right
      transform.gameObject.SetActive(false);
    } else if (ID == 3) {
      bool b = !masterControl.instance.handlesEnabled;
      masterControl.instance.toggleHandles(b);
      string s = b ? "ENABLE POS LOCK" : "DISABLE POS LOCK";
      handlepanel.label.text = s;
      handlepanel.newColor(b ? colorGreen : colorRed);
    } else if (ID == 4) {
      bool b = !masterControl.instance.jacksEnabled;
      masterControl.instance.toggleJacks(b);
      string s = b ? "ENABLE JACK LOCK" : "DISABLE JACK LOCK";
      jackpanel.label.text = s;
      jackpanel.newColor(b ? colorGreen : colorRed);
    } else if (ID == 5) {
      bool b = !menuMgr.midiOutEnabled;
      menuMgr.toggleMidiOut(b);
      string s = b ? "DISABLE MIDI OUT" : "ENABLE MIDE OUT";
      midipanel.label.text = s;
      midipanel.newColor(b ? colorRed : colorGreen);
    } else {
      curSelect = ID;
      masterControl.instance.updateWireSetting(curSelect);
      for (int i = 0; i < panels.Length; i++) {
        if (i != ID) panels[i].keyHit(false);
      }
    }
  }
}
