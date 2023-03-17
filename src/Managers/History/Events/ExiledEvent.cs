using System.Collections.Generic;

namespace TOHTOR.Managers.History.Events;

public class ExiledEvent : DeathEvent
{
    private List<PlayerControl> voters;
    private List<PlayerControl> abstainers;

    public ExiledEvent(PlayerControl deadPlayer, List<PlayerControl> voters, List<PlayerControl> abstainers) : base(deadPlayer, null)
    {
        this.voters = voters;
        this.abstainers = abstainers;
    }

    public List<PlayerControl> Abstainers() => abstainers;

    public List<PlayerControl> Voters() => voters;

    public override string SimpleName() => ModConstants.DeathNames.Exiled;
}