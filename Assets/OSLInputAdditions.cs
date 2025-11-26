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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class OSLInput
{

    private static OSLInput instance;

    public static OSLInput getInstance()
    {
        if (instance == null)
        {
            instance = new OSLInput();
            instance.Enable();
        }
        return instance;
    }

    public bool isMenuStarted(int controllerIndex)
    {
        return (Patcher.PrimaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.PrimaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool isTriggerStarted(int controllerIndex)
    {
        bool leftPressed = controllerIndex == 0 && Patcher.TriggerLeft.WasPressedThisFrame();
        bool rightPressed = controllerIndex == 1 && Patcher.TriggerRight.WasPressedThisFrame();
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return leftPressed || rightPressed || mousePressed;
    }

    public bool isTriggerReleased(int controllerIndex)
    {
        bool leftReleased = controllerIndex == 0 && Patcher.TriggerLeft.WasReleasedThisFrame();
        bool rightReleased = controllerIndex == 1 && Patcher.TriggerRight.WasReleasedThisFrame();
        bool mouseReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        return leftReleased || rightReleased || mouseReleased;
    }

    public bool isCopyStarted(int controllerIndex)
    {
        return (Patcher.SecondaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool isCopyReleased(int controllerIndex)
    {
        return (Patcher.SecondaryLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

    public bool isAnyTriggerFullPressed()
    {
        return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f || Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }

    public bool areBothTriggersFullPressed()
    {
        return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f && Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }


    public bool isTriggerHalfPressed(int controllerIndex)
    {
        if (controllerIndex == 0)
        {
            return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.05f && Patcher.TriggerLeftAnalog.ReadValue<float>() <= 0.7f;
        }
        else if (controllerIndex == 1)
        {
            return Patcher.TriggerRightAnalog.ReadValue<float>() > 0.05f && Patcher.TriggerRightAnalog.ReadValue<float>() <= 0.7f;
        }
        return false;
    }

    public bool isTriggerFullPressed(int controllerIndex)
    {
        if (controllerIndex == 0)
        {
            return Patcher.TriggerLeftAnalog.ReadValue<float>() >= 0.7f;
        }
        else if (controllerIndex == 1)
        {
            return Patcher.TriggerRightAnalog.ReadValue<float>() >= 0.7f;
        }
        return false;
    }

    public bool isTriggerPressed(int controllerIndex)
    {
        if (controllerIndex == 0)
        {
            return Patcher.TriggerLeftAnalog.ReadValue<float>() >= 0.05f;
        }
        else if (controllerIndex == 1)
        {
            return Patcher.TriggerRightAnalog.ReadValue<float>() >= 0.05f;
        }
        return false;
    }

    public bool isSidePressed(int controllerIndex)
    {
        if (controllerIndex == 0)
        {
            return Patcher.GripLeft.ReadValue<float>() >= 0.1f;
        }
        else if (controllerIndex == 1)
        {
            return Patcher.GripRight.ReadValue<float>() >= 0.1f;
        }
        return false;
    }

    public bool areBothSidesPressed()
    {
        return Patcher.GripLeft.ReadValue<float>() >= 0.1f && Patcher.GripRight.ReadValue<float>() >= 0.1f;
    }

    public bool isSecondaryPressed(int controllerIndex)
    {
        if (controllerIndex == 0)
        {
            return Patcher.SecondaryLeft.IsPressed();
        }
        else if (controllerIndex == 1)
        {
            return Patcher.SecondaryRight.IsPressed();
        }
        return false;
    }

    public bool isAnySecondaryPressed()
    {
        return Patcher.SecondaryLeft.IsPressed() || Patcher.SecondaryRight.IsPressed();
    }


}
