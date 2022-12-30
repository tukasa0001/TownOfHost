using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TownOfHost
{
    static class DoubleTriger
    {
        public static List<byte> PlayerIdList = new();

        public static Dictionary<byte, float> FirstTriggerTimer = new();
        public static Dictionary<byte, byte> FirstTriggerTarget = new();
        public static Dictionary<byte, Action> FirstTriggerAction = new();

        public static void Init()
        {
            PlayerIdList = new();
            FirstTriggerTimer = new();
            FirstTriggerAction = new();
        }
        public static void AddDoubleTrigger(this PlayerControl killer)
        {
            PlayerIdList.Add(killer.PlayerId);
        }
        public static bool CanDoubleTrigger(this PlayerControl killer)
        {
            return PlayerIdList.Contains(killer.PlayerId);
        }

        ///     一回目アクション時 false、2回目アクション時true
        public static bool CheckDoubleTrigger(this PlayerControl killer, PlayerControl target, Action firstAction)
        {
            if (FirstTriggerTimer.ContainsKey(killer.PlayerId))
            {
                if (FirstTriggerTarget[killer.PlayerId]!= target.PlayerId)
                {
                    //2回目がターゲットずれてたら最初の相手にシングルアクション
                    return false;
                }
                Logger.Info($"{killer.name} DoDoubleAction", "DobbleTrigger");
                FirstTriggerTimer.Remove(killer.PlayerId);
                FirstTriggerTarget.Remove(killer.PlayerId);
                FirstTriggerAction.Remove(killer.PlayerId);
                return true;
            }
            FirstTriggerTimer.Add(killer.PlayerId, 1f);
            FirstTriggerTarget.Add(killer.PlayerId,target.PlayerId);
            FirstTriggerAction.Add(killer.PlayerId, firstAction);
            return false;
        }
        public static void DoFirstTriggerAction()
        {
            List<byte> playerList = new(FirstTriggerTimer.Keys);
            if (!GameStates.IsInTask)
            {
                FirstTriggerTimer.Clear();
                FirstTriggerTarget.Clear();
                FirstTriggerAction.Clear();
                return;
            }
            foreach (var player in playerList)
            {
                FirstTriggerTimer[player]-= Time.fixedDeltaTime;
                if (FirstTriggerTimer[player] <= 0)
                {
                    Logger.Info($"{Utils.GetPlayerById(player).name} DoSingleAction", "DobbleTrigger");
                    FirstTriggerAction[player]();

                    FirstTriggerTimer.Remove(player);
                    FirstTriggerTarget.Remove(player);
                    FirstTriggerAction.Remove(player);
                }
            }
        }
    }
}
