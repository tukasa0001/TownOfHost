using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using static TownOfHostForE.Utils;
using HarmonyLib;

namespace TownOfHostForE.Roles.Impostor;

public sealed class Telepathisters : RoleBase, IImpostor, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Telepathisters),
            player => new Telepathisters(player),
            CustomRoles.Telepathisters,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20700,
            SetupOptionItem,
            "テレパシスターズ"
        );

    public Telepathisters(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CanSeeLastRoomInMeeting = OptionCanSeeLastRoomInMeeting.GetBool();
        VentMaxCount = OptionVentMaxCount.GetInt();

        VentCountLimit = VentMaxCount;
        //ImpostorsIdはEvilTracker内で共有
        TelepathistersId.Clear();
        var playerId = player.PlayerId;
        foreach (var target in Main.AllAlivePlayerControls)
        {
            var targetId = target.PlayerId;
            if (targetId != playerId && target.Is(CustomRoles.Telepathisters))
            {
                TelepathistersId.Add(targetId);
                TargetArrow.Add(playerId, targetId);
            }
        }
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionCanSeeLastRoomInMeeting;
    private static OptionItem OptionVentMaxCount;

    enum OptionName
    {
        TelepathistersCanSeeKillFlash,
        TelepathistersCanSeeLastRoomInMeeting,
        TelepathistersOptionVentCount
    }
    public static float KillCooldown;
    public static bool CanSeeKillFlash;
    public static bool CanSeeLastRoomInMeeting;
    public static int VentMaxCount;

    public bool CanSetTarget;
    private HashSet<byte> TelepathistersId = new(3);
    public static int VentCountLimit;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 11, OptionName.TelepathistersCanSeeKillFlash, true, false);
        OptionCanSeeLastRoomInMeeting = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TelepathistersCanSeeLastRoomInMeeting, true, false);
        OptionVentMaxCount = IntegerOptionItem.Create(RoleInfo, 13, OptionName.TelepathistersOptionVentCount, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
    }

    public float CalculateKillCooldown() => KillCooldown;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (VentCountLimit <= 0) return false;

        VentCountLimit--;
        //テレパシスターズのみ呼び出し
        TelepathistersId.Do(id => NotifyRoles(SpecifySeer: GetPlayerById(id)));
        return true;
    }

    public bool CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        if (!CanSeeKillFlash) return false;

        PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

        //シスターズによるキルかどうかの判別
        var realKiller = target.GetRealKiller() ?? killer;
        return realKiller.Is(CustomRoles.Telepathisters) && realKiller != target;
    }

    // 値取得の関数
    private bool IsTrackTarget(PlayerControl target)
        => Player.IsAlive() && target.IsAlive() && !Is(target)
        && target.Is(CustomRoles.Telepathisters);

    // 表示系の関数群
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

        TelepathistersId.RemoveWhere(id => PlayerState.GetByPlayerId(id).IsDead);

        var sb = new StringBuilder(80);
        if (TelepathistersId.Count > 0)
        {
            sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>");
            foreach (var impostorId in TelepathistersId)
            {
                sb.Append(TargetArrow.GetArrows(Player, impostorId));
            }
            sb.Append($"</color>");
        }
        return sb.ToString();
    }
    public string GetLastRoom(PlayerControl seen)
    {
        if (!(CanSeeLastRoomInMeeting && IsTrackTarget(seen))) return "";

        string text = Utils.ColorString(Palette.ImpostorRed, TargetArrow.GetArrows(Player, seen.PlayerId));
        var room = PlayerState.GetByPlayerId(seen.PlayerId).LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else
        {
            text += Utils.ColorString(Palette.ImpostorRed, "@" + GetString(room.RoomId.ToString()));
        }

        return text;
    }

    public override string GetProgressText(bool comms = false)
    {
        int count;
        if (VentCountLimit < 0) count = 0;
        else count = VentCountLimit;
        return Utils.ColorString(count > 0 ? Palette.ImpostorRed : Color.gray, $"({count})");
    }

}