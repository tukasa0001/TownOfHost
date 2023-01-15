using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TownOfHost
{
    enum ArrowType
    {
        All = 0xff,
        Target = 0x01,
        SnitchWarning = 0x02,
    }
    static class TargetArrow
    {
        class ArrowInfo
        {
            public byte From;
            public byte To;
            public ArrowType Type;
            public bool Colored;
            public ArrowInfo(byte from, byte to, ArrowType type, bool colored)
            {
                From = from;
                To = to;
                Colored = colored;
                Type = type;
            }
            public bool Equals(ArrowInfo obj)
            {
                var checkType = (Type & obj.Type) != 0;
                return checkType && From == obj.From && To == obj.To;
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
        public static void Add(byte seer, byte target, ArrowType type = ArrowType.Target, bool coloredArrow = false)
        {
            var arrowInfo = new ArrowInfo(seer, target, type, coloredArrow);
            if (!TargetArrows.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrows[arrowInfo] = "・";
        }
        /// <summary>
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void Remove(byte seer, byte target, ArrowType type = ArrowType.Target, bool coloredArrow = false)
        {
            var arrowInfo = new ArrowInfo(seer, target, type, coloredArrow);
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
        public static void RemoveAllTarget(byte seer, ArrowType type = ArrowType.Target)
        {
            var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.From == seer && k.Type == type));
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
        public static string GetArrows(PlayerControl seer, ArrowType type = ArrowType.Target)
        {
            var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId && a.Type == type));
            if (arrowList.Count == 0) return "";
            var arrows = new StringBuilder(120);
            foreach (var arrow in arrowList)
            {
                arrows.Append(TargetArrows[arrow]);
            }
            return arrows.ToString();
        }
        /// <summary>
        /// FixedUpdate毎にターゲット矢印を確認
        /// 更新があったらNotifyRolesを発行
        /// </summary>
        /// <param name="__instance"></param>
        public static void OnFixedUpdate(PlayerControl __instance)
        {
            var seer = __instance;
            var seerId = seer.PlayerId;

            var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId));
            if (arrowList.Count == 0) return;

            var update = false;
            foreach (var arrowInfo in arrowList)
            {
                var targetId = arrowInfo.To;
                var colored = arrowInfo.Colored;
                var target = Utils.GetPlayerById(targetId);
                if (!target.IsAlive())
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
                if (colored)
                {
                    arrow = $"<color={target.GetRoleColorCode()}>{arrow}</color>";
                }
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
