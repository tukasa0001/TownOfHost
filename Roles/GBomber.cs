using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;
using System.Threading;
using System.Linq;

namespace TownOfHost
{
    public static class GBomber
    {
        static readonly int Id = 1900;
        public static CustomOption GBomberKillCooldown;//キルクール
        public static CustomOption GBomberKillCountReductionValue;//キルボタンでのカウント減少値
        public static CustomOption GBomberMeetingCountReductionValue;//議論中のカウントダウンスピード 1sあたり

        public static Dictionary<byte, int> GBombAttachedPlayers = new();//管理用
        public static Dictionary<byte, int> GBombAttachedPlayersDisplay = new();//表示用

        public static int InitialTimer = 6000;

        public static Timer timer = null;


        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.GBomber);
            GBomberKillCooldown = CustomOption.Create(Id + 10, Color.white, "GBomberKillCooldown", 20, 1, 60, 1, Options.CustomRoleSpawnChances[CustomRoles.GBomber]);
            GBomberKillCountReductionValue = CustomOption.Create(Id + 20, Color.white, "GBomberKillCountReductionValue", 500, 100, 1000, 100, Options.CustomRoleSpawnChances[CustomRoles.GBomber]);
            GBomberMeetingCountReductionValue = CustomOption.Create(Id + 30, Color.white, "GBomberMeetingCountReductionValue", 2, 1, 10, 1, Options.CustomRoleSpawnChances[CustomRoles.GBomber]);
        }

        public static void Init()
        {
            GBombAttachedPlayers = new();
            GBombAttachedPlayersDisplay = new Dictionary<byte, int>(GBombAttachedPlayers);
        }


        public static void KillAction(PlayerControl killer, PlayerControl target)
        {
            Logger.Info($"target: {target.GetNameWithRole()}", "GBomber");
            //キルを防ぐ
            killer.RpcGuardAndKill(target);
            //キルクールをセットする
            Main.AllPlayerKillCooldown[killer.PlayerId] = GBomberKillCooldown.GetFloat() * 2;   //GuardAndKillの場合キルクが半分になるので2倍値

            //爆弾取り付け済みかどうか
            if (GBombAttachedPlayers.ContainsKey(target.PlayerId))
            {
                GBombAttache(target.PlayerId);
            }
            else
            {
                Logger.Info($"GBombAttached GBombCount: {GBombAttachedPlayers[target.PlayerId]} -> {GBombAttachedPlayers[target.PlayerId] - GBomberKillCountReductionValue.GetInt()}", "GBomber");
                GBombAttachedPlayers[target.PlayerId] -= GBomberKillCountReductionValue.GetInt();
            }

        }

        //爆弾の取り付け
        private static void GBombAttache(byte playerId)
        {
            GBombAttachedPlayers.Add(playerId, InitialTimer);
            GBombAttachedPlayersDisplay = new Dictionary<byte, int>(GBombAttachedPlayers);
            if (timer == null)
            {
                TimerCallback timerDelegate = new(CountDown);
                timer = new(timerDelegate, null, 0, 100);
                SetGBomberTimer();
            }
            Logger.Info($"newGBombAttached GBombCount: {GBombAttachedPlayers[playerId]}", "GBomber");
        }

        //TODO:SendRPC GBombAttachedPlayersの同期 必要か不明
        //     public static void SendRPC()
        //     {
        //         Logger.Info($"SendRPC", "GBomber");
        //         MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendGBomberState, Hazel.SendOption.Reliable, -1);
        //         writer.Write(GBombAttachedPlayers);
        //         AmongUsClient.Instance.FinishRpcImmediately(writer);
        //     }

        //TODO:ReceiveRPC GBombAttachedPlayersの同期 必要か不明
        // public static void ReceiveRPC(MessageReader msg)
        // {
        //     var playerId = msg.ReadByte();
        //     GBombAttachedPlayers = msg.ReadInt16();//TODO:msg
        //     Logger.Info($"Player{playerId}:ReceiveRPC", "FireWorks");
        // }

        //
        public static void SetGBomberTimer()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Logger.Info($"GBomberTimer Start", "GBomber");
        }

        ///Time thread
        private static void CountDown(object state)
        {
            if (!GameStates.IsInGame) Dispose();
            foreach (var p in GBombAttachedPlayers)
            {
                GBombAttachedPlayers[p.Key] -= GameStates.IsMeeting ? GBomberMeetingCountReductionValue.GetInt() : 1;
                if (CheckAlive(p)) continue;
                Explosion(p);
            }
        }
        private static bool CheckAlive(KeyValuePair<byte, int> p)
        {
            PlayerControl player = PlayerControl.AllPlayerControls.ToArray().Where(s => s.PlayerId == p.Key).FirstOrDefault();
            if (player == null) return false;
            if (player.Data.IsDead || player.Data.Disconnected)
            {
                GBombAttachedPlayers.Remove(p.Key);
                return true;
            }
            return false;
        }

        private static void Explosion(KeyValuePair<byte, int> p)
        {
            if (p.Value <= 0)
            {
                GBombAttachedPlayers.Remove(p.Key);
                PlayerControl player = PlayerControl.AllPlayerControls.ToArray().Where(s => s.PlayerId == p.Key).FirstOrDefault();
                if (player == null) return;
                if (GameStates.IsMeeting)
                {
                    //TODO:会議中に死ぬ処理.要動作確認
                    Logger.Info($"IsMeeting Explosion : {player.GetNameWithRole()}", "GBomber");
                    player.RpcExile();
                }
                else if (GameStates.IsInTask)
                {
                    Logger.Info($"IsInTask Explosion : {player.GetNameWithRole()}", "GBomber");
                    player.RpcMurderPlayer(player);
                }
                //TODO:死因
            }
        }

        public static void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
                Logger.Info($"GBomberTimer Dispose", "GBomber");
            }
        }
    }
}