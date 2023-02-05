using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.GUI;
using TownOfHost.Managers;
using VentLib.Options;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

public class Disperser: Impostor
{
    [DynElement(UI.Cooldown)]
    private Cooldown abilityCooldown;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.OnPet)]
    private void DispersePlayers()
    {
        if (abilityCooldown.NotReady()) return;
        abilityCooldown.Start();
        List<Vent> vents = Object.FindObjectsOfType<Vent>().ToList();
        if (vents.Count == 0) return;
        Game.GetAlivePlayers()
            .Where(p => p.PlayerId != MyPlayer.PlayerId)
            .Do(p =>
            {
                Vector2 ventPosition = vents.GetRandom().transform.position;
                Utils.Teleport(p.NetTransform, new Vector2(ventPosition.x, ventPosition.y + 0.3636f));
            });
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Disperse Cooldown")
                .BindFloat(v => abilityCooldown.Duration = v)
                .AddFloatRange(0, 120, 2.5f, 5, "s")
                .Build());
}