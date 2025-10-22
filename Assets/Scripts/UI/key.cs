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

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/*
 * Overview:
 *  - Represents a single keyboard key mesh and forwards its state to the parent keyboard device.
 *  - Tracks touch/trigger manipulator presence, latch visuals, and recent-note highlight material.
 *  - Resolves the appropriate material each frame so visuals stay in sync with key and latch states.
 */

public class key : manipObject
{

    public int keyValue = 0;
    public Material onMat;
    Renderer rend;
    Material offMat;
    Material glowMat;
    deviceInterface _deviceInterface;
    Material recentHighlightMaterial;
    keyboardDeviceInterface keyboardInterface;

    bool visualDirty = true;
    bool selectionHighlight;

    readonly HashSet<manipulator> insideManipulators = new HashSet<manipulator>();
    readonly HashSet<manipulator> activeManipulators = new HashSet<manipulator>();

    struct ManipulatorSubscription
    {
        public UnityAction triggerAction;
        public UnityAction releaseAction;
    }

    readonly Dictionary<manipulator, ManipulatorSubscription> manipulatorSubscriptions = new Dictionary<manipulator, ManipulatorSubscription>();

    Material latchedVisualMaterial;
    bool latchedActive;

    public bool sticky = true;
    public bool isKeyboard = false;

    public UnityEvent onKeyChangedEvent;

    public override void Awake()
    {
        base.Awake();
        _deviceInterface = transform.parent.GetComponent<deviceInterface>();
        keyboardInterface = _deviceInterface as keyboardDeviceInterface;
        rend = GetComponent<Renderer>();
        offMat = rend.sharedMaterial;
        glowMat = new Material(onMat);
    }

    bool HasActiveManipulator => activeManipulators.Count > 0;

    void MarkVisualDirty()
    {
        visualDirty = true;
    }

    Material BaseMaterial => recentHighlightMaterial != null ? recentHighlightMaterial : offMat;

    Material ResolveTargetMaterial()
    {
        if (latchedActive && isHit && !HasActiveManipulator && latchedVisualMaterial != null)
        {
            return latchedVisualMaterial;
        }

        if (isHit || HasActiveManipulator || selectionHighlight)
        {
            return glowMat;
        }

        return BaseMaterial;
    }

    void UpdateVisualState()
    {
        if (!visualDirty)
        {
            return;
        }

        visualDirty = false;
        Material target = ResolveTargetMaterial();
        if (rend.sharedMaterial != target)
        {
            rend.sharedMaterial = target;
        }
    }

    bool initialized = false;
    void Start()
    {
        initialized = true;
    }

    public void setOffMat(Material m)
    {
        offMat = m;
        MarkVisualDirty();
    }

    public bool isHit = false;

    public void keyHitCheck()
    {
        if (!initialized) return;
        bool latched = keyboardInterface != null && keyboardInterface.IsKeyLatched(keyValue);
        bool on = latched || HasActiveManipulator || curState == manipState.grabbed || toggled;

        if (on != isHit)
        {
            isHit = on;
            _deviceInterface.hit(on, keyValue);
            onKeyChangedEvent.Invoke();
            MarkVisualDirty();
        }
    }

    public void setSelectAsynch(bool on)
    {
        selectionHighlight = on;
        MarkVisualDirty();
    }

    public void phantomHit(bool on, bool triggerDeviceInterface = false)
    {
        isHit = on;
        if (triggerDeviceInterface)
        {
            _deviceInterface.hit(on, keyValue);
        }
        MarkVisualDirty();
    }

    void Update()
    {
        UpdateVisualState();
    }

    public override void onTouch(bool on, manipulator m)
    {
        if (m == null)
        {
            insideManipulators.Clear();
            activeManipulators.Clear();
            MarkVisualDirty();
            keyHitCheck();
            return;
        }

        if (on)
        {
            insideManipulators.Add(m);
            RegisterManipulator(m);

            if (m.triggerDown)
            {
                activeManipulators.Add(m);
                if (keyboardInterface != null && keyboardInterface.IsKeyLatched(keyValue))
                {
                    keyboardInterface.OnLatchedKeyTouchedWithActiveTrigger(keyValue);
                }
            }

            m.hapticPulse(700);
        }
        else
        {
            insideManipulators.Remove(m);
            activeManipulators.Remove(m);
            UnregisterManipulator(m);

        }

        MarkVisualDirty();
        keyHitCheck();
    }


    public bool toggled = false;
    public override void setState(manipState state)
    {
        if (!sticky) return;

        bool lateHitCheck = false;
        if (curState == manipState.grabbed && state != manipState.grabbed)
        {
            lateHitCheck = true;
        }

        curState = state;

        if (curState == manipState.grabbed)
        {
            toggled = !toggled;
            keyHitCheck();
        }
        else if (lateHitCheck) keyHitCheck();

        MarkVisualDirty();
    }

    public void SetRecentHighlight(Material material)
    {
        recentHighlightMaterial = material;
        MarkVisualDirty();
    }

    public void ClearRecentHighlight()
    {
        recentHighlightMaterial = null;
        MarkVisualDirty();
    }

    public void SetLatchedVisual(Material material, bool latched)
    {
        latchedVisualMaterial = material;
        latchedActive = latched && material != null;

        MarkVisualDirty();
    }

    void RegisterManipulator(manipulator manip)
    {
        if (manipulatorSubscriptions.ContainsKey(manip))
        {
            return;
        }

        var subscription = new ManipulatorSubscription
        {
            triggerAction = () => OnManipulatorTriggerPressed(manip),
            releaseAction = () => OnManipulatorTriggerReleased(manip)
        };

        manip.onInputTriggerdEvent.AddListener(subscription.triggerAction);
        manip.onInputReleasedEvent.AddListener(subscription.releaseAction);

        manipulatorSubscriptions[manip] = subscription;
    }

    void UnregisterManipulator(manipulator manip)
    {
        if (!manipulatorSubscriptions.TryGetValue(manip, out ManipulatorSubscription subscription))
        {
            return;
        }

        if (subscription.triggerAction != null)
        {
            manip.onInputTriggerdEvent.RemoveListener(subscription.triggerAction);
        }

        if (subscription.releaseAction != null)
        {
            manip.onInputReleasedEvent.RemoveListener(subscription.releaseAction);
        }

        manipulatorSubscriptions.Remove(manip);
    }

    void OnManipulatorTriggerPressed(manipulator manip)
    {
        if (!insideManipulators.Contains(manip))
        {
            return;
        }

        if (activeManipulators.Add(manip))
        {
            MarkVisualDirty();
            keyHitCheck();
        }

        keyboardInterface?.ToggleLatchState(keyValue);
    }

    void OnManipulatorTriggerReleased(manipulator manip)
    {
        if (activeManipulators.Remove(manip))
        {
            MarkVisualDirty();
            keyHitCheck();
        }
    }

    void OnDestroy()
    {
        foreach (var kvp in manipulatorSubscriptions)
        {
            var manip = kvp.Key;
            var subscription = kvp.Value;

            if (manip != null)
            {
                if (subscription.triggerAction != null)
                {
                    manip.onInputTriggerdEvent.RemoveListener(subscription.triggerAction);
                }

                if (subscription.releaseAction != null)
                {
                    manip.onInputReleasedEvent.RemoveListener(subscription.releaseAction);
                }
            }
        }

        manipulatorSubscriptions.Clear();
        insideManipulators.Clear();
        activeManipulators.Clear();
    }

}
