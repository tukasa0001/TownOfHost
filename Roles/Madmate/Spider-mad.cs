using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Animals;

namespace TownOfHostForE.Roles.Madmate;
public sealed class SpiderMad : RoleBase
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals 5:Madmate
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpiderMad),
            player => new SpiderMad(player),
            CustomRoles.SpiderMad,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            25200,
            SetupOptionItem,
            "クモマッドメイト",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public SpiderMad(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        settingWebSecond = SettingWebSecond.GetFloat();
        trapSpeed = TrapSpeed.GetFloat();
        webRadius = WebRadius.GetFloat();
        setWeb = false;
    }

    public static OptionItem SettingWebSecond;
    public static float settingWebSecond;

    public static OptionItem TrapSpeed;
    public static float trapSpeed;

    public static OptionItem WebRadius;
    public static float webRadius;

    private static bool setWeb = false;

    public static Vector3 WebPosition;

    enum OptionName
    {
        WebRadius,
        TrapSpeed,
        WebCount,
    }

    public static void SetupOptionItem()
    {
        WebRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.WebRadius, new(0.5f, 20f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        SettingWebSecond = FloatOptionItem.Create(RoleInfo, 11, OptionName.WebCount, new(1, 30, 1), 5, false)
            .SetValueFormat(OptionFormat.None);
        TrapSpeed = FloatOptionItem.Create(RoleInfo, 12, OptionName.TrapSpeed, new(0.1f, 0.9f, 0.1f), 0.5f, false)
            .SetValueFormat(OptionFormat.None);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }

    public override void OnTouchPet(PlayerControl player)
    {
        Logger.Info($"Web Set", "Spider-Mad");
        if (player == null || player.Data.IsDead) return;

        player.RpcProtectedMurderPlayer(); //設置が分かるように
        WebPosition = player.transform.position;
        setWeb = true;
        Utils.NotifyRoles();
    }

    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (GameStates.IsLobby) return;
        if (pc == null) return;
        if (!setWeb) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (target != pc)
            {
                if (!pc.IsAlive() || !target.IsAlive()) continue;

                var role = PlayerState.GetByPlayerId(target.PlayerId).MainRole;
                //インポスターは対象外
                if (role.IsImpostor()) continue;

                var dis = Vector2.Distance(WebPosition, target.transform.position);
                //罠にかかってなかったら処理なし。
                if (dis >= webRadius) continue;

                //誰かが踏んだことが分かるように
                pc.RpcProtectedMurderPlayer();
                var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
                Main.AllPlayerSpeed[target.PlayerId] = trapSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
                target.MarkDirtySettings();
                setWeb = false;

                new LateTask(() =>
                {
                    Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - trapSpeed + tmpSpeed;
                    target.MarkDirtySettings();
                }, settingWebSecond, "SpiderMadWebTrap");
            }
        }
    }
}
