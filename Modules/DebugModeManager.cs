using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TownOfHost
{
    public static class DebugModeManager
    {
        public static readonly bool AmDebugger =
#if DEBUG
    true;
#else
    false;
#endif
        public static bool IsDebugMode => EnableDebugMode != null && EnableDebugMode.GetBool();

        public static CustomOption EnableDebugMode;

        public static void SetupCustomOption()
        {
            EnableDebugMode = CustomOption.Create(2, TabGroup.MainSettings, Color.green, "EnableDebugMode", false, null, true);
        }
    }
}