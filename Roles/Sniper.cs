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
        static List<byte> playeridList = new();

        static CustomOption SniperBulletCount;
        static CustomOption SniperPrecisionShooting;

        static Dictionary<byte, Vector3> snipeBasePosition = new();
        static Dictionary<byte, int> bulletCount = new();
        static Dictionary<byte, List<byte>> shotNortify = new();
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
            playeridList = new();
            snipeBasePosition = new();
            bulletCount = new();
            shotNortify = new();
            maxBulletCount = SniperBulletCount.GetInt();
            precisionshooting = SniperPrecisionShooting.GetBool();
        }
        public static void Add(byte playerId)
        {
            playeridList.Add(playerId);
            snipeBasePosition[playerId] = new();
            bulletCount[playerId] = maxBulletCount;
            shotNortify[playerId] = new();
        }
        public static bool isEnable()
        {
            return playeridList.Count > 0;
        }
        public static void SendRPC(byte playerId, bool notify = false)
        {
            Logger.info($"Player{playerId}:SendRPC", "Sniper");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(notify);
            if (notify)
            {
                var snList = shotNortify[playerId];
                writer.Write(snList.Count());
                foreach (var sn in snList)
                {
                    writer.Write(sn);
                }
            }
            else
            {
                writer.Write(bulletCount[playerId]);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RecieveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            var notify = msg.ReadBoolean();
            if (notify)
            {
                shotNortify[playerId].Clear();
                var count = msg.ReadInt32();
                while (count > 0)
                {
                    shotNortify[playerId].Add(msg.ReadByte());
                }
            }
            else
            {
                bulletCount[playerId] = msg.ReadInt32();
            }
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
                SendRPC(pc.PlayerId);
            }
            else
            {
                Dictionary<PlayerControl, float> dot_list = new();
                //一発消費して
                bulletCount[pc.PlayerId]--;
                SendRPC(pc.PlayerId);
                Utils.NotifyRoles();

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
                    Logger.info($"{target.Data.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
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
                        var err = target_pos.magnitude;
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
                    if (snipedTarget.Is(CustomRoles.Trapper))
                    {
                        pc.TrapperKilled(snipedTarget);
                    }
                    if (snipedTarget.Is(CustomRoles.Bait))
                    {
                        Logger.info(snipedTarget.Data.PlayerName + "はBaitだった", "Sniper");
                        new LateTask(() => pc.CmdReportDeadBody(snipedTarget.Data), 0.15f, "Bait Self Report");
                    }
                    else
                    {
                        //スナイプが起きたことを聞こえそうな対象に通知したい
                        dot_list.Remove(snipedTarget);
                        var snList = shotNortify[pc.PlayerId];
                        snList.Clear();
                        foreach (var otherPc in dot_list.Keys)
                        {
                            snList.Add(otherPc.PlayerId);
                            //otherPc.RpcGuardAndKill();
                        }
                        SendRPC(pc.PlayerId, true);
                        new LateTask(
                            () =>
                            {
                                snList.Clear();
                                SendRPC(pc.PlayerId, true);
                                Utils.NotifyRoles();
                            },
                            0.5f, "Sniper shot Notify"
                            );
                    }
                }
            }
        }
        public static string GetBulletCount(PlayerControl pc)
        {
            return $"<color=#ffff00>({bulletCount[pc.PlayerId]})</color>";
        }
        public static string GetShotNotify(byte seer)
        {
            foreach (var sniper in playeridList)
            {
                var snList = shotNortify[sniper];
                if (snList.Count() > 0 && snList.Contains(seer))
                {
                    return $"<color=#ff0000><size=200%>!</size></color>";
                }
            }
            return "";
        }
    }
}

