using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Sheriff
    {
        static readonly int Id = 20400;
        static List<byte> playerIdList = new();

        public static CustomOption KillCooldown;
        public static CustomOption CanKillArsonist;
        public static CustomOption CanKillMadmate;
        public static CustomOption CanKillJester;
        public static CustomOption CanKillTerrorist;
        public static CustomOption CanKillOpportunist;
        public static CustomOption CanKillEgoist;
        public static CustomOption CanKillEgoShrodingerCat;
        public static CustomOption CanKillExecutioner;
        public static CustomOption CanKillCrewmatesAsIt;
        public static CustomOption ShotLimitOpt;

        public static Dictionary<byte, float> ShotLimit;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Sniper);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillArsonist = CustomOption.Create(Id + 17, Color.white, "SheriffCanKillArsonist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillMadmate = CustomOption.Create(Id + 11, Color.white, "SheriffCanKillMadmate", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJester = CustomOption.Create(Id + 12, Color.white, "SheriffCanKillJester", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillTerrorist = CustomOption.Create(Id + 13, Color.white, "SheriffCanKillTerrorist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillOpportunist = CustomOption.Create(Id + 14, Color.white, "SheriffCanKillOpportunist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillEgoist = CustomOption.Create(Id + 18, Color.white, "SheriffCanKillEgoist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillEgoShrodingerCat = CustomOption.Create(Id + 19, Color.white, "SheriffCanKillEgoShrodingerCat", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillExecutioner = CustomOption.Create(Id + 19, Color.white, "SheriffCanKillExecutioner", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillCrewmatesAsIt = CustomOption.Create(Id + 15, Color.white, "SheriffCanKillCrewmatesAsIt", false, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            ShotLimitOpt = CustomOption.Create(Id + 16, Color.white, "SheriffShotLimit", 15, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
            RoleInit(playerId);
        }
        private static void RoleInit(byte id)
        {
            var sheriff = Utils.GetPlayerById(id);
            if (sheriff == null) return;
            if (sheriff.Is(CustomRoles.Sheriff))
            {
                ShotLimit[id] = ShotLimitOpt.GetFloat();
                SendRPC(id);
                Logger.Info($"{sheriff.GetNameWithRole()} : 残り{ShotLimit[id]}発", "Sheriff");
            }
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            byte SheriffId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (ShotLimit.ContainsKey(SheriffId))
                ShotLimit[SheriffId] = Limit;
            else
                ShotLimit.Add(SheriffId, ShotLimitOpt.GetFloat());
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead)
                return false;

            if (ShotLimit[player.PlayerId] == 0)
            {
                //Logger.info($"{player.GetNameWithRole()} はキル可能回数に達したため、RoleTypeを守護天使に変更しました。", "Sheriff");
                //player.RpcSetRoleDesync(RoleTypes.GuardianAngel);
                //Utils.hasTasks(player.Data, false);
                //Utils.NotifyRoles();
                return false;
            }
            return true;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target, string Process)
        {
            switch (Process)
            {
                case "RemoveShotLimit":
                    ShotLimit[killer.PlayerId]--;
                    Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Sheriff");
                    SendRPC(killer.PlayerId);
                    break;
                case "Suicide":
                    if (target.CanBeKilledBySheriff())
                    {
                        PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Misfire);
                        killer.RpcMurderPlayerV2(killer);
                        if (CanKillCrewmatesAsIt.GetBool())
                            killer.RpcMurderPlayerV2(target);
                        return false;
                    }
                    break;
            }
            return true;
        }
        public static string GetShotLimit(byte playerId) => Helpers.ColorString(Color.yellow, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
        public static bool CanBeKilledBySheriff(this PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            return cRole switch
            {
                CustomRoles.Jester => CanKillJester.GetBool(),
                CustomRoles.Terrorist => CanKillTerrorist.GetBool(),
                CustomRoles.Executioner => CanKillExecutioner.GetBool(),
                CustomRoles.Opportunist => CanKillOpportunist.GetBool(),
                CustomRoles.Arsonist => CanKillArsonist.GetBool(),
                CustomRoles.Egoist => CanKillEgoist.GetBool(),
                CustomRoles.EgoSchrodingerCat => CanKillEgoShrodingerCat.GetBool(),
                CustomRoles.SchrodingerCat => true,
                _ => cRole.GetRoleType() switch
                {
                    RoleType.Impostor => true,
                    RoleType.Madmate => CanKillMadmate.GetBool(),
                    _ => false,
                }
            };
        }
    }
}