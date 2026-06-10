using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

internal sealed class OSLDeviceManifestLoader
{
    const string supportedManifestLocationDescription = "Assets/OSLDevices/Core/<Collection>/Resources/<CollectionId>, Assets/OSLDevices/Addons/<Collection>/Resources/<CollectionId> or Packages/<PackageName>/OSLDevices/<Collection>/Resources/<CollectionId>";

    public void LoadCollectionManifests(Action<OSLDeviceCollectionManifest> registerManifest)
    {
        if (registerManifest == null) return;

        OSLDeviceCollectionManifest[] manifests = Resources.LoadAll<OSLDeviceCollectionManifest>("");
        for (int i = 0; i < manifests.Length; ++i)
        {
            if (IsSupportedCollectionManifestLocation(manifests[i])) registerManifest(manifests[i]);
        }
    }

#if UNITY_EDITOR
    bool IsSupportedCollectionManifestLocation(OSLDeviceCollectionManifest manifest)
    {
        string path = AssetDatabase.GetAssetPath(manifest);
        if (string.IsNullOrWhiteSpace(path)) return true;
        path = path.Replace('\\', '/');

        if (IsSupportedAssetsCollectionPath(path, "Assets/OSLDevices/Core/")) return true;
        if (IsSupportedAssetsCollectionPath(path, "Assets/OSLDevices/Addons/")) return true;
        if (IsSupportedPackageCollectionPath(path)) return true;

        Debug.LogWarning("OpenSoundLab: Skipping device collection manifest " + manifest.collectionId + " because it is outside the supported layouts " + supportedManifestLocationDescription + ".");
        return false;
    }

    bool IsSupportedAssetsCollectionPath(string path, string root)
    {
        if (!path.StartsWith(root, StringComparison.Ordinal)) return false;

        string localPath = path.Substring(root.Length);
        int collectionEnd = localPath.IndexOf('/');
        if (collectionEnd <= 0) return false;

        string collectionFolder = localPath.Substring(0, collectionEnd);
        string supportedPrefix = root + collectionFolder + "/Resources/";
        return path.StartsWith(supportedPrefix, StringComparison.Ordinal);
    }

    bool IsSupportedPackageCollectionPath(string path)
    {
        const string packageRoot = "Packages/";
        const string packageDeviceRoot = "OSLDevices/";
        if (!path.StartsWith(packageRoot, StringComparison.Ordinal)) return false;

        string localPath = path.Substring(packageRoot.Length);
        int packageEnd = localPath.IndexOf('/');
        if (packageEnd <= 0) return false;

        string packageLocalPath = localPath.Substring(packageEnd + 1);
        if (!packageLocalPath.StartsWith(packageDeviceRoot, StringComparison.Ordinal)) return false;

        string collectionLocalPath = packageLocalPath.Substring(packageDeviceRoot.Length);
        int collectionEnd = collectionLocalPath.IndexOf('/');
        if (collectionEnd <= 0) return false;

        string collectionFolder = collectionLocalPath.Substring(0, collectionEnd);
        string supportedPrefix = packageRoot + localPath.Substring(0, packageEnd) + "/" + packageDeviceRoot + collectionFolder + "/Resources/";
        return path.StartsWith(supportedPrefix, StringComparison.Ordinal);
    }
#else
    bool IsSupportedCollectionManifestLocation(OSLDeviceCollectionManifest manifest)
    {
        return true;
    }
#endif
}
