// Copyright 2017 Google LLC
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

public class xmlUpdate {

  public List<InstrumentData> UpdateFile(string s) {
    List<InstrumentData> data = new List<InstrumentData>();

    XmlDocument xmlDoc = new XmlDocument();
    xmlDoc.Load(s);
    XmlNode newNode = xmlDoc.CreateElement("Instruments");
    foreach (XmlNode xmlNodeParent in xmlDoc.DocumentElement.ChildNodes) {
      if (xmlNodeParent.Name != "Systems" && xmlNodeParent.Name != "Plugs" && xmlNodeParent.Name != "Instruments") {
        foreach (XmlNode xmlNode in xmlNodeParent) {
          XmlSerializer serializer;
          switch (xmlNode.Name) {

            case "Compressors":
              serializer = new XmlSerializer(typeof(CompressorData), new XmlRootAttribute { ElementName = xmlNode.Name });
              break;
            case "StereoVerbs":
              serializer = new XmlSerializer(typeof(StereoVerbData), new XmlRootAttribute { ElementName = xmlNode.Name });
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
            case "SARSCov2":
              serializer = new XmlSerializer(typeof(SARSCov2Data), new XmlRootAttribute { ElementName = xmlNode.Name });
              break;
            case "Funktion":
              serializer = new XmlSerializer(typeof(FunktionData), new XmlRootAttribute { ElementName = xmlNode.Name });
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
            case "Multiples":
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
            case "Samplers":
              serializer = new XmlSerializer(typeof(SamplerData), new XmlRootAttribute { ElementName = xmlNode.Name });
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
            default:
              serializer = new XmlSerializer(typeof(InstrumentData), new XmlRootAttribute { ElementName = xmlNode.Name });
              break;
          }
          data.Add((InstrumentData)serializer.Deserialize(new XmlNodeReader(xmlNode)));

          switch (xmlNode.Name) {

            case "Compressors":
              data[data.Count - 1].deviceType = menuItem.deviceType.Compressor;
              break;
            case "StereoVerbs":
              data[data.Count - 1].deviceType = menuItem.deviceType.StereoVerb;
              break;
            case "Delays":
              data[data.Count - 1].deviceType = menuItem.deviceType.Delay;
              break;
            case "Scopes":
              data[data.Count - 1].deviceType = menuItem.deviceType.Scope;
              break;
            case "Quantizers":
              data[data.Count - 1].deviceType = menuItem.deviceType.Quantizer;
              break;

            case "ADs":
              data[data.Count - 1].deviceType = menuItem.deviceType.AD;
              break;
            case "SequencerCVs":
              data[data.Count - 1].deviceType = menuItem.deviceType.SequencerCV;
              break;
            case "SampleHolds":
              data[data.Count - 1].deviceType = menuItem.deviceType.SampleHold;
              break;
            case "Glides":
              data[data.Count - 1].deviceType = menuItem.deviceType.Glide;
              break;
            case "Gains":
              data[data.Count - 1].deviceType = menuItem.deviceType.Gain;
              break;
            case "SARSCov2":
              data[data.Count - 1].deviceType = menuItem.deviceType.SARSCov2;
              break;
            case "Funktion":
              data[data.Count - 1].deviceType = menuItem.deviceType.Funktion;
              break;
            case "Oscillators":
              data[data.Count - 1].deviceType = menuItem.deviceType.Oscillator;
              break;
            case "TapeGroups":
              data[data.Count - 1].deviceType = menuItem.deviceType.TapeGroup;
              data[data.Count - 1].scale = Vector3.one;
              break;
            case "Speaker":
              data[data.Count - 1].deviceType = menuItem.deviceType.Speaker;
              break;
            case "Drums":
              data[data.Count - 1].deviceType = menuItem.deviceType.Drum;
              break;
            case "Multiples":
              data[data.Count - 1].deviceType = menuItem.deviceType.Multiple;
              break;
            case "Recorders":
              data[data.Count - 1].deviceType = menuItem.deviceType.Recorder;
              break;
            case "Loopers":
              data[data.Count - 1].deviceType = menuItem.deviceType.Looper;
              break;
            case "Mixers":
              data[data.Count - 1].deviceType = menuItem.deviceType.Mixer;
              break;
            case "Maracas":
              data[data.Count - 1].deviceType = menuItem.deviceType.Maracas;
              break;
            case "XyloRolls":
              data[data.Count - 1].deviceType = menuItem.deviceType.XyloRoll;
              break;
            case "TouchPads":
              data[data.Count - 1].deviceType = menuItem.deviceType.TouchPad;
              break;
            case "Microphones":
              data[data.Count - 1].deviceType = menuItem.deviceType.Microphone;
              break;
            case "Cameras":
              data[data.Count - 1].deviceType = menuItem.deviceType.Camera;
              break;
            case "ControlCubes":
              data[data.Count - 1].deviceType = menuItem.deviceType.ControlCube;
              break;
            case "VCAs":
              data[data.Count - 1].deviceType = menuItem.deviceType.VCA;
              break;
            case "Reverbs":
              data[data.Count - 1].deviceType = menuItem.deviceType.Reverb;
              break;
            case "Samplers":
              data[data.Count - 1].deviceType = menuItem.deviceType.Sampler;
              break;
            case "Sequencers":
              data[data.Count - 1].deviceType = menuItem.deviceType.Sequencer;
              break;
            case "Keyboards":
              data[data.Count - 1].deviceType = menuItem.deviceType.Keyboard;
              break;
            case "Tapes":
              data[data.Count - 1].deviceType = menuItem.deviceType.Tapes;
              data[data.Count - 1].scale = Vector3.one;
              break;
            case "Noise":
              data[data.Count - 1].deviceType = menuItem.deviceType.Noise;
              break;
            case "Filters":
              data[data.Count - 1].deviceType = menuItem.deviceType.Filter;
              break;
            case "Artefacts":
                data[data.Count - 1].deviceType = menuItem.deviceType.Artefact;
                break;
            default:
              break;
          }
        }
      }
    }
    return data;

  }

  menuItem.deviceType getDeviceType(string s) {
    if (s == "Drums") return menuItem.deviceType.Drum;
    if (s == "TapeGroups") return menuItem.deviceType.TapeGroup;

    return menuItem.deviceType.Oscillator;
  }
}

