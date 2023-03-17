namespace TOHTOR.Managers.History.Events;

public interface IDeathEvent : IRecipientEvent
{
    /// <summary>
    /// Non-descriptive name of the event IE "Exiled", "Murdered", "Suicide", etc
    /// </summary>
    /// <returns>Simple name of the event</returns>
    public string SimpleName();
}