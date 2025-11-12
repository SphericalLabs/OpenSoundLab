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
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class xmlUpdate
{

    public List<InstrumentData> UpdateFile(string s)
    {
        List<InstrumentData> data = new List<InstrumentData>();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(s);
        XmlNode newNode = xmlDoc.CreateElement("Instruments");
        foreach (XmlNode xmlNodeParent in xmlDoc.DocumentElement.ChildNodes)
        {
            if (xmlNodeParent.Name != "Systems" && xmlNodeParent.Name != "Plugs" && xmlNodeParent.Name != "Instruments")
            {
                foreach (XmlNode xmlNode in xmlNodeParent)
                {
                    XmlSerializer serializer;
                    switch (xmlNode.Name)
                    {
                        case "DCs":
                            serializer = new XmlSerializer(typeof(DCData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Tutorialss":
                            serializer = new XmlSerializer(typeof(TutorialsData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Polarizers":
                            serializer = new XmlSerializer(typeof(PolarizerData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Compressors":
                            serializer = new XmlSerializer(typeof(CompressorData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Freeverbs":
                            serializer = new XmlSerializer(typeof(FreeverbData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Delays":
                            serializer = new XmlSerializer(typeof(DelayData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Scopes":
                            serializer = new XmlSerializer(typeof(ScopeData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Quantizers":
                            serializer = new XmlSerializer(typeof(QuantizerData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "ADs":
                            serializer = new XmlSerializer(typeof(ADData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "SequencerCVs":
                            serializer = new XmlSerializer(typeof(SequencerCVData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "SampleHolds":
                            serializer = new XmlSerializer(typeof(SampleHoldData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Glides":
                            serializer = new XmlSerializer(typeof(GlideData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Gains":
                            serializer = new XmlSerializer(typeof(GainData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Oscillator":
                            serializer = new XmlSerializer(typeof(OscillatorData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "TapeGroups":
                            serializer = new XmlSerializer(typeof(TapeGroupData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Speaker":
                            serializer = new XmlSerializer(typeof(SpeakerData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Drums":
                            serializer = new XmlSerializer(typeof(DrumData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "MultiMixes":
                            serializer = new XmlSerializer(typeof(MultipleData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "MultiSplits":
                            serializer = new XmlSerializer(typeof(MultipleData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Recorders":
                            serializer = new XmlSerializer(typeof(RecorderData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Loopers":
                            serializer = new XmlSerializer(typeof(LooperData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Mixers":
                            serializer = new XmlSerializer(typeof(MixerData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Maracas":
                            serializer = new XmlSerializer(typeof(MaracaData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "XyloRolls":
                            serializer = new XmlSerializer(typeof(XyloRollData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "TouchPads":
                            serializer = new XmlSerializer(typeof(TouchPadData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Microphones":
                            serializer = new XmlSerializer(typeof(MicrophoneData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Cameras":
                            serializer = new XmlSerializer(typeof(CameraData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "ControlCubes":
                            serializer = new XmlSerializer(typeof(ControlCubeData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "VCAs":
                            serializer = new XmlSerializer(typeof(vcaData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Reverbs":
                            serializer = new XmlSerializer(typeof(ReverbData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Sequencers":
                            serializer = new XmlSerializer(typeof(SequencerData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Keyboards":
                            serializer = new XmlSerializer(typeof(KeyboardData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Tapes":
                            serializer = new XmlSerializer(typeof(InstrumentData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Noises":
                            serializer = new XmlSerializer(typeof(NoiseData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Filters":
                            serializer = new XmlSerializer(typeof(FilterData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "Artefacts":
                            serializer = new XmlSerializer(typeof(ArtefactData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "SamplerOnes":
                            serializer = new XmlSerializer(typeof(SamplerOneData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        case "SamplerTwos":
                            serializer = new XmlSerializer(typeof(SamplerTwoData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                        default:
                            serializer = new XmlSerializer(typeof(InstrumentData), new XmlRootAttribute { ElementName = xmlNode.Name });
                            break;
                    }

                    data.Add((InstrumentData)serializer.Deserialize(new XmlNodeReader(xmlNode)));

                    switch (xmlNode.Name)
                    {
                        case "DCs":
                            data[data.Count - 1].deviceType = DeviceType.DC;
                            break;
                        case "Tutorialss":
                            data[data.Count - 1].deviceType = DeviceType.Tutorials;
                            break;
                        case "Polarizers":
                            data[data.Count - 1].deviceType = DeviceType.Polarizer;
                            break;
                        case "Compressors":
                            data[data.Count - 1].deviceType = DeviceType.Compressor;
                            break;
                        case "Freeverbs":
                            data[data.Count - 1].deviceType = DeviceType.Freeverb;
                            break;
                        case "Delays":
                            data[data.Count - 1].deviceType = DeviceType.Delay;
                            break;
                        case "Scopes":
                            data[data.Count - 1].deviceType = DeviceType.Scope;
                            break;
                        case "Quantizers":
                            data[data.Count - 1].deviceType = DeviceType.Quantizer;
                            break;
                        case "ADs":
                            data[data.Count - 1].deviceType = DeviceType.AD;
                            break;
                        case "SequencerCVs":
                            data[data.Count - 1].deviceType = DeviceType.SequencerCV;
                            break;
                        case "SampleHolds":
                            data[data.Count - 1].deviceType = DeviceType.SampleHold;
                            break;
                        case "Glides":
                            data[data.Count - 1].deviceType = DeviceType.Glide;
                            break;
                        case "Gains":
                            data[data.Count - 1].deviceType = DeviceType.Gain;
                            break;
                        case "Oscillators":
                            data[data.Count - 1].deviceType = DeviceType.Oscillator;
                            break;
                        case "TapeGroups":
                            data[data.Count - 1].deviceType = DeviceType.TapeGroup;
                            data[data.Count - 1].scale = Vector3.one;
                            break;
                        case "Speaker":
                            data[data.Count - 1].deviceType = DeviceType.Speaker;
                            break;
                        case "Drums":
                            data[data.Count - 1].deviceType = DeviceType.Drum;
                            break;
                        case "Recorders":
                            data[data.Count - 1].deviceType = DeviceType.Recorder;
                            break;
                        case "Loopers":
                            data[data.Count - 1].deviceType = DeviceType.Looper;
                            break;
                        case "Mixers":
                            data[data.Count - 1].deviceType = DeviceType.Mixer;
                            break;
                        case "Maracas":
                            data[data.Count - 1].deviceType = DeviceType.Maracas;
                            break;
                        case "XyloRolls":
                            data[data.Count - 1].deviceType = DeviceType.XyloRoll;
                            break;
                        case "TouchPads":
                            data[data.Count - 1].deviceType = DeviceType.TouchPad;
                            break;
                        case "Microphones":
                            data[data.Count - 1].deviceType = DeviceType.Microphone;
                            break;
                        case "Cameras":
                            data[data.Count - 1].deviceType = DeviceType.Camera;
                            break;
                        case "ControlCubes":
                            data[data.Count - 1].deviceType = DeviceType.ControlCube;
                            break;
                        case "VCAs":
                            data[data.Count - 1].deviceType = DeviceType.VCA;
                            break;
                        case "Reverbs":
                            data[data.Count - 1].deviceType = DeviceType.Reverb;
                            break;
                        case "Sequencers":
                            data[data.Count - 1].deviceType = DeviceType.Sequencer;
                            break;
                        case "Keyboards":
                            data[data.Count - 1].deviceType = DeviceType.Keyboard;
                            break;
                        case "Tapes":
                            data[data.Count - 1].deviceType = DeviceType.Tapes;
                            data[data.Count - 1].scale = Vector3.one;
                            break;
                        case "Noise":
                            data[data.Count - 1].deviceType = DeviceType.Noise;
                            break;
                        case "Filters":
                            data[data.Count - 1].deviceType = DeviceType.Filter;
                            break;
                        case "Artefacts":
                            data[data.Count - 1].deviceType = DeviceType.Artefact;
                            break;
                        case "SamplerOnes":
                            data[data.Count - 1].deviceType = DeviceType.SamplerOne;
                            break;
                        case "SamplerTwos":
                            data[data.Count - 1].deviceType = DeviceType.SamplerTwo;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        return data;

    }

    DeviceType getDeviceType(string s)
    {
        if (s == "Drums") return DeviceType.Drum;
        if (s == "TapeGroups") return DeviceType.TapeGroup;

        return DeviceType.Oscillator;
    }
}

