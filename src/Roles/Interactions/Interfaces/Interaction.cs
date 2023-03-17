namespace TOHTOR.Roles.Interactions.Interfaces;

// ReSharper disable once InconsistentNaming
public interface Interaction
{
    public CustomRole Emitter();

    public Intent Intent();
}