// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
using UnityEngine.UI;
using TMPro;

public class ManipInputField : ManipUiObject
{
    private TMP_InputField inputField;

    protected override void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.customCaretColor = true;
        inputField.caretColor = Color.white;

        // The MRTK keyboard sometimes re-selects TMP input fields with the caret at 0,
        // which makes subsequent key presses insert at the front and flips the typed string.
        inputField.onSelect.AddListener(onInputSelected);
        base.Start();
    }

    public override void OnGrab()
    {
        base.OnGrab();
        inputField.Select();
        moveCaretToEnd();
        StartCoroutine(moveCaretToEndNextFrame());
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(onInputSelected);
        }
    }

    private void onInputSelected(string value)
    {
        moveCaretToEnd();
        StartCoroutine(moveCaretToEndNextFrame());
    }

    private IEnumerator moveCaretToEndNextFrame()
    {
        // TMP/MRTK can reset the caret again after the initial select callback, so we
        // force one more update on the next frame to keep typing appended at the end.
        // Otherwise the resulting string is flipped
        yield return null;
        moveCaretToEnd();
    }

    private void moveCaretToEnd()
    {
        int end = inputField.text.Length;
        inputField.caretPosition = end;
        inputField.selectionAnchorPosition = end;
        inputField.selectionFocusPosition = end;
        inputField.MoveTextEnd(false);
    }
}
