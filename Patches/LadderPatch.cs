using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    public class FallFromLadder
    {
        public static Dictionary<byte, Vector3> TargetLadderData;
        private static int Chance => Options.LadderDeathChance.GetSelection() + 1;
        public static void Reset()
        {
            TargetLadderData = new();
        }
        public static void OnClimbLadder(PlayerPhysics player, Ladder source)
        {
            if (!Options.LadderDeath.GetBool()) return;
            var sourcePos = source.transform.position;
            var targetPos = source.Destination.transform.position;
            //降りているのかを検知
            if (sourcePos.y > targetPos.y)
            {
                int chance = UnityEngine.Random.Range(1, 10);
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
                    new LateTask(() =>
                    {
                        Vector2 targetPos = (Vector2)TargetLadderData[player.PlayerId] + new Vector2(0.1f, 0f);
                        ushort num = (ushort)(player.NetTransform.XRange.ReverseLerp(targetPos.x) * 65535f);
                        ushort num2 = (ushort)(player.NetTransform.YRange.ReverseLerp(targetPos.y) * 65535f);
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
                        PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.Fall);
                        PlayerState.SetDead(player.PlayerId);
                    }, 0.05f, "LadderFallTask");
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
