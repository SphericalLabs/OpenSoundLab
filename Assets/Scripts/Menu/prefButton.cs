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
}
