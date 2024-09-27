// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
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

﻿using UnityEngine;
using System.Collections;

public class prefButton : manipObject {

    public pauseMenu menu;
    public Renderer gearRenderer;
    menuManager manager;

    Material mat;
    Color normalColor;
    Color selectColor;
    Color grabColor;

    bool tipsOn = false;

    public override void Awake()
    {
        base.Awake();
        normalColor = Color.HSVToRGB(0.6f, 1f, 0.5f);
        selectColor = Color.HSVToRGB(0.6f, 1f, 0.7f);
        grabColor = Color.HSVToRGB(0.6f, 1f, 1f);

        mat = gearRenderer.sharedMaterial;
        mat.SetColor("_TintColor", normalColor);
        manager = transform.parent.parent.GetComponent<menuManager>();
    }

    public override void setState(manipState state)
    {
        if (curState == state) return;

        curState = state;

        if (curState == manipState.none)
        {
            mat.SetColor("_TintColor", normalColor);
        }
        else if (curState == manipState.selected)
        {
            mat.SetColor("_TintColor", selectColor);
            manager.SelectAudio();
        }
        else if (curState == manipState.grabbed)
        {
            mat.SetColor("_TintColor", grabColor);
            menu.toggleMenu();
            manager.GrabAudio();
        }
    }

  public override void onTouch(bool enter, manipulator m)
  {
    if (enter)
    {
      if (m != null)
      {
        if (m.emptyGrab)
        {
          setState(manipState.grabbed);
          m.hapticPulse(1000);
        }
      }
    }
    else
    {
      setState(manipState.none);
    }
  }
}
