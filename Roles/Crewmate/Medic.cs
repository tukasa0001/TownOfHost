using System.Collections.Generic;
using System.Text;

using AmongUs.GameOptions;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Translator;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Medic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Medic),
            player => new Medic(player),
            CustomRoles.Medic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            40900,
            null,
            "メディック",
            "#6495ed",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Medic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    public static List<Medic> Medics = new();

    PlayerControl GuardPlayer = null;
    bool UseVent = new();

    public override void Add()
    {
        Medics.Add(this);
        GuardPlayer = null;
        UseVent = true;

        Player.AddVentSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = UseVent ? 0f : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    public static bool GuardPlayerCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        // メディックに守られていなければなにもせず返す
        if (!IsGuard(target)) return true;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        killer.RpcProtectedMurderPlayer(target); //killer側のみ。斬られた側は見れない。

        foreach (var medic in Medics)
        {
            if (medic.GuardPlayer == target)
            {
                medic.GuardPlayer = null; break;
            }
        }
        info.CanKill = false;
        return true;
    }
    public static bool IsGuard(PlayerControl target)
    {
        foreach (var medic in Medics)
        {
            if(target == medic.GuardPlayer) return true;
        }
        return false;
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!UseVent) return false;

        GuardPlayer = Player.VentPlayerSelect(() =>
        {
            UseVent = false;
        });

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (GuardPlayer != null && seen == GuardPlayer)
        {
            return Utils.ColorString(RoleInfo.RoleColor, "Σ");
        }
        return string.Empty;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !UseVent || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (GuardPlayer == null)
            str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(GuardPlayer.GetRealName());
        }
        return str.ToString();
    }

    public override string GetAbilityButtonText() => GetString("ChangeButtonText");
}