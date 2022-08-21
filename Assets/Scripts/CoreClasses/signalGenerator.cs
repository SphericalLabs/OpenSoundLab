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

﻿using UnityEngine;
using System.Collections;

public class signalGenerator : MonoBehaviour
{
    protected bool hasAlreadyBeenCalledInThisBufferRun = false;

    public int index = 0;

    protected double _sampleRate;
    protected double _sampleDuration;
    public double _phase;

    public bool useNativeIfAvailable = true;

    float levelVal = 1;

    protected const int MAX_BUFFER_LENGTH = 2048; // Very important to enforce this 

    public virtual void Awake()
    {
        _phase = 0;
        _sampleRate = AudioSettings.outputSampleRate;
        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    }

    public virtual menuItem.deviceType queryDeviceType()
    {
        return menuItem.deviceType.Max;
    }

    public virtual void updateTape(string s)
    {
        
    }
  
    public virtual void trigger(int c)
    {
        
    }

    public virtual void trigger(int c, float f)
    {

    }

    public virtual void modBuffer(double dspTime, int channels, float[] buffer)
    {

    }

    public virtual void processBuffer(float[] buffer, double dspTime, int channels)
    {

    }

    protected bool recursionCheckPre(){
        if (hasAlreadyBeenCalledInThisBufferRun)
        {
          hasAlreadyBeenCalledInThisBufferRun = false;
          return false;
        }
        hasAlreadyBeenCalledInThisBufferRun = true;
        return true;
    }

    protected void recursionCheckPost(){
        hasAlreadyBeenCalledInThisBufferRun = false;
    }

    public virtual float[] getBuffer(double dspTime, int channels, int bufferLength, bool modFreq = false, float requestedFreq = 440f, float detuneAmount = 0)
    {
        float[] buffer = new float[bufferLength];

        for (int i = 0; i < buffer.Length; i += channels)
        {
            double sample = Mathf.Sin((float)_phase * 2 * Mathf.PI);

            float frequency = 440;
            float amplitude = 0.5f;

            _phase += frequency * _sampleDuration;

            if (_phase > 1.0) _phase -= 1.0;

            buffer[i] = (float)sample * amplitude;
            buffer[i + 1] = (float)sample * amplitude;

            dspTime += _sampleDuration;
        }

        return buffer;
    }
}