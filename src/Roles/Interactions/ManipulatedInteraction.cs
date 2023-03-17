using TOHTOR.Roles.Interactions.Interfaces;

namespace TOHTOR.Roles.Interactions;

public class ManipulatedInteraction : SimpleInteraction, IManipulatedInteraction
{
    private PlayerControl manipulator;

    public ManipulatedInteraction(Intent intent, CustomRole victim, PlayerControl manipulator) : base(intent, victim)
    {
        this.manipulator = manipulator;
    }

    public PlayerControl Manipulator() => manipulator;
}