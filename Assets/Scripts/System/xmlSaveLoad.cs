// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

[XmlRoot("SynthSet")]
public class xmlSaveLoad {
  [XmlArray("Instruments"), XmlArrayItem("Instruments")]
  public List<InstrumentData> InstrumentList = new List<InstrumentData>();

  [XmlArray("Plugs"), XmlArrayItem("Plugs")]
  public List<PlugData> PlugList = new List<PlugData>();

  [XmlArray("Systems"), XmlArrayItem("Systems")]
  public List<SystemData> SystemList = new List<SystemData>();

  public static xmlSaveLoad LoadFromFile(string path) {
    XmlSerializer serializer = new XmlSerializer(typeof(xmlSaveLoad));
    using (var stream = new FileStream(path, FileMode.Open)) {
      return serializer.Deserialize(stream) as xmlSaveLoad;
    }
  }

  public void SaveToFile(string path) {
    XmlSerializer serializer = new XmlSerializer(typeof(xmlSaveLoad));
    using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8"))) {
      serializer.Serialize(stream, this);
    }
  }

}