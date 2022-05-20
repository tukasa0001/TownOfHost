using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class SlaveDriver
    {
        static int Id = 2700;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SlaveDriver);
        }
        public static void SlaveDriverKillTargetTaskCheck(this PlayerControl __instance, PlayerControl target)
        {

        }
    }
}