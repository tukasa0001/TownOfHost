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
        public static void NinjaShapeShiftingKill(this PlayerControl __instance, PlayerControl target)
        {
            if (main.CheckShapeshift[__instance.PlayerId])
            {
                Logger.info("Ninja ShapeShifting kill");
                __instance.RpcGuardAndKill(target);
                main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 2;
                NinjaKillTarget.Add(target);
                __instance.CustomSyncSettings();//負荷軽減のため、__instanceだけがCustomSyncSettingsを実行
            }
        }
        public static void ShapeShiftCheck(this PlayerControl pc, bool shapeshifting)
        {
            if (!shapeshifting)
            Logger.info("Ninja ShapeShift Release");
            {
                foreach (var ni in NinjaKillTarget)
                {
                    if (!ni.Data.IsDead)
                    {
                        main.AllPlayerKillCooldown[pc.PlayerId] = Options.BHDefaultKillCooldown.GetFloat();
                        pc.RpcMurderPlayer(ni);
                        NinjaKillTarget.Remove(ni);
                        pc.RpcRevertShapeshift(true);//他視点シェイプシフトが解除されないように見える場合があるため強制解除
                        pc.CustomSyncSettings();//負荷軽減のため、pcだけがCustomSyncSettingsを実行
                    }
                }
            }
        }
    }
}