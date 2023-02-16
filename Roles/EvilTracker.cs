using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class EvilTracker
    {
        private static readonly int Id = 2900;
        private static List<byte> playerIdList = new();

        private static OptionItem CanSeeKillFlash;
        private static OptionItem CanResetTargetAfterMeeting;
        public static OptionItem CanSeeLastRoomInMeeting;
        public static OptionItem CanCreateMadmate;

        public static Dictionary<byte, byte> Target = new();
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
            Target.Add(playerId, byte.MaxValue);
            CanSetTarget.Add(playerId, true);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyGameOptions(byte playerId)
        {
            AURoleOptions.ShapeshifterCooldown = CanTarget(playerId) ? 5f : 255f;
            AURoleOptions.ShapeshifterDuration = 1f;
        }
        public static void GetAbilityButtonText(HudManager __instance, byte playerId)
        {
            __instance.AbilityButton.ToggleVisible(CanSetTarget[playerId]);
            __instance.AbilityButton.OverrideText($"{GetString("EvilTrackerChangeButtonText")}");
        }

        // 値取得の関数
        private static bool CanTarget(byte playerId)
            => !Main.PlayerStates[playerId].IsDead && CanSetTarget.TryGetValue(playerId, out var value) && value;
        private static byte GetTarget(byte playerId)
            => Target.TryGetValue(playerId, out var targetId) ? targetId : byte.MaxValue;
        public static bool IsTrackTarget(PlayerControl seer, PlayerControl target, bool includeImpostors = true)
            => seer.IsAlive() && seer.Is(CustomRoles.EvilTracker)
            && target.IsAlive() && seer != target
            && ((includeImpostors && target.Is(RoleType.Impostor)) || GetTarget(seer.PlayerId) == target.PlayerId);
        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target)
        {
            return CanSeeKillFlash.GetBool() && Utils.IsImpostorKill(killer, target);
        }

        // 各所で呼ばれる処理
        public static void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting)
        {
            if (!CanTarget(shapeshifter.PlayerId) || !shapeshifting) return;
            if (!target.IsAlive() || target.Is(RoleType.Impostor)) return;

            SetTarget(shapeshifter.PlayerId, target.PlayerId);
            Logger.Info($"{shapeshifter.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "EvilTrackerTarget");
            shapeshifter.MarkDirtySettings();
            Utils.NotifyRoles();
        }
        public static void FixedUpdate(PlayerControl pc)
        {
            if (!pc.Is(CustomRoles.EvilTracker)) return;
            var targetId = GetTarget(pc.PlayerId);
            if (targetId == byte.MaxValue) return;
            var target = Utils.GetPlayerById(targetId);
            if (pc.IsAlive() && target.IsAlive()) return;
            //EvilTrackerのターゲット削除
            SetTarget(pc.PlayerId);
            Logger.Info($"{pc.GetNameWithRole()}のターゲットが無効だったため、ターゲットを削除しました", "EvilTracker");
            Utils.NotifyRoles();
        }
        public static void AfterMeetingTasks()
        {
            if (CanResetTargetAfterMeeting.GetBool())
                SetTarget();
        }
        ///<summary>
        ///引数が両方空：再設定可能に,
        ///trackerIdのみ：該当IDのターゲット削除,
        ///trackerIdとtargetId両方あり：該当IDのプレイヤーをターゲットに設定
        ///</summary>
        public static void SetTarget(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
        {
            if (trackerId == byte.MaxValue) // ターゲット再設定可能に
                foreach (var playerId in playerIdList)
                    CanSetTarget[playerId] = true;
            else if (targetId == byte.MaxValue) // ターゲット削除
                Target[trackerId] = byte.MaxValue;
            else
            {
                Target[trackerId] = targetId; // ターゲット設定
                CanSetTarget[trackerId] = false; // ターゲット再設定不可に
            }

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, SendOption.Reliable, -1);
            writer.Write(trackerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte trackerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            SetTarget(trackerId, targetId);
        }

        // 表示系の関数
        public static string GetMarker(byte playerId) => CanTarget(playerId) ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "◁") : "";
        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => IsTrackTarget(seer, target, false) ? Utils.ColorString(Palette.ImpostorRed, "◀") : "";
        public static string GetTargetArrowForModClient(PlayerControl seer, PlayerControl target)
        {
            var update = false;
            string Suffix = "";
            foreach (var pc in Main.AllPlayerControls)
            {
                //発見対象じゃ無ければ次
                if (!IsTrackTarget(target, pc)) continue;
                update = FixedUpdatePatch.CheckArrowUpdate(target, pc, update, pc.Is(RoleType.Impostor));
                var arrow = Main.targetArrows[(target.PlayerId, pc.PlayerId)];
                if (IsTrackTarget(target, pc, false)) arrow = Utils.ColorString(Color.white, arrow);
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
        public static string GetTargetArrowForVanilla(bool isMeeting, PlayerControl seer)
        {
            //ミーティング以外では矢印表示
            if (isMeeting) return "";
            string SelfSuffix = "";
            foreach (var arrow in Main.targetArrows)
            {
                var target = Utils.GetPlayerById(arrow.Key.Item2);
                if (arrow.Key.Item1 == seer.PlayerId && IsTrackTarget(seer, target))
                    SelfSuffix += Utils.ColorString(target.Is(RoleType.Impostor) ? Palette.ImpostorRed : Color.white, arrow.Value);
            }
            return SelfSuffix;
        }
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