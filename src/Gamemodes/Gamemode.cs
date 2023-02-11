using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Victory;
using VentLib.Options.OptionElement;

namespace TownOfHost.Gamemodes;

public abstract class Gamemode: IGamemode
{
    public Dictionary<GameAction, List<MethodInfo>> boundActions = new();
    //private TempOption tempOption = new();


    public abstract string GetName();
    public abstract IEnumerable<GameOptionTab> EnabledTabs();
    public virtual GameAction IgnoredActions() => (GameAction)33554432;

    public virtual void Activate() {}

    public virtual void Deactivate() {}

    public virtual void FixedUpdate() { }

    public abstract void AssignRoles(List<PlayerControl> players);

    public abstract void SetupWinConditions(WinDelegate winDelegate);

    protected void BindAction(GameAction action, Delegate method)
    {
        List<MethodInfo> methods = boundActions.GetValueOrDefault(action, new List<MethodInfo>());
        methods.Add(method.Method);
        boundActions[action] = methods;
    }

    public void Trigger(GameAction action, params object[] args)
    {
        boundActions.GetValueOrDefault(action, new List<MethodInfo>()).Do(m => m.InvokeAligned(this, args));
    }

    public void AddOption(Option Option)
    {
        //tempOption.Add(Option);
    }
}