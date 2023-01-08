using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes;

public abstract class Gamemode: IGamemode
{
    public abstract string GetName();

    public virtual bool AllowSabotage() => true;

    public virtual bool AllowBodyReport() => true;

    public abstract void AssignRoles(List<PlayerControl> players);

    public abstract IEnumerable<GameOptionTab> EnabledTabs();

    public virtual void FixedUpdate() { }

    public abstract void SetupWinConditions(WinDelegate winDelegate);
}