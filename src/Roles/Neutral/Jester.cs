using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using TownOfHost.RPC;
using UnityEngine;

namespace TownOfHost.Roles;

public class Jester : CustomRole
{
    private bool canUseVents;
    private bool canDieBySheriff;
    private bool impostorVision;
    private int ventCooldown;

    public override bool CanBeKilledBySheriff() => canDieBySheriff;


    [RoleAction(RoleActionType.SelfExiled)]
    public void JesterWin()
    {
        RpcV2.Immediate(PlayerControl.LocalPlayer.PlayerId, (byte)CustomRPCOLD.EndGame)
            .Write((byte)CustomWinner.Jester)
            .Write(MyPlayer.PlayerId)
            .SendToHost();

        "Jester Won".DebugLog();
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(opt =>
                opt.Name("Has Impostor Vision").Bind(v => impostorVision = (bool)v).AddOnOffValues().Build())
            .AddSubOption(opt =>
                opt.Name("Can Be Killed By Sheriff").Bind(v => canDieBySheriff = (bool)v).AddOnOffValues(false)
                    .Build())
            .AddSubOption(opt => opt.Name("Can Use Vents")
                .Bind(v => canUseVents = (bool)v)
                .AddOnOffValues()
                .ShowSubOptionsWhen(v => (bool)v)
                .AddSubOption(opt => opt.Name("Vent Cooldown")
                    .AddValues(4, 0, 5, 10, 15, 20, 25)
                    .Bind(v => ventCooldown = (int)v)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .VanillaRole(canUseVents ? RoleTypes.Engineer : RoleTypes.Crewmate)
            .SpecialType(SpecialType.Neutral)
            .RoleColor(new Color(0.93f, 0.38f, 0.65f))
            .OptionOverride(Override.CrewLightMod,
                () => GameOptionsManager.Instance.CurrentGameOptions.AsNormalOptions()!.ImpostorLightMod,
                () => impostorVision);
    }
}