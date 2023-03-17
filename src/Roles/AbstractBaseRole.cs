using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Text.Json;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.Managers;
using TOHTOR.Options;
using TOHTOR.Roles.Extra;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

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

    internal GameOption Options;

    public string EnglishRoleName { get; private set; }
    private readonly Dictionary<Type, List<MethodInfo>> roleInteractions = new();
    private readonly Dictionary<Faction, List<MethodInfo>> factionInteractions = new();
    private readonly Dictionary<RoleActionType, List<RoleAction>> roleActions = new();

    protected List<GameOptionOverride> roleSpecificGameOptionOverrides = new();

    private static GameOptionBuilder RoleOptionsBuilder => roleOptionsBuilder.Clone();
    private static GameOptionBuilder roleOptionsBuilder = new GameOptionBuilder()
        .AddIntRange(0, 100, 10, 0, "%")
        .ShowSubOptionPredicate(value => ((int)value) > 0);

    protected AbstractBaseRole()
    {
        this.EnglishRoleName = this.GetType().Name.Replace("CRole", "").Replace("Role", "");
        VentLogger.Debug($"Role Name: {EnglishRoleName}");
        CreateInstanceBasedVariables();
        // Why? Modify may reference uncreated options, yet when setting up options developers may try to reference
        // RoleColor (which is white until after Modify)
        // To solve this we call Modify to TRY to setup the role color, crashing once it requires uncreated options
        // The modify at the end of this method is the "real" modify
        RoleModifier _;
        try {
            _ = _editors.Aggregate(Modify(new RoleModifier(this)), (current, editor) => editor.HookModifier(current));
        } catch { }
        this.roleSpecificGameOptionOverrides.Clear();

        Options = _editors.Aggregate(GetGameOptionBuilder(), (current, editor) => editor.HookOptions(current)).Build();
        if (Options.GetValueText() != "N/A")
        {
            if (Options.Tab == null)
            {
                if (this is GM) { /*ignored*/ }
                if (this is Subrole.Subrole)
                    Options.Tab = DefaultTabs.MiscTab;
                else if (this.Factions.IsImpostor())
                    Options.Tab = DefaultTabs.ImpostorsTab;
                else if (this.Factions.IsCrewmate())
                    Options.Tab = DefaultTabs.CrewmateTab;
                else if (this.SpecialType is SpecialType.NeutralKilling or SpecialType.Neutral)
                    Options.Tab = DefaultTabs.NeutralTab;
                else
                    Options.Tab = DefaultTabs.MiscTab;
            }
            Options.Register(OptionManager.GetManager(file: "role_options.txt"), OptionLoadMode.LoadOrCreate);
        }

        SetupRoleActions();
        //options.valueHolder?.UpdateBinding();
        _ = _editors.Aggregate(Modify(new RoleModifier(this)), (current, editor) => editor.HookModifier(current));
        //options. = RoleName;
    }

    private void SetupRoleActions()
    {
        Enum.GetValues<RoleActionType>().Do(action => this.roleActions.Add(action, new List<RoleAction>()));
        this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SelectMany(method => method.GetCustomAttributes<RoleActionAttribute>().Select(a => (a, method)))
            .Where(t => t.a.Subclassing || t.method.DeclaringType == this.GetType())
            .Select(t => new RoleAction(t.Item1, t.method))
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

        VentLogger.Log(LogLevel.All, $"Registering Action {action.ActionType} => {action.method.Name} (from: \"{action.method.DeclaringType}\")", "RegisterAction");
        if (action.ActionType is RoleActionType.FixedUpdate &&
            currentActions.Count > 0)
            throw new ConstraintException("RoleActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

        if (action.Attribute.Subclassing || action.method.DeclaringType == this.GetType())
            currentActions.Add(action);

        this.roleActions[action.ActionType] = currentActions;
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
            if (MyPlayer != null && !MyPlayer.IsAlive() && !action.WorksAfterDeath) return;
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

    protected void CreateInstanceBasedVariables()
    {


        this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(Cooldown) || (f.FieldType.IsGenericType && typeof(Optional<>).IsAssignableFrom(f.FieldType.GetGenericTypeDefinition())))
            .Do(f =>
            {
                if (f.FieldType == typeof(Cooldown)) CreateCooldown(f);
                else CreateOptional(f);
            });
    }

    private void CreateCooldown(FieldInfo fieldInfo)
    {
        Cooldown? value = (Cooldown)fieldInfo.GetValue(this);
        Cooldown setValue = value == null ? new Cooldown() : value.Clone();
        value?.TimeRemaining();
        fieldInfo.SetValue(this, setValue);
    }

    private void CreateOptional(FieldInfo fieldInfo)
    {
        ConstructorInfo GetConstructor(Type[] parameters) => AccessTools.Constructor(fieldInfo.FieldType, parameters);

        object? optional = fieldInfo.GetValue(this);
        ConstructorInfo constructor = GetOptionalConstructor(fieldInfo, optional == null);
        object? setValue = constructor.Invoke(optional == null ? Array.Empty<object>() : new[] { optional });
        fieldInfo.SetValue(this, setValue);
    }

    private ConstructorInfo GetOptionalConstructor(FieldInfo info, bool isNull)
    {
        if (isNull) return AccessTools.Constructor(info.FieldType, Array.Empty<Type>());
        return info.FieldType.GetConstructors().First(c =>
            c.GetParameters().SelectWhere(p => p.ParameterType, t => t!.IsGenericType).Any(tt =>
                tt!.GetGenericTypeDefinition().IsAssignableTo(typeof(Optional<>))));
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


    public GameOptionBuilder GetGameOptionBuilder() {
        GameOptionBuilder b = RoleOptionsBuilder.Color(RoleColor).Bind(val => this.Chance = (int)val)
            .SubOption(s => s.Name("Maximum")
                .AddIntRange(1, 15)
                .Bind(val => this.Count = (int)val)
                .ShowSubOptionPredicate(v => 1 < (int)v)
                .SubOption(subsequent => subsequent
                    .Name(Localizer.Get("Roles.Options.SubsequentChance"))
                    .AddIntRange(10, 100, 10, 0, "%")
                    .BindInt(v => AdditionalChance = v)
                    .Build())
                .Build());

        return RegisterOptions(b);
    }

    protected virtual GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    {
        optionStream.LocaleName($"Roles.{EnglishRoleName}.RoleName").Key(EnglishRoleName).Description(Localizer.Get($"Roles.{EnglishRoleName}.Blurb")).IsHeader(true);
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

        public RoleModifier OptionOverride(Override option, object? value, Func<bool>? condition = null)
        {
            myRole.roleSpecificGameOptionOverrides.Add(new GameOptionOverride(option, value, condition));
            return this;
        }

        public RoleModifier OptionOverride(Override option, Func<object> valueSupplier, Func<bool>? condition = null)
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

        public virtual GameOptionBuilder HookOptions(GameOptionBuilder optionStream) {
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
                .SelectMany(method => method.GetCustomAttributes<RoleActionAttribute>().Select(a => (a, method)))
                .Where(t => t.a.Subclassing || t.method.DeclaringType == this.GetType())
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