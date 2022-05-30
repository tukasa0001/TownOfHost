using System.Collections.Generic;

namespace TownOfHost
{
    public static class Ninja
    {
        static readonly int Id = 2600;
        static readonly List<PlayerControl> NinjaKillTarget = new();
        static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Ninja);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void KillCheck(this PlayerControl __instance, PlayerControl target)
        {
            if (Main.CheckShapeshift[__instance.PlayerId])
            {
                Logger.Info("Ninja ShapeShifting kill", "Ninja");
                __instance.RpcGuardAndKill(target);
                Main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 2;
                NinjaKillTarget.Add(target);
                __instance.CustomSyncSettings();//負荷軽減のため、__instanceだけがCustomSyncSettingsを実行
            }
        }
        public static void ShapeShiftCheck(this PlayerControl pc, bool shapeshifting)
        {
            if (!shapeshifting)
            Logger.Info("ShapeShift Release", "Ninja");
            {
                foreach (var ni in NinjaKillTarget)
                {
                    if (!ni.Data.IsDead)
                    {
                        Main.AllPlayerKillCooldown[pc.PlayerId] = Options.BHDefaultKillCooldown.GetFloat();
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