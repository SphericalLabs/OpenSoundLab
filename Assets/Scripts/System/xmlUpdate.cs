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

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

public class xmlUpdate
{
    public List<InstrumentData> UpdateFile(string path)
    {
        List<InstrumentData> data = new List<InstrumentData>();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(path);
        if (xmlDoc.DocumentElement == null) return data;

        foreach (XmlNode xmlNodeParent in xmlDoc.DocumentElement.ChildNodes)
        {
            if (isCurrentSaveSection(xmlNodeParent.Name)) continue;

            foreach (XmlNode xmlNode in xmlNodeParent.ChildNodes)
            {
                InstrumentData instrumentData = deserializeLegacyInstrument(xmlNode);
                if (instrumentData != null) data.Add(instrumentData);
            }
        }

        return data;
    }

    bool isCurrentSaveSection(string sectionName)
    {
        return sectionName == "Systems" || sectionName == "Plugs" || sectionName == "Instruments";
    }

    InstrumentData deserializeLegacyInstrument(XmlNode xmlNode)
    {
        if (xmlNode == null) return null;

        if (!OSLDeviceRegistry.TryGetByLegacyXmlRootName(xmlNode.Name, out OSLDeviceRegistration registration))
        {
            Debug.LogWarning("OpenSoundLab: Skipping unknown legacy XML device root " + xmlNode.Name + ".");
            return null;
        }

        Type dataType = resolveDataType(xmlNode.Name, registration);
        XmlSerializer serializer = new XmlSerializer(dataType, new XmlRootAttribute { ElementName = xmlNode.Name });
        InstrumentData data = (InstrumentData)serializer.Deserialize(new XmlNodeReader(xmlNode));
        data.deviceType = registration.deviceId;

        if (registration.deviceId == "TapeGroup" || registration.deviceId == "Tapes")
        {
            data.scale = Vector3.one;
        }

        return data;
    }

    Type resolveDataType(string xmlRootName, OSLDeviceRegistration registration)
    {
        if (registration == null) return typeof(InstrumentData);

        if (isXmlRootForDeviceId(xmlRootName, registration.deviceId) ||
            isXmlRootForDeviceId(xmlRootName, registration.deviceLocalId))
        {
            return registration.dataType != null ? registration.dataType : typeof(InstrumentData);
        }

        if (registration.legacyDeviceIds != null)
        {
            for (int i = 0; i < registration.legacyDeviceIds.Length; ++i)
            {
                if (!isXmlRootForDeviceId(xmlRootName, registration.legacyDeviceIds[i])) continue;

                Type legacyDataType = findTypeAtIndex(registration.legacyDataTypeNames, i);
                return legacyDataType != null ? legacyDataType : registration.dataType ?? typeof(InstrumentData);
            }
        }

        return registration.dataType != null ? registration.dataType : typeof(InstrumentData);
    }

    Type findTypeAtIndex(string[] typeNames, int index)
    {
        if (typeNames == null || index < 0 || index >= typeNames.Length) return null;
        return findType(typeNames[index]);
    }

    Type findType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        Type directType = Type.GetType(typeName);
        if (directType != null) return directType;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; ++i)
        {
            Type resolvedType = assemblies[i].GetType(typeName);
            if (resolvedType != null) return resolvedType;
        }

        return null;
    }

    bool isXmlRootForDeviceId(string xmlRootName, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(xmlRootName) || string.IsNullOrWhiteSpace(deviceId)) return false;
        return xmlRootName == deviceId ||
               xmlRootName == deviceId + "s" ||
               xmlRootName == pluralizeDeviceId(deviceId);
    }

    string pluralizeDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return "";
        if (deviceId.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            deviceId.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
            deviceId.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            deviceId.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            deviceId.EndsWith("z", StringComparison.OrdinalIgnoreCase))
        {
            return deviceId + "es";
        }

        if (deviceId.EndsWith("y", StringComparison.OrdinalIgnoreCase) && deviceId.Length > 1)
        {
            return deviceId.Substring(0, deviceId.Length - 1) + "ies";
        }

        return deviceId + "s";
    }
}
