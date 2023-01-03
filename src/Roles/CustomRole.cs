using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;
using TownOfHost.RPC;
using UnityEngine;

namespace TownOfHost.Roles;

public abstract class CustomRole : AbstractBaseRole
{
    private bool escorted = false;
    private bool impostorRoleVent;

    public virtual bool CanVent() => (this.VirtualRole is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Engineer || this.DesyncRole is RoleTypes.Impostor or RoleTypes.Shapeshifter && !baseCannotVent) || StaticOptions.allRolesCanVent;
    public virtual bool CanBeKilledBySheriff() => this.VirtualRole is RoleTypes.Impostor or RoleTypes.Shapeshifter;
    public virtual bool HasTasks() => this is Crewmate;
    public bool IsDesyncRole() => this.DesyncRole != null;
    public virtual bool CanBeKilled() => true;
    public virtual bool IsAllied(PlayerControl player) => this.Factions.Any(f => f.IsAllied(player.GetCustomRole().Factions)) && player.GetCustomRole().Factions.Any(f => f.IsAllied(this.Factions));
    private HashSet<GameOptionOverride> currentOverrides = new();


    /// <summary>
    /// Utilized for "live" instances of the class AKA when the game is actually being played
    /// </summary>
    /// <returns>Shallow clone of this class (except for certain fields such as roleOptions being a deep clone)</returns>
    public CustomRole Instantiate(PlayerControl player)
    {
        CustomRole cloned = (CustomRole)this.MemberwiseClone();
        cloned.MyPlayer = player;
        cloned.roleSpecificGameOptionOverrides = new();
        cloned.currentOverrides = new();
        cloned.Modify(new RoleModifier(cloned));

        cloned.Setup(player);
        cloned.SetupUI(player.GetDynamicName());
        player.GetDynamicName().Render();
        if (StaticOptions.allRolesCanVent && cloned.VirtualRole == RoleTypes.Crewmate)
            cloned.VirtualRole = RoleTypes.Engineer;
        return cloned;
    }

    public bool IsEnabled() => this.Chance > 0 && this.Count > 0;

    public virtual void OnGameStart() { }

    /// <summary>
    /// Adds a GameOverride that continuously modifies this instances game options until removed
    /// </summary>
    /// <param name="optionOverride">Override to apply whenever SyncOptions is called</param>
    protected void AddOverride(GameOptionOverride optionOverride) => currentOverrides.Add(optionOverride);
    /// <summary>
    /// Removes a continuous GameOverride
    /// </summary>
    /// <param name="optionOverride">Override to remove</param>
    protected void RemoveOverride(GameOptionOverride optionOverride) => currentOverrides.Remove(optionOverride);
    /// <summary>
    /// Removes a continuous GameOverride
    /// </summary>
    /// <param name="override">Override type to remove</param>
    protected void RemoveOverride(Override @override) => currentOverrides.RemoveWhere(o => o.Option == @override);

    // Useful for shorthand delegation
    public void SyncOptions() => SyncOptions(null);

    public void SyncOptions(IEnumerable<GameOptionOverride> newOverrides = null)
    {
        if (MyPlayer == null || !AmongUsClient.Instance.AmHost) return;
        List<GameOptionOverride> thisList = new(currentOverrides);

        thisList.AddRange(this.roleSpecificGameOptionOverrides);
        if (newOverrides != null) thisList.AddRange(newOverrides);

        thisList.PrettyString().DebugLog($"Sending Overrides To {MyPlayer.GetNameWithRole()}: ");

        DesyncOptions.SendModifiedOptions(thisList, MyPlayer);
    }


    public void AssignRole(bool desync = false)
    {
        Type parentType = this.GetType().BaseType;
        Logger.Info($"{MyPlayer.GetRawName()} | {parentType} | {this.RoleName}", "ROLE SET");
        // Always will be desynced
        // Here we do a "lazy" check for (all?) conditions that'd cause a role to need to be desync
        if (this.DesyncRole != null || this is Impostor)
        {

            Logger.Info($"{this.RoleName} is desync", "DesyncInfo");


            // Get the ACTUAL role to assign the player
            RoleTypes assignedType = this.DesyncRole ?? this.VirtualRole;
            // Get the opposite type of this role
            if (MyPlayer == PlayerControl.LocalPlayer && AmongUsClient.Instance.AmHost)
                MyPlayer.SetRole(assignedType); // Required because the rpc below doesn't target host
            else
            {
                // Send information to client about their new role
                Logger.Info($"Sending role ({assignedType}) information to {MyPlayer.GetRawName()}", "");
                RpcV2.Immediate(MyPlayer.NetId, (byte)RpcCalls.SetRole).Write((byte)assignedType)
                    .Send(MyPlayer.GetClientId());
            }

            // Determine roles that are "allied" with this one(based on method overrides)
            int[] allies = Game.GetAllPlayers().Where(p => IsAllied(p) || p.PlayerId == MyPlayer.PlayerId).Select(p => p.GetClientId()).ToArray();

            allies.Select(id => Utils.GetPlayerByClientId(id).GetRawName()).ToList().PrettyString().DebugLog($"{this.RoleName}'s allies are: ");
            //int[] allies = allies.Where(ally => ally.is)
            // Send to all clients, excluding allies, that you're a crewmate
            RpcV2.Immediate(MyPlayer.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).SendToAll(exclude: allies);
            // Send to allies your real role
            RpcV2.Immediate(MyPlayer.NetId, (byte)RpcCalls.SetRole).Write((byte)assignedType).SendToFollowing(include: allies);
            // Finally, for all players that are not your allies make them crewmates
            PlayerControl.AllPlayerControls.ToArray()
                .Where(pc => !allies.Contains(pc.GetClientId()) && pc.PlayerId != MyPlayer.PlayerId)
                .Do(pc => RpcV2.Immediate(pc.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).Send(MyPlayer.GetClientId()));
            if (this.Factions.Contains(Faction.Impostors)) allies.Do(p => MyPlayer.GetDynamicName().RenderAsIf(GameState.InMeeting, specific: p));
        }
        else
            MyPlayer.RpcSetRole(this.VirtualRole);
        HudManager.Instance.SetHudActive(true);
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        /*OptionPage page = this.SpecialType switch
        {
            SpecialType.Neutral => OptionManager.NeutralPage,
            SpecialType.NeutralKilling => OptionManager.NeutEvilPage,
            SpecialType.Coven => OptionManager.ImpostorPage,
            _ => this.IsImpostor() ? OptionManager.ImpostorPage : OptionManager.CrewmatePage
        };*/
        optionStream.Name(RoleName).IsHeader(true);
        return optionStream;
    }

    private void SetupUI(DynamicName name)
    {
        Dictionary<UI, Type> declaringComponents = new();

        CreateCooldowns();
        this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(f => f.GetCustomAttribute<DynElement>() != null)
            .Do(f =>
            {
                DynElement dynElement = f.GetCustomAttribute<DynElement>();
                bool isCooldown = false;
                try { isCooldown = f.GetValue(this) is Cooldown; }
                catch { /*ignored*/ }
                if (declaringComponents.TryGetValue(dynElement.Component, out Type type))
                    if (type.IsAssignableTo(f.DeclaringType)) return;

                declaringComponents.Add(dynElement.Component, f.DeclaringType);
                name.SetComponentValue(dynElement.Component, new DynamicString(() =>
                {
                    string value = f.GetValue(this)?.ToString() ?? "N/A";
                    return isCooldown ? value == "0" ? "" : $"<color=#ed9247>CD:</color> {Color.white.Colorize(value + "s")}" : value;
                }));
            });

        this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<DynElement>() != null)
            .Do(m =>
            {
                DynElement dynElement = m.GetCustomAttribute<DynElement>();
                if (m.GetParameters().Length > 0)
                    throw new ConstraintException("Methods marked by DynElement must have no parameters");

                if (declaringComponents.TryGetValue(dynElement.Component, out Type type))
                    if (type.IsAssignableTo(m.DeclaringType)) return;

                declaringComponents.Add(dynElement.Component, m.DeclaringType);
                name.SetComponentValue(dynElement.Component, new DynamicString(() => m.Invoke(this, null)?.ToString() ?? "N/A"));
            });
    }
}