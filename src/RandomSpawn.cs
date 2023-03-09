using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace TOHTOR;

public class RandomSpawn
{
    public Dictionary<string, Vector2> SkeldLocations = new()
    {
        ["Cafeteria"] = new(-1.0f, 3.0f),
        ["Weapons"] = new(9.3f, 1.0f),
        ["O2"] = new(6.5f, -3.8f),
        ["Navigation"] = new(16.5f, -4.8f),
        ["Shields"] = new(9.3f, -12.3f),
        ["Communications"] = new(4.0f, -15.5f),
        ["Storage"] = new(-1.5f, -15.5f),
        ["Admin"] = new(4.5f, -7.9f),
        ["Electrical"] = new(-7.5f, -8.8f),
        ["LowerEngine"] = new(-17.0f, -13.5f),
        ["UpperEngine"] = new(-17.0f, -1.3f),
        ["Security"] = new(-13.5f, -5.5f),
        ["Reactor"] = new(-20.5f, -5.5f),
        ["MedBay"] = new(-9.0f, -4.0f)
    };

    public Dictionary<string, Vector2> MiraLocations = new()
    {
        ["Cafeteria"] = new(25.5f, 2.0f),
        ["Balcony"] = new(24.0f, -2.0f),
        ["Storage"] = new(19.5f, 4.0f),
        ["ThreeWay"] = new(17.8f, 11.5f),
        ["Communications"] = new(15.3f, 3.8f),
        ["MedBay"] = new(15.5f, -0.5f),
        ["LockerRoom"] = new(9.0f, 1.0f),
        ["Decontamination"] = new(6.1f, 6.0f),
        ["Laboratory"] = new(9.5f, 12.0f),
        ["Reactor"] = new(2.5f, 10.5f),
        ["Launchpad"] = new(-4.5f, 2.0f),
        ["Admin"] = new(21.0f, 17.5f),
        ["Office"] = new(15.0f, 19.0f),
        ["Greenhouse"] = new(17.8f, 23.0f)
    };

    public Dictionary<string, Vector2> PolusLocations = new()
    {
        ["Office1"] = new(19.5f, -18.0f),
        ["Office2"] = new(26.0f, -17.0f),
        ["Admin"] = new(24.0f, -22.5f),
        ["Communications"] = new(12.5f, -16.0f),
        ["Weapons"] = new(12.0f, -23.5f),
        ["BoilerRoom"] = new(2.3f, -24.0f),
        ["O2"] = new(2.0f, -17.5f),
        ["Electrical"] = new(9.5f, -12.5f),
        ["Security"] = new(3.0f, -12.0f),
        ["Dropship"] = new(16.7f, -3.0f),
        ["Storage"] = new(20.5f, -12.0f),
        ["Rocket"] = new(26.7f, -8.5f),
        ["Laboratory"] = new(36.5f, -7.5f),
        ["Toilet"] = new(34.0f, -10.0f),
        ["SpecimenRoom"] = new(36.5f, -22.0f)
    };

    public Dictionary<string, Vector2> AirshipLocations = new()
    {
        ["Brig"] = new(-0.7f, 8.5f),
        ["Engine"] = new(-0.7f, -1.0f),
        ["Kitchen"] = new(-7.0f, -11.5f),
        ["CargoBay"] = new(33.5f, -1.5f),
        ["Records"] = new(20.0f, 10.5f),
        ["MainHall"] = new(15.5f, 0.0f),
        ["NapRoom"] = new(6.3f, 2.5f),
        ["MeetingRoom"] = new(17.1f, 14.9f),
        ["GapRoom"] = new(12.0f, 8.5f),
        ["Vault"] = new(-8.9f, 12.2f),
        ["Communications"] = new(-13.3f, 1.3f),
        ["Cockpit"] = new(-23.5f, -1.6f),
        ["Armory"] = new(-10.3f, -5.9f),
        ["ViewingDeck"] = new(-13.7f, -12.6f),
        ["Security"] = new(5.8f, -10.8f),
        ["Electrical"] = new(16.3f, -8.8f),
        ["Medical"] = new(29.0f, -6.2f),
        ["Toilet"] = new(30.9f, 6.8f),
        ["Showers"] = new(21.2f, -0.8f)
    };

    private Dictionary<string, Vector2> usedLocations = null!;
    private List<string>? availableLocations = null!;

    private void ResetLocations()
    {
        if (ShipStatus.Instance is AirshipStatus) usedLocations = AirshipLocations;
        else
            usedLocations = (ShipStatus.Instance.Type) switch
            {
                ShipStatus.MapType.Ship => SkeldLocations,
                ShipStatus.MapType.Hq => MiraLocations,
                ShipStatus.MapType.Pb => PolusLocations,
                _ => throw new ArgumentOutOfRangeException()
            };
        availableLocations = usedLocations.Keys.ToList();
    }

    public void Spawn(PlayerControl player)
    {
        // ReSharper disable once MergeIntoNegatedPattern
        if (availableLocations == null || availableLocations.Count <= 0) ResetLocations();
        Utils.Teleport(player.NetTransform, usedLocations[availableLocations!.PopRandom()]);
    }

        /*return StaticOptions.AirshipAdditionalSpawn
        ? positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value
    : positions.ToArray()[0..6].OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;*/

}