using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    public static class CustomWinnerHolder
    {
        // 勝者のチームが格納されます。
        // リザルトの背景色の決定などに使用されます。
        public static CustomWinner WinnerTeam;
        // 追加勝利するプレイヤーのチームが格納されます。
        // リザルトの表示に使用されます。
        public static HashSet<AdditionalWinners> AdditionalWinnerTeams;
        // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
        // チームとなる第三陣営の処理に最適です。
        public static HashSet<CustomRoles> WinnerRoles;
        // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
        // 単独勝利する第三陣営の処理に最適です。
        public static HashSet<byte> WinnerIds;

        public static void Reset()
        {
            WinnerTeam = CustomWinner.Default;
            AdditionalWinnerTeams = new();
            WinnerRoles = new();
            WinnerIds = new();
        }

        public static MessageWriter WriteTo(MessageWriter writer)
        {
            writer.Write((int)WinnerTeam);

            writer.Write(AdditionalWinnerTeams.Count);
            foreach (var wt in AdditionalWinnerTeams)
                writer.Write((int)wt);

            writer.Write(WinnerRoles.Count);
            foreach (var wr in WinnerRoles)
                writer.Write((int)wr);

            writer.Write(WinnerIds.Count);
            foreach (var id in WinnerIds)
                writer.Write(id);

            return writer;
        }
        public static void ReadFrom(MessageReader reader)
        {
            WinnerTeam = (CustomWinner)reader.ReadInt32();

            AdditionalWinnerTeams = new();
            int AdditionalWinnerTeamsCount = reader.ReadInt32();
            for (int i = 0; i < AdditionalWinnerTeamsCount; i++)
                AdditionalWinnerTeams.Add((AdditionalWinners)reader.ReadInt32());

            WinnerRoles = new();
            int WinnerRolesCount = reader.ReadInt32();
            for (int i = 0; i < WinnerRolesCount; i++)
                WinnerRoles.Add((CustomRoles)reader.ReadInt32());

            WinnerIds = new();
            int WinnerIdsCount = reader.ReadInt32();
            for (int i = 0; i < WinnerIdsCount; i++)
                WinnerIds.Add(reader.ReadByte());
        }
    }
}