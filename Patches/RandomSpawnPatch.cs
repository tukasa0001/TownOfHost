using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    public class AirshipRandomSpawnPatch
    {
        public static Dictionary<byte, int> NumOfTP = new();

        private static Vector2
        MeetingRoom = new(17.1f, 14.9f),
        GapRoom = new(12.1f, 8.7f),
        Brig = new(-0.7f, 8.5f),
        Vault = new(-8.9f, 12.2f),
        Engine = new(-0.7f, -1.0f),
        Communications = new(-13.3f, 1.3f),
        Cockpit = new(-23.5f, -1.6f),
        Armory = new(-10.3f, -5.9f),
        Kitchen = new(-7.0f, -11.5f),
        ViewingDeck = new(-13.7f, -12.6f),
        Security = new(5.8f, -10.8f),
        Electrical = new(16.3f, -8.8f),
        Medical = new(29.0f, -6.2f),
        CargoBay = new(33.5f, -1.5f),
        Lounge = new(28.9f, 5.1f),
        Records = new(20.0f, 10.5f),
        Showers = new(21.2f, -0.8f),
        MainHall = new(15.5f, 0.0f);

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        class CustomNetworkTransformPatch
        {
            public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if (!(Options.AirshipRandomSpawn.GetBool() && PlayerControl.GameOptions.MapId == 4)) return; //ランダムスポーンが無効か、マップがエアシップじゃなかったらreturn
                if (position == new Vector2(-25f, 40f)) return; //最初の湧き地点ならreturn

                if (GameStates.IsInTask)
                {
                    var player = PlayerControl.AllPlayerControls.ToArray().Where(p => p.NetTransform == __instance).FirstOrDefault();
                    if (player == null)
                    {
                        Logger.Warn("プレイヤーがnullだよぉ！", "AirshipRandomSpawn");
                        return;
                    }
                    if (player.Is(CustomRoles.GM)) return; //GMは対象外に

                    NumOfTP[player.PlayerId]++;

                    if (NumOfTP.TryGetValue(player.PlayerId, out var num) && num == 2)
                    {
                        NumOfTP[player.PlayerId] = 3;
                        var Location = SelectSpawnLocation();
                        TP(player.NetTransform, Location);
                        Logger.Info(player.Data.PlayerName + " : " + Location.ToString(), "AirshipRandomSpawn");
                    }
                }
            }
        }
        private static void TP(CustomNetworkTransform __instance, Vector2 Location)
        {
            if (AmongUsClient.Instance.AmHost)
                __instance.SnapTo(Location);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            __instance.WriteVector2(Location, writer);
            writer.Write(__instance.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        private static Vector2 SelectSpawnLocation()
        {
            var rand = new System.Random();
            var Locations = new List<Vector2>()
            {
                Brig,
                Engine,
                Kitchen,
                CargoBay,
                Records,
                MainHall
            };
            if (Options.AirshipAdditionalSpawn.GetBool()) //追加位置がオンなら
            {
                var AdditionalLocations = new Vector2[]
                {
                    MeetingRoom,
                    GapRoom,
                    Vault,
                    Communications,
                    Cockpit,
                    Armory,
                    ViewingDeck,
                    Security,
                    Electrical,
                    Medical,
                    Lounge,
                    Showers,
                };
                Locations.AddRange(AdditionalLocations); //湧き位置リストに追加位置を入れる
            }
            var SpawnLocation = Locations[rand.Next(0, Locations.Count)];
            return SpawnLocation;
        }
        [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Begin))]
        class SpawnInMinigamePatch
        {
            public static void Prefix()
            {
                PlayerControl.AllPlayerControls.ToArray().Do(pc => NumOfTP[pc.PlayerId] = 0);
            }
        }
    }
}