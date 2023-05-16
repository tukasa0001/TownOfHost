using System.Collections.Generic;
using Hazel;
using Il2CppSystem.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilTracker : RoleBase, IImpostor, IKillFlashSeeable, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(EvilTracker),
            player => new EvilTracker(player),
            CustomRoles.EvilTracker,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            2900,
            SetupOptionItem,
            "et",
            canMakeMadmate: () => OptionCanCreateMadmate.GetBool()
        );

    public EvilTracker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        playerIdList = new();
        ImpostorsId = new();

        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CurrentTargetMode = (TargetMode)OptionTargetMode.GetValue();
        RoleTypes = CurrentTargetMode == TargetMode.Never ? RoleTypes.Impostor : RoleTypes.Shapeshifter;
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
        CanCreateMadmate = OptionCanCreateMadmate.GetBool() && CurrentTargetMode != TargetMode.Never;

        var playerId = player.PlayerId;

        playerIdList.Add(playerId);
        TargetId = byte.MaxValue;
        CanSetTarget = CurrentTargetMode != TargetMode.Never;
        //ImpostorsIdはEvilTracker内で共有
        ImpostorsId.Clear();
        foreach (var target in Main.AllAlivePlayerControls)
        {
            var targetId = target.PlayerId;
            if (targetId != playerId && target.Is(CustomRoleTypes.Impostor))
            {
                ImpostorsId.Add(targetId);
                TargetArrow.Add(playerId, targetId);
            }
        }
    }

    private static List<byte> playerIdList = new();
    private static BooleanOptionItem OptionCanSeeKillFlash;
    private static StringOptionItem OptionTargetMode;
    private static BooleanOptionItem OptionCanSeeLastRoomInMeeting;
    private static BooleanOptionItem OptionCanCreateMadmate;

    enum OptionName
    {
        EvilTrackerCanSeeKillFlash,
        EvilTrackerTargetMode,
        EvilTrackerCanSeeLastRoomInMeeting,
    }
    public static bool CanSeeKillFlash;
    private static TargetMode CurrentTargetMode;
    public static RoleTypes RoleTypes;
    public static bool CanSeeLastRoomInMeeting;
    private static bool CanCreateMadmate;

    public byte TargetId;
    public bool CanSetTarget;
    private HashSet<byte> ImpostorsId = new(3);

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

    private static void SetupOptionItem()
    {
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilTrackerCanSeeKillFlash, true, false);
        OptionTargetMode = StringOptionItem.Create(RoleInfo, 11, OptionName.EvilTrackerTargetMode, TargetModeText, 2, false);
        OptionCanCreateMadmate = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanCreateMadmate, false, false);
        OptionCanCreateMadmate.SetParent(OptionTargetMode);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilTrackerCanSeeLastRoomInMeeting, false, false);
    }
    public bool CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        if (!CanSeeKillFlash) return false;

        PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

        //インポスターによるキルかどうかの判別
        var realKiller = target.GetRealKiller() ?? killer;
        return realKiller.Is(CustomRoleTypes.Impostor) && realKiller != target;
    }
    public bool CanMakeSidekick() => CanCreateMadmate; // ISidekickable

    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetEvilTrackerTarget);
        sender.Writer.Write(TargetId);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetEvilTrackerTarget) return;

        byte targetId = reader.ReadByte();
        SetTarget(Player.PlayerId, targetId);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = CanTarget() ? 1f : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override string GetAbilityButtonText() => GetString("EvilTrackerChangeButtonText");
    public override bool CanUseAbilityButton() => CanTarget();

    // 値取得の関数
    private bool CanTarget() => Player.IsAlive() && CanSetTarget;
    private bool IsTrackTarget(PlayerControl target)
        => Player.IsAlive() && target.IsAlive() && !Is(target)
        && (target.Is(CustomRoleTypes.Impostor) || TargetId == target.PlayerId);

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
            Player.MarkDirtySettings();
        }
        var target = Utils.GetPlayerById(TargetId);
        if (!Player.IsAlive() || !target.IsAlive())
            SetTarget();
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
    }

    ///<summary>
    ///引数が両方空：再設定可能に,
    ///trackerIdのみ：該当IDのターゲット削除,
    ///trackerIdとtargetId両方あり：該当IDのプレイヤーをターゲットに設定
    ///</summary>
    public void SetTarget(byte trackerId = byte.MaxValue, byte targetId = byte.MaxValue)
    {
        if (trackerId == byte.MaxValue) // ターゲット再設定可能に
            CanSetTarget = true;
        else if (targetId == byte.MaxValue) // ターゲット削除
            TargetId = byte.MaxValue;
        else
        {
            TargetId = targetId; // ターゲット設定
            if (CurrentTargetMode != TargetMode.Always)
                CanSetTarget = false; // ターゲット再設定不可に
            TargetArrow.Add(trackerId, targetId);
        }

        if (!AmongUsClient.Instance.AmHost) return;
        SendRPC();
    }

    // 表示系の関数群
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool _ = false)
    {
        seen ??= seer;
        return TargetId == seen.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "◀") : "";
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting)
        {
            var roomName = GetLastRoom(seen);
            // 空のときにタグを付けると，suffixが空ではない判定となりなにもない3行目が表示される
            return roomName.Length == 0 ? "" : $"<size=1.5>{roomName}</size>";
        }
        else
        {
            return GetArrows(seen);
        }
    }
    private string GetArrows(PlayerControl seen)
    {
        if (!Is(seen)) return "";

        var trackerId = Player.PlayerId;

        ImpostorsId.RemoveWhere(id => PlayerState.GetByPlayerId(id).IsDead);

        var sb = new StringBuilder(80);
        if (ImpostorsId.Count > 0)
        {
            sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>");
            foreach (var impostorId in ImpostorsId)
            {
                sb.Append(TargetArrow.GetArrows(Player, impostorId));
            }
            sb.Append($"</color>");
        }

        if (TargetId != byte.MaxValue)
        {
            sb.Append(Utils.ColorString(Color.white, TargetArrow.GetArrows(Player, TargetId)));
        }
        return sb.ToString();
    }
    public string GetLastRoom(PlayerControl seen)
    {
        if (!(CanSeeLastRoomInMeeting && IsTrackTarget(seen))) return "";

        string text = "";
        var room = seen.GetPlainShipRoom();
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else
        {
            text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
        }

        return text;
    }
}