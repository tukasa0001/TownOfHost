using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class Sniper
    {
        static readonly int Id = 1800;
        static List<byte> PlayerIdList = new();

        static OptionItem SniperBulletCount;
        static OptionItem SniperPrecisionShooting;
        static OptionItem SniperAimAssist;
        static OptionItem SniperAimAssistOnshot;

        static Dictionary<byte, byte> snipeTarget = new();
        static Dictionary<byte, Vector3> snipeBasePosition = new();
        static Dictionary<byte, Vector3> LastPosition = new();
        static Dictionary<byte, int> bulletCount = new();
        static Dictionary<byte, List<byte>> shotNotify = new();
        static Dictionary<byte, bool> IsAim = new();
        static Dictionary<byte, float> AimTime = new();

        static bool meetingReset;

        static int maxBulletCount;
        static bool precisionShooting;
        static bool AimAssist;
        static bool AimAssistOneshot;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sniper);
            SniperBulletCount = IntegerOptionItem.Create(Id + 10, "SniperBulletCount", new(1, 5, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sniper])
                .SetValueFormat(OptionFormat.Pieces);
            SniperPrecisionShooting = BooleanOptionItem.Create(Id + 11, "SniperPrecisionShooting", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sniper]);
            SniperAimAssist = BooleanOptionItem.Create(Id + 12, "SniperAimAssist", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sniper]);
            SniperAimAssistOnshot = BooleanOptionItem.Create(Id + 13, "SniperAimAssistOneshot", false, TabGroup.ImpostorRoles, false).SetParent(SniperAimAssist);
        }
        public static void Init()
        {
            Logger.Disable("Sniper");

            PlayerIdList = new();
            IsEnable = false;

            snipeBasePosition = new();
            LastPosition = new();
            snipeTarget = new();
            bulletCount = new();
            shotNotify = new();
            IsAim = new();
            AimTime = new();
            meetingReset = false;

            maxBulletCount = SniperBulletCount.GetInt();
            precisionShooting = SniperPrecisionShooting.GetBool();
            AimAssist = SniperAimAssist.GetBool();
            AimAssistOneshot = SniperAimAssistOnshot.GetBool();
        }
        public static void Add(byte playerId)
        {
            PlayerIdList.Add(playerId);
            IsEnable = true;

            snipeBasePosition[playerId] = new();
            LastPosition[playerId] = new();
            snipeTarget[playerId] = 0x7F;
            bulletCount[playerId] = maxBulletCount;
            shotNotify[playerId] = new();
            IsAim[playerId] = false;
            AimTime[playerId] = 0f;
        }
        public static bool IsEnable;
        public static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);
        public static void SendRPC(byte playerId)
        {
            Logger.Info($"Player{playerId}:SendRPC", "Sniper");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            var snList = shotNotify[playerId];
            writer.Write(snList.Count());
            foreach (var sn in snList)
            {
                writer.Write(sn);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            shotNotify[playerId].Clear();
            var count = msg.ReadInt32();
            while (count > 0)
            {
                shotNotify[playerId].Add(msg.ReadByte());
                count--;
            }
            Logger.Info($"Player{playerId}:ReceiveRPC", "Sniper");
        }
        public static bool CanUseKillButton(PlayerControl pc)
        {
            if (!pc.IsAlive()) return false;
            var canUse = false;
            if (!bulletCount.ContainsKey(pc.PlayerId))
            {
                Logger.Info($" Sniper not Init yet.", "Sniper");
                return false;
            }
            if (bulletCount[pc.PlayerId] <= 0)
            {
                canUse = true;
            }
            Logger.Info($" CanUseKillButton:{canUse}", "Sniper");
            return canUse;
        }
        public static Dictionary<PlayerControl, float> GetSnipeTargets(PlayerControl sniper)
        {
            var targets = new Dictionary<PlayerControl, float>();
            //変身開始地点→解除地点のベクトル
            var snipeBasePos = snipeBasePosition[sniper.PlayerId];
            var snipePos = sniper.transform.position;
            var dir = (snipePos - snipeBasePos).normalized;

            //至近距離で外す対策に一歩後ろから判定を開始する
            snipePos -= dir;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                //自分には当たらない
                if (target.PlayerId == sniper.PlayerId) continue;
                //死んでいない対象の方角ベクトル作成
                var target_pos = target.transform.position - snipePos;
                //自分より後ろの場合はあたらない
                if (target_pos.magnitude < 1) continue;
                //正規化して
                var target_dir = target_pos.normalized;
                //内積を取る
                var target_dot = Vector3.Dot(dir, target_dir);
                Logger.Info($"{target?.Data?.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
                Logger.Info($"  Dot={target_dot}", "Sniper");

                //ある程度正確なら登録
                if (target_dot < 0.995) continue;

                if (precisionShooting)
                {
                    //射線との誤差確認
                    //単位ベクトルとの外積をとれば大きさ=誤差になる。
                    var err = Vector3.Cross(dir, target_pos).magnitude;
                    Logger.Info($"  err={err}", "Sniper");
                    if (err < 0.5)
                    {
                        //ある程度正確なら登録
                        targets.Add(target, err);
                    }
                }
                else
                {
                    //近い順に判定する
                    var err = target_pos.magnitude;
                    Logger.Info($"  err={err}", "Sniper");
                    targets.Add(target, err);
                }
            }
            return targets;

        }
        public static void OnShapeshift(PlayerControl pc, bool shapeshifting)
        {
            if (!IsThisRole(pc.PlayerId) || !pc.IsAlive()) return;

            var sniper = pc;
            var sniperId = sniper.PlayerId;

            if (bulletCount[sniperId] <= 0) return;

            //スナイパーで弾が残ってたら
            if (shapeshifting)
            {
                //Aim開始
                meetingReset = false;

                //スナイプ地点の登録
                snipeBasePosition[sniperId] = sniper.transform.position;

                LastPosition[sniperId] = sniper.transform.position;
                IsAim[sniperId] = true;
                AimTime[sniperId] = 0f;

                return;
            }

            //エイム終了
            IsAim[sniperId] = false;
            AimTime[sniperId] = 0f;

            //ミーティングによる変身解除なら射撃しない
            if (meetingReset)
            {
                meetingReset = false;
                return;
            }

            //一発消費して
            bulletCount[sniperId]--;

            //命中判定はホストのみ行う
            if (!AmongUsClient.Instance.AmHost) return;

            var targets = GetSnipeTargets(sniper);

            if (targets.Count != 0)
            {
                //一番正確な対象がターゲット
                var snipedTarget = targets.OrderBy(c => c.Value).First().Key;
                snipeTarget[sniperId] = snipedTarget.PlayerId;
                snipedTarget.CheckMurder(snipedTarget);
                //あたった通知
                sniper.RpcGuardAndKill();
                snipeTarget[sniperId] = 0x7F;

                //スナイプが起きたことを聞こえそうな対象に通知したい
                targets.Remove(snipedTarget);
                var snList = shotNotify[sniperId];
                snList.Clear();
                foreach (var otherPc in targets.Keys)
                {
                    snList.Add(otherPc.PlayerId);
                    Utils.NotifyRoles(SpecifySeer: otherPc);
                }
                SendRPC(sniperId);
                new LateTask(
                    () =>
                    {
                        snList.Clear();
                        foreach (var otherPc in targets.Keys)
                        {
                            Utils.NotifyRoles(SpecifySeer: otherPc);
                        }
                        SendRPC(sniperId);
                    },
                    0.5f, "Sniper shot Notify"
                    );
            }
        }
        public static void OnFixedUpdate(PlayerControl pc)
        {
            if (!IsThisRole(pc.PlayerId) || !pc.IsAlive()) return;

            if (!AimAssist) return;

            var sniper = pc;
            var sniperId = sniper.PlayerId;
            if (!IsAim[sniperId]) return;

            if (!GameStates.IsInTask)
            {
                //エイム終了
                IsAim[sniperId] = false;
                AimTime[sniperId] = 0f;
                return;
            }

            var pos = sniper.transform.position;
            if (pos != LastPosition[sniperId])
            {
                AimTime[sniperId] = 0f;
                LastPosition[sniperId] = pos;
            }
            else
            {
                AimTime[sniperId] += Time.fixedDeltaTime;
                Utils.NotifyRoles(SpecifySeer: sniper);
            }
        }
        public static void OnReportDeadBody()
        {
            meetingReset = true;
        }
        public static string GetBulletCount(byte playerId)
        {
            return IsThisRole(playerId) ? Utils.ColorString(Color.yellow, $"({bulletCount[playerId]})") : "";
        }
        public static bool TryGetSniper(byte targetId, ref PlayerControl sniper)
        {
            foreach (var kvp in snipeTarget)
            {
                if (kvp.Value == targetId)
                {
                    sniper = Utils.GetPlayerById(kvp.Key);
                    return true;
                }
            }
            return false;
        }
        public static string GetShotNotify(byte seerId)
        {
            if (AimAssist && IsThisRole(seerId))
            {
                //エイムアシスト中のスナイパー
                if (0.5f < AimTime[seerId] && (!AimAssistOneshot || AimTime[seerId] < 1.0f))
                {
                    if (GetSnipeTargets(Utils.GetPlayerById(seerId)).Count > 0)
                    {
                        return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "◎")}</size>";
                    }
                }
            }
            else
            {
                //射撃音が聞こえるプレイヤー
                foreach (var sniperId in PlayerIdList)
                {
                    var snList = shotNotify[sniperId];
                    if (snList.Count() > 0 && snList.Contains(seerId))
                    {
                        return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "!")}</size>";
                    }
                }
            }
            return "";
        }
        public static void OverrideShapeText(byte id)
        {
            if (IsThisRole(id))
                HudManager.Instance.AbilityButton.OverrideText(GetString(bulletCount[id] <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText"));
        }
    }
}
