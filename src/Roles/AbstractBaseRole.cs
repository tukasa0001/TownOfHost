using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

// Some people hate using "Base" and "Abstract" in class names but I used both so now I'm a war-criminal :)
[Localized(Group = "Roles")]
public abstract class AbstractBaseRole
{
    public PlayerControl MyPlayer { get; protected set;  }
    private static bool ROLE_DEBUG = true;

    public static T Ref<T>() where T : CustomRole
    {
        int roleId = CustomRoleManager.GetRoleId(typeof(T));
        if (roleId != -1) return (T)CustomRoleManager.GetRoleFromId(roleId);
        if (ROLE_DEBUG)
            VentLogger.Warn($"Illegally Constructing Role for {typeof(T)}", "RoleWarning");
        return (T)typeof(T).GetConstructor(Array.Empty<Type>()).Invoke(null);
    }

    public RoleEditor? Editor { get; internal set; }
    private static List<RoleEditor> _editors = new();


    public string Description => Localizer.Get($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Description");
    public string Blurb => Localizer.Get($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Blurb");

    public string RoleName {
        get {
            string name = Localizer.Get($"Roles.{EnglishRoleName.RemoveHtmlTags()}.RoleName");
            return name == "N/A" ? EnglishRoleName : name;
        }
    }

    public RoleTypes RealRole => DesyncRole ?? VirtualRole;
    public RoleTypes? DesyncRole;
    public RoleTypes VirtualRole;
    public Faction[] Factions { get; private set; } = { Faction.Crewmates };
    public SpecialType SpecialType = SpecialType.None;
    public Color RoleColor = Color.white;
    public bool IsSubrole { get; private set; }
    public int Chance { get; private set;  }
    public int Count { get; private set; }
    public int AdditionalChance { get; private set; }
    protected bool BaseCanVent;

    private OptionHolder options;

    public string EnglishRoleName { get; private set; }
    private readonly Dictionary<Type, List<MethodInfo>> roleInteractions = new();
    private readonly Dictionary<Faction, List<MethodInfo>> factionInteractions = new();
    private readonly Dictionary<RoleActionType, List<RoleAction>> roleActions = new();

    protected List<GameOptionOverride> roleSpecificGameOptionOverrides = new();

    private static SmartOptionBuilder RoleOptionsBuilder => roleOptionsBuilder.Clone();
    private static SmartOptionBuilder roleOptionsBuilder = new SmartOptionBuilder()
        .AddIntRangeValues(0, 100, 10, 0, "%")
        .ShowSubOptionsWhen(value => ((int)value) > 0);

    protected AbstractBaseRole()
    {
        this.EnglishRoleName = this.GetType().Name.Replace("CRole", "").Replace("Role", "");
        CreateCooldowns();
        // Why? Modify may reference uncreated options, yet when setting up options developers may try to reference
        // RoleColor (which is white until after Modify)
        // To solve this we call Modify to TRY to setup the role color, crashing once it requires uncreated options
        // The modify at the end of this method is the "real" modify
        RoleModifier _;
        try {
            _ = _editors.Aggregate(Modify(new RoleModifier(this)), (current, editor) => editor.HookModifier(current));
        } catch { }
        this.roleSpecificGameOptionOverrides.Clear();

        options = _editors.Aggregate(GetOptionBuilder(), (current, editor) => editor.HookOptions(current)).Build();
        if (options.Name != null || options.GetAsString() != "N/A")
        {
            TOHPlugin.OptionManager.Add(options);
            if (options.Tab == null)
            {
                if (this is GM) { /*ignored*/ }
                if (this is Subrole)
                    options.Tab = DefaultTabs.MiscTab;
                else if (this.Factions.IsImpostor())
                    options.Tab = DefaultTabs.ImpostorsTab;
                else if (this.Factions.IsCrewmate())
                    options.Tab = DefaultTabs.CrewmateTab;
                else if (this.SpecialType is SpecialType.NeutralKilling or SpecialType.Neutral)
                    options.Tab = DefaultTabs.NeutralTab;
                else
                    options.Tab = DefaultTabs.MiscTab;
            }
        }

        SetupRoleActions();
        SetupRoleInteractions();
        options.valueHolder?.UpdateBinding();
        _ = _editors.Aggregate(Modify(new RoleModifier(this)), (current, editor) => editor.HookModifier(current));
        options.Name = RoleName;
    }

    private void SetupRoleActions()
    {
        Enum.GetValues<RoleActionType>().Do(action => this.roleActions.Add(action, new List<RoleAction>()));

        this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SelectWhere(method => (method.GetCustomAttribute<RoleActionAttribute>(), method), t => t.Item1 != null)
            .Select(t => new RoleAction(t.Item1!, t.method))
            .Do(AddRoleAction);
    }

    private void AddRoleAction(RoleAction action)
    {
        List<RoleAction> currentActions = this.roleActions.GetValueOrDefault(action.ActionType, new List<RoleAction>());

        if (action.Attribute.Override != null) {
            int overrideIndex = currentActions.FindIndex(m => m.method.Name == action.Attribute.Override);
            if (overrideIndex != -1) currentActions[overrideIndex] = action;
            this.roleActions[action.ActionType] = currentActions;
            return;
        }

        VentLogger.Log(LogLevel.All, $"Registering Action {this.GetType()} || {action.ActionType} => {action.method.Name}");
        if (action.ActionType is RoleActionType.FixedUpdate &&
            currentActions.Count > 0)
            throw new ConstraintException("RoleActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

        if (action.Attribute.Subclassing || action.method.DeclaringType == this.GetType())
            currentActions.Add(action);

        this.roleActions[action.ActionType] = currentActions;
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
            List<RoleAction> methods = roleActions[RoleActionType.FixedUpdate];
            if (methods.Count == 0) return;
            methods[0].ExecuteFixed(this);
            return;
        }

        handle.ActionType = actionType;
        parameters = parameters.AddToArray(handle);
        // Block ALL triggers if not host


        bool inBlockList = MyPlayer != null && CustomRoleManager.RoleBlockedPlayers.Contains(MyPlayer.PlayerId);
        roleActions[actionType].Do(action =>
        {
            if (StaticOptions.LogAllActions)
            {
                VentLogger.Trace($"{MyPlayer.GetNameWithRole()} :: {actionType.ToString()}", "ActionLog");
                VentLogger.Trace($"Parameters: {parameters.StrJoin()} :: Blocked? {inBlockList && action.Blockable}", "ActionLog");
            }
            if (!inBlockList || !action.Blockable)
                action.Execute(this, parameters);
        });
    }

    // lol this method is such a hack it's funny
    public IEnumerable<(RoleAction, AbstractBaseRole)> GetActions(RoleActionType actionType) => roleActions[actionType].Select(action => (action, this));


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


    public SmartOptionBuilder GetOptionBuilder() {
        SmartOptionBuilder b = RoleOptionsBuilder.Color(RoleColor).Bind(val => this.Chance = (int)val)
            .AddSubOption(s => s.Name("Maximum")
                .AddValues(1..15)
                .Bind(val => this.Count = (int)val)
                .ShowSubOptionsWhen(v => 1 < (int)v)
                .AddSubOption(subsequent => subsequent
                    .Name(Localizer.Get("Roles.Options.SubsequentChance"))
                    .AddIntRangeValues(10, 100, 10, 0, "%")
                    .BindInt(v => AdditionalChance = v)
                    .Build())
                .Build());

        return RegisterOptions(b);
    }

    protected virtual SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        optionStream.Display(EnglishRoleName, () => RoleName).IsHeader(true);
        return optionStream;
    }

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
            myRole.BaseCanVent = canVent;
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
            myRole.EnglishRoleName = adjustedName;
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

    public abstract class RoleEditor
    {
        internal AbstractBaseRole FrozenRole { get; }
        internal AbstractBaseRole ModdedRole = null!;
        internal CustomRole? RoleInstance;

        internal RoleEditor(AbstractBaseRole baseRole)
        {
            this.FrozenRole = baseRole;
        }

        internal AbstractBaseRole StartLink()
        {
            _editors.Clear();
            _editors.Add(this);
            this.ModdedRole = (AbstractBaseRole)Activator.CreateInstance(FrozenRole.GetType())!;
            this.ModdedRole.Editor = this;
            _editors.Clear();
            this.SetupActions();
            OnLink();
            return ModdedRole;
        }

        internal RoleEditor Instantiate(CustomRole role, PlayerControl player)
        {
            RoleEditor cloned = (RoleEditor)this.MemberwiseClone();
            cloned.RoleInstance = role;
            cloned.HookSetup(player);
            return cloned;
        }

        public virtual void HookSetup(PlayerControl myPlayer) { }

        public virtual RoleModifier HookModifier(RoleModifier modifier) {
            return modifier;
        }

        public virtual SmartOptionBuilder HookOptions(SmartOptionBuilder optionStream) {
            return optionStream;
        }

        public abstract void OnLink();

        private void PatchHook(object?[] args, ModifiedAction action, MethodInfo baseMethod)
        {
            if (action.Behaviour is ModifiedBehaviour.PatchBefore)
            {
                object? result = action.method.InvokeAligned(args);
                if (action.method.ReturnType == typeof(bool) && (result == null || (bool)result))
                    baseMethod.InvokeAligned(args);
                return;
            }

            baseMethod.InvokeAligned(args);
            action.method.InvokeAligned(args);
        }

        private void SetupActions()
        {
            this.GetType().GetMethods(BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectWhere(method => (method.GetCustomAttribute<RoleActionAttribute>(), method), t => t.Item1 != null)
                .Select(t => t.Item1 is ModifiedActionAttribute modded ? new ModifiedAction(modded, t.method) : new RoleAction(t.Item1!, t.method))
                .Do(action =>
                {
                    if (action is not ModifiedAction modded) ModdedRole.AddRoleAction(action);
                    else {
                        List<RoleAction> currentActions = ModdedRole.roleActions.GetValueOrDefault(action.ActionType, new List<RoleAction>());

                        switch (modded.Behaviour)
                        {
                            case ModifiedBehaviour.Replace:
                                currentActions.Clear();
                                currentActions.Add(modded);
                                break;
                            case ModifiedBehaviour.Addition:
                                currentActions.Add(modded);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        ModdedRole.roleActions[action.ActionType] = currentActions;
                    }
                });
        }
    }
}