using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class Sheriff : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            new(
                typeof(Sheriff),
                player => new Sheriff(player),
                CustomRoles.Sheriff,
                RoleTypes.Impostor,
                CustomRoleTypes.Crewmate,
                20400,
                SetupOptionItem,
                "#f8cd46",
                true
            );
        public Sheriff(PlayerControl player)
        : base(
            RoleInfo,
            player,
            false
        )
        {
            ShotLimit = ShotLimitOpt.GetInt();
            CurrentKillCooldown = KillCooldown.GetFloat();
        }

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        private static OptionItem ShotLimitOpt;
        private static OptionItem CanKillAllAlive;
        public static OptionItem CanKillNeutrals;
        enum OptionName
        {
            KillCooldown,
            SheriffMisfireKillsTarget,
            SheriffShotLimit,
            SheriffCanKillAllAlive,
            SheriffCanKillNeutrals,
            SheriffCanKill,
        }
        public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
        public int ShotLimit = 0;
        public float CurrentKillCooldown = 30;
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        private static void SetupOptionItem()
        {
            var id = RoleInfo.ConfigId;
            var tab = RoleInfo.Tab;
            var parent = RoleInfo.RoleOption;
            KillCooldown = FloatOptionItem.Create(id + 10, OptionName.KillCooldown, new(0f, 990f, 1f), 30f, tab, false).SetParent(parent)
                .SetValueFormat(OptionFormat.Seconds);
            MisfireKillsTarget = BooleanOptionItem.Create(id + 11, OptionName.SheriffMisfireKillsTarget, false, tab, false).SetParent(parent);
            ShotLimitOpt = IntegerOptionItem.Create(id + 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, tab, false).SetParent(parent)
                .SetValueFormat(OptionFormat.Times);
            CanKillAllAlive = BooleanOptionItem.Create(id + 15, OptionName.SheriffCanKillAllAlive, true, tab, false).SetParent(parent);
            SetUpKillTargetOption(CustomRoles.Madmate, id + 13);
            CanKillNeutrals = StringOptionItem.Create(id + 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, tab, false).SetParent(parent);
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
            KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
            KillTargetOptions[role].ReplacementDictionary = replacementDic;
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
        public override float SetKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
        public override bool CanUseKillButton()
            => !Main.PlayerStates[Player.PlayerId].IsDead
            && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
            && ShotLimit > 0;

        // == CheckMurder関連処理 ==
        public override IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target, CustomRoleManager.CheckMurderInfo info)
        {
            yield return 1_001_000;
            ShotLimit--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "Sheriff");
            SendRPC();
            if (!CanBeKilledBy(target))
            {
                killer.RpcMurderPlayer(killer);
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                if (!MisfireKillsTarget.GetBool())
                {
                    info.CancelAndAbort();
                }
            }
            killer.ResetKillCooldown();
            yield break;
        }
        // ==/CheckMurder関連処理 ==
        public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
        public static bool CanBeKilledBy(PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            return cRole.GetCustomRoleTypes() switch
            {
                CustomRoleTypes.Impostor => true,
                CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
                _ => false,
            };
        }
    }
}