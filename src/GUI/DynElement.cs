using System;
using TownOfHost.Interface.Menus.CustomNameMenu;
namespace TownOfHost.Interface;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class DynElement: Attribute
{
    public UI Component;


    public DynElement(UI component)
    {
        this.Component = component;
    }
}