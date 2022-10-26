using System;
using System.Collections.Generic;
using System.Linq;


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
    }
}