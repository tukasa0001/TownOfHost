using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Sheriff
    {
        private static readonly int Id = 20400;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        private static CustomOption CanKillArsonist;
        private static CustomOption CanKillMadmate;
        private static CustomOption CanKillJester;
        private static CustomOption CanKillTerrorist;
        private static CustomOption CanKillOpportunist;
        private static CustomOption CanKillEgoist;
        private static CustomOption CanKillEgoShrodingerCat;
        private static CustomOption CanKillExecutioner;
        private static CustomOption CanKillJackal;
        private static CustomOption CanKillJShrodingerCat;
        private static CustomOption MisfireKillsTarget;
        private static CustomOption ShotLimitOpt;

        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Sheriff);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillArsonist = CustomOption.Create(Id + 17, Color.white, "SheriffCanKillArsonist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillMadmate = CustomOption.Create(Id + 11, Color.white, "SheriffCanKillMadmate", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJester = CustomOption.Create(Id + 12, Color.white, "SheriffCanKillJester", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillTerrorist = CustomOption.Create(Id + 13, Color.white, "SheriffCanKillTerrorist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillOpportunist = CustomOption.Create(Id + 14, Color.white, "SheriffCanKillOpportunist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillEgoist = CustomOption.Create(Id + 18, Color.white, "SheriffCanKillEgoist", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillEgoShrodingerCat = CustomOption.Create(Id + 19, Color.white, "SheriffCanKillEgoShrodingerCat", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillExecutioner = CustomOption.Create(Id + 20, Color.white, "SheriffCanKillExecutioner", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJackal = CustomOption.Create(Id + 23, Color.white, "SheriffCanKillJackal", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJShrodingerCat = CustomOption.Create(Id + 24, Color.white, "SheriffCanKillJShrodingerCat", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            MisfireKillsTarget = CustomOption.Create(Id + 15, Color.white, "SheriffMisfireKillsTarget", false, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            ShotLimitOpt = CustomOption.Create(Id + 16, Color.white, "SheriffShotLimit", 15, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
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
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
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
                    if (!target.CanBeKilledBySheriff())
                    {
                        PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Misfire);
                        killer.RpcMurderPlayer(killer);
                        if (MisfireKillsTarget.GetBool())
                            killer.RpcMurderPlayer(target);
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
                CustomRoles.Jackal => CanKillJackal.GetBool(),
                CustomRoles.JSchrodingerCat => CanKillJShrodingerCat.GetBool(),
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