using System;
using UnityEngine;

public static class OSLDeviceBuildPolicy
{
    public const string OpenMultiLabSuffix = "OpenMultiLab";
    public const string OpenSoundLabSuffix = "OpenSoundLab";

    public static string ApplicationId => Application.identifier;

    public static bool AllowsLocalDeveloperContent => EndsWithApplicationId(OpenMultiLabSuffix);

    public static bool IsOpenSoundLabReleaseId => EndsWithApplicationId(OpenSoundLabSuffix);

    static bool EndsWithApplicationId(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix)) return false;
        string applicationId = ApplicationId;
        if (string.IsNullOrWhiteSpace(applicationId)) return false;
        return applicationId.EndsWith(suffix, StringComparison.Ordinal);
    }
}
