using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Ninja
    {
        static int id = 2600;
        static bool NinjaShapeShift = false;
        static List<PlayerControl> NinjaKillTarget = new List<PlayerControl>();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Ninja);
        }
        public static void NinjaShapeShiftingKill(PlayerControl __instance, PlayerControl target)
        {
            if (main.CheckShapeshift[__instance.PlayerId])
            {
                Logger.Info("Ninja ShapeShifting kill");
                NinjaShapeShift = true;
                __instance.RpcGuardAndKill(target);
                NinjaKillTarget.Add(target);
            }
        }
        public static void ShapeShiftCheck(PlayerControl pc, bool shapeshifting)
        {
            if (!shapeshifting)
            {
                foreach (var ni in NinjaKillTarget)
                {
                    pc.RpcMurderPlayer(NinjaKillTarget);
                }
                NinjaShapeShift = false;
            }
        }
    }
}