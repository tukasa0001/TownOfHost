using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        private static Dictionary<byte, bool> isDeadCache;
    }
}