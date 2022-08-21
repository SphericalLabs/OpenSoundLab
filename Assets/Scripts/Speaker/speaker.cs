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
using System.Runtime.InteropServices;

public class speaker : MonoBehaviour
{

    public float volume = 1;
    public signalGenerator incoming;
    public bool monoMode = false;
    public bool leftOn = true;

    [DllImport("SoundStageNative")]
    public static extern void MultiplyArrayBySingleValue(float[] buffer, int length, float val);

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (incoming == null) return;
        double dspTime = AudioSettings.dspTime;
        incoming.processBuffer(buffer, dspTime, channels);

        if (monoMode)
        {
            // please note: unity only uses left channel if spatialize is on!
            // todo: how to handle non-spatialize settings?
            if (leftOn)
            {
                
                //NATIVE!
                for (int n = 0; n < buffer.Length; n += 2)
                {
                    buffer[n] = buffer[n + 1] = buffer[n] * volume;
                    // no need for dealing with right channel
                }
            }
            else
            {
                //NATIVE!
                for (int n = 0; n < buffer.Length; n += 2)
                {
                    buffer[n] = buffer[n + 1] = buffer[n + 1] * volume;
                    // no need for dealing with right channel
                }
            }
        } else {
            if(volume != 1)
                MultiplyArrayBySingleValue(buffer, buffer.Length, volume);
        }
    }
}
