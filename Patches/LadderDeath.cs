using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    public class LadderDeath
    {
        public static void Reset()
        {
            TargetLadderDatas = new();
        }
        public static void FixedUpdate(PlayerControl player)
        {
            if (player.Data.Disconnected) return;
            if (TargetLadderDatas.ContainsKey(player.PlayerId))
            {
                if (Vector2.Distance(TargetLadderDatas[player.PlayerId], player.transform.position) < 0.5f)
                {
                    if (player.Data.IsDead) return;
                    player.Data.IsDead = true;
                    new LateTask(() => {
                        player.RpcMurderPlayerV2(player);
                        PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.LadderDeath);
                        RPC.SendDeathReason(player.PlayerId, PlayerState.DeathReason.LadderDeath);
                    }, 0.05f);
                }
            }
        }
        public static Dictionary<byte, Vector3> TargetLadderDatas;
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
        class ladder
        {
            static int Chance => Options.DeathChance.GetSelection() + 1;
            public static void Postfix(PlayerPhysics __instance, Ladder source, byte climbLadderSid)
            {
                if (!Options.LadderDeath.GetBool()) return;
                var sourcepos = source.transform.position;
                var targetpos = source.Destination.transform.position;
                //降りているのかを検知
                if (sourcepos.y > targetpos.y)
                {
                    int chance = UnityEngine.Random.Range(1, 10);
                    if (Chance <= chance)
                    {
                        TargetLadderDatas[__instance.myPlayer.PlayerId] = targetpos;
                    }
                }
            }
        }
    }
}
