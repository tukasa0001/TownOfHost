namespace TownOfHost.Player;

public struct PlayerState
{
    public static PlayerState Alive = new() { state = InnerState.Alive, deathReason = null };
    public static PlayerState Dead = new() { state = InnerState.Dead, deathReason = null };

    private InnerState state = InnerState.Unknown;
    private DeathReason? deathReason = null;

    public PlayerState() { }

    public override bool Equals(object? obj)
    {
        if (obj is not PlayerState other) return false;
        return other.state == this.state;
    }

    public override int GetHashCode() => this.state.GetHashCode();

    public static bool operator ==(PlayerState c1, PlayerState c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(PlayerState c1, PlayerState c2)
    {
        return !c1.Equals(c2);
    }

    private enum InnerState
    {
        Unknown,
        Alive,
        Dead
    }


}