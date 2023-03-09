using System;

namespace TOHTOR.GUI;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class DynElement: Attribute
{
    public UI Component;


    public DynElement(UI component)
    {
        this.Component = component;
    }
}