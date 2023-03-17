namespace TOHTOR.Roles.Events;

public class GenericAbilityEvent : AbilityEvent
{
    private string message;

    public GenericAbilityEvent(PlayerControl user, string message, bool completed = true) : base(user, completed)
    {
        this.message = message;
    }

    public override string Message() => message;
}