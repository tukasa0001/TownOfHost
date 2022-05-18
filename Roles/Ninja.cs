using System.Collections.Generic;

namespace TownOfHost
{
    public static class Ninja
    {
        static int Id = 2600;
        static List<PlayerControl> NinjaKillTarget = new List<PlayerControl>();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Ninja);
        }
        public static void NinjaShapeShiftingKill(PlayerControl __instance, PlayerControl target)
        {
            if (main.CheckShapeshift[__instance.PlayerId])
            {
                Logger.info("Ninja ShapeShifting kill");
                main.AllPlayerKillCooldown[__instance.PlayerId] *= 2;
                __instance.RpcGuardAndKill(target);
                NinjaKillTarget.Add(target);
            }
        }
        public static void ShapeShiftCheck(PlayerControl pc, bool shapeshifting)
        {
            if (!shapeshifting)
            Logger.info("Ninja ShapeShift Release");
            {
                foreach (var ni in NinjaKillTarget)
                {
                    pc.RpcMurderPlayer(ni);
                    NinjaKillTarget.Remove(ni);
                    pc.RpcRevertShapeshift(true);//他視点シェイプシフトが解除されないように見える場合があるため、強制解除
                    pc.RpcGuardAndKill(pc);
                    return;
                }
            }
        }
    }
}