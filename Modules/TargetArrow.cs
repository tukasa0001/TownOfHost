using System;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Attributes;
using UnityEngine;

namespace TownOfHost
{
    static class TargetArrow
    {
        class ArrowInfo
        {
            public byte From;
            public byte To;
            public ArrowInfo(byte from, byte to)
            {
                From = from;
                To = to;
            }
            public bool Equals(ArrowInfo obj)
            {
                return From == obj.From && To == obj.To;
            }
            public override string ToString()
            {
                return $"(From:{From} To:{To})";
            }
        }

        static Dictionary<ArrowInfo, string> TargetArrows = new();
        static readonly string[] Arrows = {
            "↑",
            "↗",
            "→",
            "↘",
            "↓",
            "↙",
            "←",
            "↖",
            "・"
        };

        [GameModuleInitializer]
        public static void Init()
        {
            TargetArrows.Clear();
        }
        /// <summary>
        /// 新たにターゲット矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        /// <param name="coloredArrow"></param>
        public static void Add(byte seer, byte target)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            if (!TargetArrows.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrows[arrowInfo] = "・";
        }
        /// <summary>
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void Remove(byte seer, byte target)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.Equals(arrowInfo)));
            foreach (var a in removeList)
            {
                TargetArrows.Remove(a);
            }
        }
        /// <summary>
        /// タイプの同じターゲットの全削除
        /// </summary>
        /// <param name="seer"></param>
        public static void RemoveAllTarget(byte seer)
        {
            var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.From == seer));
            foreach (var arrowInfo in removeList)
            {
                TargetArrows.Remove(arrowInfo);
            }
        }
        /// <summary>
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string GetArrows(PlayerControl seer, params byte[] targets)
        {
            var arrows = "";
            foreach (var arrowInfo in TargetArrows.Keys.Where(ai => ai.From == seer.PlayerId && targets.Contains(ai.To)))
            {
                arrows += TargetArrows[arrowInfo];
            }
            return arrows;
        }
        /// <summary>
        /// FixedUpdate毎にターゲット矢印を確認
        /// 更新があったらNotifyRolesを発行
        /// </summary>
        /// <param name="seer"></param>
        public static void OnFixedUpdate(PlayerControl seer)
        {
            if (!GameStates.IsInTask) return;

            var seerId = seer.PlayerId;
            var seerIsDead = !seer.IsAlive();

            var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId));
            if (arrowList.Count == 0) return;

            var update = false;
            foreach (var arrowInfo in arrowList)
            {
                var targetId = arrowInfo.To;
                var target = Utils.GetPlayerById(targetId);
                if (seerIsDead || !target.IsAlive())
                {
                    TargetArrows.Remove(arrowInfo);
                    update = true;
                    continue;
                }
                //対象の方角ベクトルを取る
                var dir = target.transform.position - seer.transform.position;
                int index;
                if (dir.magnitude < 2)
                {
                    //近い時はドット表示
                    index = 8;
                }
                else
                {
                    //-22.5～22.5度を0とするindexに変換
                    // 下が0度、左側が+180まで右側が-180まで
                    // 180度足すことで上が0度の時計回り
                    // 45度単位のindexにするため45/2を加算
                    var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                    index = ((int)(angle / 45)) % 8;
                }
                var arrow = Arrows[index];
                if (TargetArrows[arrowInfo] != arrow)
                {
                    TargetArrows[arrowInfo] = arrow;
                    update = true;
                }
            }
            if (update)
            {
                Utils.NotifyRoles(SpecifySeer: seer);
            }
        }
    }
}
