using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Sniper
    {
        static readonly int Id = 1800;
        static List<byte> playerIdList = new();

        static CustomOption SniperBulletCount;
        static CustomOption SniperPrecisionShooting;

        static Dictionary<byte, byte> snipeTarget = new();
        static Dictionary<byte, Vector3> snipeBasePosition = new();
        static Dictionary<byte, int> bulletCount = new();
        static Dictionary<byte, List<byte>> shotNotify = new();
        static Dictionary<byte, bool> meetingReset = new();

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
            playerIdList = new();
            snipeBasePosition = new();
            snipeTarget = new();
            bulletCount = new();
            shotNotify = new();
            meetingReset = new();

            maxBulletCount = SniperBulletCount.GetInt();
            precisionshooting = SniperPrecisionShooting.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            snipeBasePosition[playerId] = new();
            snipeTarget[playerId] = 0x7F;
            bulletCount[playerId] = maxBulletCount;
            shotNotify[playerId] = new();
            meetingReset[playerId] = false;
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void SendRPC(byte playerId, bool notify = false)
        {
            Logger.Info($"Player{playerId}:SendRPC", "Sniper");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(snipeTarget[playerId]);
            writer.Write(notify);
            if (notify)
            {
                var snList = shotNotify[playerId];
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

        public static void ReceiveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            snipeTarget[playerId] = msg.ReadByte();
            var notify = msg.ReadBoolean();
            if (notify)
            {
                shotNotify[playerId].Clear();
                var count = msg.ReadInt32();
                while (count > 0)
                {
                    shotNotify[playerId].Add(msg.ReadByte());
                }
            }
            else
            {
                bulletCount[playerId] = msg.ReadInt32();
            }
            Logger.Info($"Player{playerId}:ReceiveRPC", "Sniper");
        }
        public static bool CanUseKillButton(PlayerControl pc)
        {
            if (pc.Data.IsDead) return false;
            var canUse = false;
            if (bulletCount[pc.PlayerId] <= 0)
            {
                canUse = true;
            }
//            Logger.Info($" CanUseKillButton:{canUse}", "Sniper");
            return canUse;
        }
        public static void ShapeShiftCheck(PlayerControl pc, bool shapeshifting)
        {
            if (bulletCount[pc.PlayerId] <= 0) return;
            if (PlayerState.isDead[pc.PlayerId]) return;
            //スナイパーで弾が残ってたら
            if (shapeshifting)
            {
                meetingReset[pc.PlayerId] = false;

                //変身のタイミングでスナイプ地点の登録
                snipeBasePosition[pc.PlayerId] = pc.transform.position;
                snipeTarget[pc.PlayerId] = 0x7F;
                SendRPC(pc.PlayerId);
            }
            else
            {
                if (meetingReset[pc.PlayerId])
                {
                    meetingReset[pc.PlayerId] = false;
                    return;
                }
                Dictionary<PlayerControl, float> dot_list = new();
                //一発消費して
                bulletCount[pc.PlayerId]--;
                SendRPC(pc.PlayerId);
                Utils.NotifyRoles(SpecifySeer:pc);

                //変身開始地点→解除地点のベクトル
                var snipeBasePos = snipeBasePosition[pc.PlayerId];
                var snipePos = pc.transform.position;
                var dir = (snipePos - snipeBasePos).normalized;

                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    //死者や自分には当たらない
                    if (PlayerState.isDead[target.PlayerId] || target.PlayerId == pc.PlayerId) continue;
                    //死んでいない対象の方角ベクトル作成
                    var target_pos = target.transform.position - snipePos;
                    //正規化して
                    var target_dir = target_pos.normalized;
                    //内積を取る
                    var target_dot = Vector3.Dot(dir, target_dir);
                    Logger.Info($"{target?.Data?.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
                    Logger.Info($"  Dot={target_dot}", "Sniper");
                    if (precisionshooting)
                    {
                        if (target_dot < 0.99) continue;
                        //ある程度正確ならターゲットとの誤差確認
                        //単位ベクトルとの外積をとれば大きさ=誤差になる。
                        var err = Vector3.Cross(dir, target_pos).magnitude;
                        Logger.Info($"  err={err}", "Sniper");
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
                        Logger.Info($"  err={err}", "Sniper");
                        dot_list.Add(target, err);
                    }
                }
                if (dot_list.Count != 0)
                {
                    //一番正確な対象がターゲット
                    var snipedTarget = dot_list.OrderBy(c => c.Value).First().Key;
                    snipeTarget[pc.PlayerId] = snipedTarget.PlayerId;
                    PlayerState.SetDeathReason(snipedTarget.PlayerId, PlayerState.DeathReason.Sniped);
                    snipedTarget.CheckMurder(snipedTarget);
                    //キル出来た通知
                    pc.RpcGuardAndKill();

                    //スナイプが起きたことを聞こえそうな対象に通知したい
                    dot_list.Remove(snipedTarget);
                    var snList = shotNotify[pc.PlayerId];
                    snList.Clear();
                    foreach (var otherPc in dot_list.Keys)
                    {
                        snList.Add(otherPc.PlayerId);
                    }
                    SendRPC(pc.PlayerId, true);
                    Utils.NotifyRoles();
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
        public static void OnStartMeeting()
        {
            foreach (var sniper in playerIdList)
                meetingReset[sniper] = true;
        }
        public static string GetBulletCount(byte playerId)
        {
            return $"<color=#ffff00>({bulletCount[playerId]})</color>";
        }
        public static byte GetSniper(byte target)
        {
            return snipeTarget.Where(st => st.Value == target).First().Key;
        }
        public static string GetShotNotify(byte seer)
        {
            foreach (var sniper in playerIdList)
            {
                var snList = shotNotify[sniper];
                if (snList.Count() > 0 && snList.Contains(seer))
                {
                    return $"<color=#ff0000><size=200%>!</size></color>";
                }
            }
            return "";
        }
    }
}