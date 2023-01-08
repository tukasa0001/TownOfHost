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
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

// Some people hate using "Base" and "Abstract" in class names but I used both so now I'm a war-criminal :)
public abstract class AbstractBaseRole
{
    public PlayerControl MyPlayer { get; protected set;  }
    private static bool ROLE_DEBUG = true;

    public static T Ref<T>() where T : CustomRole
    {
        int roleId = CustomRoleManager.GetRoleId(typeof(T));
        if (roleId == -1)
        {
            if (ROLE_DEBUG)
                Logger.Warn($"Illegally Constructing Role for {typeof(T)}", "RoleWarning");
                return (T)typeof(T).GetConstructor(Array.Empty<Type>()).Invoke(null);
            throw new NullReferenceException($"Pseudo-static reference for {typeof(T)} not set in RoleManager");
        }

        return (T) CustomRoleManager.GetRoleFromId(roleId);
    }

    public string RoleName => (TOHPlugin.ForceJapanese.Value ? SupportedLangs.Japanese : SupportedLangs.English).GetStringOrDefault(englishRoleName);

    public RoleTypes? DesyncRole;
    public RoleTypes VirtualRole;
    public Faction[] Factions = { Faction.Crewmates };
    public SpecialType SpecialType = SpecialType.None;
    public Color RoleColor = Color.white;
    public bool IsSubrole;
    public int Chance;
    public int Count;
    protected bool baseCanVent;

    protected string englishRoleName;
    protected Dictionary<Type, List<MethodInfo>> roleInteractions = new();
    protected Dictionary<Faction, List<MethodInfo>> factionInteractions = new();
    protected Dictionary<RoleActionType, List<Tuple<MethodInfo, RoleAction>>> RoleActions = new();
    protected List<GameOptionOverride> roleSpecificGameOptionOverrides = new();

    private static SmartOptionBuilder RoleOptionsBuilder => roleOptionsBuilder.Clone();
    private static SmartOptionBuilder roleOptionsBuilder = new SmartOptionBuilder()
        //.Page(OptionManager.CrewmatePage)
        .AddValue(0, suffix: "%").AddValue(10, suffix: "%").AddValue(20, suffix: "%").AddValue(30, suffix: "%").AddValue(40, suffix: "%")
        .AddValue(50, suffix: "%").AddValue(60, suffix: "%").AddValue(70, suffix: "%").AddValue(80, suffix: "%").AddValue(90, suffix: "%").AddValue(100, suffix: "%")
        .ShowSubOptionsWhen(value => ((int)value) > 0);

    protected AbstractBaseRole()
    {
        this.englishRoleName = this.GetType().Name.Replace("CRole", "").Replace("Role", "");
        CreateCooldowns();
        // Why? Modify may reference uncreated options, yet when setting up options developers may try to reference
        // RoleColor (which is white until after Modify)
        // To solve this we call Modify to TRY to setup the role color, crashing once it requires uncreated options
        // The modify at the end of this method is the "real" modify
        try { Modify(new RoleModifier(this)); } catch { }
        this.roleSpecificGameOptionOverrides.Clear();

        SmartOptionBuilder b = RoleOptionsBuilder.Color(RoleColor).Bind(val => this.Chance = (int)val)
            .AddSubOption(s => s.Name("Maximum")
                .AddValues(1..15)
                .Bind(val => this.Count = (int)val)
                .Build());

        OptionHolder options = RegisterOptions(b).Build();
        if (options.Name != null || options.GetAsString() != "N/A")
        {
            TOHPlugin.OptionManager.Add(options);
            if (options.Tab == null)
            {
                if (this is GM) { /*ignored*/ }
                else if (this.Factions.IsImpostor())
                    options.Tab = DefaultTabs.ImpostorsTab;
                else if (this.Factions.IsCrewmate())
                    options.Tab = DefaultTabs.CrewmateTab;
                else if (this is Subrole)
                    options.Tab = DefaultTabs.MiscTab;
                else if (this.SpecialType is SpecialType.NeutralKilling or SpecialType.Neutral)
                    options.Tab = DefaultTabs.NeutralTab;
                else
                    options.Tab = DefaultTabs.MiscTab;
            }
        }

        SetupRoleActions();
        SetupRoleInteractions();
        options.valueHolder?.UpdateBinding();
        Modify(new RoleModifier(this));

        //this.roleSpecificGameOptionOverrides.PrettyString().DebugLog($"Overrides for {RoleName}");

    }

    private void SetupRoleActions()
    {
        Enum.GetValues<RoleActionType>().Do(action => this.RoleActions.Add(action, new List<Tuple<MethodInfo, RoleAction>>()));

        this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<RoleAction>() != null)
            .Do(method =>
            {
                RoleAction attribute = method.GetCustomAttribute<RoleAction>()!;
                List<Tuple<MethodInfo, RoleAction>> currentMethods = this.RoleActions[attribute.ActionType];


                if (attribute.Override != null) {
                    int overrideIndex = currentMethods.FindIndex(m => m.Item1.Name == attribute.Override);
                    if (overrideIndex != -1) currentMethods[overrideIndex] = new Tuple<MethodInfo, RoleAction>(method, attribute);
                    return;
                }

                $"{this.GetType()} || {attribute.ActionType} => {method.Name}".DebugLog();
                if (attribute.ActionType is RoleActionType.FixedUpdate &&
                    this.RoleActions[RoleActionType.FixedUpdate].Count > 0)
                    throw new ConstraintException("RoleActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

                if (attribute.Subclassing || method.DeclaringType == this.GetType())
                    this.RoleActions[attribute.ActionType].Add(new Tuple<MethodInfo, RoleAction>(method, attribute));
            });
    }

    private void SetupRoleInteractions()
    {
        this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.GetCustomAttributes<RoleInteraction>().Any())
            .Do(method =>
            {
                if (method.ReturnType != typeof(InteractionResult))
                    throw new ArgumentException("Methods annotated with RoleInteraction must have a return type of InteractionResult");

                method.GetCustomAttributes<RoleInteraction>().Do(interaction => {
                    if (interaction.RoleType != null)
                    {
                        if (!this.roleInteractions.ContainsKey(interaction.RoleType)) roleInteractions.Add(interaction.RoleType, new List<MethodInfo>());
                        this.roleInteractions[interaction.RoleType].Add(method);
                    } else if (interaction.RoleFaction != null)
                    {
                        if (!this.factionInteractions.ContainsKey(interaction.RoleFaction)) factionInteractions.Add(interaction.RoleFaction, new List<MethodInfo>());
                        this.factionInteractions[interaction.RoleFaction].Add(method);
                    }
                });
            });
    }

    public void Trigger(RoleActionType actionType, ref ActionHandle handle, params object[] parameters)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (actionType == RoleActionType.FixedUpdate)
        {
            List<Tuple<MethodInfo, RoleAction>> methods = RoleActions[RoleActionType.FixedUpdate];
            if (methods.Count == 0) return;
            methods[0].Item1.Invoke(this, null);
            return;
        }

        handle.ActionType = actionType;
        parameters = parameters.AddToArray(handle);
        // Block ALL triggers if not host


        bool inBlockList = MyPlayer != null && CustomRoleManager.RoleBlockedPlayers.Contains(MyPlayer.PlayerId);
        RoleActions[actionType].Do(method =>
        {
            if (StaticOptions.LogAllActions)
            {
                Logger.Blue($"{MyPlayer.GetNameWithRole()} :: {actionType.ToString()}", "ActionLog");
                Logger.Blue($"Parameters: {parameters.PrettyString()} :: Blocked? {inBlockList && method.Item2.Blockable}", "ActionLog");
            }
            if (!inBlockList || !method.Item2.Blockable)
                method.Item1.InvokeAligned(this, parameters);
        });
    }

    // lol this method is such a hack it's funny
    public IEnumerable<Tuple<MethodInfo, RoleAction, AbstractBaseRole>> GetActions(RoleActionType actionType) => RoleActions[actionType].Select(tuple => new Tuple<MethodInfo, RoleAction, AbstractBaseRole>(tuple.Item1, tuple.Item2, this));


    // Currently role interaction >> faction interaction and I do not trigger faction interaction if role interaction was triggered
    // This is because in some scenarios the "default" faction interaction is not what is wanted when the role is targeted
    // Maybe revisit TODO
    protected InteractionResult CheckInteractions(CustomRole role, params object[] parameters)
    {
        List<MethodInfo> interactionsWithRole = roleInteractions.GetValueOrDefault(role.GetType());
        IEnumerable<MethodInfo> interactionsWithFaction = role.Factions
            .SelectMany(f => factionInteractions.GetValueOrDefault(f, new List<MethodInfo>()));

        if (interactionsWithRole == null && interactionsWithFaction == null) return InteractionResult.Proceed;
        if (interactionsWithRole != null)
            return interactionsWithRole.All(interaction =>
                (InteractionResult) interaction.InvokeAligned(this, parameters) == InteractionResult.Proceed)
                ? InteractionResult.Proceed
                : InteractionResult.Halt;

        return interactionsWithFaction.All(interaction =>
            (InteractionResult) interaction.InvokeAligned(this, parameters) == InteractionResult.Proceed)
            ? InteractionResult.Proceed
            : InteractionResult.Halt;
    }

    protected void CreateCooldowns()
    {
        this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(Cooldown))
            .Do(f =>
            {
                Cooldown value = (Cooldown)f.GetValue(this);
                Cooldown setValue = value == null ? new Cooldown() : value.Clone();
                value?.TimeRemaining();
                f.SetValue(this, setValue);
            });
    }

    /// <summary>
    /// This method is called when the role class is Instantiated (during role selection),
    /// thus allowing modifications to the specific player attached to this role
    /// </summary>
    /// <param name="player">The player assigned to this role</param>
    protected virtual void Setup(PlayerControl player) { }

    /// <summary>
    /// Forced method that allows CustomRoles to provide unique definitions for themselves
    /// </summary>
    /// <param name="roleModifier">Automatically supplied RoleFactory used for class specifications</param>
    /// <returns>Provided <b>OR</b> new RoleFactory</returns>
    protected abstract RoleModifier Modify(RoleModifier roleModifier);

    // TODO: eventually make abstract
    protected abstract SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream);


    public bool Is(CustomRole role) => this.GetType() == role.GetType();


    public override string ToString()
    {
        return this.RoleName;
    }

    public class RoleModifier
    {
        private AbstractBaseRole myRole;

        public RoleModifier(AbstractBaseRole role)
        {
            this.myRole = role;
        }

        public RoleModifier DesyncRole(RoleTypes? desyncRole)
        {
            myRole.DesyncRole = desyncRole;
            return this;
        }

        public RoleModifier VanillaRole(RoleTypes vanillaRole)
        {
            myRole.VirtualRole = vanillaRole;
            return this;
        }

        public RoleModifier SpecialType(SpecialType specialType)
        {
            myRole.SpecialType = specialType;
            return this;
        }

        public RoleModifier Factions(params Faction[] faction)
        {
            myRole.Factions = faction;
            return this;
        }

        public RoleModifier CanVent(bool canVent)
        {
            myRole.baseCanVent = canVent;
            return this;
        }


        public RoleModifier Subrole(bool isSubrole)
        {
            myRole.IsSubrole = isSubrole;
            return this;
        }

        public RoleModifier OptionOverride(Override option, object value, Func<bool> condition = null)
        {
            myRole.roleSpecificGameOptionOverrides.Add(new GameOptionOverride(option, value, condition));
            return this;
        }

        public RoleModifier OptionOverride(Override option, Func<object> valueSupplier, Func<bool> condition = null)
        {
            $"Setting Option Override For {option}".DebugLog();
            myRole.roleSpecificGameOptionOverrides.Add(new GameOptionOverride(option, valueSupplier, condition));
            return this;
        }

        public RoleModifier OptionOverride(GameOptionOverride @override)
        {
            myRole.roleSpecificGameOptionOverrides.Add(@override);
            return this;
        }

        public RoleModifier RoleName(string adjustedName)
        {
            myRole.englishRoleName = adjustedName;
            return this;
        }

        public RoleModifier RoleColor(string htmlColor)
        {
            if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
                myRole.RoleColor = color;
            return this;
        }

        public RoleModifier RoleColor(Color color)
        {
            myRole.RoleColor = color;
            return this;
        }
    }
}