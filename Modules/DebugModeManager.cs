using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TownOfHost
{
    public static class DebugModeManager
    {
        public static readonly bool IsDebugBuild =
#if DEBUG
    true;
#else
    false;
#endif
        public static bool IsDebugMatch => EnableDebugMatch != null && EnableDebugMatch.GetBool();

        public static CustomOption EnableDebugMatch;

        public static void SetupCustomOption()
        {
            EnableDebugMatch = CustomOption.Create(2, TabGroup.MainSettings, Color.green, "EnableDebugMatch", false, null, true);
        }
    }
}