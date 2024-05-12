using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TownOfHostForE.Attributes;

namespace TownOfHostForE
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

        class ArrowInfoP
        {
            public byte From;
            public Vector3 To;
            public ArrowInfoP(byte from, Vector3 to)
            {
                From = from;
                To = to;
            }
            public bool Equals(ArrowInfoP obj)
            {
                return From == obj.From && To == obj.To;
            }
            public override string ToString()
            {
                return $"(From:{From} To:{To})";
            }
        }

        static Dictionary<ArrowInfo, string> TargetArrows = new();
        static Dictionary<ArrowInfo, string> TargetArrowsD = new();
        static Dictionary<ArrowInfo, string> TargetArrowsP = new();
        static Dictionary<ArrowInfo, List<string>> TargetArrowsPM = new();
        static Dictionary<byte, Vector3> DeadPosition = new();
        static Dictionary<byte, Vector3> targetPosition = new();
        static Dictionary<byte, List<Vector3>> targetPositionM = new();
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
            TargetArrowsD.Clear();
            DeadPosition.Clear();
            TargetArrowsP.Clear();
            targetPosition.Clear();
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
        /// 新たに死体の矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        /// <param name="coloredArrow"></param>
        public static void Add(byte seer, DeadBody target)
        {
            var arrowInfo = new ArrowInfo(seer, target.ParentId);
            DeadPosition[target.ParentId] = target.transform.position;
            if (!TargetArrowsD.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrowsD[arrowInfo] = "・";
        }
        /// <summary>
        /// 新たに場所の矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        /// <param name="coloredArrow"></param>
        public static void Add(byte seer, byte target, Vector3 targetPositionV)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            targetPosition[seer] = targetPositionV;
            if (!TargetArrowsP.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrowsP[arrowInfo] = "・";
        }
        /// <summary>
        /// 新たに位置の矢印対象を登録
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void AddMulti(byte seer, byte target, Vector3 targetPositionV, bool Reset = false)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            if (Reset)
            {
                targetPositionM.Clear();
                TargetArrowsPM.Clear();
            }

            //見る側とみられる側が同じ
            if (seer == target)
            {
                if (!targetPositionM.ContainsKey(seer))
                {
                    targetPositionM[seer] = new List<Vector3>();
                }

                if (!TargetArrowsPM.Any(x => x.Key.Equals(arrowInfo)))
                {
                    TargetArrowsPM[arrowInfo] = new List<string>
                            {
                                "・"
                            };
                }
                else
                {
                    ////一つだけ取れるはず
                    var targetData = TargetArrowsPM.Where(x => x.Key.Equals(arrowInfo));
                    ArrowInfo nowinfo = targetData.FirstOrDefault().Key;
                    TargetArrowsPM[nowinfo].Add("・");
                }

                targetPositionM[seer].Add(targetPositionV);
                return;
            }
            //違う場合(ニャオハとか)
            else if (targetPositionM.ContainsKey(seer))
            {
                //べた固定で座標保存(1以上増やさない)
                targetPositionM[seer][0] = targetPositionV;
            }
            else
            {
                targetPositionM[seer] = new List<Vector3>
                        {
                            targetPositionV
                        };
            }
            if (!TargetArrowsPM.Any(a => a.Key.Equals(arrowInfo)))
                TargetArrowsPM[arrowInfo].Add("・");
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
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void Remove(byte seer, DeadBody target)
        {
            var arrowInfo = new ArrowInfo(seer, target.ParentId);
            //var removeList = new List<ArrowInfoD>(TargetArrowsD.Keys.Where(k => k.Equals(arrowInfo)));
            var removeList = new List<ArrowInfo>(TargetArrowsD.Keys.Where(k => k.Equals(arrowInfo)));
            foreach (var a in removeList)
            {
                TargetArrowsD.Remove(a);
                DeadPosition.Remove(a.To);
            }
        }
        /// <summary>
        /// ターゲットの削除
        /// </summary>
        /// <param name="seer"></param>
        /// <param name="target"></param>
        public static void RemoveP(byte seer, byte target)
        {
            var arrowInfo = new ArrowInfo(seer, target);
            var removeList = new List<ArrowInfo>(TargetArrowsP.Keys.Where(k => k.Equals(arrowInfo)));
            foreach (var a in removeList)
            {
                TargetArrowsP.Remove(a);
                targetPosition.Remove(a.From);
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
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string GetArrowsP(PlayerControl targets, params byte[] seer)
        {
            var arrows = "";
            foreach (var arrowInfo in TargetArrowsP.Keys.Where(ai => seer.Contains(ai.From) && ai.To == targets.PlayerId))
            {
                arrows += TargetArrowsP[arrowInfo];
            }
            return arrows;
        }
        /// <summary>
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string GetArrowsPM(PlayerControl targets, params byte[] seer)
        {
            var arrows = "";
            foreach (var arrowInfo in TargetArrowsPM.Keys.Where(ai => seer.Contains(ai.From) && ai.To == targets.PlayerId))
            {
                foreach (string arrow in TargetArrowsPM[arrowInfo])
                {
                    arrows += arrow;
                }
            }

            return arrows;
        }
        /// <summary>
        /// 見ることのできるすべてのターゲット矢印を取得
        /// </summary>
        /// <param name="seer"></param>
        /// <returns></returns>
        public static string GetArrows(PlayerControl seer, params DeadBody[] targets)
        {
            var arrows = "";
            List<byte> targetsB = new();
            foreach (var db in targets)
            {
                targetsB.Add(db.ParentId);
            }
            foreach (var arrowInfo in TargetArrowsD.Keys.Where(ai => ai.From == seer.PlayerId && targetsB.Contains(ai.To)))
            {
                arrows += TargetArrowsD[arrowInfo];
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
            var arrowListD = new List<ArrowInfo>(TargetArrowsD.Keys.Where(a => a.From == seer.PlayerId));
            var arrowListP = new List<ArrowInfo>(TargetArrowsP.Keys.Where(a => a.From == seer.PlayerId));
            var arrowListPM = new List<ArrowInfo>(TargetArrowsPM.Keys.Where(a => a.From == seer.PlayerId));
            if (arrowList.Count == 0 && arrowListD.Count == 0 && arrowListP.Count == 0 && arrowListPM.Count == 0) return;
            var nUpdate = false;
            var dUpdate = false;
            var pUpdate = false;
            var mUpdate = false;

            nUpdate = NormalUpdate(arrowList, seerIsDead, seer);

            //死体処理
            dUpdate = DeadbodyUpdate(arrowListD, seerIsDead, seer);

            //ポジション処理
            pUpdate = PositionUpdate(arrowListP, seerIsDead, seer);

            //複数ポジション処理
            mUpdate = MultiPositionUpdate(arrowListPM, seerIsDead, seer);

            if (nUpdate || dUpdate || pUpdate || mUpdate)
            {
                Utils.NotifyRoles(SpecifySeer: seer);
            }
        }

        private static bool NormalUpdate(List<ArrowInfo> arrowList, bool seerIsDead, PlayerControl seer)
        {
            bool update = false;
            if (arrowList.Count > 0)
            {
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
            }
            return update;
        }
        private static bool DeadbodyUpdate(List<ArrowInfo> arrowListD, bool seerIsDead, PlayerControl seer)
        {
            bool update = false;

            if (arrowListD.Count > 0)
            {
                foreach (var arrowInfo in arrowListD)
                {
                    var target = arrowInfo.To;
                    if (seerIsDead)
                    {
                        TargetArrowsD.Remove(arrowInfo);
                        DeadPosition.Remove(target);
                        update = true;
                        continue;
                    }
                    //対象の方角ベクトルを取る
                    var dir = DeadPosition[target] - seer.transform.position;
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
                    if (TargetArrowsD[arrowInfo] != arrow)
                    {
                        TargetArrowsD[arrowInfo] = arrow;
                        update = true;
                    }
                }
            }
            return update;
        }

        private static bool PositionUpdate(List<ArrowInfo> arrowListP, bool seerIsDead, PlayerControl seer)
        {
            bool update = false;

            if (arrowListP.Count > 0)
            {
                foreach (var arrowInfo in arrowListP)
                {
                    if (seerIsDead)
                    {
                        TargetArrowsP.Remove(arrowInfo);
                        targetPosition.Remove(seer.PlayerId);
                        update = true;
                        continue;
                    }
                    //対象の方角ベクトルを取る
                    var dir = targetPosition[seer.PlayerId] - seer.transform.position;
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
                    if (TargetArrowsP[arrowInfo] != arrow)
                    {
                        TargetArrowsP[arrowInfo] = arrow;
                        update = true;
                    }
                }
            }
            return update;
        }

        private static bool MultiPositionUpdate(List<ArrowInfo> arrowListP, bool seerIsDead, PlayerControl seer)
        {
            bool update = false;

            if (arrowListP.Count > 0)
            {
                foreach (var arrowInfo in arrowListP)
                {
                    if (seerIsDead)
                    {
                        TargetArrowsPM.Remove(arrowInfo);
                        targetPositionM.Remove(seer.PlayerId);
                        update = true;
                        continue;
                    }

                    int nowIndex = 0;
                    foreach (var position in targetPositionM[seer.PlayerId])
                    {
                        //対象の方角ベクトルを取る
                        //var dir = targetPosition[seer.PlayerId] - seer.transform.position;
                        var dir = position - seer.transform.position;
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
                        if (TargetArrowsPM[arrowInfo][nowIndex] != arrow)
                        {
                            TargetArrowsPM[arrowInfo][nowIndex] = arrow;
                            update = true;
                        }

                        nowIndex++;
                    }
                }
            }
            return update;
        }
    }
}
