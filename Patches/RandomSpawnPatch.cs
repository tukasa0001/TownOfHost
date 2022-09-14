using System;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    class RandomSpawn
    {
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        public class CustomNetworkTransformPatch
        {
            public static Dictionary<byte, int> NumOfTP = new();
            public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
            {
            }
        }
        public static void TP(CustomNetworkTransform nt, Vector2 location)
        {
            if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            nt.WriteVector2(location, writer);
            writer.Write(nt.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public abstract class SpawnMap
        {
            public virtual void RandomTeleport(PlayerControl player)
            {
                var location = GetLocation();
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawn");
                TP(player.NetTransform, location);
            }
            public abstract Vector2 GetLocation();
        }

        public class SkeldSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
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
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
        public class MiraHQSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
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
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
        public class PolusSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
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
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
    }
}