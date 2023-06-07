using System.Collections.Generic;
using System.Text;
using Hazel;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Witch : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Witch),
                player => new Witch(player),
                CustomRoles.Witch,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                1500,
                SetupOptionItem,
                "wi"
            );
        public Witch(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }
        public override void OnDestroy()
        {
            Witches.Clear();
            SpelledPlayer.Clear();
            CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
        }
        public static OptionItem OptionModeSwitchAction;
        enum OptionName
        {
            WitchModeSwitchAction,
        }
        public enum SwitchTrigger
        {
            TriggerKill,
            TriggerVent,
            TriggerDouble,
        };

        public bool IsSpellMode;
        public List<byte> SpelledPlayer = new();
        public SwitchTrigger NowSwitchTrigger;

        public static List<Witch> Witches = new();
        public static void SetupOptionItem()
        {
            OptionModeSwitchAction = StringOptionItem.Create(RoleInfo, 10, OptionName.WitchModeSwitchAction, EnumHelper.GetAllNames<SwitchTrigger>(), 0, false);
        }
        public override void Add()
        {
            IsSpellMode = false;
            SpelledPlayer.Clear();
            NowSwitchTrigger = (SwitchTrigger)OptionModeSwitchAction.GetValue();
            Witches.Add(this);
            Player.AddDoubleTrigger();

        }
        private void SendRPC(bool doSpell, byte target = 255)
        {
            using var sender = CreateSender(CustomRPC.WitchSync);
            sender.Writer.Write(doSpell);
            if (doSpell)
            {
                sender.Writer.Write(target);
            }
            else
            {
                sender.Writer.Write(IsSpellMode);
            }
        }

        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.WitchSync) return;

            var doSpel = reader.ReadBoolean();
            if (doSpel)
            {
                var spelledId = reader.ReadByte();
                if (spelledId == 255)
                {
                    SpelledPlayer.Clear();
                }
                else
                {
                    SpelledPlayer.Add(spelledId);
                }
            }
            else
            {
                IsSpellMode = reader.ReadBoolean();
            }
        }
        public void SwitchSpellMode(bool kill)
        {
            bool needSwitch = false;
            switch (NowSwitchTrigger)
            {
                case SwitchTrigger.TriggerKill:
                    needSwitch = kill;
                    break;
                case SwitchTrigger.TriggerVent:
                    needSwitch = !kill;
                    break;
            }
            if (needSwitch)
            {
                IsSpellMode = !IsSpellMode;
                SendRPC(false);
                Utils.NotifyRoles(SpecifySeer: Player);
            }
        }
        public static bool IsSpelled(byte target = 255)
        {
            foreach (var witch in Witches)
            {
                if (target == 255 && witch.SpelledPlayer.Count != 0) return true;

                if (witch.SpelledPlayer.Contains(target))
                {
                    return true;
                }
            }
            return false;
        }
        public void SetSpelled(PlayerControl target)
        {
            if (!IsSpelled(target.PlayerId))
            {
                SpelledPlayer.Add(target.PlayerId);
                SendRPC(true, target.PlayerId);
                //キルクールの適正化
                Player.SetKillCooldown();
            }
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var (killer, target) = info.AttemptTuple;
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                info.DoKill = killer.CheckDoubleTrigger(target, () => { SetSpelled(target); });
            }
            else
            {
                if (IsSpellMode)
                {//呪いならキルしない
                    info.DoKill = false;
                    SetSpelled(target);
                }
                SwitchSpellMode(true);
            }
            //切れない相手ならキルキャンセル
            info.DoKill &= info.CanKill;
        }
        public override void AfterMeetingTasks()
        {
            if (Player.IsAlive() || MyState.DeathReason != CustomDeathReason.Vote)
            {//吊られなかった時呪いキル発動
                var spelledIdList = new List<byte>();
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (SpelledPlayer.Contains(pc.PlayerId) && !Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                    {
                        pc.SetRealKiller(Player);
                        spelledIdList.Add(pc.PlayerId);
                    }
                }
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Spell, spelledIdList.ToArray());
            }
            //実行してもしなくても呪いはすべて解除
            SpelledPlayer.Clear();
            if (AmongUsClient.Instance.AmHost)
                SendRPC(true);
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting && IsSpelled(seen.PlayerId))
            {
                return Utils.ColorString(Palette.ImpostorRed, "†");
            }
            return "";
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (!Is(seen) || isForMeeting) return "";

            var sb = new StringBuilder();
            sb.Append(isForHud ? GetString("WitchCurrentMode") : "Mode:");
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                sb.Append(GetString("WitchModeDouble"));
            }
            else
            {
                sb.Append(IsSpellMode ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
            }
            return sb.ToString();
        }
        public bool OverrideKillButtonText(out string text)
        {
            if (NowSwitchTrigger != SwitchTrigger.TriggerDouble && IsSpellMode)
            {
                text = GetString("WitchSpellButtonText");
                return true;
            }
            text = default;
            return false;
        }
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            if (NowSwitchTrigger is SwitchTrigger.TriggerVent)
            {
                SwitchSpellMode(false);
            }
            return true;
        }
    }
}