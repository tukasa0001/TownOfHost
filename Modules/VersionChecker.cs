using System;
using UnityEngine;

namespace TownOfHost.Modules;

public static class VersionChecker
{
    public static readonly Version LowestSupportedVersion = new("2023.3.28");
    public static bool IsSupported { get; private set; } = true;

    public static void Check()
    {
        var amongUsVersion = Version.Parse(Application.version);
        IsSupported = amongUsVersion >= LowestSupportedVersion;
        if (!IsSupported)
        {
            ErrorText.Instance.AddError(ErrorCode.UnsupportedVersion);
        }
    }
}
