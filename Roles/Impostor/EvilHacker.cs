using System.Text;
using AmongUs.GameOptions;
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

    private static void SetupOptionItems()
    {
        OptionCanSeeDeadMark = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilHackerCanSeeDeadMark, true, false);
        OptionCanSeeImpostorMark = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerCanSeeImpostorMark, true, false);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EvilHackerCanSeeKillFlash, true, false);
        OptionCanSeeMurderRoom = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilHackerCanSeeMurderRoom, true, false, OptionCanSeeKillFlash);
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
        var title = Utils.ColorString(Color.green, Translator.GetString("Message.LastAdminInfo"));

        _ = new LateTask(() =>
        {
            if (GameStates.IsInGame)
            {
                Utils.SendMessage(message, Player.PlayerId, title);
            }
        }, 4f, "EvilHacker Admin Message");

        return;
    }
    public bool CheckKillFlash(MurderInfo info) =>
        canSeeKillFlash && !info.IsSuicide && !info.IsFakeSuicide && !info.IsAccident && info.AttemptKiller.Is(CustomRoleTypes.Impostor);

    private const char ImpostorMark = '★';
}
