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
        public static SimpleRoleInfo RoleInfo { get; } =
            new(
                CustomRoles.Sheriff,
                RoleType.Crewmate,
                20400,
                "#f8cd46",
                SetupCustomOption
            );

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        private static OptionItem ShotLimitOpt;
        private static OptionItem CanKillAllAlive;
        public static OptionItem CanKillNeutrals;
        public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
        public Sheriff(PlayerControl player)
        : base(
            player,
            false
        )
        { }
        public int ShotLimit = 0;
        public float CurrentKillCooldown = 30;
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        public static void SetupCustomOption()
        {
            var id = RoleInfo.ConfigId;
            var tab = RoleInfo.Tab;
            var roleName = RoleInfo.RoleName;
            SetupRoleOptions(id, tab, roleName);
            var roleOption = RoleInfo.RoleOption;
            KillCooldown = FloatOptionItem.Create(id + 10, "KillCooldown", new(0f, 990f, 1f), 30f, tab, false).SetParent(roleOption)
                .SetValueFormat(OptionFormat.Seconds);
            MisfireKillsTarget = BooleanOptionItem.Create(id + 11, "SheriffMisfireKillsTarget", false, tab, false).SetParent(roleOption);
            ShotLimitOpt = IntegerOptionItem.Create(id + 12, "SheriffShotLimit", new(1, 15, 1), 15, tab, false).SetParent(roleOption)
                .SetValueFormat(OptionFormat.Times);
            CanKillAllAlive = BooleanOptionItem.Create(id + 15, "SheriffCanKillAllAlive", true, tab, false).SetParent(roleOption);
            SetUpKillTargetOption(CustomRoles.Madmate, id + 13);
            CanKillNeutrals = StringOptionItem.Create(id + 14, "SheriffCanKillNeutrals", KillOption, 0, tab, false).SetParent(roleOption);
            SetUpNeutralOptions(id + 30);
        }
        public static void SetUpNeutralOptions(int id)
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
        public static void SetUpKillTargetOption(CustomRoles role, int id, bool defaultValue = true, OptionItem parent = null)
        {
            if (parent == null) parent = RoleInfo.RoleOption;
            var roleName = Utils.GetRoleName(role) + role switch
            {
                CustomRoles.EgoSchrodingerCat => $" {GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Egoist) } })}",
                CustomRoles.JSchrodingerCat => $" {GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Jackal) } })}",
                _ => "",
            };
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            KillTargetOptions[role] = BooleanOptionItem.Create(id, "SheriffCanKill%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
            KillTargetOptions[role].ReplacementDictionary = replacementDic;
        }
        public override void Init()
        {
            ShotLimit = ShotLimitOpt.GetInt();
            CurrentKillCooldown = KillCooldown.GetFloat();
        }
        public override void Add()
        {
            var playerId = Player.PlayerId;
            CurrentKillCooldown = KillCooldown.GetFloat();

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            ShotLimit = ShotLimitOpt.GetInt();
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Sheriff");
        }
        private void SendRPC()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(ShotLimit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.SetSheriffShotLimit) return;

            ShotLimit = reader.ReadInt32();
        }
        public override void SetKillCooldown() => Main.AllPlayerKillCooldown[Player.PlayerId] = CanUseKillButton() ? CurrentKillCooldown : 0f;
        public override bool CanUseKillButton()
            => !Main.PlayerStates[Player.PlayerId].IsDead
            && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
            && ShotLimit > 0;

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
        public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
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