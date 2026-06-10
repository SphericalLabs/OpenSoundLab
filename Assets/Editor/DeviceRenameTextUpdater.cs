using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

internal static class DeviceRenameTextUpdater
{
    public static void Apply(DeviceRenamePlan plan, List<string> errors)
    {
        if (!plan.request.rewriteScripts) return;

        for (int i = 0; i < plan.textRewrites.Count; ++i)
        {
            ApplyRewrite(plan.textRewrites[i], errors);
        }
    }

    static void ApplyRewrite(DeviceRenameTextRewrite rewrite, List<string> errors)
    {
        if (!Directory.Exists(rewrite.scriptFolder))
        {
            errors.Add("Script folder is missing: " + rewrite.scriptFolder);
            return;
        }

        RewriteScriptsInFolder(rewrite);
        if (!rewrite.appendLegacyDataShell) return;

        string interfacePath = rewrite.legacyInterfaceScriptPath;
        if (string.IsNullOrWhiteSpace(interfacePath)) interfacePath = FindInterfaceScript(rewrite.scriptFolder, rewrite.legacyLocalDeviceId);
        AppendLegacyDataShell(interfacePath, rewrite.oldNamespace, rewrite.oldCanonicalDeviceId, rewrite.oldDataClassName, rewrite.newDataTypeName);
    }

    static void RewriteScriptsInFolder(DeviceRenameTextRewrite rewrite)
    {
        string[] files = Directory.GetFiles(rewrite.scriptFolder, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; ++i)
        {
            string source = File.ReadAllText(files[i]);
            string updated = source;
            updated = ReplaceText(updated, rewrite.oldCanonicalDeviceId, rewrite.newCanonicalDeviceId);
            updated = ReplaceText(updated, rewrite.oldDataTypeName, rewrite.newDataTypeName);
            updated = ReplaceText(updated, rewrite.oldNamespace, rewrite.newNamespace);
            updated = ReplaceIdentifier(updated, rewrite.oldInterfaceClassName, rewrite.newInterfaceClassName);
            updated = ReplaceIdentifier(updated, rewrite.oldSignalClassName, rewrite.newSignalClassName);
            updated = ReplaceIdentifier(updated, rewrite.oldDataClassName, rewrite.newDataClassName);
            updated = NormalizeDataClassDeclaration(updated, rewrite.newDataClassName);
            if (updated != source) File.WriteAllText(files[i], updated);
        }
    }

    static string ReplaceText(string source, string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(oldValue)) return source;
        if (oldValue == newValue) return source;
        return source.Replace(oldValue, newValue);
    }

    static string NormalizeDataClassDeclaration(string source, string dataClassName)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(dataClassName)) return source;

        string className = Regex.Escape(dataClassName);
        string pattern = @"public\s+class\s+[A-Za-z_][A-Za-z0-9_\.]*\." + className + @"\s*:\s*(?:global::)?InstrumentData";
        return Regex.Replace(source, pattern, "public class " + dataClassName + " : InstrumentData");
    }

    static string ReplaceIdentifier(string source, string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(oldValue)) return source;
        if (oldValue == newValue) return source;

        string pattern = "(?<![A-Za-z0-9_\\.:])" + Regex.Escape(oldValue) + "(?![A-Za-z0-9_])";
        return Regex.Replace(source, pattern, newValue);
    }

    static void AppendLegacyDataShell(string path, string oldNamespace, string oldCanonicalDeviceId, string oldDataClassName, string newDataTypeName)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (string.IsNullOrWhiteSpace(oldCanonicalDeviceId) || string.IsNullOrWhiteSpace(oldDataClassName) || string.IsNullOrWhiteSpace(newDataTypeName)) return;
        if (!File.Exists(path)) return;

        string source = File.ReadAllText(path);
        if (source.Contains("[XmlType(\"" + oldCanonicalDeviceId + "\")]") || source.Contains("[XmlType(\"" + oldCanonicalDeviceId + "\")")) return;

        string declaration = "[XmlType(\"" + oldCanonicalDeviceId + "\")]\npublic class " + oldDataClassName + " : global::" + newDataTypeName + "\n{\n}\n";
        string shell = string.IsNullOrWhiteSpace(oldNamespace) ? declaration : "namespace " + oldNamespace + "\n{\n" + declaration + "}\n";
        File.WriteAllText(path, source + "\n" + shell);
    }

    static string FindInterfaceScript(string scriptFolder, string localDeviceId)
    {
        if (string.IsNullOrWhiteSpace(scriptFolder) || !Directory.Exists(scriptFolder)) return "";

        string expected = scriptFolder + "/" + OSLDeviceIdentityCompatibility.ToLowerCamel(OSLDeviceIdentityCompatibility.ToPascalIdentifier(localDeviceId)) + "DeviceInterface.cs";
        if (File.Exists(expected)) return expected;

        string[] files = Directory.GetFiles(scriptFolder, "*DeviceInterface.cs", SearchOption.AllDirectories);
        return files.Length > 0 ? files[0].Replace('\\', '/') : "";
    }
}
