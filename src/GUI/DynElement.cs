using System;

namespace TownOfHost.GUI;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class DynElement: Attribute
{
    public UI Component;


    public DynElement(UI component)
    {
        this.Component = component;
    }
}