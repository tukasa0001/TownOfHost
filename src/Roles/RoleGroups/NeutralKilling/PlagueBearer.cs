using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Managers;
using TOHTOR.Managers.History.Events;
using TOHTOR.Options;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options;
using VentLib.Options.Events;
using VentLib.Options.Game;
using VentLib.Utilities.Optionals;
using static TOHTOR.Managers.CustomRoleManager;

namespace TOHTOR.Roles.RoleGroups.NeutralKilling;

public class PlagueBearer: NeutralKillingBase
{
    private HashSet<byte> infectedPlayers;
    private int cooldownSetting;
    private float customCooldown;
    private int alivePlayers;

    [DynElement(UI.Counter)]
    private string InfectionCounter() => RoleUtils.Counter(infectedPlayers.Count, alivePlayers, RoleColor);

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        infectedPlayers = new HashSet<byte>();
    }

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (MyPlayer.InteractWith(target, SimpleInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return false;
        MyPlayer.RpcGuardAndKill(target);
        Game.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, target, $"{MyPlayer.GetRawName()} infected {target.GetRawName()}."));

        infectedPlayers.Add(target.PlayerId);
        CheckPestilenceTransform();

        return false;
    }

    [RoleAction(RoleActionType.RoundStart)]
    [RoleAction(RoleActionType.RoundEnd)]
    [RoleAction(RoleActionType.AnyDeath)]
    public void CheckPestilenceTransform(ActionHandle? handle = null)
    {
        handle ??= ActionHandle.NoInit();
        if (handle.ActionType is RoleActionType.RoundStart or RoleActionType.RoundEnd)
        {
            alivePlayers = Game.GetAlivePlayers().Count() - 1;
            VentLogger.Fatal($"Alive Players: {alivePlayers}");
        }
        if (!Game.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).All(p => infectedPlayers.Contains(p.PlayerId))) return;
        Game.AssignRole(MyPlayer, Static.Pestilence);
        Game.GameHistory.AddEvent(new RoleChangeEvent(MyPlayer, Static.Pestilence));
        MyPlayer.GetDynamicName().RemoveComponentValue(UI.Counter);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Infect Cooldown")
                .Value(v => v.Text("Same as Default Kill Cooldown").Value(0).Build())
                .Value(v => v.Text("Half Default Kill Cooldown").Value(1).Build())
                .Value(v => v.Text("Custom Cooldown").Value(2).Build())
                .BindInt(i => cooldownSetting = i)
                .ShowSubOptionPredicate(o => (int)o == 2)
                .SubOption(sub2 => sub2
                    .Name("Custom Infect Cooldown")
                    .AddFloatRange(5f, 120f, 2.5f, 8, "s")
                    .BindFloat(f => customCooldown = f)
                    .Build())
                .Build())
            .SubOption(sub => sub
                .Name("Pestilence Settings")
                .Color(new Color(0.22f, 0.22f, 0.22f))
                .Value(v => v.Text("Show").Color(Color.cyan).Value(true).Build())
                .Value(v => v.Text("Hide").Color(Color.red).Value(false).Build())
                .BindEvent(ev =>
                {
                    OptionHelpers.GetChildren(ev.Source(), true).ForEach(opt => opt.NotifySubscribers(new OptionValueEvent(opt, new Optional<object>(opt.GetValue()), opt.GetValue())));
                    if (!(bool)ev.NewValue()) Utils.RunUntilSuccess(() => Static.Pestilence.SetDefaultSettings(), 1f);
                })
                .ShowSubOptionPredicate(o => (bool)o)
                .SubOption(sub2 => sub2
                    .Name("Unblockable Kill")
                    .AddOnOffValues(false)
                    .BindBool(b => Utils.RunUntilSuccess(() => Static.Pestilence.UnblockableAttacks = b, 1f))
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("Invincibility Settings")
                    .Value(v => v.Text("Default").Value(false).Color(Color.cyan).Build())
                    .Value(v => v.Text("Custom").Value(true).Color(new(0.45f, 0.31f, 0.72f)).Build())
                    .ShowSubOptionPredicate(o => (bool)o)
                    .SubOption(sub3 =>  sub3
                        .Name("Immune to Manipulated Attackers")
                        .AddOnOffValues(false)
                        .BindBool(b => Utils.RunUntilSuccess(() => Static.Pestilence.ImmuneToManipulated = b, 1f))
                        .Build())
                    .SubOption(sub3 => sub3
                        .Name("Immune to Ranged Attacks")
                        .AddOnOffValues(false)
                        .BindBool(b => Utils.RunUntilSuccess(() => Static.Pestilence.ImmuneToRangedAttacks = b, 1f))
                        .Build())
                    .SubOption(sub3 => sub3
                        .Name("Immune to Delayed Attacks")
                        .AddOnOffValues(false)
                        .BindBool(b => Utils.RunUntilSuccess(() => Static.Pestilence.ImmuneToDelayedAttacks = b, 1f))
                        .Build())
                    .SubOption(sub3 => sub3
                        .Name("Immune to Arsonist Ignite")
                        .AddOnOffValues(false)
                        .BindBool(b => Utils.RunUntilSuccess(() => Static.Pestilence.ImmuneToArsonist = b, 1f))
                        .Build())
                    .Build())
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.9f, 1f, 0.7f))
            .CanVent(false)
            .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.KillCooldown) * 2, () => cooldownSetting == 0)
            .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.KillCooldown), () => cooldownSetting == 1)
            .OptionOverride(Override.KillCooldown, () => customCooldown * 2, () => cooldownSetting == 2);


    private class InfectEvent : TargetedAbilityEvent
    {
        public InfectEvent(PlayerControl source, PlayerControl target, bool successful = true) : base(source, target, successful)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} infected {Game.GetName(Target())}";
    }
}
