using System;

namespace TownOfHost.Roles;

[AttributeUsage(AttributeTargets.Method)]
public class RoleAction: Attribute
{
    public RoleActionType ActionType { get; }
    public Priority Priority { get; }
    public bool Blockable { set; get; }
    /// <summary>
    /// If provided, overrides any methods of the same action with the same name from any parent classes
    /// </summary>
    public String? Override;
    /// <summary>
    /// Dictates whether this action should be utilized in subclasses of the class declaring this method <b>Default: True</b>
    /// </summary>
    public bool Subclassing = true;

    public RoleAction(RoleActionType actionType, Priority priority = Priority.NoPriority)
    {
        this.ActionType = actionType;
        this.Priority = priority;
        this.Blockable = actionType is not RoleActionType.AnyDeath or RoleActionType.FixedUpdate or RoleActionType.Unshapeshift or RoleActionType.RoundStart or RoleActionType.RoundEnd;
    }

    public override string ToString() => $"RoleAction(type={ActionType}, Priority={Priority}, Blockable={Blockable}, Subclassing={Subclassing}, Override={Override})";
}

public enum Priority
{
    First,
    NoPriority,
    Last
}

public enum RoleActionType
{
    OnPet,
    AnyEnterVent,
    VentExit,
    SuccessfulAngelProtect,
    SabotageStarted,
    /// <summary>
    /// Triggered when any one player fixes any part of a sabotage (I.E MiraHQ Comms) <br></br>
    /// Parameters: (SabotageType type, PlayerControl fixer, byte fixBit)
    /// </summary>
    SabotagePartialFix,
    SabotageFixed,
    Shapeshift,
    Unshapeshift,
    AttemptKill,
    MyDeath,
    SelfExiled,
    OtherExiled,
    RoundStart,
    RoundEnd,
    SelfReportBody,
    /// <summary>
    /// Triggers when any player reports a body. <br></br>Parameters: (PlayerControl reporter, PlayerInfo reported)
    /// </summary>
    AnyReportedBody,
    TaskComplete,
    FixedUpdate,
    AnyDeath,

}