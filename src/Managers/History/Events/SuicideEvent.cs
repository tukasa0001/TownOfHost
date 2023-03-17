using TOHTOR.API;

namespace TOHTOR.Managers.History.Events;

public class SuicideEvent : DeathEvent
{
    public SuicideEvent(PlayerControl player) : base(player, player)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Suicide;

    public override string Message() => $"{Game.GetName(Player())} suicided.";
}