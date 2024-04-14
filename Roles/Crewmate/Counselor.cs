using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using UnityEngine;
using static TownOfHostForE.Translator;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Counselor : RoleBase
{

    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Counselor),
            player => new Counselor(player),
            CustomRoles.Counselor,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            21200,
            SetupOptionItem,
            "ネゴシエーター",
            "#ffffff",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Counselor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
    }

    private static OptionItem OptionKillCooldown;
    public static bool IsCoolTimeOn = true;
    public static PlayerControl KillWaitPlayerSelect = new();
    public static PlayerControl KillWaitPlayer = new();
    public static bool CanKillFlag = new();
    static bool IsComplete = false;

    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static float KillCooldown = 30;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false);
    }
    public override void Add()
    {
        KillWaitPlayerSelect = null;
        KillWaitPlayer = null;
        IsCoolTimeOn = true;
        IsComplete = false;

        Player.AddVentSelect();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseKillButton() ? (IsCoolTimeOn ? KillCooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    public override bool OnCompleteTask()
    {
        if (!IsComplete && IsTaskFinished)
        {
            IsComplete = true;
        }
        return true;
    }


    public bool CanUseKillButton()
        => Player.IsAlive()
        && IsComplete
        && !CanKillFlag;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseKillButton()) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        KillWaitPlayerSelect = Player.VentPlayerSelect(() =>
        {
            KillWaitPlayer = KillWaitPlayerSelect;
            IsCoolTimeOn = true;
            Player.MarkDirtySettings();
        });

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;
        if (KillWaitPlayer == null) return;

        if (Player == null || !Player.IsAlive())
        {
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            return;
        }

        Vector2 GSpos = Player.transform.position;//GSの位置

        var target = KillWaitPlayer;
        float targetDistance = Vector2.Distance(GSpos, target.transform.position);

        var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
        if (targetDistance <= KillRange && Player.CanMove && target.CanMove)
        {
            CanKillFlag = true;
            Logger.Info($"{Player.GetNameWithRole()} : クルーを作った", "GrudgeSheriff");
            Player.RpcResetAbilityCooldown();

            var cRole = target.GetCustomRole();
            if (target.IsAlive())
            {
                player.RpcProtectedMurderPlayer(); //変えたことが分かるように。
                if (cRole.GetCustomRoleTypes() != CustomRoleTypes.Impostor)
                {
                    target.RpcSetCustomRole(CustomRoles.Crewmate);
                    target.RpcProtectedMurderPlayer(); //変えられたことが分かるように。
                    Logger.Info($"Make Crew:{target.name}", "Kill");
                    Logger.Info($"Make Crew:{cRole}", "Kill");
                }
            }

            Utils.MarkEveryoneDirtySettings();
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            Utils.NotifyRoles();
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseKillButton() || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (KillWaitPlayerSelect == null)
            str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(KillWaitPlayerSelect.GetRealName());
        }
        return str.ToString();
    }
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");
}