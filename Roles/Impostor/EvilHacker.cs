using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilHacker : RoleBase, IImpostor, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3100,
            SetupOptionItems,
            "eh"
        );
    public EvilHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeDeadMark = OptionCanSeeDeadMark.GetBool();
        canSeeImpostorMark = OptionCanSeeImpostorMark.GetBool();
        canSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        canSeeMurderRoom = OptionCanSeeMurderRoom.GetBool();

        CustomRoleManager.OnMurderPlayerOthers.Add(HandleMurderRoomNotify);
        instances.Add(this);
    }
    public override void OnDestroy()
    {
        instances.Remove(this);
    }

    private static OptionItem OptionCanSeeDeadMark;
    private static OptionItem OptionCanSeeImpostorMark;
    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionCanSeeMurderRoom;
    private enum OptionName
    {
        EvilHackerCanSeeDeadMark,
        EvilHackerCanSeeImpostorMark,
        EvilHackerCanSeeKillFlash,
        EvilHackerCanSeeMurderRoom,
    }
    private static bool canSeeDeadMark;
    private static bool canSeeImpostorMark;
    private static bool canSeeKillFlash;
    private static bool canSeeMurderRoom;

    private static HashSet<EvilHacker> instances = new(1);

    private HashSet<MurderNotify> activeNotifies = new(2);

    private static void SetupOptionItems()
    {
        OptionCanSeeDeadMark = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilHackerCanSeeDeadMark, true, false);
        OptionCanSeeImpostorMark = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerCanSeeImpostorMark, true, false);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EvilHackerCanSeeKillFlash, true, false);
        OptionCanSeeMurderRoom = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilHackerCanSeeMurderRoom, true, false, OptionCanSeeKillFlash);
    }
    /// <summary>相方がキルした部屋を通知する設定がオンなら各プレイヤーに通知を行う</summary>
    private static void HandleMurderRoomNotify(MurderInfo info)
    {
        if (canSeeMurderRoom)
        {
            foreach (var evilHacker in instances)
            {
                evilHacker.OnMurderPlayer(info);
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (!Player.IsAlive())
        {
            return;
        }
        var admins = AdminProvider.CalculateAdmin();
        var builder = new StringBuilder(512);

        // 送信するメッセージを生成
        foreach (var admin in admins)
        {
            var entry = admin.Value;
            if (entry.TotalPlayers <= 0)
            {
                continue;
            }
            // インポスターがいるなら星マークを付ける
            if (canSeeImpostorMark && entry.NumImpostors > 0)
            {
                builder.Append(ImpostorMark);
            }
            // 部屋名と合計プレイヤー数を表記
            builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
            builder.Append(": ");
            builder.Append(entry.TotalPlayers);
            // 死体があったら死体の数を書く
            if (canSeeDeadMark && entry.NumDeadBodies > 0)
            {
                builder.Append('(').Append(Translator.GetString("Deadbody"));
                builder.Append('×').Append(entry.NumDeadBodies).Append(')');
            }
            builder.Append('\n');
        }

        // 送信
        var message = builder.ToString();
        var title = Utils.ColorString(Color.green, Translator.GetString("LastAdminInfo"));

        _ = new LateTask(() =>
        {
            if (GameStates.IsInGame)
            {
                Utils.SendMessage(message, Player.PlayerId, title, false);
            }
        }, 4f, "EvilHacker Admin Message");
        return;
    }
    private void OnMurderPlayer(MurderInfo info)
    {
        // 生きてる間に相方のキルでキルフラが鳴った場合に通知を出す
        if (!Player.IsAlive() || !CheckKillFlash(info) || info.AttemptKiller == Player)
        {
            return;
        }
        RpcCreateMurderNotify(info.AttemptTarget.GetPlainShipRoom()?.RoomId ?? SystemTypes.Hallway);
    }
    private void RpcCreateMurderNotify(SystemTypes room)
    {
        CreateMurderNotify(room);
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender(CustomRPC.EvilHackerCreateMurderNotify);
            sender.Writer.Write((byte)room);
        }
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.EvilHackerCreateMurderNotify)
        {
            CreateMurderNotify((SystemTypes)reader.ReadByte());
        }
    }
    /// <summary>
    /// 名前の下にキル発生通知を出す
    /// </summary>
    /// <param name="room">キルが起きた部屋</param>
    private void CreateMurderNotify(SystemTypes room)
    {
        activeNotifies.Add(new()
        {
            CreatedAt = DateTime.Now,
            Room = room,
        });
        if (AmongUsClient.Instance.AmHost)
        {
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        // 古い通知の削除処理 Mod入りは自分でやる
        if (!AmongUsClient.Instance.AmHost && Player != PlayerControl.LocalPlayer)
        {
            return;
        }
        if (activeNotifies.Count <= 0)
        {
            return;
        }
        // NotifyRolesを実行するかどうかのフラグ
        var doNotifyRoles = false;
        // 古い通知があれば削除
        foreach (var notify in activeNotifies)
        {
            if (DateTime.Now - notify.CreatedAt > NotifyDuration)
            {
                activeNotifies.Remove(notify);
                doNotifyRoles = true;
            }
        }
        if (doNotifyRoles && AmongUsClient.Instance.AmHost)
        {
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting || !canSeeMurderRoom || seer != Player || seen != Player || activeNotifies.Count <= 0)
        {
            return base.GetSuffix(seer, seen, isForMeeting);
        }
        var roomNames = activeNotifies.Select(notify => DestroyableSingleton<TranslationController>.Instance.GetString(notify.Room));
        return Utils.ColorString(Color.green, $"{Translator.GetString("MurderNotify")}: {string.Join(", ", roomNames)}");
    }
    public bool CheckKillFlash(MurderInfo info) =>
        canSeeKillFlash && !info.IsSuicide && !info.IsAccident && info.AttemptKiller.Is(CustomRoleTypes.Impostor);

    private static readonly string ImpostorMark = "★".Color(Palette.ImpostorRed);
    /// <summary>相方がキルしたときに名前の下に通知を表示する長さ</summary>
    private static readonly TimeSpan NotifyDuration = TimeSpan.FromSeconds(10);

    private readonly struct MurderNotify
    {
        /// <summary>通知が作成された時間</summary>
        public DateTime CreatedAt { get; init; }
        /// <summary>キルが起きた部屋</summary>
        public SystemTypes Room { get; init; }
    }
}
