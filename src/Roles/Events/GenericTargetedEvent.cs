namespace TOHTOR.Roles.Events;

public class GenericTargetedEvent : TargetedAbilityEvent
{
    private readonly string message;

    public GenericTargetedEvent(PlayerControl source, PlayerControl target, string message, bool successful = true) : base(source, target, successful)
    {
        this.message = message;
    }

    public override string Message() => message;
}