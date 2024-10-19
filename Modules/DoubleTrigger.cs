using System;
using System.Collections.Generic;
using MS.Internal.Xml.XPath;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost
{
    public static class DoubleTrigger
    {
        class DoubleTriggerData
        {
            public PlayerControl Target;
            public float Timer;
            public Func<PlayerControl, PlayerControl, bool> SingleAction;
            public Func<PlayerControl, PlayerControl, bool> DoubleAction;

            public DoubleTriggerData(Func<PlayerControl, PlayerControl, bool> singleAction, Func<PlayerControl, PlayerControl, bool> doubleAction)
            {
                Timer = -1f;
                Target = null;
                SingleAction = singleAction;
                DoubleAction = doubleAction;
            }
        }
        static Dictionary<byte, DoubleTriggerData> DoubleTriggerList = new(15);

        static readonly float DoubleTriggerTime = 0.3f;

        [GameModuleInitializer]
        public static void Init()
        {
            DoubleTriggerList.Clear();
        }
        public static void AddDoubleTrigger(this PlayerControl killer)
        {
            if (killer.GetRoleClass() is not IDoubleTrigger role) throw new Exception($"{killer.name} is Not IDoubleTrigger!");
            DoubleTriggerList[killer.PlayerId] = new DoubleTriggerData(role.SingleAction, role.DoubleAction);
        }
        /// <summary>
        /// ダブルトリガーの処理
        /// </summary>
        /// <param name="info"></param>
        /// <returns>true:処理した false:処理しない</returns>
        public static bool OnCheckMurderAsKiller(MurderInfo info)
        {
            var (killer, target) = info.AttemptTuple;
            if (!DoubleTriggerList.TryGetValue(killer.PlayerId, out var triggerData)) return false;
            if (triggerData.Target == null)
            {
                //シングルアクション候補
                triggerData.Target = target;
                triggerData.Timer = DoubleTriggerTime;
                //シングルアクション時はキル間隔を無視
                CheckMurderPatch.TimeSinceLastKill.Remove(killer.PlayerId);
                Logger.Info($"{killer.name} stand by SingleAction to{triggerData.Target}", "DoubleTrigger");
                info.DoKill = false;
            }
            else
            {
                //ダブルクリック対象がターゲットとずれたとしても元々のターゲットを優先
                Logger.Info($"{killer.name} DoDoubleAction to{triggerData.Target}", "DoubleTrigger");
                info.DoKill = triggerData.DoubleAction(killer, triggerData.Target);
                //シングス処理をキャンセルするためnullにする
                triggerData.Target = null;
            }
            return true;
        }
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!DoubleTriggerList.TryGetValue(player.PlayerId, out var triggerData)) return;
            if (!GameStates.IsInTask)
            {
                triggerData.Target = null;
            }
            if (triggerData.Target == null) return;

            triggerData.Timer -= Time.fixedDeltaTime;
            if (triggerData.Timer < 0)
            {
                Logger.Info($"{player.name} DoSingleAction", "DoubleTrigger");
                triggerData.SingleAction(player, triggerData.Target);
                triggerData.Target = null;
            }
        }
    }
}
