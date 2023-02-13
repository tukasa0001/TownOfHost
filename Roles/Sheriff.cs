using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

using TownOfHost.Roles;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost
{
    public class Sheriff : RoleBase
    {
        public static readonly RoleInfoBase BasicInfo = new(
                CustomRoles.Sheriff,
                RoleType.Crewmate,
                20400,
                "#f8cd46"
            );
        public Sheriff()
        : base(
            BasicInfo,
            false
        )
        { }

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        private static OptionItem ShotLimitOpt;
        private static OptionItem CanKillAllAlive;
        public static OptionItem CanKillNeutrals;
        public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        public override void SetupCustomOption()
        {
            SetupRoleOptions(ConfigId, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
            KillCooldown = FloatOptionItem.Create(ConfigId + 10, "KillCooldown", new(0f, 990f, 1f), 30f, Tab, false).SetParent(RoleOption)
                .SetValueFormat(OptionFormat.Seconds);
            MisfireKillsTarget = BooleanOptionItem.Create(ConfigId + 11, "SheriffMisfireKillsTarget", false, Tab, false).SetParent(RoleOption);
            ShotLimitOpt = IntegerOptionItem.Create(ConfigId + 12, "SheriffShotLimit", new(1, 15, 1), 15, Tab, false).SetParent(RoleOption)
                .SetValueFormat(OptionFormat.Times);
            CanKillAllAlive = BooleanOptionItem.Create(ConfigId + 15, "SheriffCanKillAllAlive", true, Tab, false).SetParent(RoleOption);
            SetUpKillTargetOption(CustomRoles.Madmate, ConfigId + 13);
            CanKillNeutrals = StringOptionItem.Create(ConfigId + 14, "SheriffCanKillNeutrals", KillOption, 0, Tab, false).SetParent(RoleOption);
            SetUpNeutralOptions(ConfigId + 30);
        }
        public void SetUpNeutralOptions(int id)
        {
            foreach (var neutral in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsNeutral()).ToArray())
            {
                if (neutral is CustomRoles.SchrodingerCat
                            or CustomRoles.HASFox
                            or CustomRoles.HASTroll) continue;
                SetUpKillTargetOption(neutral, id, true, CanKillNeutrals);
                id++;
            }
        }
        public void SetUpKillTargetOption(CustomRoles role, int id, bool defaultValue = true, OptionItem parent = null)
        {
            if (parent == null) parent = RoleOption;
            var roleName = Utils.GetRoleName(role) + role switch
            {
                CustomRoles.EgoSchrodingerCat => $" {GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Egoist) } })}",
                CustomRoles.JSchrodingerCat => $" {GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Jackal) } })}",
                _ => "",
            };
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            KillTargetOptions[role] = BooleanOptionItem.Create(id, "SheriffCanKill%role%", defaultValue, Tab, false).SetParent(parent);
            KillTargetOptions[role].ReplacementDictionary = replacementDic;
        }
        public override void Init()
        {
            base.Init();
            ShotLimit = new();
            CurrentKillCooldown = new();
        }
        public override void Add(byte playerId)
        {
            base.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
        }
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.SetSheriffShotLimit) return;

            byte SheriffId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (ShotLimit.ContainsKey(SheriffId))
                ShotLimit[SheriffId] = Limit;
            else
                ShotLimit.Add(SheriffId, ShotLimitOpt.GetFloat());
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 0f;
        public override bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
            && ShotLimit[playerId] > 0;

        // == CheckMurder関連処理 ==
        /*public override IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            ShotLimit[killer.PlayerId]--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Sheriff");
            SendRPC(killer.PlayerId);
            if (!CanBeKilledBy(target))
            {
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                killer.RpcMurderPlayer(killer);
                return MisfireKillsTarget.GetBool();
            }
            SetKillCooldown(killer.PlayerId);
            return true;
        }*/
        // ==/CheckMurder関連処理 ==
        public override string GetProgressText(byte playerId, bool comms = false) => Utils.ColorString(CanUseKillButton(playerId) ? Color.yellow : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
        public static bool CanBeKilledBy(PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            return cRole.GetRoleType() switch
            {
                RoleType.Impostor => true,
                RoleType.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                RoleType.Neutral => CanKillNeutrals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
                _ => false,
            };
        }
    }
}