using System;
using UnityEngine;

namespace TownOfHost.Modules;

public static class VersionChecker
{
    public static bool IsSupported { get; private set; } = true;

    public static void Check()
    {
        var amongUsVersion = Version.Parse(Application.version);
        var lowestSupportedVersion = Version.Parse(Main.LowestSupportedVersion);
        IsSupported = amongUsVersion >= lowestSupportedVersion;
        if (!IsSupported)
        {
            ErrorText.Instance.AddError(ErrorCode.UnsupportedVersion);
        }
    }
}
