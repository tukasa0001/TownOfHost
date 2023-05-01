using System.Collections.Generic;
using Hazel;
using Il2CppSystem.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilTracker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    new(
        typeof(EvilTracker),
        player => new EvilTracker(player),
        CustomRoles.EvilTracker,
        () => RoleTypes.Shapeshifter,
        CustomRoleTypes.Impostor,
        2900,
        SetupOptionItem
    );

    public EvilTracker(PlayerControl player)
    : base(
        RoleInfo,
        player
        )
    {
        playerIdList = new();
        Target = new();
        CanSetTarget = new();
        ImpostorsId = new();

        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CurrentTargetMode = (TargetMode)OptionTargetMode.GetValue();
        RoleTypes = CurrentTargetMode == TargetMode.Never ? RoleTypes.Impostor : RoleTypes.Shapeshifter;
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
        CanCreateMadmate = OptionCanCreateMadmate.GetBool() && CurrentTargetMode != TargetMode.Never;
    }

    private static List<byte> playerIdList = new();
    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionTargetMode;
    private static OptionItem OptionCanSeeLastRoomInMeeting;
    private static OptionItem OptionCanCreateMadmate;

    enum OptionName
    {
        EvilTrackerCanSeeKillFlash,
        EvilTrackerTargetMode,
        EvilTrackerCanSeeLastRoomInMeeting,
        CanCreateMadmate,
    }
    private static bool CanSeeKillFlash;
    private static TargetMode CurrentTargetMode;
    public static RoleTypes RoleTypes;
    public static bool CanSeeLastRoomInMeeting;
    public static bool CanCreateMadmate;
    public static Dictionary<byte, byte> Target = new();
    public static Dictionary<byte, bool> CanSetTarget = new();
    private static Dictionary<byte, HashSet<byte>> ImpostorsId = new();

    private static bool EvilTrackerCanSeeKillFlash;
    private static int EvilTrackerTargetMode;
    private static bool EvilTrackerCanSeeLastRoomInMeeting;
    private static bool EvilTrackerCanCreateMadmate;

    private enum TargetMode
    {
        Never,
        OnceInGame,
        EveryMeeting,
        Always,
    };
    private static readonly string[] TargetModeText =
    {
            "EvilTrackerTargetMode.Never",
            "EvilTrackerTargetMode.OnceInGame",
            "EvilTrackerTargetMode.EveryMeeting",
            "EvilTrackerTargetMode.Always",
    };

    public static void SetupOptionItem()
    {
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilTrackerCanSeeKillFlash, true, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        OptionTargetMode = StringOptionItem.Create(RoleInfo, 11, OptionName.EvilTrackerTargetMode, TargetModeText, 2, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        OptionCanCreateMadmate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CanCreateMadmate, false, false)
            .SetParent(OptionTargetMode);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilTrackerCanSeeLastRoomInMeeting, false, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.EvilTracker]);
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;

        playerIdList.Add(playerId);
        Target.Add(playerId, byte.MaxValue);
        CanSetTarget.Add(playerId, CurrentTargetMode != TargetMode.Never);
        //ImpostorsIdはEvilTracker内で共有
        ImpostorsId[playerId] = new();
        foreach (var target in Main.AllAlivePlayerControls)
        {
            var targetId = target.PlayerId;
            if (targetId != playerId && target.Is(CustomRoleTypes.Impostor))
            {
                ImpostorsId[playerId].Add(targetId);
                TargetArrow.Add(playerId, targetId);
            }
        }
    }

    private void SendRPC(byte trackerId, byte targetId)
    {
        using var sender = CreateSender(CustomRPC.SetEvilTrackerTarget);
        sender.Writer.Write(trackerId);
        sender.Writer.Write(targetId);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte trackerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        SetTarget(trackerId, targetId);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = CanTarget() ? 1f : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override string GetAbilityButtonText()
    {
        DestroyableSingleton<HudManager>.Instance.AbilityButton.ToggleVisible(CanTarget());
        return GetString("EvilTrackerChangeButtonText");
    }

    // 値取得の関数
    private bool CanTarget()
        => Player.IsAlive() && CanSetTarget.TryGetValue(Player.PlayerId, out var value) && value;
    private static byte GetTargetId(byte playerId)
        => Target.TryGetValue(playerId, out var targetId) ? targetId : byte.MaxValue;
    public static bool IsTrackTarget(PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && playerIdList.Contains(seer.PlayerId)
        && target.IsAlive() && seer != target
        && (target.Is(CustomRoleTypes.Impostor) || GetTargetId(seer.PlayerId) == target.PlayerId);
    public static bool KillFlashCheck(PlayerControl killer, PlayerControl target)
    {
        if (!CanSeeKillFlash) return false;
        //インポスターによるキルかどうかの判別
        var realKiller = target.GetRealKiller() ?? killer;
        return realKiller.Is(CustomRoleTypes.Impostor) && realKiller != target;
    }

    // 各所で呼ばれる処理
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = !Is(target);
        if (!CanTarget() || !shapeshifting) return;
        if (target == null || target.Is(CustomRoleTypes.Impostor)) return;

        SetTarget(Player.PlayerId, target.PlayerId);
        Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "EvilTrackerTarget");
        Player.MarkDirtySettings();
        Utils.NotifyRoles();
    }
    public override void AfterMeetingTasks()
    {
        if (CurrentTargetMode == TargetMode.EveryMeeting)
        {
            SetTarget();
            Utils.MarkEveryoneDirtySettings();
        }
        foreach (var playerId in playerIdList)
        {
            var pc = Utils.GetPlayerById(playerId);
            var target = Utils.GetPlayerById(GetTargetId(playerId));
            if (!pc.IsAlive() || !target.IsAlive())
                SetTarget(playerId);
            pc?.SyncSettings();
            pc?.RpcResetAbilityCooldown();
        }
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
            if (CurrentTargetMode != TargetMode.Always)
                CanSetTarget[trackerId] = false; // ターゲット再設定不可に
            TargetArrow.Add(trackerId, targetId);
        }

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, SendOption.Reliable, -1);
        writer.Write(trackerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // 表示系の関数
    public string GetMarker(byte playerId) => CanTarget() ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "◁") : "";
    public static string GetTargetMark(PlayerControl seer, PlayerControl target) => GetTargetId(seer.PlayerId) == target.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "◀") : "";
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target)
    {
        if (!GameStates.IsInTask || !target.Is(CustomRoles.EvilTracker)) return "";

        var trackerId = target.PlayerId;
        if (seer.PlayerId != trackerId) return "";

        ImpostorsId[trackerId].RemoveWhere(id => Main.PlayerStates[id].IsDead);

        var sb = new StringBuilder(80);
        if (ImpostorsId[trackerId].Count > 0)
        {
            sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>");
            foreach (var impostorId in ImpostorsId[trackerId])
            {
                sb.Append(TargetArrow.GetArrows(target, impostorId));
            }
            sb.Append($"</color>");
        }

        var targetId = Target[trackerId];
        if (targetId != byte.MaxValue)
        {
            sb.Append(Utils.ColorString(Color.white, TargetArrow.GetArrows(target, targetId)));
        }
        return sb.ToString();
    }
    public static string GetArrowAndLastRoom(PlayerControl seer, PlayerControl target)
    {
        string text = Utils.ColorString(Palette.ImpostorRed, TargetArrow.GetArrows(seer, target.PlayerId));
        var room = Main.PlayerStates[target.PlayerId].LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
        return text;
    }
}
}