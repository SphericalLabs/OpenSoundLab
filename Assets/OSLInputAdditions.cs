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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class OSLInput
{

    private static OSLInput instance;
    private HandInputAdapter handInputAdapter;
    private int handStateFrame = -1;
    private bool handsActive;
    private float handPressThreshold = 0.05f;
    private float handFullThreshold = 0.7f;
    private float handGripThreshold = 0.1f;
    private bool[] handTriggerPressed = new bool[2];
    private bool[] handTriggerStarted = new bool[2];
    private bool[] handTriggerReleased = new bool[2];
    private float[] handTriggerValues = new float[2];
    private float[] handGripValues = new float[2];
    private bool[] handPrimaryPressed = new bool[2];
    private bool[] handPrimaryStarted = new bool[2];
    private bool[] handPrimaryReleased = new bool[2];
    private bool[] handSecondaryPressed = new bool[2];
    private bool[] handSecondaryStarted = new bool[2];
    private bool[] handSecondaryReleased = new bool[2];
    private float[] handThumbPinch = new float[2];
    private float[] handMiddlePinch = new float[2];
    private float[] handRingPinch = new float[2];
    private float[] handPinkyPinch = new float[2];

    public static OSLInput getInstance()
    {
        if (instance == null)
        {
            instance = new OSLInput();
            instance.Enable();
        }
        return instance;
    }

    private HandInputAdapter getHandAdapter()
    {
        if (handInputAdapter != null) return handInputAdapter;
        handInputAdapter = HandInputAdapter.Instance;
        if (handInputAdapter == null) handInputAdapter = UnityEngine.Object.FindObjectOfType<HandInputAdapter>();
        return handInputAdapter;
    }

    private void resetHandState()
    {
        handsActive = false;
        Array.Clear(handTriggerPressed, 0, handTriggerPressed.Length);
        Array.Clear(handTriggerStarted, 0, handTriggerStarted.Length);
        Array.Clear(handTriggerReleased, 0, handTriggerReleased.Length);
        Array.Clear(handTriggerValues, 0, handTriggerValues.Length);
        Array.Clear(handGripValues, 0, handGripValues.Length);
        Array.Clear(handPrimaryPressed, 0, handPrimaryPressed.Length);
        Array.Clear(handPrimaryStarted, 0, handPrimaryStarted.Length);
        Array.Clear(handPrimaryReleased, 0, handPrimaryReleased.Length);
        Array.Clear(handSecondaryPressed, 0, handSecondaryPressed.Length);
        Array.Clear(handSecondaryStarted, 0, handSecondaryStarted.Length);
        Array.Clear(handSecondaryReleased, 0, handSecondaryReleased.Length);
        Array.Clear(handThumbPinch, 0, handThumbPinch.Length);
        Array.Clear(handMiddlePinch, 0, handMiddlePinch.Length);
        Array.Clear(handRingPinch, 0, handRingPinch.Length);
        Array.Clear(handPinkyPinch, 0, handPinkyPinch.Length);
    }

    private void cacheHandState()
    {
        if (handStateFrame == Time.frameCount) return;
        handStateFrame = Time.frameCount;

        HandInputAdapter adapter = getHandAdapter();
        handsActive = adapter != null && adapter.handsActiveThisFrame;
        if (!handsActive)
        {
            resetHandState();
            return;
        }

        float pressThreshold = adapter.pinchThreshold;
        float fullThreshold = adapter.fullPinchThreshold;
        float gripThreshold = adapter.fistThreshold;

        for (int i = 0; i < 2; i++)
        {
            float trigger = adapter.getIndexPinchStrength(i);
            float primary = adapter.getMiddlePinchStrength(i);
            float secondary = adapter.getRingPinchStrength(i);
            float thumb = adapter.getThumbPinchStrength(i);
            float middle = adapter.getMiddlePinchStrength(i);
            float ring = adapter.getRingPinchStrength(i);
            float pinky = adapter.getPinkyPinchStrength(i);
            float fist = adapter.getFistStrength(i);

            bool triggerPressed = trigger >= pressThreshold;
            bool primaryPressed = primary >= pressThreshold;
            bool secondaryPressed = secondary >= pressThreshold;

            handTriggerStarted[i] = !handTriggerPressed[i] && triggerPressed;
            handTriggerReleased[i] = handTriggerPressed[i] && !triggerPressed;
            handTriggerPressed[i] = triggerPressed;
            handTriggerValues[i] = trigger;

            handPrimaryStarted[i] = !handPrimaryPressed[i] && primaryPressed;
            handPrimaryReleased[i] = handPrimaryPressed[i] && !primaryPressed;
            handPrimaryPressed[i] = primaryPressed;

            handSecondaryStarted[i] = !handSecondaryPressed[i] && secondaryPressed;
            handSecondaryReleased[i] = handSecondaryPressed[i] && !secondaryPressed;
            handSecondaryPressed[i] = secondaryPressed;

            handGripValues[i] = fist;
            handThumbPinch[i] = thumb;
            handMiddlePinch[i] = middle;
            handRingPinch[i] = ring;
            handPinkyPinch[i] = pinky;
        }

        handFullThreshold = fullThreshold;
        handPressThreshold = pressThreshold;
        handGripThreshold = gripThreshold;
    }

    public bool isUsingHands()
    {
        cacheHandState();
        return handsActive;
    }

    public bool isMenuStarted(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1) && handPrimaryStarted[controllerIndex])
        {
            return true;
        }
        return (Patcher.PrimaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.PrimaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool isTriggerStarted(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1) && handTriggerStarted[controllerIndex])
        {
            return true;
        }
        bool leftPressed = controllerIndex == 0 && Patcher.TriggerLeft.WasPressedThisFrame();
        bool rightPressed = controllerIndex == 1 && Patcher.TriggerRight.WasPressedThisFrame();
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return leftPressed || rightPressed || mousePressed;
    }

    public bool isTriggerReleased(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1) && handTriggerReleased[controllerIndex])
        {
            return true;
        }
        bool leftReleased = controllerIndex == 0 && Patcher.TriggerLeft.WasReleasedThisFrame();
        bool rightReleased = controllerIndex == 1 && Patcher.TriggerRight.WasReleasedThisFrame();
        bool mouseReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        return leftReleased || rightReleased || mouseReleased;
    }

    public bool isCopyStarted(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1) && handSecondaryStarted[controllerIndex])
        {
            return true;
        }
        return (Patcher.SecondaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool isCopyReleased(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1) && handSecondaryReleased[controllerIndex])
        {
            return true;
        }
        return (Patcher.SecondaryLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

    public bool isAnyTriggerFullPressed()
    {
        cacheHandState();
        if (handsActive) return handTriggerValues[0] > handFullThreshold || handTriggerValues[1] > handFullThreshold;
        return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f || Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }

    public bool areBothTriggersFullPressed()
    {
        cacheHandState();
        if (handsActive) return handTriggerValues[0] > handFullThreshold && handTriggerValues[1] > handFullThreshold;
        return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f && Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }


    public bool isTriggerHalfPressed(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1))
        {
            return handTriggerValues[controllerIndex] > handPressThreshold && handTriggerValues[controllerIndex] <= handFullThreshold;
        }
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
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1))
        {
            return handTriggerValues[controllerIndex] >= handFullThreshold;
        }
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
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1))
        {
            return handTriggerValues[controllerIndex] >= handPressThreshold;
        }
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
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1))
        {
            return handGripValues[controllerIndex] >= handGripThreshold;
        }
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
        cacheHandState();
        if (handsActive) return handGripValues[0] >= handGripThreshold && handGripValues[1] >= handGripThreshold;
        return Patcher.GripLeft.ReadValue<float>() >= 0.1f && Patcher.GripRight.ReadValue<float>() >= 0.1f;
    }

    public bool isSecondaryPressed(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1))
        {
            return handSecondaryPressed[controllerIndex];
        }
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
        cacheHandState();
        if (handsActive) return handSecondaryPressed[0] || handSecondaryPressed[1];
        return Patcher.SecondaryLeft.IsPressed() || Patcher.SecondaryRight.IsPressed();
    }

    public float getThumbPinchStrength(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1)) return handThumbPinch[controllerIndex];
        return 0f;
    }

    public float getMiddlePinchStrength(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1)) return handMiddlePinch[controllerIndex];
        return 0f;
    }

    public float getRingPinchStrength(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1)) return handRingPinch[controllerIndex];
        return 0f;
    }

    public float getPinkyPinchStrength(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1)) return handPinkyPinch[controllerIndex];
        return 0f;
    }

    public float getFistStrength(int controllerIndex)
    {
        cacheHandState();
        if (handsActive && (controllerIndex == 0 || controllerIndex == 1)) return handGripValues[controllerIndex];
        return 0f;
    }


}
