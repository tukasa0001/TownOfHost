using System;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Roles.Neutral;
using UnityEngine;
using VentLib.Logging;

namespace TownOfHost.Roles;

// This is going to be the longest option list :(
public class Investigator : Crewmate
{
    protected static readonly List<Tuple<Type, Color, InvestOptCategory>> InvestCategoryList = new()
    {
        new Tuple<Type, Color, InvestOptCategory>(typeof(Amnesiac), new Color(0.51f, 0.87f, 0.99f), InvestOptCategory.NeutralPassive),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Executioner), new Color(0.55f, 0.17f, 0.33f), InvestOptCategory.NeutralPassive),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Jester), new Color(0.93f, 0.38f, 0.65f), InvestOptCategory.NeutralPassive),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Opportunist), Color.green, InvestOptCategory.NeutralPassive),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Phantom), new Color(0.51f, 0.87f, 0.99f), InvestOptCategory.NeutralPassive),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Survivor), new Color(1f, 0.9f, 0.3f), InvestOptCategory.NeutralPassive),

        new Tuple<Type, Color, InvestOptCategory>(typeof(Arsonist), new Color(1f, 0.4f, 0.2f), InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(BloodKnight), Utils.ConvertHexToColor("#630000"), InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(CrewPostor), Utils.ConvertHexToColor("#DC6601"), InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Egoist), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Glitch), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Hacker), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Jackal), new Color(0f, 0.71f, 0.92f), InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Juggernaut), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Marksman), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(NeutWitch), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Pestilence), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Pirate), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(PlagueBearer), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Poisoner), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Swapper), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Vulture), Color.green, InvestOptCategory.NeutralKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Werewolf), Color.green, InvestOptCategory.NeutralKilling),

        new Tuple<Type, Color, InvestOptCategory>(typeof(Sheriff), new Color(0.97f, 0.8f, 0.27f), InvestOptCategory.CrewmateKilling),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Veteran), new Color(0.6f, 0.5f, 0.25f), InvestOptCategory.CrewmateKilling),

        new Tuple<Type, Color, InvestOptCategory>(typeof(Conjuror), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Coven), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(CovenWitch), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(HexMaster), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Medusa), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Mimic), Color.red, InvestOptCategory.Coven),
        new Tuple<Type, Color, InvestOptCategory>(typeof(PotionMaster), Color.red, InvestOptCategory.Coven),

        new Tuple<Type, Color, InvestOptCategory>(typeof(Madmate), Color.red, InvestOptCategory.Madmate),
        new Tuple<Type, Color, InvestOptCategory>(typeof(MadGuardian), Color.red, InvestOptCategory.Madmate),
        new Tuple<Type, Color, InvestOptCategory>(typeof(MadSnitch), Color.red, InvestOptCategory.Madmate),
        new Tuple<Type, Color, InvestOptCategory>(typeof(Parasite), Color.red, InvestOptCategory.Madmate),
    };

    [DynElement(UI.Cooldown)]
    private Cooldown abilityCooldown;
    private NIOpt neutralPassiveRed;
    private NIOpt neutralKillingRed;
    private NIOpt crewmateKillingRed;
    private NIOpt covenPurple;
    private NIOpt madmateRed;

    private List<int> redRoles = new();
    private List<byte> investigated;

    protected override void Setup(PlayerControl player)
    {
        investigated = new List<byte>();
        base.Setup(player);
    }

    [RoleAction(RoleActionType.OnPet)]
    private void Investigate()
    {
        if (abilityCooldown.NotReady()) return;
        List<PlayerControl> players = MyPlayer.GetPlayersInAbilityRangeSorted().Where(p => !investigated.Contains(p.PlayerId)).ToList();
        if (players.Count == 0) return;

        abilityCooldown.Start();
        PlayerControl player = players[0];
        InteractionResult result = CheckInteractions(player.GetCustomRole(), player);
        if (result is InteractionResult.Halt) return;

        investigated.Add(player.PlayerId);
        CustomRole role = player.GetCustomRole();

        int categoryIndex = InvestCategoryList.FindIndex(tuple => tuple.Item1 == role.GetType());
        InvestOptCategory category = categoryIndex != -1 ? InvestCategoryList[categoryIndex].Item3 : InvestOptCategory.None;

        Color good = new(0.35f, 0.71f, 0.33f);
        Color bad = new(0.72f, 0.04f, 0f);
        Color purple = new(0.45f, 0.31f, 0.72f);

        bool roleIsInRoles = redRoles.Contains(categoryIndex);
        Color color = (category) switch
        {
            InvestOptCategory.None => role.Factions.IsImpostor() ? bad : good,
            InvestOptCategory.NeutralPassive => neutralPassiveRed is NIOpt.All ? bad : neutralPassiveRed is NIOpt.None ? good : roleIsInRoles ? bad : good,
            InvestOptCategory.NeutralKilling => neutralKillingRed is NIOpt.All ? bad : neutralKillingRed is NIOpt.None ? good : roleIsInRoles ? bad : good,
            InvestOptCategory.CrewmateKilling => crewmateKillingRed is NIOpt.All ? bad : crewmateKillingRed is NIOpt.None ? good : roleIsInRoles ? bad : good,
            InvestOptCategory.Coven => covenPurple is NIOpt.All ? purple : covenPurple is NIOpt.None ? good : roleIsInRoles ? purple : good,
            InvestOptCategory.Madmate => madmateRed is NIOpt.All ? bad : madmateRed is NIOpt.None ? good : roleIsInRoles ? bad : good,
            _ => throw new ArgumentOutOfRangeException()
        };
        VentLogger.Old($"{player.GetNameWithRole()} is type {role.GetType()} and falls under category \"{category}\". Player is in redRoles list? {roleIsInRoles}. Player's name should be color: {color.ToTextColor()}", "InvestigateInfo");

        player.GetDynamicName().AddRule(GameState.Roaming, UI.Name, new DynamicString(color.Colorize("{0}")), MyPlayer.PlayerId);
        player.GetDynamicName().AddRule(GameState.InMeeting, UI.Name, new DynamicString(color.Colorize("{0}")), MyPlayer.PlayerId);
        player.GetDynamicName().RenderFor(MyPlayer);
    }

    [RoleInteraction(typeof(Veteran))]
    private InteractionResult InvestigatorVeteranInteraction(PlayerControl veteran) => veteran.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;


    // This is the most complicated options because of all the individual settings
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        SmartOptionBuilder neutPassiveBuilder = new SmartOptionBuilder(null, 1)
            .Name("Neutral Passive are Red")
            .BindInt(v => neutralPassiveRed = (NIOpt)v)
            .ShowSubOptionsWhen(v => (int)v >= 2)
            .AddValue(v => v.Text("None").Value(1).Color(Color.red).Build())
            .AddValue(v => v.Text("All").Value(0).Color(Color.cyan).Build())
            .AddValue(v => v.Text("Individual").Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build());
        SmartOptionBuilder neutKillBuilder = new SmartOptionBuilder(null, 1)
            .Name("Neutral Killing are Red")
            .BindInt(v => neutralKillingRed = (NIOpt)v)
            .ShowSubOptionsWhen(v => (int)v >= 2)
            .AddValue(v => v.Text("None").Value(1).Color(Color.red).Build())
            .AddValue(v => v.Text("All").Value(0).Color(Color.cyan).Build())
            .AddValue(v => v.Text("Individual").Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build());
        SmartOptionBuilder crewmateKillBuilder = new SmartOptionBuilder(null, 1)
            .Name("Crewmate Killing are Red")
            .BindInt(v => crewmateKillingRed = (NIOpt)v)
            .ShowSubOptionsWhen(v => (int)v >= 2)
            .AddValue(v => v.Text("None").Value(1).Color(Color.red).Build())
            .AddValue(v => v.Text("All").Value(0).Color(Color.cyan).Build())
            .AddValue(v => v.Text("Individual").Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build());
        SmartOptionBuilder covenBuilder = new SmartOptionBuilder(null, 1)
            .Name("Coven are Purple")
            .BindInt(v => covenPurple = (NIOpt)v)
            .ShowSubOptionsWhen(v => (int)v >= 2)
            .AddValue(v => v.Text("None").Value(1).Color(Color.red).Build())
            .AddValue(v => v.Text("All").Value(0).Color(Color.cyan).Build())
            .AddValue(v => v.Text("Individual").Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build());
        SmartOptionBuilder madmateBuilder = new SmartOptionBuilder(null, 1)
            .Name("Madmate are Red")
            .BindInt(v => madmateRed = (NIOpt)v)
            .ShowSubOptionsWhen(v => (int)v >= 2)
            .AddValue(v => v.Text("None").Value(1).Color(Color.red).Build())
            .AddValue(v => v.Text("All").Value(0).Color(Color.cyan).Build())
            .AddValue(v => v.Text("Individual").Value(2).Color(new Color(0.73f, 0.58f, 1f)).Build());

        SmartOptionBuilder[] builders = { neutPassiveBuilder, neutKillBuilder, crewmateKillBuilder, covenBuilder, madmateBuilder };

        for (int i = 0; i < InvestCategoryList.Count; i++)
        {
            Tuple<Type, Color, InvestOptCategory> item = InvestCategoryList[i];
            SmartOptionBuilder builder = builders[(int)item.Item3 - 1];

            var i1 = i;
            builder.AddSubOption(sub => sub
                .Name(item.Item1.Name)
                .Color(item.Item2)
                .Bind(v =>
                {
                    if ((bool)v)
                        redRoles.Add(i1);
                    else
                        redRoles.Remove(i1);
                })
                .AddOnOffValues(false)
                .Build());
        }


        return base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Investigate Cooldown")
                .BindFloat(v => abilityCooldown.Duration = v)
                .AddFloatRangeValues(2.5f, 120, 2.5f, 10, "s")
                .Build())
            .AddSubOption(_ => builders[0].Build())
            .AddSubOption(_ => builders[1].Build())
            .AddSubOption(_ => builders[2].Build())
            .AddSubOption(_ => builders[3].Build())
            .AddSubOption(_ => builders[4].Build());
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1f, 0.79f, 0.51f));


    protected enum NIOpt
    {
        All,
        None,
        Individual
    }

    protected enum InvestOptCategory
    {
        None,
        NeutralPassive,
        NeutralKilling,
        CrewmateKilling,
        Coven,
        Madmate
    }
}