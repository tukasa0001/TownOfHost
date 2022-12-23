using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public static class Sniper
    {
        static readonly int Id = 1800;
        static List<byte> playerIdList = new();

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
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sniper);
            SniperBulletCount = IntegerOptionItem.Create(Id + 10, "SniperBulletCount", new(1, 5, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper])
                .SetValueFormat(OptionFormat.Pieces);
            SniperPrecisionShooting = BooleanOptionItem.Create(Id + 11, "SniperPrecisionShooting", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
            SniperAimAssist = BooleanOptionItem.Create(Id + 12, "SniperAimAssist", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sniper]);
            SniperAimAssistOnshot = BooleanOptionItem.Create(Id + 13, "SniperAimAssistOneshot", false, TabGroup.ImpostorRoles, false).SetParent(SniperAimAssist);
        }
        public static void Init()
        {
            Logger.Disable("Sniper");

            playerIdList = new();
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
            playerIdList.Add(playerId);
            snipeBasePosition[playerId] = new();
            LastPosition[playerId] = new();
            snipeTarget[playerId] = 0x7F;
            bulletCount[playerId] = maxBulletCount;
            shotNotify[playerId] = new();
            IsAim[playerId] = false;
            AimTime[playerId] = 0f;
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void SendRPC(byte playerId)
        {
            Logger.Info($"Player{playerId}:SendRPC", "Sniper");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SniperSync, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(snipeTarget[playerId]);
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
            snipeTarget[playerId] = msg.ReadByte();
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

            foreach (var target in PlayerControl.AllPlayerControls)
            {
                //死者や自分には当たらない
                if (!target.IsAlive() || target.PlayerId == sniper.PlayerId) continue;
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
        public static void Sniping(PlayerControl pc, bool shapeshifting)
        {
            if (!pc.Is(CustomRoles.Sniper) || !pc.IsAlive()) return;

            if (bulletCount[pc.PlayerId] <= 0) return;

            //スナイパーで弾が残ってたら
            if (shapeshifting)
            {
                //Aim開始
                meetingReset = false;

                //スナイプ地点の登録
                snipeBasePosition[pc.PlayerId] = pc.transform.position;

                LastPosition[pc.PlayerId] = pc.transform.position;
                IsAim[pc.PlayerId] = true;
                AimTime[pc.PlayerId] = 0f;

                return;
            }

            //エイム終了
            IsAim[pc.PlayerId] = false;
            AimTime[pc.PlayerId] = 0f;

            //ミーティングによる変身解除なら射撃しない
            if (meetingReset)
            {
                meetingReset = false;
                return;
            }

            //一発消費して
            bulletCount[pc.PlayerId]--;

            //命中判定はホストのみ行う
            if (!AmongUsClient.Instance.AmHost) return;

            var targets = GetSnipeTargets(pc);

            if (targets.Count != 0)
            {
                //一番正確な対象がターゲット
                var snipedTarget = targets.OrderBy(c => c.Value).First().Key;
                snipeTarget[pc.PlayerId] = snipedTarget.PlayerId;
                Main.PlayerStates[snipedTarget.PlayerId].deathReason = PlayerState.DeathReason.Sniped;
                snipedTarget.CheckMurder(snipedTarget);
                //キル出来た通知
                pc.RpcGuardAndKill();

                //スナイプが起きたことを聞こえそうな対象に通知したい
                targets.Remove(snipedTarget);
                var snList = shotNotify[pc.PlayerId];
                snList.Clear();
                foreach (var otherPc in targets.Keys)
                {
                    snList.Add(otherPc.PlayerId);
                    Utils.NotifyRoles(SpecifySeer: otherPc);
                }
                SendRPC(pc.PlayerId);
                new LateTask(
                    () =>
                    {
                        snList.Clear();
                        foreach (var otherPc in targets.Keys)
                        {
                            Utils.NotifyRoles(SpecifySeer: otherPc);
                        }
                        SendRPC(pc.PlayerId);
                    },
                    0.5f, "Sniper shot Notify"
                    );
            }
        }
        public static void Aiming(PlayerControl pc)
        {
            if (!GameStates.IsInTask) return;
            if (!playerIdList.Contains(pc.PlayerId) || !pc.IsAlive()) return;

            if (!AimAssist) return;

            var sniper = pc;
            var sniperId = sniper.PlayerId;
            if (!IsAim[sniperId]) return;

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
        public static void OnStartMeeting()
        {
            meetingReset = true;
        }
        public static string GetBulletCount(byte playerId)
        {
            return Utils.ColorString(Color.yellow, $"({bulletCount[playerId]})");
        }
        public static byte GetSniper(byte target)
        {
            return snipeTarget.Where(st => st.Value == target).First().Key;
        }
        public static string GetShotNotify(byte seer)
        {
            if (AimAssist && playerIdList.Contains(seer))
            {
                //エイムアシスト中のスナイパー
                if (0.5f < AimTime[seer] && (!AimAssistOneshot || AimTime[seer] < 1.0f))
                {
                    if (GetSnipeTargets(Utils.GetPlayerById(seer)).Count > 0)
                    {
                        return $"<size=200%>{Utils.ColorString(Color.red, "◎")}</size>";
                    }
                }
            }
            else
            {
                //射撃音が聞こえるプレイヤー
                foreach (var sniper in playerIdList)
                {
                    var snList = shotNotify[sniper];
                    if (snList.Count() > 0 && snList.Contains(seer))
                    {
                        return $"<size=200%>{Utils.ColorString(Color.red, "!")}</size>";
                    }
                }
            }
            return "";
        }
        public static string OverrideShapeText(byte id) => GetString(bulletCount[id] <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText");

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody)),HarmonyPostfix]
        public static void OnReportDeadBody(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            OnStartMeeting();
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift)),HarmonyPrefix]
        public static void OnShapeshift(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            var shapeshifting = __instance.PlayerId != target.PlayerId;
            Sniping(__instance, shapeshifting);
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate)),HarmonyPostfix]
        public static void OnFixedUpdate(PlayerControl __instance)
        {
            Aiming(__instance);
        }
    }
}
