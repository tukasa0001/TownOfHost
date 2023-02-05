using System.Collections.Generic;
using VentLib.Options;
using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles;

public class Bastion: Engineer
{
    // Here we can use the vent button as cooldown
    private HashSet<int> bombedVents;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        bombedVents = new HashSet<int>();
    }

    [RoleAction(RoleActionType.MyEnterVent)]
    private void PlantBomb(Vent vent)
    {
        if (bombedVents.Contains(vent.Id))
            RoleUtils.RoleCheckedMurder(MyPlayer, MyPlayer);
        else
            bombedVents.Add(vent.Id);
    }

    [RoleAction(RoleActionType.AnyEnterVent)]
    private void EnterVent(Vent vent, PlayerControl player)
    {
        bool isBombed = bombedVents.Remove(vent.Id);
        if (isBombed)
            RoleUtils.RoleCheckedMurder(player, player);
    }

    [RoleAction(RoleActionType.RoundEnd)]
    private void ClearVents() => bombedVents.Clear();

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Plant Bomb Cooldown")
                .BindFloat(v => VentCooldown = v)
                .AddFloatRange(2, 120, 2.5f, 8, "s")
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor("#524f4d")
            .CanVent(false);
}