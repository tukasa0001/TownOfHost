using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Sheriff
    {
        private static readonly int Id = 20400;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        private static OptionItem ShotLimitOpt;
        public static OptionItem CanKillMadmates;
        public static OptionItem CanKillNeutrals;
        public static OptionItem CanKillJester;
        public static OptionItem CanKillTerrorist;
        public static OptionItem CanKillOpportunist;
        public static OptionItem CanKillArsonist;
        public static OptionItem CanKillEgoist;
        public static OptionItem CanKillEgoSchrodingerCat;
        public static OptionItem CanKillExecutioner;
        public static OptionItem CanKillJackal;
        public static OptionItem CanKillJSchrodingerCat;

        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        public static Dictionary<string, string> SheriffCanKillRole(CustomRoles role)
        {
            var roleName = Utils.GetRoleName(role);
            if (role == CustomRoles.EgoSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Egoist) } });
            if (role == CustomRoles.JSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Jackal) } });
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            return replacementDic;
        }
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "KillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], format: "Seconds");
            MisfireKillsTarget = OptionItem.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "SheriffMisfireKillsTarget", false, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            ShotLimitOpt = OptionItem.Create(Id + 12, TabGroup.CrewmateRoles, Color.white, "SheriffShotLimit", 15, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], format: "Times");
            CanKillMadmates = OptionItem.Create(Id + 13, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], replacementDic: SheriffCanKillRole(CustomRoles.Madmate));
            CanKillNeutrals = OptionItem.Create(Id + 14, TabGroup.CrewmateRoles, Color.white, "SheriffCanKillNeutrals", KillOption, KillOption[0], Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJester = OptionItem.Create(Id + 15, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Jester));
            CanKillTerrorist = OptionItem.Create(Id + 16, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Terrorist));
            CanKillOpportunist = OptionItem.Create(Id + 17, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Opportunist));
            CanKillArsonist = OptionItem.Create(Id + 18, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Arsonist));
            CanKillEgoist = OptionItem.Create(Id + 19, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Egoist));
            CanKillEgoSchrodingerCat = OptionItem.Create(Id + 20, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.EgoSchrodingerCat));
            CanKillExecutioner = OptionItem.Create(Id + 21, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Executioner));
            CanKillJackal = OptionItem.Create(Id + 22, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Jackal));
            CanKillJSchrodingerCat = OptionItem.Create(Id + 23, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.JSchrodingerCat));
        }
        public static void Init()
        {
            playerIdList = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(Utils.GetPlayerById(id)) ? CurrentKillCooldown[id] : 0f;
        public static int ShotLimit(PlayerControl player)
            => ShotLimitOpt.GetInt() - player.GetKillCount(!(PlayerState.GetDeathReason(player.PlayerId) == PlayerState.DeathReason.Misfire));
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead)
                return false;

            if (ShotLimit(player) == 0)
            {
                //Logger.info($"{player.GetNameWithRole()} はキル可能回数に達したため、RoleTypeを守護天使に変更しました。", "Sheriff");
                //player.RpcSetRoleDesync(RoleTypes.GuardianAngel);
                //Utils.hasTasks(player.Data, false);
                //Utils.NotifyRoles();
                return false;
            }
            return true;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (!target.CanBeKilledBySheriff())
            {
                PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Misfire);
                killer.RpcMurderPlayer(killer);
                return MisfireKillsTarget.GetBool();
            }
            SetKillCooldown(killer.PlayerId);
            return true;
        }
        public static string GetShotLimit(byte playerId) => Utils.ColorString(Color.yellow, $"({ShotLimit(Utils.GetPlayerById(playerId))})");
        public static bool CanBeKilledBySheriff(this PlayerControl player)
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
                    RoleType.Impostor => true,
                    RoleType.Madmate => CanKillMadmates.GetBool(),
                    _ => false,
                }
            };
        }
    }
}