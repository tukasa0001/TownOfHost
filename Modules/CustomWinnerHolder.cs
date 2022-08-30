using System;
using System.Collections.Generic;
using System.Linq;
using InnerNet;

namespace TownOfHost
{
    public static class CustomWinnerHolder
    {
        // 勝者のチームが格納されます。
        // リザルトの背景色の決定などに使用されます。
        public static CustomWinner WinnerTeam;
        // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
        // チームとなる第三陣営の処理に最適です。
        public static List<CustomRoles> WinnerRoles;
        // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
        // 単独勝利する第三陣営の処理に最適です。
        public static List<byte> WinnerIds;

        public static void Reset()
        {
            WinnerTeam = CustomWinner.Default;
            WinnerRoles = new();
            WinnerIds = new();
        }
    }
}