using System.Collections.Generic;
using TownOfHost.Options;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes;

public abstract class Gamemode: IGamemode
{
    public abstract string GetName();
    public abstract IEnumerable<GameOptionTab> EnabledTabs();
    public virtual GameAction IgnoredActions() => (GameAction)33554432;

    public virtual void Activate() {}

    public virtual void Deactivate() {}

    public virtual void FixedUpdate() { }

    public abstract void AssignRoles(List<PlayerControl> players);

    public abstract void SetupWinConditions(WinDelegate winDelegate);
}