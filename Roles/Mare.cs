using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Mare
    {
        private static readonly int Id = 2300;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldownInLightsOut;
        private static CustomOption SpeedInLightsOut;


        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Mare);
            SpeedInLightsOut = CustomOption.Create(Id + 10, Color.white, "MareSpeedInLightsOut", 2f, 0.25f, 3f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.Mare]);
            KillCooldownInLightsOut = CustomOption.Create(Id + 11, Color.white, "MareKillCooldownInLightsOut", 15f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Mare]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte mare)
        {
            playerIdList.Add(mare);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static float GetKillCooldown => Utils.IsActive(SystemTypes.Electrical) ? KillCooldownInLightsOut.GetFloat() : Options.DefaultKillCooldown;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = GetKillCooldown;
        public static void ApplyGameOptions(GameOptionsData opt, byte playerId)
        {
            Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.PlayerSpeedMod;
            if (Utils.IsActive(SystemTypes.Electrical))//もし停電発生した場合
                Main.AllPlayerSpeed[playerId] = SpeedInLightsOut.GetFloat();//Mareの速度を設定した値にする
        }

        public static void OnCheckMurder(PlayerControl killer)
        {
        }
        public static void FixedUpdate(PlayerControl player)
        {
        }
    }
}