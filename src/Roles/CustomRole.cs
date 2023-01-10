using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;
using VentLib;

namespace TownOfHost.Roles;

public abstract class CustomRole : AbstractBaseRole, IRpcSendable<CustomRole>
{
    static CustomRole()
    {
        AbstractConstructors.Register(typeof(CustomRole), r => CustomRoleManager.GetRoleFromId(r.ReadInt32()));
    }

    public virtual bool CanVent() => baseCanVent || StaticOptions.AllRolesCanVent;
    public virtual bool CanBeKilled() => true;
    public virtual bool CanBeKilledBySheriff() => this.VirtualRole is RoleTypes.Impostor or RoleTypes.Shapeshifter;
    public virtual bool HasTasks() => this is Crewmate;
    public bool IsDesyncRole() => this.DesyncRole != null;
    public virtual bool IsAllied(PlayerControl player) => this.Factions.Any(f => f.IsAllied(player.GetCustomRole().Factions)) && player.GetCustomRole().Factions.Any(f => f.IsAllied(this.Factions));
    private HashSet<GameOptionOverride> currentOverrides = new();


    /// <summary>
    /// Utilized for "live" instances of the class AKA when the game is actually being played
    /// </summary>
    /// <returns>Shallow clone of this class (except for certain fields such as roleOptions being a deep clone)</returns>
    public CustomRole Instantiate(PlayerControl player)
    {
        CustomRole cloned = Clone();
        cloned.MyPlayer = player;

        cloned.Setup(player);
        cloned.SetupUI(player.GetDynamicName());
        player.GetDynamicName().Render();
        if (StaticOptions.AllRolesCanVent && cloned.VirtualRole == RoleTypes.Crewmate)
            cloned.VirtualRole = RoleTypes.Engineer;
        return cloned;
    }

    public CustomRole Clone()
    {
        CustomRole cloned = (CustomRole)this.MemberwiseClone();
        cloned.roleSpecificGameOptionOverrides = new();
        cloned.currentOverrides = new();
        cloned.Modify(new RoleModifier(cloned));
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


    public void Assign(bool desync = false)
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
            PlayerControl[] allies = Game.GetAllPlayers().Where(p => IsAllied(p) || p.PlayerId == MyPlayer.PlayerId).ToArray();
            int[] alliesCID = allies.Select(p => p.GetClientId()).ToArray();

            allies.Select(player => player.GetRawName()).PrettyString().DebugLog($"{this.RoleName}'s allies are: ");
            //int[] allies = allies.Where(ally => ally.is)
            // Send to all clients, excluding allies, that you're a crewmate
            RpcV2.Immediate(MyPlayer.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).SendToAll(exclude: alliesCID);
            // Send to allies your real role
            RpcV2.Immediate(MyPlayer.NetId, (byte)RpcCalls.SetRole).Write((byte)assignedType).SendToFollowing(include: alliesCID);
            // Finally, for all players that are not your allies make them crewmates
            Game.GetAllPlayers()
                .Where(pc => !alliesCID.Contains(pc.GetClientId()) && pc.PlayerId != MyPlayer.PlayerId)
                .Do(pc => RpcV2.Immediate(pc.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).Send(MyPlayer.GetClientId()));
            ShowRoleToTeammates(allies);
        }
        else
            MyPlayer.RpcSetRole(this.VirtualRole);
        HudManager.Instance.SetHudActive(true);
    }

    private void ShowRoleToTeammates(IEnumerable<PlayerControl> allies)
    {
        // Currently only impostors can show each other their roles
        if (!this.Factions.IsImpostor()) return;
        DynamicName myName = MyPlayer.GetDynamicName();
        allies.Where(ally => ally.PlayerId != MyPlayer.PlayerId).Do(ally=>
        {
            myName.AddRule(GameState.InIntro, UI.Role, playerId: ally.PlayerId);
            myName.AddRule(GameState.InMeeting, UI.Role, playerId: ally.PlayerId);
            myName.AddRule(GameState.Roaming, UI.Role, playerId: ally.PlayerId);
        });
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
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

    public CustomRole Read(MessageReader reader)
    {
        return CustomRoleManager.GetRoleFromId(reader.ReadInt32());
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(CustomRoleManager.GetRoleId(this));
    }
}