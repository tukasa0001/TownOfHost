using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using TownOfHost;

namespace TownOfHost
{
    public abstract class OptionItem
    {
    }

    public enum TabGroup
    {
        MainSettings,
        ImpostorRoles,
        CrewmateRoles,
        NeutralRoles,
        Addons
    }
    public enum OptionFormat
    {
        None,
        Players,
        Seconds,
        Percent,
        Times,
        Multiplier,
        Votes,
        Pieces,
    }
}