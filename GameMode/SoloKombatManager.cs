using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.RandomSpawn;

namespace TOHE;

internal static class SoloKombatManager
{
    private static Dictionary<byte, float> PlayerHPMax = new();
    private static Dictionary<byte, float> PlayerHP = new();
    private static Dictionary<byte, float> PlayerHPReco = new();
    private static Dictionary<byte, float> PlayerATK = new();
    private static Dictionary<byte, float> PlayerDF = new();

    public static bool SoloAlive(this PlayerControl pc) => pc.HP() > 0f;

    public static float HPMAX(this PlayerControl pc) => PlayerHPMax[pc.PlayerId];
    public static float HP(this PlayerControl pc) => PlayerHP[pc.PlayerId];
    public static float HPRECO(this PlayerControl pc) => PlayerHPReco[pc.PlayerId];
    public static float ATK(this PlayerControl pc) => PlayerATK[pc.PlayerId];
    public static float DF(this PlayerControl pc) => PlayerDF[pc.PlayerId];

    private static Dictionary<byte, float> originalSpeed = new();
    public static Dictionary<byte, int> KBScore = new();
    public static int RoundTime = new();

    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.SoloKombat) return;

        PlayerHPMax = new();
        PlayerHP = new();
        PlayerHPReco = new();
        PlayerATK = new();
        PlayerDF = new();

        LastHurt = new();
        originalSpeed = new();
        BackCountdown = new();
        KBScore = new();
        RoundTime = Options.KB_GameTime.GetInt() + 8;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            PlayerHPMax.TryAdd(pc.PlayerId, Options.KB_HPMax.GetFloat());
            PlayerHP.TryAdd(pc.PlayerId, Options.KB_HPMax.GetFloat());
            PlayerHPReco.TryAdd(pc.PlayerId, Options.KB_RecoverPerSecond.GetFloat());
            PlayerATK.TryAdd(pc.PlayerId, Options.KB_ATK.GetFloat());
            PlayerDF.TryAdd(pc.PlayerId, 0f);

            KBScore.TryAdd(pc.PlayerId, 0);

            LastHurt.TryAdd(pc.PlayerId, Utils.GetTimeStamp(DateTime.Now));
        }
    }
    private static void SendRPCSyncKBBackCountdown(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKBBackCountdown, SendOption.Reliable, player.GetClientId());
        int x = BackCountdown.TryGetValue(player.PlayerId, out var value) ? value : -1;
        writer.Write(x);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCSyncBackCountdown(MessageReader reader)
    {
        int num = reader.ReadInt32();
        if (num == -1)
            BackCountdown.Remove(PlayerControl.LocalPlayer.PlayerId);
        else
        {
            BackCountdown.TryAdd(PlayerControl.LocalPlayer.PlayerId, num);
            BackCountdown[PlayerControl.LocalPlayer.PlayerId] = num;
        }
    }
    private static void SendRPCSyncKBPlayer(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKBPlayer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(PlayerHPMax[playerId]);
        writer.Write(PlayerHP[playerId]);
        writer.Write(PlayerHPReco[playerId]);
        writer.Write(PlayerATK[playerId]);
        writer.Write(PlayerDF[playerId]);
        writer.Write(KBScore[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCSyncKBPlayer(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        PlayerHPMax[PlayerId] = reader.ReadSingle();
        PlayerHP[PlayerId] = reader.ReadSingle();
        PlayerHPReco[PlayerId] = reader.ReadSingle();
        PlayerATK[PlayerId] = reader.ReadSingle();
        PlayerDF[PlayerId] = reader.ReadSingle();
        KBScore[PlayerId] = reader.ReadInt32();
    }
    public static void SendRPCSyncNameNotify(PlayerControl pc)
    {
        if (pc.AmOwner || !pc.IsModClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKBNameNotify, SendOption.Reliable, pc.GetClientId());
        if (NameNotify.ContainsKey(pc.PlayerId))
            writer.Write(NameNotify[pc.PlayerId].Item1);
        else writer.Write("");
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCSyncNameNotify(MessageReader reader)
    {
        var name = reader.ReadString();
        NameNotify.Remove(PlayerControl.LocalPlayer.PlayerId);
        if (name != null && name != "")
            NameNotify.Add(PlayerControl.LocalPlayer.PlayerId, (name, 0));
    }
    public static string GetDisplayHealth(PlayerControl pc)
        => pc.SoloAlive() ? Utils.ColorString(GetHealthColor(pc), $"{(int)pc.HP()}/{(int)pc.HPMAX()}") : "";
    private static Color32 GetHealthColor(PlayerControl pc)
    {
        var x = (int)(pc.HP() / pc.HPMAX() * 10 * 50);
        int R = 255; int G = 255; int B = 0;
        if (x > 255) R -= (x - 255); else G = x;
        return new Color32((byte)R, (byte)G, (byte)B, byte.MaxValue);
    }
    public static Dictionary<byte, (string, long)> NameNotify = new();
    public static void GetNameNotify(PlayerControl player, ref string name)
    {
        if (Options.CurrentGameMode != CustomGameMode.SoloKombat || player == null) return;
        if (BackCountdown.ContainsKey(player.PlayerId))
        {
            name = string.Format(Translator.GetString("KBBackCountDown"), BackCountdown[player.PlayerId]);
            NameNotify.Remove(player.PlayerId);
            return;
        }
        if (NameNotify.ContainsKey(player.PlayerId))
        {
            name = NameNotify[player.PlayerId].Item1;
            return;
        }
    }
    public static string GetDisplayScore(byte playerId)
    {
        int rank = GetRankOfScore(playerId);
        string score = KBScore.TryGetValue(playerId, out var s) ? $"{s}" : "Invalid";
        string text = string.Format(Translator.GetString("KBDisplayScore"), rank.ToString(), score);
        Color color = Utils.GetRoleColor(CustomRoles.KB_Normal);
        return Utils.ColorString(color, text);
    }
    public static int GetRankOfScore(byte playerId)
    {
        try
        {
            int ms = KBScore[playerId];
            int rank = 1 + KBScore.Values.Where(x => x > ms).Count();
            rank += KBScore.Where(x => x.Value == ms).ToList().IndexOf(new(playerId, ms));
            return rank;
        }
        catch
        {
            return Main.AllPlayerControls.Count();
        }
    }
    public static string GetHudText()
    {
        return string.Format(Translator.GetString("KBTimeRemain"), RoundTime.ToString());
    }
    public static void OnPlayerAttack(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || Options.CurrentGameMode != CustomGameMode.SoloKombat) return;
        if (!killer.SoloAlive() || !target.SoloAlive() || target.inVent) return;

        var dmg = killer.ATK() - target.DF();
        PlayerHP[target.PlayerId] = Math.Max(0f, target.HP() - dmg);

        if (!target.SoloAlive())
        {
            OnPlyaerDead(target);
            OnPlayerKill(killer);
        }

        LastHurt[target.PlayerId] = Utils.GetTimeStamp(DateTime.Now);

        killer.SetKillCooldownV2(1f, target);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        if (!target.IsModClient() && !target.AmOwner)
            target.RpcGuardAndKill(colorId: 5);

        SendRPCSyncKBPlayer(target.PlayerId);
        Utils.NotifyRoles(killer);
        Utils.NotifyRoles(target);
    }
    public static void OnPlayerBack(PlayerControl pc)
    {
        BackCountdown.Remove(pc.PlayerId);
        PlayerHP[pc.PlayerId] = pc.HPMAX();
        SendRPCSyncKBPlayer(pc.PlayerId);

        LastHurt[pc.PlayerId] = Utils.GetTimeStamp(DateTime.Now);
        Main.AllPlayerSpeed[pc.PlayerId] = Main.AllPlayerSpeed[pc.PlayerId] - 0.3f + originalSpeed[pc.PlayerId];
        pc.MarkDirtySettings();

        RPC.PlaySoundRPC(pc.PlayerId, Sounds.TaskComplete);
        pc.RpcGuardAndKill(colorId: 1);

        PlayerRandomSpwan(pc);
    }
    public static void PlayerRandomSpwan(PlayerControl pc)
    {
        SpawnMap map;
        switch (Main.NormalOptions.MapId)
        {
            case 0:
                map = new SkeldSpawnMap();
                map.RandomTeleport(pc);
                break;
            case 1:
                map = new MiraHQSpawnMap();
                map.RandomTeleport(pc);
                break;
            case 2:
                map = new PolusSpawnMap();
                map.RandomTeleport(pc);
                break;
        }
    }
    public static void OnPlyaerDead(PlayerControl target)
    {
        originalSpeed.Remove(target.PlayerId);
        originalSpeed.Add(target.PlayerId, Main.AllPlayerSpeed[target.PlayerId]);

        Utils.TP(target.NetTransform, Pelican.GetBlackRoomPS());
        Main.AllPlayerSpeed[target.PlayerId] = 0.3f;
        target.MarkDirtySettings();

        BackCountdown.TryAdd(target.PlayerId, Options.KB_ResurrectionWaitingTime.GetInt());
        SendRPCSyncKBBackCountdown(target);
    }
    public static void OnPlayerKill(PlayerControl killer)
    {
        killer.KillFlash();
        if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            PlayerControl.LocalPlayer.KillFlash();

        KBScore[killer.PlayerId]++;

        float addRate = IRandom.Instance.Next(3, 5 + GetRankOfScore(killer.PlayerId)) / 100f;
        addRate *= Options.KB_KillBonusMultiplier.GetFloat();
        float addin;
        switch (IRandom.Instance.Next(0, 3))
        {
            case 0:
                addin = killer.HPMAX() * addRate;
                PlayerHPMax[killer.PlayerId] += addin;
                AddNameNotify(killer, string.Format(Translator.GetString("KB_Buff_HPMax"), addin.ToString("0.0#####")));
                break;
            case 1:
                addin = killer.HPRECO() * addRate * 2;
                PlayerHPReco[killer.PlayerId] += addin;
                AddNameNotify(killer, string.Format(Translator.GetString("KB_Buff_HPReco"), addin.ToString("0.0#####")));
                break;
            case 2:
                addin = killer.ATK() * addRate;
                PlayerATK[killer.PlayerId] += addin;
                AddNameNotify(killer, string.Format(Translator.GetString("KB_Buff_ATK"), addin.ToString("0.0#####")));
                break;
        }
    }
    public static void AddNameNotify(PlayerControl pc, string text, int time = 5)
    {
        NameNotify.Remove(pc.PlayerId);
        NameNotify.Add(pc.PlayerId, (text, Utils.GetTimeStamp(DateTime.Now) + time));
        SendRPCSyncNameNotify(pc);
        SendRPCSyncKBPlayer(pc.PlayerId);
        Utils.NotifyRoles(pc);
    }

    private static Dictionary<byte, int> BackCountdown = new();
    private static Dictionary<byte, long> LastHurt = new();

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static long LastFixedUpdate = new();
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.SoloKombat) return;

            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    // 锁定死亡玩家在小黑屋
                    if (!pc.SoloAlive())
                    {
                        if (!pc.inVent)
                        {
                            var pos = Pelican.GetBlackRoomPS();
                            var dis = Vector2.Distance(pos, pc.GetTruePosition());
                            if (dis > 1f) Utils.TP(pc.NetTransform, pos);
                        }
                        else
                        {
                            pc.MyPhysics.RpcBootFromVent(Main.LastEnteredVent[pc.PlayerId].Id);
                        }
                    }
                }

                if (LastFixedUpdate == Utils.GetTimeStamp(DateTime.Now)) return;
                LastFixedUpdate = Utils.GetTimeStamp(DateTime.Now);

                // 减少全局倒计时
                RoundTime--;

                if (!AmongUsClient.Instance.AmHost) return;

                foreach (var pc in Main.AllPlayerControls)
                {
                    bool notifyRoles = false;
                    // 每秒回复血量
                    if (LastHurt[pc.PlayerId] + Options.KB_RecoverAfterSecond.GetInt() < Utils.GetTimeStamp(DateTime.Now) && pc.HP() < pc.HPMAX() && pc.SoloAlive() && !pc.inVent)
                    {
                        PlayerHP[pc.PlayerId] += pc.HPRECO();
                        PlayerHP[pc.PlayerId] = Math.Min(pc.HPMAX(), pc.HP());
                        SendRPCSyncKBPlayer(pc.PlayerId);
                        notifyRoles = true;
                    }
                    // 复活玩家随机复活（二次确认）
                    if (pc.SoloAlive() && !pc.inVent)
                    {
                        var pos = Pelican.GetBlackRoomPS();
                        var dis = Vector2.Distance(pos, pc.GetTruePosition());
                        if (dis < 1.1f) PlayerRandomSpwan(pc);
                    }
                    // 复活倒计时
                    if (BackCountdown.ContainsKey(pc.PlayerId))
                    {
                        BackCountdown[pc.PlayerId]--;
                        if (BackCountdown[pc.PlayerId] <= 0)
                            OnPlayerBack(pc);
                        SendRPCSyncKBBackCountdown(pc);
                        notifyRoles = true;
                    }
                    // 清除过期的提示信息
                    if (NameNotify.ContainsKey(pc.PlayerId) && NameNotify[pc.PlayerId].Item2 < Utils.GetTimeStamp(DateTime.Now))
                    {
                        NameNotify.Remove(pc.PlayerId);
                        SendRPCSyncNameNotify(pc);
                        notifyRoles = true;
                    }
                    // 必要时刷新玩家名字
                    if (notifyRoles) Utils.NotifyRoles(pc);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
        class KBCoEnterVentPatch
        {
            public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
            {
                if (!AmongUsClient.Instance.AmHost || Options.CurrentGameMode != CustomGameMode.SoloKombat) return true;
                return __instance.myPlayer.SoloAlive();
            }
        }
        [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
        class KBEnterVentPatch
        {
            public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
            {
                if (!AmongUsClient.Instance.AmHost || Options.CurrentGameMode != CustomGameMode.SoloKombat) return;
                if (!pc.SoloAlive())
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
            }
        }
    }
}