#nullable enable
using System;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class GameOptionOverride
{
    public readonly Override Option;
    private readonly object value;
    private readonly Func<object>? supplier;
    private readonly Func<bool>? condition;

    public GameOptionOverride(Override option, object value, Func<bool>? condition = null)
    {
        this.Option = option;
        this.value = value;
        this.condition = condition;
    }

    public GameOptionOverride(Override option, Func<object> valueSupplier, Func<bool>? condition = null)
    {
        this.Option = option;
        this.supplier = valueSupplier;
        this.condition = condition;
    }


    public void ApplyTo(IGameOptions options)
    {
        if (condition != null && !condition.Invoke()) return;
        NormalGameOptionsV07? normalOptions = options.AsNormalOptions();
        if (normalOptions == null) return;
        switch (Option)
        {
            case Override.DiscussionTime:
                normalOptions.DiscussionTime = (int)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.DiscussionTime);
                break;
            case Override.VotingTime:
                normalOptions.VotingTime = (int)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.VotingTime);
                break;
            case Override.PlayerSpeedMod:
                normalOptions.PlayerSpeedMod = (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.PlayerSpeedMod);
                break;
            case Override.CrewLightMod:
                normalOptions.CrewLightMod = (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.CrewLightMod);
                break;
            case Override.ImpostorLightMod:
                normalOptions.ImpostorLightMod = (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.CrewLightMod);
                break;
            case Override.KillCooldown:
                normalOptions.KillCooldown = (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.KillCooldown);
                break;
            case Override.ShapeshiftDuration:
                normalOptions.SetFloat(FloatOptionNames.ShapeshifterDuration, (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.GetFloat(FloatOptionNames.ShapeshifterDuration)));
                break;
            case Override.ShapeshiftCooldown:
                normalOptions.SetFloat(FloatOptionNames.ShapeshifterCooldown, (float)(GetValue() ?? DesyncOptions.OriginalHostOptions.AsNormalOptions()!.GetFloat(FloatOptionNames.ShapeshifterCooldown)));
                break;
            default:
                Logger.Warn($"Invalid Option Override: {this}", "ApplyOverride");
                break;
        }
    }

    private object? GetValue() => supplier == null ? value : supplier.Invoke();

    public override bool Equals(object? obj)
    {
        if (obj is not GameOptionOverride @override) return false;
        return @override.Option == this.Option;
    }

    public override int GetHashCode()
    {
        return this.Option.GetHashCode();
    }

    public override string ToString()
    {
        return $"GameOptionOverride(override={Option}, value={value})";
    }
}

public enum Override
{
    // Role overrides
    CanUseVent,


    // Game override
    DiscussionTime,
    VotingTime,
    PlayerSpeedMod,
    CrewLightMod,
    ImpostorLightMod,
    KillCooldown,

    // Role specific overrides
    ShapeshiftDuration,
    ShapeshiftCooldown
}
