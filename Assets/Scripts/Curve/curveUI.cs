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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class curveUI : manipObject
{
    public GameObject posIndicator;
    public override void Awake()
    {
        base.Awake();
        posIndicator.SetActive(false);
    }

    public override void selectUpdate(Transform t)
    {
        updatePosIndicator(t.position);
    }

    public override void grabUpdate(Transform t)
    {
        updatePosIndicator(t.position);
    }

    void updatePosIndicator(Vector3 worldpos)
    {
        Vector3 pos = transform.InverseTransformPoint(worldpos);
        pos.z = 0;
        pos.x = Mathf.Clamp(pos.x, -.5f, .5f);
        pos.y = Mathf.Clamp(pos.y, -.5f, .5f);
        posIndicator.transform.localPosition = pos;
    }

    public override void setState(manipState state)
    {
        curState = state;
        if (curState == manipState.none)
        {
            posIndicator.SetActive(false);
        }
        else if (curState == manipState.selected)
        {
            posIndicator.SetActive(true);
            if (selectObj != null) updatePosIndicator(selectObj.position);
        }
        else if (curState == manipState.grabbed)
        {
            posIndicator.SetActive(true);
            if (manipulatorObj != null) updatePosIndicator(manipulatorObj.position);
        }
    }
}
