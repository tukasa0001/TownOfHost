using System;
// ReSharper disable InvalidXmlDocComment

namespace TOHTOR.Roles.Internals.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class RoleActionAttribute: Attribute
{
    public RoleActionType ActionType { get; }
    public bool WorksAfterDeath { get; }
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

    public RoleActionAttribute(RoleActionType actionType, bool worksAfterDeath = false, Priority priority = Priority.NoPriority)
    {
        this.ActionType = actionType;
        this.WorksAfterDeath = worksAfterDeath;
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
    /// <summary>
    /// Represents no action
    /// </summary>
    None,
    OnPet,
    /// <summary>
    /// Triggers whenever the player enters a vent (this INCLUDES vent activation)
    /// Parameters: (Vent vent)
    /// </summary>
    MyEnterVent,
    /// <summary>
    /// Triggered when a player ACTUALLY enters a vent (not just Vent activation)
    /// Parameters: (Vent vent, PlayerControl venter)
    /// </summary>
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
    Attack,
    /// <summary>
    /// Triggered when a killer attempts to kill another player. Cancelling causes the kill to not go through
    /// Parameters: (PlayerControl killer, PlayerControl target, Optional<IDeathEvent> deathEvent)
    /// </summary>
    PlayerAttacked,
    MyDeath,
    SelfExiled,
    OtherExiled,
    /// <summary>
    /// Triggers on Round Start (end of meetings, and start of game)
    /// Parameters: (bool isRoundOne)
    /// </summary>
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
    /// <summary>
    /// Triggers when my player votes for someone (or skips)
    /// </summary>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    MyVote,
    /// <summary>
    /// Triggers when any player votes for someone (or skips)
    /// </summary>
    /// <param name="voter"><see cref="PlayerControl"/> the player voting</param>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    AnyVote,
    /// <summary>
    /// Triggers whenever another player interacts with THIS role
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    Interaction,
    /// <summary>
    /// Triggers whenever another player interacts with any other player
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="target"><see cref="PlayerControl"/> the player being interacted with</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    AnyInteraction
}