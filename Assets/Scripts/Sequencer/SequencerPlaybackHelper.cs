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

using System;
using UnityEngine;

public class SequencerPlaybackHelper
{
    sequencerDeviceInterface sequencer;

    int selectedStep = 0;
    int targetStep = 0;
    int curStep = 0;

    bool runningUpdated = false;
    bool phaseSyncPending = false;

    float[] lastClockSig = new float[] { 0, 0 };
    float[] lastResetSig = new float[] { 0, 0 };

    float[] audioPhaseBuffer = new float[2048];
    float[] audioClockBuffer = new float[2048];
    float[] audioResetBuffer = new float[2048];

    signalGenerator clockGenerator;
    signalGenerator resetGenerator;
    signalGenerator phaseGenerator;

    public SequencerPlaybackHelper(sequencerDeviceInterface sequencer)
    {
        this.sequencer = sequencer;
    }

    public int getCurStep()
    {
        return curStep;
    }

    public void setCurStep(int value)
    {
        curStep = value;
    }

    public int getTargetStep()
    {
        return targetStep;
    }

    public void setTargetStep(int value)
    {
        targetStep = value;
    }

    public void updateGenerators()
    {
        if (clockGenerator != sequencer.clockJack.signal)
        {
            clockGenerator = sequencer.clockJack.signal;
        }

        if (resetGenerator != sequencer.resetJack.signal)
        {
            resetGenerator = sequencer.resetJack.signal;
        }

        if (phaseGenerator != sequencer.phaseJack.signal)
        {
            phaseGenerator = sequencer.phaseJack.signal;
        }
    }

    public void selectStep(int s, bool silent = false)
    {
        targetStep = s;
        selectedStep = s;

        if (silent) return;

        // This is called from the audio thread and updates signals for sample accuracy.
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[] rowMutes = sequencer.getRowMutes();
        trigSignalGenerator[] trigGenerators = sequencer.getRowTriggerGenerators();
        cvSignalGenerator[] cvGenerators = sequencer.getRowCvGenerators();

        for (int row = 0; row < curDimensions[0]; row++)
        {
            if (rowMutes[row].isHit) continue;

            trigGenerators[row].setSignal(sequencer.stepBools[sequencer.activePattern, row, targetStep]);
            cvGenerators[row].setSignal(sequencer.stepFloats[sequencer.activePattern, row, targetStep] * 2f - 1f);
        }
    }

    public void selectStepUpdate()
    {
        if (targetStep == curStep) return;
        if (curStep < sequencer.dimensions[1]) stepOff(curStep);
        curStep = targetStep;
        stepOn(curStep);
        sequencer.stepSelect.updatePos(-sequencer.getCubeConst() * curStep);
    }

    public void executeNextStep()
    {
        if (sequencer.stepSelect.curState == manipObject.manipState.grabbed) return;

        int s = 1;

        if (runningUpdated)
        {
            s = 0;
            runningUpdated = false;
        }

        int next = (targetStep + s) % sequencer.dimensions[1];
        selectStep(next);
    }

    void stepOff(int step)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[,] stepButtons = sequencer.getStepButtons();

        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (stepButtons[i, step] != null) stepButtons[i, step].Highlight(false);
        }
    }

    void stepOn(int step)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[,] stepButtons = sequencer.getStepButtons();

        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (stepButtons[i, step] != null) stepButtons[i, step].Highlight(true);
        }
    }

    void resetSteps()
    {
        selectStep(0);
    }

    public void onAudioFilterRead(float[] buffer, int channels)
    {
        if (audioPhaseBuffer.Length != buffer.Length)
        {
            Array.Resize(ref audioPhaseBuffer, buffer.Length);
        }

        if (audioClockBuffer.Length != buffer.Length)
        {
            Array.Resize(ref audioClockBuffer, buffer.Length);
        }

        if (audioResetBuffer.Length != buffer.Length)
        {
            Array.Resize(ref audioResetBuffer, buffer.Length);
        }

        // Phase mode is implicit: if the phase jack is patched (signal present), we follow phase; otherwise we run on clock/reset.
        bool phaseMode = phaseGenerator != null;

        if (phaseMode) // Phase mode
        {
            if (!sequencer.running) return;

            phaseGenerator.processBuffer(audioPhaseBuffer, AudioSettings.dspTime, channels);

            // Map phase directly to step.
            float latestPhase = audioPhaseBuffer[buffer.Length - channels];
            int s = Mathf.FloorToInt(latestPhase * sequencer.dimensions[1]);
            s = Mathf.Clamp(s, 0, sequencer.dimensions[1] - 1);
            if (phaseSyncPending || s != targetStep)
            {
                // SelectStep handles the signal generator updates, which is needed here.
                selectStep(s);
                runningUpdated = true;
                phaseSyncPending = false;
            }
        }
        else // Clock (Trigger) mode
        {
            if (resetGenerator != null)
            {
                resetGenerator.processBuffer(audioResetBuffer, AudioSettings.dspTime, channels);

                for (int i = 0; i < buffer.Length; i += channels)
                {
                    if (signalGenerator.isRisingEdge(audioResetBuffer[i], lastResetSig[1]))
                    {
                        resetSteps();
                    }
                    lastResetSig[0] = lastResetSig[1];
                    lastResetSig[1] = audioResetBuffer[i];
                }
            }

            if (!sequencer.running) return;

            if (clockGenerator != null)
            {
                clockGenerator.processBuffer(audioClockBuffer, AudioSettings.dspTime, channels);

                for (int i = 0; i < buffer.Length; i += channels)
                {
                    if (signalGenerator.isRisingEdge(audioClockBuffer[i], lastClockSig[1]))
                    {
                        executeNextStep();
                    }
                    lastClockSig[0] = lastClockSig[1];
                    lastClockSig[1] = audioClockBuffer[i];
                }
            }
        }
    }

    public void updateStepSelect(bool forced = false)
    {
        // bugfix for randomly skipped / missed steps in sequencer.
        // this routine would fire even if the step selector handle was not touched or grabbed.
        // this could be due to an multithread issue between main and audio thread, which is still unsolved.
        if (!forced && sequencer.stepSelect.curState != manipObject.manipState.grabbed) return;

        int s = (int)Mathf.Round(sequencer.stepSelect.transform.localPosition.x / -sequencer.getCubeConst());
        if (s == selectedStep) return;
        sequencer.stepSelect.pulse();
        selectedStep = s;
        selectStep(s);
    }

    public void togglePlay(bool on)
    {
        sequencer.running = on;
        if (on)
        {
            runningUpdated = true;
            phaseSyncPending = true;
        }
        else
        {
            phaseSyncPending = false;
        }
    }
}
