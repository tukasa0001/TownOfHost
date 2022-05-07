using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TownOfHost
{
    public static class Sniper
    {
        static int Id = 1800;

        static CustomOption SniperBulletCount;
        static CustomOption SniperPrecisionShooting;

        static Dictionary<byte, Vector3> snipeBasePosition = new();
        static Dictionary<byte, int> bulletCount = new();
        static int maxBulletCount;
        static bool precisionshooting;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Sniper);
            SniperBulletCount = CustomOption.Create(Id + 10, Color.white, "SniperBulletCount", 5f, 1f, 3f, 1f, Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
            SniperPrecisionShooting = CustomOption.Create(Id + 11, Color.white, "SniperPrecisionShooting", false, Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
        }
        public static void Init()
        {
            snipeBasePosition = new();
            bulletCount = new();
            maxBulletCount = SniperBulletCount.GetInt();
            precisionshooting = SniperPrecisionShooting.GetBool();
        }
        public static void Add(byte playerId)
        {
            snipeBasePosition[playerId] = new();
            bulletCount[playerId] = maxBulletCount;
        }
        public static void SendRPC(byte playerId)
        {
            Logger.info($"Player{playerId}:SendRPC", "Sniper");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(bulletCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RecieveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            bulletCount[playerId] = msg.ReadInt32();
            Logger.info($"Player{playerId}:RecieveRPC", "Sniper");
        }
        public static bool CanUseKillButton(PlayerControl pc)
        {
            if (pc.Data.IsDead) return false;
            var canUse = false;
            if (bulletCount[pc.PlayerId] == 0)
            {
                canUse = true;
            }
            Logger.info($" CanUseKillButton:{canUse}", "Sniper");
            return canUse;
        }
        public static void ShapeShiftCheck(PlayerControl pc, bool shapeShifted)
        {
            //スナイパーで弾が残ってたら
            if (!shapeShifted)
            {
                //変身のタイミングでスナイプ地点の登録
                snipeBasePosition[pc.PlayerId] = pc.transform.position;
            }
            else
            {
                Dictionary<PlayerControl, float> dot_list = new();
                //一発消費して
                bulletCount[pc.PlayerId]--;
                //変身開始地点→解除地点のベクトル
                var snipeBasePos = snipeBasePosition[pc.PlayerId];
                var snipePos = pc.transform.position;
                var dir = (snipePos - snipeBasePos).normalized;

                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    if (target.Data.IsDead || target.PlayerId == pc.PlayerId) continue;
                    //死んでいない対象の方角ベクトル作成
                    var target_pos = target.transform.position - snipePos;
                    //正規化して
                    var target_dir = target_pos.normalized;
                    //内積を取る
                    var target_dot = Vector3.Dot(dir, target_dir);
                    Logger.info($"{target.name}:pos={target_pos} dir={target_dir}", "Sniper");
                    Logger.info($"  Dot={target_dot}", "Sniper");
                    if (precisionshooting)
                    {
                        if (target_dot < 0.99) continue;
                        //ある程度正確ならターゲットとの誤差確認
                        var snipe_point = dir * target_pos.magnitude;
                        var err = (snipe_point - target_pos).magnitude;
                        Logger.info($"  err={err}", "Sniper");
                        if (err < 1.0)
                        {
                            //ある程度正確なら登録
                            dot_list.Add(target, err);

                        }
                    }
                    else
                    {
                        if (target_dot < 0.98) continue;
                        //ある程度正確なら登録
                        var err = 1 - target_dot;
                        Logger.info($"  err={err}", "Sniper");
                        dot_list.Add(target, err);
                    }
                }
                if (dot_list.Count != 0)
                {
                    //一番正確な対象がターゲット
                    var snipedTarget = dot_list.OrderBy(c => c.Value).First().Key;
                    PlayerState.setDeathReason(snipedTarget.PlayerId, PlayerState.DeathReason.Sniped);
                    snipedTarget.RpcMurderPlayer(snipedTarget);
                    //キル出来た通知
                    pc.RpcGuardAndKill();

                    //スナイプが起きたことを聞こえそうな対象に通知したい
                    dot_list.Remove(snipedTarget);
                    foreach (var otherPc in dot_list.Keys)
                    {
                        //otherPc.RpcGuardAndKill();
                    }
                }
            }
            SendRPC(pc.PlayerId);
            Utils.NotifyRoles();
        }
        public static string GetBulletCount(PlayerControl pc)
        {
            return $"<color=#ffff00>({bulletCount[pc.PlayerId]})</color>";
        }
    }
}

