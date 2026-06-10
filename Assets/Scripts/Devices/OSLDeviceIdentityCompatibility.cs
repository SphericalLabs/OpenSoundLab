using System;
using UnityEngine;

public static class OSLDeviceIdentityCompatibility
{
    public const string CoreVendorId = "io.sphericals";
    public const string CoreDomain = "io.sphericals.osl";
    public const string CoreCollectionDomain = CoreDomain + ".core";
    public const string AddonCollectionSegment = "addons";

    public static string BuildCanonicalDeviceId(string collectionId, string deviceLocalId)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) return deviceLocalId;
        if (string.IsNullOrWhiteSpace(deviceLocalId)) return collectionId;
        return collectionId + "." + deviceLocalId;
    }

    public static string BuildCoreCollectionId(string collectionLocalId)
    {
        if (string.IsNullOrWhiteSpace(collectionLocalId)) return CoreCollectionDomain;
        return CoreCollectionDomain + "." + NormalizeDomain(collectionLocalId);
    }

    public static string BuildAddonCollectionId(string makerDomain, string collectionLocalId)
    {
        string collectionDomain = GetAddonCollectionDomain(makerDomain);
        if (string.IsNullOrWhiteSpace(collectionLocalId)) return collectionDomain;
        return collectionDomain + "." + NormalizeDomain(collectionLocalId);
    }

    public static string GetAddonCollectionDomain(string makerDomain)
    {
        string domain = NormalizeDomain(makerDomain);
        if (string.IsNullOrWhiteSpace(domain)) return AddonCollectionSegment;
        if (domain.EndsWith("." + AddonCollectionSegment, StringComparison.Ordinal)) return domain;
        return domain + "." + AddonCollectionSegment;
    }

    public static string GetLocalIdFromCanonicalId(string canonicalDeviceId)
    {
        if (string.IsNullOrWhiteSpace(canonicalDeviceId)) return "";

        int separatorIndex = canonicalDeviceId.LastIndexOf('.');
        if (separatorIndex < 0 || separatorIndex >= canonicalDeviceId.Length - 1) return canonicalDeviceId;
        return canonicalDeviceId.Substring(separatorIndex + 1);
    }

    public static string GetResourceName(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return "";

        string normalizedPath = resourcePath.Replace('\\', '/');
        int slashIndex = normalizedPath.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex >= normalizedPath.Length - 1) return normalizedPath;
        return normalizedPath.Substring(slashIndex + 1);
    }

    public static string GetResourcePathFromAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return "";

        string normalizedPath = assetPath.Replace('\\', '/');
        int resourcesIndex = normalizedPath.LastIndexOf("/Resources/", StringComparison.Ordinal);
        if (resourcesIndex < 0) return "";

        string relativePath = normalizedPath.Substring(resourcesIndex + "/Resources/".Length);
        int extensionIndex = relativePath.LastIndexOf('.');
        if (extensionIndex > 0) relativePath = relativePath.Substring(0, extensionIndex);
        return relativePath;
    }

    public static string GetParentPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";

        string normalizedPath = path.Replace('\\', '/');
        int slashIndex = normalizedPath.LastIndexOf('/');
        if (slashIndex <= 0) return "";
        return normalizedPath.Substring(0, slashIndex);
    }

    public static string NormalizeDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return value.Trim().ToLowerInvariant();
    }

    public static bool IsNamespacePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        string[] pieces = value.Split('.');
        for (int i = 0; i < pieces.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(pieces[i])) return false;
            if (CleanIdentifier(pieces[i]) != pieces[i]) return false;
        }

        return true;
    }

    public static string ToNamespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        string[] pieces = value.Split('.');
        for (int i = 0; i < pieces.Length; ++i) pieces[i] = CleanIdentifier(pieces[i]);
        return string.Join(".", pieces);
    }

    public static string CleanIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        string clean = value.Trim();
        if (!IsIdentifierStart(clean[0])) return "";

        for (int i = 0; i < clean.Length; ++i)
        {
            if (!IsIdentifierPart(clean[i])) return "";
        }

        return clean;
    }

    public static string ToPascalIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        string[] pieces = value.Split(new char[] { '.', '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string result = "";
        for (int i = 0; i < pieces.Length; ++i)
        {
            string piece = CleanIdentifier(pieces[i]);
            if (string.IsNullOrWhiteSpace(piece)) continue;
            result += char.ToUpperInvariant(piece[0]) + (piece.Length > 1 ? piece.Substring(1) : "");
        }

        return result;
    }

    public static string ToLowerCamel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return char.ToLowerInvariant(value[0]) + (value.Length > 1 ? value.Substring(1) : "");
    }

    public static string ToDisplayName(string value)
    {
        string pascal = ToPascalIdentifier(value);
        if (string.IsNullOrWhiteSpace(pascal)) return value;
        return SplitPascal(pascal);
    }

    public static string SplitPascal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        string result = "";
        for (int i = 0; i < value.Length; ++i)
        {
            char current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1])) result += " ";
            result += current;
        }

        return result;
    }

    public static string GetCoreCollectionLocalId(DeviceCategory deviceCategory)
    {
        return deviceCategory.ToString().ToLowerInvariant();
    }

    public static string GetCategoryDisplayName(DeviceCategory deviceCategory)
    {
        return SplitPascal(deviceCategory.ToString());
    }

    public static string GetVendorId(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return "";
        int marker = domain.IndexOf(".osl", StringComparison.Ordinal);
        if (marker > 0) return domain.Substring(0, marker);

        marker = domain.IndexOf(".opensoundlab", StringComparison.Ordinal);
        if (marker > 0) return domain.Substring(0, marker);

        return domain;
    }

    public static string GetNamespaceName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return "";

        int separator = typeName.LastIndexOf('.');
        if (separator <= 0) return "";
        return typeName.Substring(0, separator);
    }

    public static string GetTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return "";

        int separator = typeName.LastIndexOf('.');
        if (separator < 0 || separator >= typeName.Length - 1) return typeName;
        return typeName.Substring(separator + 1);
    }

    public static string PluralizeDeviceId(string deviceId)
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

    public static string[] AddUnique(string[] values, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return values != null ? values : new string[0];
        if (values == null) values = new string[0];

        for (int i = 0; i < values.Length; ++i)
        {
            if (values[i] == value) return values;
        }

        string[] next = new string[values.Length + 1];
        Array.Copy(values, next, values.Length);
        next[next.Length - 1] = value;
        return next;
    }

    static bool IsIdentifierStart(char value)
    {
        return value == '_' || char.IsLetter(value);
    }

    static bool IsIdentifierPart(char value)
    {
        return value == '_' || char.IsLetterOrDigit(value);
    }
}
