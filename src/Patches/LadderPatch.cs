using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Options;
using TownOfHost.RPC;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Patches
{
    public class FallFromLadder
    {
        public static Dictionary<byte, Vector3> TargetLadderData;
        // TODO: FIX THIS LOL
        private static int Chance => StaticOptions.LadderDeathChance;
        public static void Reset()
        {
            TargetLadderData = new();
        }
        public static void OnClimbLadder(PlayerPhysics player, Ladder source)
        {
            if (!StaticOptions.LadderDeath) return;
            var sourcePos = source.transform.position;
            var targetPos = source.Destination.transform.position;
            //降りているのかを検知
            if (sourcePos.y > targetPos.y)
            {
                int chance = UnityEngine.Random.RandomRangeInt(1, 101);
                if (chance <= Chance)
                {
                    TargetLadderData[player.myPlayer.PlayerId] = targetPos;
                }
            }
        }
        public static void FixedUpdate(PlayerControl player)
        {
            if (player.Data.Disconnected) return;
            if (TargetLadderData.ContainsKey(player.PlayerId))
            {
                if (Vector2.Distance(TargetLadderData[player.PlayerId], player.transform.position) < 0.5f)
                {
                    if (player.Data.IsDead) return;
                    //LateTaskを入れるため、先に死亡判定を入れておく
                    player.Data.IsDead = true;
                    Async.ScheduleInStep(() =>
                    {
                        Vector2 targetPos = (Vector2)TargetLadderData[player.PlayerId] + new Vector2(0.1f, 0f);
                        ushort num = (ushort)(NetHelpers.XRange.ReverseLerp(targetPos.x) * 65535f);
                        ushort num2 = (ushort)(NetHelpers.YRange.ReverseLerp(targetPos.y) * 65535f);
                        CustomRpcSender sender = CustomRpcSender.Create("LadderFallRpc", sendOption: Hazel.SendOption.None);
                        sender.AutoStartRpc(player.NetTransform.NetId, (byte)RpcCalls.SnapTo)
                                .Write(num)
                                .Write(num2)
                        .EndRpc();
                        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.MurderPlayer)
                                .WriteNetObject(player)
                        .EndRpc();
                        sender.SendMessage();
                        player.NetTransform.SnapTo(targetPos);
                        player.MurderPlayer(player);
                    }, 0.05f);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
    class LadderPatch
    {
        public static void Postfix(PlayerPhysics __instance, Ladder source, byte climbLadderSid)
        {
            FallFromLadder.OnClimbLadder(__instance, source);
        }
    }
}