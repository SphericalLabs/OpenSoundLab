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
using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

[XmlRoot("SynthSet")]
public class xmlSaveLoad
{
    [XmlArray("Instruments"), XmlArrayItem("Instruments")]
    public List<InstrumentData> InstrumentList = new List<InstrumentData>();

    [XmlArray("Plugs"), XmlArrayItem("Plugs")]
    public List<PlugData> PlugList = new List<PlugData>();

    [XmlArray("Systems"), XmlArrayItem("Systems")]
    public List<SystemData> SystemList = new List<SystemData>();

    public static xmlSaveLoad LoadFromFile(string path)
    {
        Type[] registryDataTypes = OSLDeviceRegistry.GetInstrumentDataTypes();
        XmlSerializer serializer = createSerializer(registryDataTypes);

        try
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as xmlSaveLoad;
            }
        }
        catch (InvalidOperationException)
        {
            string registrySafeXml = removeUnknownInstrumentTypes(path, registryDataTypes, out int skippedCount);
            if (skippedCount == 0) throw;

            Debug.LogWarning("OpenSoundLab: Skipped " + skippedCount + " instrument(s) from missing or disabled addons while loading " + Path.GetFileName(path) + ".");
            using (StringReader stream = new StringReader(registrySafeXml))
            {
                return serializer.Deserialize(stream) as xmlSaveLoad;
            }
        }
    }

    public void SaveToFile(string path)
    {
        XmlSerializer serializer = createSerializer(OSLDeviceRegistry.GetInstrumentDataTypes());
        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }
    }

    static XmlSerializer createSerializer(Type[] registryDataTypes)
    {
        Type[] serializerDataTypes = filterSerializerDataTypes(registryDataTypes);
        if (serializerDataTypes.Length == 0) return new XmlSerializer(typeof(xmlSaveLoad));
        return new XmlSerializer(typeof(xmlSaveLoad), serializerDataTypes);
    }

    static Type[] filterSerializerDataTypes(Type[] registryDataTypes)
    {
        List<Type> types = new List<Type>();
        if (registryDataTypes == null) return types.ToArray();

        for (int i = 0; i < registryDataTypes.Length; ++i)
        {
            Type dataType = registryDataTypes[i];
            if (dataType == null || dataType == typeof(InstrumentData)) continue;
            if (types.Contains(dataType)) continue;
            types.Add(dataType);
        }

        return types.ToArray();
    }

    static string removeUnknownInstrumentTypes(string path, Type[] registryDataTypes, out int skippedCount)
    {
        skippedCount = 0;

        XmlDocument document = new XmlDocument();
        document.Load(path);

        List<string> knownTypeNames = buildKnownInstrumentTypeNames(registryDataTypes);
        XmlNodeList instrumentNodes = document.SelectNodes("/SynthSet/Instruments/Instruments");
        if (instrumentNodes == null) return document.OuterXml;

        List<XmlNode> nodesToRemove = new List<XmlNode>();
        List<int> removedJackIds = new List<int>();
        for (int i = 0; i < instrumentNodes.Count; ++i)
        {
            XmlNode node = instrumentNodes[i];
            string typeName = getInstrumentTypeName(node);
            if (string.IsNullOrEmpty(typeName) || knownTypeNames.Contains(typeName)) continue;

            nodesToRemove.Add(node);
            collectJackIds(node, removedJackIds);
        }

        for (int i = 0; i < nodesToRemove.Count; ++i)
        {
            XmlNode node = nodesToRemove[i];
            if (node.ParentNode == null) continue;

            string typeName = getInstrumentTypeName(node);
            XmlNode deviceType = node.SelectSingleNode("deviceType");
            string label = deviceType != null && !string.IsNullOrEmpty(deviceType.InnerText) ? deviceType.InnerText : typeName;
            Debug.LogWarning("OpenSoundLab: Skipping missing addon device " + label + " (" + typeName + ").");
            node.ParentNode.RemoveChild(node);
            skippedCount++;
        }

        removePlugsConnectedToMissingJacks(document, removedJackIds);
        return document.OuterXml;
    }

    static void collectJackIds(XmlNode instrumentNode, List<int> jackIds)
    {
        if (instrumentNode == null || jackIds == null) return;

        for (int i = 0; i < instrumentNode.ChildNodes.Count; ++i)
        {
            XmlNode child = instrumentNode.ChildNodes[i];
            if (child == null || child.Name == "ID" || !child.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(child.InnerText, out int id)) continue;
            if (!jackIds.Contains(id)) jackIds.Add(id);
        }
    }

    static void removePlugsConnectedToMissingJacks(XmlDocument document, List<int> removedJackIds)
    {
        if (document == null || removedJackIds == null || removedJackIds.Count == 0) return;

        XmlNodeList plugNodes = document.SelectNodes("/SynthSet/Plugs/Plugs");
        if (plugNodes == null) return;

        List<int> removedPlugIds = new List<int>();
        for (int i = 0; i < plugNodes.Count; ++i)
        {
            XmlNode plugNode = plugNodes[i];
            if (!tryReadIntChild(plugNode, "connected", out int connectedJackId)) continue;
            if (!removedJackIds.Contains(connectedJackId)) continue;

            if (tryReadIntChild(plugNode, "ID", out int plugId) && !removedPlugIds.Contains(plugId))
            {
                removedPlugIds.Add(plugId);
            }
            if (tryReadIntChild(plugNode, "otherPlug", out int otherPlugId) && !removedPlugIds.Contains(otherPlugId))
            {
                removedPlugIds.Add(otherPlugId);
            }
        }

        List<XmlNode> plugsToRemove = new List<XmlNode>();
        for (int i = 0; i < plugNodes.Count; ++i)
        {
            XmlNode plugNode = plugNodes[i];
            bool removePlug = tryReadIntChild(plugNode, "connected", out int connectedJackId) && removedJackIds.Contains(connectedJackId);
            if (!removePlug && tryReadIntChild(plugNode, "ID", out int plugId)) removePlug = removedPlugIds.Contains(plugId);
            if (!removePlug && tryReadIntChild(plugNode, "otherPlug", out int otherPlugId)) removePlug = removedPlugIds.Contains(otherPlugId);
            if (removePlug) plugsToRemove.Add(plugNode);
        }

        for (int i = 0; i < plugsToRemove.Count; ++i)
        {
            XmlNode plugNode = plugsToRemove[i];
            if (plugNode.ParentNode != null) plugNode.ParentNode.RemoveChild(plugNode);
        }
    }

    static bool tryReadIntChild(XmlNode node, string childName, out int value)
    {
        value = 0;
        if (node == null) return false;

        XmlNode child = node.SelectSingleNode(childName);
        return child != null && int.TryParse(child.InnerText, out value);
    }

    static List<string> buildKnownInstrumentTypeNames(Type[] registryDataTypes)
    {
        List<string> typeNames = new List<string>();
        if (registryDataTypes != null)
        {
            for (int i = 0; i < registryDataTypes.Length; ++i)
            {
                addTypeNames(typeNames, registryDataTypes[i]);
            }
        }

        return typeNames;
    }

    static void addTypeNames(List<string> typeNames, Type type)
    {
        if (type == null) return;

        if (!typeNames.Contains(type.Name)) typeNames.Add(type.Name);

        object[] xmlTypes = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);
        for (int i = 0; i < xmlTypes.Length; ++i)
        {
            XmlTypeAttribute xmlType = xmlTypes[i] as XmlTypeAttribute;
            if (xmlType != null && !string.IsNullOrEmpty(xmlType.TypeName) && !typeNames.Contains(xmlType.TypeName))
            {
                typeNames.Add(xmlType.TypeName);
            }
        }
    }

    static string getInstrumentTypeName(XmlNode node)
    {
        if (node == null || node.Attributes == null) return "";

        XmlAttribute typeAttribute = node.Attributes["type", "http://www.w3.org/2001/XMLSchema-instance"];
        if (typeAttribute == null || string.IsNullOrEmpty(typeAttribute.Value)) return "";

        string typeName = typeAttribute.Value;
        int prefixIndex = typeName.IndexOf(':');
        if (prefixIndex >= 0 && prefixIndex < typeName.Length - 1)
        {
            typeName = typeName.Substring(prefixIndex + 1);
        }

        return typeName;
    }

}
