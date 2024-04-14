using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TownOfHostForE.Attributes;

namespace TownOfHostForE
{
    static class VentSelect
    {
        public static List<byte> PlayerIdList = new();

        public static Dictionary<byte, float> StandbyTimer = new();
        public static Dictionary<byte, byte> SelectTarget = new();
        public static Dictionary<byte, Action> SelectedAction = new();
        public static Dictionary<byte, int> NowSelectNumber = new();

        [GameModuleInitializer]
        public static void Init()
        {
            PlayerIdList = new();
            StandbyTimer = new();
            SelectTarget = new();
            SelectedAction = new();
            NowSelectNumber = new();
        }
        public static void AddVentSelect(this PlayerControl killer)
        {
            PlayerIdList.Add(killer.PlayerId);
        }
        public static bool CanVentSelect(this PlayerControl killer)
        {
            return PlayerIdList.Contains(killer.PlayerId);
        }

        public static PlayerControl VentPlayerSelect(this PlayerControl ventedPlayer, Action firstAction)
        {
            var playerId = ventedPlayer.PlayerId;
            if (StandbyTimer.ContainsKey(playerId)) //タイマーに名前がある＝既に処理を開始している時
            {
                StandbyTimer.Remove(playerId);
            }
            else //初めての時
            {
                NowSelectNumber.Add(playerId, 0);
                SelectedAction.Add(playerId, firstAction);
            }

            int i = 0;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target == ventedPlayer) continue;
                if (i < NowSelectNumber[playerId])
                {
                    i++;
                    Logger.Info($"{ventedPlayer.name} {i} < {NowSelectNumber[playerId]}", "VentSelect");
                    continue;
                } //2人目以降、処理を飛ばす時

                SelectTarget.Remove(playerId);
                SelectTarget.Add(playerId, target.PlayerId);

                Logger.Info($"{ventedPlayer.name} GuardPlayerSelectNow:{target.name}", "VentSelect");

                if (NowSelectNumber[playerId] + 2 >= Main.AllAlivePlayerControls.Count()) //もし最後のプレイヤーを設定した時、次が初めのプレイヤーになるようにセット
                {
                    NowSelectNumber[playerId] = 0;
                    Logger.Info($"{ventedPlayer.name} {NowSelectNumber[playerId] + 1} < {Main.AllAlivePlayerControls.Count()} ,{NowSelectNumber[playerId]}", "VentSelect");
                }
                else
                {
                    NowSelectNumber[playerId]++;
                    Logger.Info($"{ventedPlayer.name} {NowSelectNumber[playerId] + 1} < {Main.AllAlivePlayerControls.Count()} ,{NowSelectNumber[playerId]}", "VentSelect");
                }
                break;
            }
            StandbyTimer.Add(ventedPlayer.PlayerId, 3f);

            return Utils.GetPlayerById(SelectTarget[playerId]);
        }
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInTask)
            {
                StandbyTimer.Clear();
                SelectTarget.Clear();
                SelectedAction.Clear();
                return;
            }

            var playerId = player.PlayerId;
            if (!StandbyTimer.ContainsKey(playerId)) return;

            StandbyTimer[playerId] -= Time.fixedDeltaTime;
            if (StandbyTimer[playerId] <= 0)   //ガードプレイヤー確定、処理開始
            {
                Logger.Info($"{player.name} DoVentSelect", "VentSelect");
                SelectedAction[playerId]();

                player.RpcProtectedMurderPlayer(); //設定完了のパリン

                StandbyTimer.Remove(playerId);
                SelectTarget.Remove(playerId);
                NowSelectNumber.Remove(playerId);
                SelectedAction.Remove(playerId);

                Utils.NotifyRoles(SpecifySeer: player);
            }
        }
    }
}