using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    static class TargetArrow
    {
        static Dictionary<(byte, byte), string> TargetArrows = new();
        static Dictionary<byte, bool> TargetArrowsColored = new();
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
            TargetArrowsColored.Clear();
        }
        /// <summary>
        /// 新たにターゲット矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        /// <param name="coloredArrow"></param>
        public static void Add(byte seer, byte target, bool coloredArrow)
        {
            TargetArrows[(seer, target)] = "・";
            TargetArrowsColored[seer] = coloredArrow;
        }
        /// <summary>
        /// 既存のターゲットの代わりを登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void Change(byte seer, byte target)
        {
            RemoveAllTarget(seer);
            Add(seer, target, TargetArrowsColored[seer]);
        }
        /// <summary>
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void Remove(byte seer, byte target)
        {
            TargetArrows.Remove((seer, target));
        }
        /// <summary>
        /// ターゲットの全削除
        /// </summary>
        /// <param name="seer"></param>
        public static void RemoveAllTarget(byte seer)
        {
            var targetList = new List<byte>(TargetArrows.Keys.Where(k => k.Item1 == seer).Select(k => k.Item2));
            foreach (var target in targetList)
            {
                TargetArrows.Remove((seer, target));
            }
        }
        /// <summary>
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string GetArrows(PlayerControl seer)
        {
            var targetList = new List<byte>(TargetArrows.Keys.Where(k => k.Item1 == seer.PlayerId).Select(k => k.Item2));
            if (targetList.Count == 0) return "";
            var arrows = new StringBuilder(120);
            foreach (var target in targetList)
            {
                arrows.Append(TargetArrows[(seer.PlayerId, target)]);
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

            var targetList = new List<byte>(TargetArrows.Keys.Where(k => k.Item1 == seer.PlayerId).Select(k => k.Item2));
            if (targetList.Count == 0) return;

            var colored = TargetArrowsColored[seerId];
            var update = false;
            foreach (var targetId in targetList)
            {
                var target = Utils.GetPlayerById(targetId);
                if (!target.IsAlive())
                {
                    Remove(seer.PlayerId, targetId);
                    update = true;
                    continue;
                }
                //対象の方角ベクトルを取る
                var dir = target.transform.position - seer.transform.position;
                byte index;
                if (dir.magnitude < 2)
                {
                    //近い時はドット表示
                    index = 8;
                }
                else
                {
                    //-22.5～22.5度を0とするindexに変換
                    var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                    index = (byte)(((int)(angle / 45)) % 8);
                }
                var arrow = Arrows[index];
                if (colored)
                {
                    arrow = $"<color={target.GetRoleColorCode()}>{arrow}</color>";
                }
                if (TargetArrows[(seerId, targetId)] != arrow)
                {
                    TargetArrows[(seerId, targetId)] = arrow;
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
