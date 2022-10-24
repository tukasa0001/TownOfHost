using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class CorruptSheriff
    {
        private static readonly int Id = 20400;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        private static CustomOption MisfireKillsTarget;
        private static CustomOption ShotLimitOpt;
        public static CustomOption CanKillMadmates;
        public static CustomOption CanKillNeutrals;
        public static CustomOption CanKillJester;
        public static CustomOption CanKillTerrorist;
        public static CustomOption CanKillOpportunist;
        public static CustomOption CanKillArsonist;
        public static CustomOption CanKillEgoist;
        public static CustomOption CanKillEgoSchrodingerCat;
        public static CustomOption CanKillExecutioner;
        public static CustomOption CanKillJackal;
        public static CustomOption CanKillJSchrodingerCat;

        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        public static Dictionary<string, string> CSheriffCanKillRole(CustomRoles role)
        {
            var roleName = Utils.GetRoleName(role);
            if (role == CustomRoles.EgoSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Egoist) } });
            if (role == CustomRoles.JSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Jackal) } });
            Dictionary<string, string> replacementDic = new() { { "%role%", Helpers.ColorString(Utils.GetRoleColor(role), roleName) } };
            return replacementDic;
        }
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.CorruptSheriff, 1);
            KillCooldown = CustomOption.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "KillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.CorruptSheriff]);
            MisfireKillsTarget = CustomOption.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "CSheriffMisfireKillsTarget", false, Options.CustomRoleSpawnChances[CustomRoles.CorruptSheriff]);
            ShotLimitOpt = CustomOption.Create(Id + 12, TabGroup.NeutralRoles, Color.white, "CSheriffShotLimit", 15, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.CorruptSheriff]);
            CanKillNeutrals = CustomOption.Create(Id + 14, TabGroup.NeutralRoles, Color.white, "CSheriffCanKillNeutrals", KillOption, KillOption[1], Options.CustomRoleSpawnChances[CustomRoles.CorruptSheriff]);
            CanKillJester = CustomOption.Create(Id + 15, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Jester));
            CanKillTerrorist = CustomOption.Create(Id + 16, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Terrorist));
            CanKillOpportunist = CustomOption.Create(Id + 17, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Opportunist));
            CanKillArsonist = CustomOption.Create(Id + 18, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Arsonist));
            CanKillEgoist = CustomOption.Create(Id + 19, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Egoist));
            CanKillEgoSchrodingerCat = CustomOption.Create(Id + 20, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.EgoSchrodingerCat));
            CanKillExecutioner = CustomOption.Create(Id + 21, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Executioner));
            CanKillJackal = CustomOption.Create(Id + 22, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", true, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.Jackal));
            CanKillJSchrodingerCat = CustomOption.Create(Id + 23, TabGroup.NeutralRoles, Color.white, "CSheriffCanKill%role%", false, CanKillNeutrals, replacementDic: CSheriffCanKillRole(CustomRoles.JSchrodingerCat));
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
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "CorruptSheriff");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCSheriffShotLimit, SendOption.Reliable, -1);
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
                    Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "CorruptSheriff");
                    SendRPC(killer.PlayerId);
                    break;
                case "Suicide":
                    if (!target.CanBeKilledByCorruptSheriff())
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
        public static bool CanBeKilledByCorruptSheriff(this PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            if (CanKillNeutrals.GetSelection() == 0 && cRole.IsNeutral()) return true;
            return cRole switch
            {
                CustomRoles.Jester => CanKillJester.GetBool(),
                CustomRoles.Terrorist => CanKillTerrorist.GetBool(),
                CustomRoles.Executioner => CanKillExecutioner.GetBool(),
                CustomRoles.Opportunist => CanKillOpportunist.GetBool(),
                CustomRoles.Arsonist => CanKillArsonist.GetBool(),
                CustomRoles.Egoist => CanKillEgoist.GetBool(),
                CustomRoles.EgoSchrodingerCat => CanKillEgoSchrodingerCat.GetBool(),
                CustomRoles.Jackal => CanKillJackal.GetBool(),
                CustomRoles.JSchrodingerCat => CanKillJSchrodingerCat.GetBool(),
                CustomRoles.SchrodingerCat => true,
                _ => cRole.GetRoleType() switch
                {
                    RoleType.Impostor => false,
                    RoleType.Crewmate => true,
                    RoleType.Madmate => CanKillMadmates.GetBool(),
                    _ => false,
                }
            };
        }
    }
}