using System;

namespace VentLib.Localization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SubgroupProvider: Attribute
{
    public string? ParentGroup;

    public SubgroupProvider() { }

    public SubgroupProvider(string? parentGroup)
    {
        ParentGroup = parentGroup;
    }
}