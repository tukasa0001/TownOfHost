using System;
using TOHTOR.Factions;

namespace TOHTOR.Roles.Internals.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RoleInteraction: Attribute
{
    public Type RoleType { get; }

    public Faction RoleFaction { get;  }

    public RoleInteraction(Type role)
    {
        this.RoleType = role;
    }

    public RoleInteraction(Faction faction)
    {
        this.RoleFaction = faction;
    }
}

public enum InteractionResult
{
    Proceed,
    Halt
}