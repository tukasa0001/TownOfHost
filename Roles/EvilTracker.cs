using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class EvilTracker
    {
        private static readonly int Id = 2900;
        public static List<byte> playerIdList = new();

        public static OptionItem CanSeeKillFlash;
        public static OptionItem CanResetTargetAfterMeeting;
        public static OptionItem CanSeeLastRoomInMeeting;
        public static OptionItem CanCreateMadmate;

        public static Dictionary<byte, PlayerControl> Target = new();
        public static Dictionary<byte, bool> CanSetTarget = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilTracker);
            CanSeeKillFlash = BooleanOptionItem.Create(Id + 10, "EvilTrackerCanSeeKillFlash", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
            CanResetTargetAfterMeeting = BooleanOptionItem.Create(Id + 11, "EvilTrackerResetTargetAfterMeeting", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
            CanSeeLastRoomInMeeting = BooleanOptionItem.Create(Id + 12, "EvilTrackerCanSeeLastRoomInMeeting", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
            CanCreateMadmate = BooleanOptionItem.Create(Id + 20, "CanCreateMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
            CanSetTarget = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CanSetTarget.Add(playerId, true);
            Utils.GetPlayerById(playerId).GetTarget();
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void RPCSetTarget(byte trackerId, int targetId)
        {
            switch (targetId)
            {
                case -2:
                    CanSetTarget[trackerId] = true;
                    return;
                case -1:
                    Target.Remove(trackerId);
                    return;
                default:
                    var target = Utils.GetPlayerById(targetId);
                    if (target != null)
                    {
                        Target[trackerId] = target;
                        CanSetTarget[trackerId] = false;
                    }
                    return;
            }
        }

        public static void ApplyGameOptions(byte playerId)
        {
            AURoleOptions.ShapeshifterCooldown = CanSetTarget[playerId] ? 5f : 255f;
            AURoleOptions.ShapeshifterDuration = 1f;
        }
        public static void GetAbilityButtonText(HudManager __instance, byte playerId)
        {
            __instance.AbilityButton.ToggleVisible(CanSetTarget[playerId]);
            __instance.AbilityButton.OverrideText($"{GetString("EvilTrackerChangeButtonText")}");
        }
        public static void SendTarget(byte EvilTrackerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, Hazel.SendOption.Reliable, -1);
            writer.Write(EvilTrackerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRemoveTarget(byte EvilTrackerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, Hazel.SendOption.Reliable, -1);
            writer.Write(EvilTrackerId);
            writer.Write(-1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static PlayerControl GetTarget(this PlayerControl player)
        {
            if (player == null) return null;
            if (Target == null) Target = new Dictionary<byte, PlayerControl>();
            if (!Target.TryGetValue(player.PlayerId, out var target))
            {
                Target.Add(player.PlayerId, null);
                target = player.RemoveTarget();
            }
            return target;
        }
        public static PlayerControl RemoveTarget(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return null;
            Target[player.PlayerId] = null;
            Logger.Info($"プレイヤー{player.GetNameWithRole()}のターゲットを削除", "EvilTracker");
            SendRemoveTarget(player.PlayerId);
            return Target[player.PlayerId];
        }
        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target)
        {
            return CanSeeKillFlash.GetBool() && Utils.IsImpostorKill(killer, target);
        }
        public static string GetMarker(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor).ShadeColor(0.5f), CanSetTarget[playerId] ? "◁" : "");
        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), seer.GetTarget() == target ? "◀" : "");
        public static string UtilsGetTargetArrow(bool isMeeting, PlayerControl seer)
        {
            //ミーティング以外では矢印表示
            if (isMeeting) return "";
            string SelfSuffix = "";
            foreach (var arrow in Main.targetArrows)
            {
                var target = Utils.GetPlayerById(arrow.Key.Item2);
                bool EvilTrackerTarget = seer.GetTarget() == target;
                if (arrow.Key.Item1 == seer.PlayerId && !Main.PlayerStates[arrow.Key.Item2].IsDead && (target.GetCustomRole().IsImpostor() || EvilTrackerTarget))
                    SelfSuffix += EvilTrackerTarget ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), arrow.Value) : arrow.Value;
            }
            return SelfSuffix;
        }
        public static string PCGetTargetArrow(PlayerControl seer, PlayerControl target)
        {
            var update = false;
            string Suffix = "";
            foreach (var pc in Main.AllPlayerControls)
            {
                //発見対象じゃ無ければ次
                if (!IsTrackTarget(target, pc)) continue;

                update = FixedUpdatePatch.CheckArrowUpdate(target, pc, update, pc.GetCustomRole().IsImpostor());
                var key = (target.PlayerId, pc.PlayerId);
                var arrow = Main.targetArrows[key];
                if (target.GetTarget() == pc) arrow = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), arrow);
                if (target.AmOwner)
                {
                    //MODなら矢印表示
                    Suffix += arrow;
                }
            }
            if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
            {
                //更新があったら非Modに通知
                Utils.NotifyRoles(SpecifySeer: target);
            }
            return Suffix;
        }
        public static void EnableResetTargetAfterMeeting(PlayerControl pc)
        {
            if (!CanResetTargetAfterMeeting.GetBool()) return;
            CanSetTarget[pc.PlayerId] = true;
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, Hazel.SendOption.Reliable, -1);
            writer.Write(pc.PlayerId);
            writer.Write(-2);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void Shapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting)
        {
            if (CanSetTarget[shapeshifter.PlayerId] && shapeshifting)
            {
                if (!target.Data.IsDead && !target.GetCustomRole().IsImpostor())
                {
                    Target[shapeshifter.PlayerId] = target;
                    CanSetTarget[shapeshifter.PlayerId] = false;
                    SendTarget(shapeshifter.PlayerId, target.PlayerId);
                    Logger.Info($"{shapeshifter.GetNameWithRole()}のターゲットを{Target[shapeshifter.PlayerId].GetNameWithRole()}に設定", "EvilTrackerTarget");
                }
                Utils.MarkEveryoneDirtySettings();
                Utils.NotifyRoles();
            }
        }
        public static void FixedUpdate(PlayerControl pc)
        {
            if (!pc.Is(CustomRoles.EvilTracker)) return;
            var target = pc.GetTarget();
            //EvilTrackerのターゲット削除
            if (pc != target && target != null && (target.Data.IsDead || target.Data.Disconnected))
            {
                Target[pc.PlayerId] = null;
                pc.RemoveTarget();
                Logger.Info($"{pc.GetNameWithRole()}のターゲットが無効だったため、ターゲットを削除しました", "EvilTracker");
                Utils.NotifyRoles();
            }
        }
        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
            => seer.IsAlive() && seer.Is(CustomRoles.EvilTracker)
            && target.IsAlive() && seer != target
            && (target.Is(RoleType.Impostor) || seer.GetTarget() == target);
        public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
        {
            string text = Utils.ColorString(Palette.ImpostorRed, Main.targetArrows[(seer.PlayerId, target.PlayerId)]);
            var room = Main.PlayerStates[target.PlayerId].LastRoom;
            if (room == null) text += Utils.ColorString(Color.gray, $"@{GetString("FailToTrack")}");
            else text += Utils.ColorString(Palette.ImpostorRed, $"@{room.RoomId.GetRoomName()}");
            return text;
        }
    }
}