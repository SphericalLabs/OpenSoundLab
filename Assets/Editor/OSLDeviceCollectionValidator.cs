using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class OSLDeviceCollectionValidator
{
    const string examplesPath = "Assets/StreamingAssetsPreZip/Examples";
    const string addonAssetsPath = "Assets/OSLDevices/Addons/";
    const string packageDevicesSegment = "/OSLDevices/";

    public sealed class ValidationReport
    {
        public int errors;
        public int warnings;

        public void Error(string message)
        {
            errors++;
            Debug.LogError("OpenSoundLab Device Validation: " + message);
        }

        public void Warning(string message)
        {
            warnings++;
            Debug.LogWarning("OpenSoundLab Device Validation: " + message);
        }
    }

    [MenuItem("OpenSoundLab/Devices/Validate Collections")]
    public static void ValidateCollectionsMenu()
    {
        ValidationReport report = ValidateCollections();
        if (report.errors > 0)
        {
            Debug.LogError("OpenSoundLab Device Validation failed with " + report.errors + " error(s) and " + report.warnings + " warning(s).");
            return;
        }

        Debug.Log("OpenSoundLab Device Validation passed with " + report.warnings + " warning(s).");
    }

    public static ValidationReport ValidateCollections()
    {
        OSLDeviceRegistry.Refresh();

        ValidationReport report = new ValidationReport();
        List<OSLDeviceCollectionRegistration> collections = OSLDeviceRegistry.GetCollections();
        List<OSLDeviceRegistration> registrations = OSLDeviceRegistry.GetAll();
        List<string> storeSkus = validateStoreProductCatalogs(report);

        validateCollections(report, collections);
        validateRegistrations(report, registrations, storeSkus);
        validateExampleXmlFixtures(report);

        return report;
    }

    static List<string> validateStoreProductCatalogs(ValidationReport report)
    {
        List<string> storeSkus = new List<string>();
        Dictionary<string, string> duplicateStoreSkus = new Dictionary<string, string>();
        OSLStoreProductCatalog[] catalogs = Resources.LoadAll<OSLStoreProductCatalog>("");

        for (int catalogIndex = 0; catalogs != null && catalogIndex < catalogs.Length; ++catalogIndex)
        {
            OSLStoreProductCatalog catalog = catalogs[catalogIndex];
            if (catalog == null || catalog.products == null) continue;

            string catalogName = catalog.name;
            for (int productIndex = 0; productIndex < catalog.products.Length; ++productIndex)
            {
                OSLStoreProductDefinition product = catalog.products[productIndex];
                string label = catalogName + " product " + productIndex;
                if (product == null) continue;
                if (string.IsNullOrWhiteSpace(product.sku))
                {
                    report.Warning(label + " has no SKU.");
                    continue;
                }

                if (!storeSkus.Contains(product.sku)) storeSkus.Add(product.sku);
                checkDuplicate(report, duplicateStoreSkus, product.sku, label, "Duplicate Store SKU");
            }
        }

        return storeSkus;
    }

    static void validateCollections(ValidationReport report, List<OSLDeviceCollectionRegistration> collections)
    {
        Dictionary<string, string> collectionIds = new Dictionary<string, string>();
        Dictionary<string, string> resourceSlugs = new Dictionary<string, string>();

        for (int i = 0; i < collections.Count; ++i)
        {
            OSLDeviceCollectionRegistration collection = collections[i];
            if (collection == null) continue;

            require(report, collection.collectionId, "Collection without collectionId.");
            require(report, collection.resourceSlug, "Collection " + collection.collectionId + " has no resourceSlug.");
            checkDuplicate(report, collectionIds, collection.collectionId, collection.collectionId, "Duplicate collectionId");
            checkDuplicate(report, resourceSlugs, collection.resourceSlug, collection.collectionId, "Duplicate resourceSlug");
            validateCollectionSourceId(report, collection);
            validateCollectionLocation(report, collection);
        }
    }

    static void validateCollectionSourceId(ValidationReport report, OSLDeviceCollectionRegistration collection)
    {
        if (collection == null || string.IsNullOrWhiteSpace(collection.collectionId)) return;

        if (collection.source == OSLCollectionSource.Core &&
            !collection.collectionId.StartsWith(OSLDeviceIdentityCompatibility.CoreCollectionDomain + ".", StringComparison.Ordinal))
        {
            report.Warning("Core collection " + collection.collectionId + " should use " + OSLDeviceIdentityCompatibility.CoreCollectionDomain + ".<collection>.");
        }

        if (collection.source == OSLCollectionSource.Local &&
            !collection.collectionId.Contains("." + OSLDeviceIdentityCompatibility.AddonCollectionSegment + "."))
        {
            report.Warning("Local collection " + collection.collectionId + " should include ." + OSLDeviceIdentityCompatibility.AddonCollectionSegment + ". in its collection id.");
        }

        if (collection.source == OSLCollectionSource.Package &&
            !collection.collectionId.Contains("." + OSLDeviceIdentityCompatibility.AddonCollectionSegment + "."))
        {
            report.Warning("Package collection " + collection.collectionId + " should include ." + OSLDeviceIdentityCompatibility.AddonCollectionSegment + ". in its collection id.");
        }
    }

    static void validateCollectionLocation(ValidationReport report, OSLDeviceCollectionRegistration collection)
    {
        if (collection == null || collection.collectionManifest == null) return;

        string path = AssetDatabase.GetAssetPath(collection.collectionManifest).Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(path)) return;

        if (collection.source == OSLCollectionSource.Core && !path.StartsWith("Assets/OSLDevices/Core/", StringComparison.Ordinal))
        {
            report.Error("Core collection " + collection.collectionId + " is outside Assets/OSLDevices/Core: " + path);
            return;
        }

        if (collection.source == OSLCollectionSource.Local)
        {
            bool inProjectAddons = path.StartsWith(addonAssetsPath, StringComparison.Ordinal);
            string packageDevicesRoot = getPackageDevicesRoot(path);
            if (!inProjectAddons && string.IsNullOrWhiteSpace(packageDevicesRoot))
            {
                report.Warning("Local collection " + collection.collectionId + " is outside Assets/OSLDevices/Addons or Packages/<PackageName>/OSLDevices: " + path);
            }

            if (inProjectAddons) validateCollectionFolderName(report, collection, path, addonAssetsPath);
            if (!string.IsNullOrWhiteSpace(packageDevicesRoot)) validateCollectionFolderName(report, collection, path, packageDevicesRoot);
        }

        string expectedSegment = "/Resources/" + collection.resourceSlug + "/";
        if (!path.Contains(expectedSegment))
        {
            report.Warning("Collection " + collection.collectionId + " manifest is not under Resources/" + collection.resourceSlug + ": " + path);
        }

        string expectedFilename = collection.collectionId + ".asset";
        if (Path.GetFileName(path) != expectedFilename)
        {
            report.Warning("Collection " + collection.collectionId + " manifest filename should be " + expectedFilename + ": " + path);
        }
    }

    static string getPackageDevicesRoot(string path)
    {
        if (!path.StartsWith("Packages/", StringComparison.Ordinal)) return "";

        int segmentIndex = path.IndexOf(packageDevicesSegment, StringComparison.Ordinal);
        if (segmentIndex < 0) return "";
        return path.Substring(0, segmentIndex + packageDevicesSegment.Length);
    }

    static void validateCollectionFolderName(ValidationReport report, OSLDeviceCollectionRegistration collection, string path, string rootPrefix)
    {
        string relativePath = path.Substring(rootPrefix.Length);
        int slashIndex = relativePath.IndexOf('/');
        string folderName = slashIndex >= 0 ? relativePath.Substring(0, slashIndex) : relativePath;
        if (folderName != collection.collectionId)
        {
            report.Warning("Local collection " + collection.collectionId + " folder should use its stable collection id: " + path);
        }
    }

    static void validateRegistrations(ValidationReport report, List<OSLDeviceRegistration> registrations, List<string> storeSkus)
    {
        Dictionary<string, string> canonicalDeviceIds = new Dictionary<string, string>();
        Dictionary<string, string> localIdsByCollection = new Dictionary<string, string>();
        Dictionary<string, string> legacyDeviceIds = new Dictionary<string, string>();
        Dictionary<string, string> legacyDataTypeNames = new Dictionary<string, string>();
        Dictionary<string, string> legacyXmlRootNames = new Dictionary<string, string>();
        Dictionary<string, string> prefabResourcePaths = new Dictionary<string, string>();
        Dictionary<string, string> menuPrefabResourcePaths = new Dictionary<string, string>();
        Dictionary<string, string> networkPrefabNames = new Dictionary<string, string>();

        for (int i = 0; i < registrations.Count; ++i)
        {
            OSLDeviceRegistration registration = registrations[i];
            if (registration == null) continue;

            string label = describe(registration);
            require(report, registration.collectionId, label + " has no owning collection.");
            require(report, registration.deviceLocalId, label + " has no deviceLocalId.");
            require(report, registration.canonicalDeviceId, label + " has no canonicalDeviceId.");

            checkDuplicate(report, canonicalDeviceIds, registration.canonicalDeviceId, label, "Duplicate canonicalDeviceId");
            checkDuplicate(report, localIdsByCollection, registration.collectionId + "/" + registration.deviceLocalId, label, "Duplicate deviceLocalId in collection");
            checkDuplicate(report, prefabResourcePaths, registration.prefabResourcePath, label, "Duplicate prefabResourcePath");
            checkDuplicate(report, menuPrefabResourcePaths, registration.menuPrefabResourcePath, label, "Duplicate menuPrefabResourcePath");
            checkArrayDuplicates(report, legacyDeviceIds, registration.legacyDeviceIds, label, "Ambiguous legacyDeviceId");
            checkArrayDuplicates(report, legacyDataTypeNames, registration.legacyDataTypeNames, label, "Ambiguous legacyDataTypeName");
            checkXmlRootAliases(report, legacyXmlRootNames, registration);

            validateResources(report, registration);
            validateDataTypes(report, registration);
            validateProductInfo(report, registration, storeSkus);
            validateNetworkSpawn(report, networkPrefabNames, registration);
        }
    }

    static void validateProductInfo(ValidationReport report, OSLDeviceRegistration registration, List<string> knownStoreSkus)
    {
        if (registration == null) return;
        if (registration.RequiresEntitlement && registration.StoreSkus.Length == 0)
        {
            report.Warning(describe(registration) + " requires entitlement but has no storeSku/storeSkus.");
        }

        string[] storeSkus = registration.StoreSkus;
        for (int i = 0; i < storeSkus.Length; ++i)
        {
            if (storeSkus[i] == registration.canonicalDeviceId)
            {
                report.Warning(describe(registration) + " uses the same value for storeSku and canonicalDeviceId.");
            }

            if (knownStoreSkus != null && !knownStoreSkus.Contains(storeSkus[i]))
            {
                report.Error(describe(registration) + " references unknown Store SKU '" + storeSkus[i] + "'. Add it to an OSLStoreProductCatalog asset.");
            }
        }
    }

    static void validateResources(ValidationReport report, OSLDeviceRegistration registration)
    {
        string label = describe(registration);

        if (string.IsNullOrWhiteSpace(registration.prefabResourcePath))
        {
            report.Error(label + " has no prefabResourcePath.");
        }
        else if (Resources.Load<GameObject>(registration.prefabResourcePath) == null)
        {
            report.Error(label + " prefabResourcePath is missing: " + registration.prefabResourcePath);
        }

        if (registration.showInMenu)
        {
            if (string.IsNullOrWhiteSpace(registration.menuPrefabResourcePath))
            {
                report.Error(label + " is visible but has no menuPrefabResourcePath.");
            }
            else if (Resources.Load<GameObject>(registration.menuPrefabResourcePath) == null)
            {
                report.Error(label + " menuPrefabResourcePath is missing: " + registration.menuPrefabResourcePath);
            }

            if (string.IsNullOrWhiteSpace(registration.symbolResourcePath))
            {
                report.Error(label + " is visible but has no symbolResourcePath.");
            }
            else if (Resources.Load<Texture>(registration.symbolResourcePath) == null)
            {
                report.Error(label + " symbolResourcePath is missing: " + registration.symbolResourcePath);
            }
        }
    }

    static void validateDataTypes(ValidationReport report, OSLDeviceRegistration registration)
    {
        if (registration == null || string.IsNullOrWhiteSpace(registration.dataTypeName)) return;
        if (registration.dataTypeName == "InstrumentData") return;
        if (registration.dataType == null) report.Error(describe(registration) + " dataTypeName is missing: " + registration.dataTypeName);
    }

    static void validateNetworkSpawn(ValidationReport report, Dictionary<string, string> networkPrefabNames, OSLDeviceRegistration registration)
    {
        if (registration == null) return;
        if (!registration.showInMenu) return;

        if (string.IsNullOrWhiteSpace(registration.prefabResourcePath))
        {
            report.Error(describe(registration) + " has no prefabResourcePath for network spawn.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(registration.prefabResourcePath);
        if (prefab == null) return;
        if (prefab.GetComponent<Mirror.NetworkIdentity>() == null)
        {
            report.Error(describe(registration) + " prefab has no NetworkIdentity: " + registration.prefabResourcePath);
            return;
        }

        string prefabName = getResourceName(registration.prefabResourcePath);
        checkDuplicate(report, networkPrefabNames, prefabName, describe(registration), "Duplicate network prefab name");
    }

    static void validateExampleXmlFixtures(ValidationReport report)
    {
        if (!Directory.Exists(examplesPath))
        {
            report.Warning("Example XML fixture folder is missing: " + examplesPath);
            return;
        }

        string[] files = Directory.GetFiles(examplesPath, "*.xml", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; ++i)
        {
            validateExampleXmlFixture(report, files[i]);
        }
    }

    static void validateExampleXmlFixture(ValidationReport report, string path)
    {
        xmlSaveLoad save;
        try
        {
            save = xmlSaveLoad.LoadFromFile(path);
        }
        catch (Exception exception)
        {
            report.Error("Failed to load XML fixture " + path + ": " + exception.Message);
            return;
        }

        if (save == null)
        {
            report.Error("XML fixture loaded as null: " + path);
            return;
        }

        validateFixtureDevices(report, path, save);
        validateFixtureCableEndpoints(report, path, save);
    }

    static void validateFixtureDevices(ValidationReport report, string path, xmlSaveLoad save)
    {
        for (int i = 0; i < save.InstrumentList.Count; ++i)
        {
            InstrumentData data = save.InstrumentList[i];
            if (data == null) continue;

            if (!OSLDeviceRegistry.TryGet(data.deviceType, out OSLDeviceRegistration registration))
            {
                report.Warning(Path.GetFileName(path) + " references missing device " + data.deviceType + ".");
                continue;
            }

            if (registration.dataType != null && !registration.dataType.IsAssignableFrom(data.GetType()))
            {
                report.Warning(Path.GetFileName(path) + " has device " + data.deviceType + " with data type " + data.GetType().Name + " but registry primary type is " + registration.dataType.Name + ".");
            }
        }
    }

    static void validateFixtureCableEndpoints(ValidationReport report, string path, xmlSaveLoad save)
    {
        List<int> jackIds = new List<int>();
        for (int i = 0; i < save.InstrumentList.Count; ++i)
        {
            collectJackIds(save.InstrumentList[i], jackIds);
        }

        for (int i = 0; i < save.PlugList.Count; ++i)
        {
            PlugData plug = save.PlugList[i];
            if (plug == null) continue;
            if (!jackIds.Contains(plug.connected))
            {
                report.Warning(Path.GetFileName(path) + " plug " + plug.ID + " connects to missing jack " + plug.connected + ".");
            }
        }
    }

    static void collectJackIds(InstrumentData data, List<int> jackIds)
    {
        if (data == null || jackIds == null) return;

        FieldInfo[] fields = data.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo field = fields[i];
            if (field.FieldType != typeof(int)) continue;
            if (field.Name == "ID") continue;
            if (!field.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase) &&
                !field.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase)) continue;

            int id = (int)field.GetValue(data);
            if (id != 0 && !jackIds.Contains(id)) jackIds.Add(id);
        }
    }

    static void checkXmlRootAliases(ValidationReport report, Dictionary<string, string> aliases, OSLDeviceRegistration registration)
    {
        if (registration == null) return;

        if (registration.IsCore)
        {
            checkXmlRootAlias(report, aliases, registration.deviceId, registration);
            checkXmlRootAlias(report, aliases, registration.deviceLocalId, registration);
        }
        else
        {
            checkXmlRootAlias(report, aliases, registration.canonicalDeviceId, registration);
        }

        if (registration.legacyDeviceIds != null)
        {
            for (int i = 0; i < registration.legacyDeviceIds.Length; ++i)
            {
                checkXmlRootAlias(report, aliases, registration.legacyDeviceIds[i], registration);
            }
        }

        checkArrayDuplicates(report, aliases, registration.legacyXmlRootNames, describe(registration), "Ambiguous legacyXmlRootName");
    }

    static void checkXmlRootAlias(ValidationReport report, Dictionary<string, string> aliases, string deviceId, OSLDeviceRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        string label = describe(registration);
        checkDuplicate(report, aliases, deviceId, label, "Ambiguous legacyXmlRootName");
        checkDuplicate(report, aliases, deviceId + "s", label, "Ambiguous legacyXmlRootName");
        checkDuplicate(report, aliases, pluralizeDeviceId(deviceId), label, "Ambiguous legacyXmlRootName");
    }

    static void checkArrayDuplicates(ValidationReport report, Dictionary<string, string> lookup, string[] values, string label, string message)
    {
        if (values == null) return;
        for (int i = 0; i < values.Length; ++i)
        {
            checkDuplicate(report, lookup, values[i], label, message);
        }
    }

    static void checkDuplicate(ValidationReport report, Dictionary<string, string> lookup, string key, string label, string message)
    {
        if (lookup == null || string.IsNullOrWhiteSpace(key)) return;
        if (!lookup.TryGetValue(key, out string existing))
        {
            lookup[key] = label;
            return;
        }

        if (existing == label) return;
        report.Error(message + " '" + key + "' between " + existing + " and " + label + ".");
    }

    static void require(ValidationReport report, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) report.Error(message);
    }

    static string describe(OSLDeviceRegistration registration)
    {
        if (registration == null) return "unknown";
        if (!string.IsNullOrWhiteSpace(registration.canonicalDeviceId)) return registration.canonicalDeviceId;
        if (!string.IsNullOrWhiteSpace(registration.deviceId)) return registration.deviceId;
        return "unknown";
    }

    static string getResourceName(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return "";

        string normalizedPath = resourcePath.Replace('\\', '/');
        int slashIndex = normalizedPath.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex >= normalizedPath.Length - 1) return normalizedPath;
        return normalizedPath.Substring(slashIndex + 1);
    }

    static string pluralizeDeviceId(string deviceId)
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
