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

using System.Runtime.InteropServices;
using UnityEngine;
using System;

// Please note: this is a stub, it is not yet working
// The idea is that native dsp code can log to Unity console

public class NativeLogger
{

    static IntPtr delegatePtr;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogDelegate(int level, string str);

    public static IntPtr getLogCallback()
    {
        if (delegatePtr == null)
        {
            LogDelegate callback_delegate = new LogDelegate(LogCallback);
            delegatePtr = Marshal.GetFunctionPointerForDelegate(callback_delegate);
        }
        return delegatePtr;
    }

    public static void LogCallback(int level, string msg)
    {
        if (level == 0)
            Debug.Log(msg);
        else if (level == 1)
            Debug.LogWarning(msg);
        else if (level == 2)
            Debug.LogError(msg);
    }

}
