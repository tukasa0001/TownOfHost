using System.Collections.Generic;
using Hazel;

using TownOfHost.Attributes;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class CustomWinnerHolder
    {
        // 勝者のチームが格納されます。
        // リザルトの背景色の決定などに使用されます。
        // 注: この変数を変更する時、WinnerRoles・WinnerIdsを同時に変更しないと予期せぬ勝者が現れる可能性があります。
        public static CustomWinner WinnerTeam;
        // 追加勝利するプレイヤーの役職が格納されます。
        // リザルトの表示に使用されます。
        public static HashSet<CustomRoles> AdditionalWinnerRoles;
        // 勝者の役職が格納され、この変数に格納されている役職のプレイヤーは全員勝利となります。
        // チームとなるニュートラルの処理に最適です。
        public static HashSet<CustomRoles> WinnerRoles;
        // 勝者のPlayerIDが格納され、このIDを持つプレイヤーは全員勝利します。
        // 単独勝利するニュートラルの処理に最適です。
        public static HashSet<byte> WinnerIds;

        [GameModuleInitializer, PluginModuleInitializer]
        public static void Reset()
        {
            WinnerTeam = CustomWinner.Default;
            AdditionalWinnerRoles = new();
            WinnerRoles = new();
            WinnerIds = new();
        }
        public static void ClearWinners()
        {
            WinnerRoles.Clear();
            WinnerIds.Clear();
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、AdditionalWinnerRolesに追加します。</para></summary>
        public static void SetWinnerOrAdditonalWinner(CustomWinner winner)
        {
            if (WinnerTeam == CustomWinner.Default) WinnerTeam = winner;
            else AdditionalWinnerRoles.Add((CustomRoles)winner);
        }
        /// <summary><para>WinnerTeamに値を代入します。</para><para>すでに代入されている場合、既存の値をAdditionalWinnerRolesに追加してから代入します。</para></summary>
        public static void ShiftWinnerAndSetWinner(CustomWinner winner)
        {
            if (WinnerTeam != CustomWinner.Default)
                AdditionalWinnerRoles.Add((CustomRoles)WinnerTeam);
            WinnerTeam = winner;
        }
        /// <summary><para>既存の値をすべて削除してから、WinnerTeamに値を代入します。</para></summary>
        public static void ResetAndSetWinner(CustomWinner winner)
        {
            Reset();
            WinnerTeam = winner;
        }

        public static MessageWriter WriteTo(MessageWriter writer)
        {
            writer.Write((int)WinnerTeam);

            writer.Write(AdditionalWinnerRoles.Count);
            foreach (var wr in AdditionalWinnerRoles)
                writer.Write((int)wr);

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

            AdditionalWinnerRoles = new();
            int AdditionalWinnerRolesCount = reader.ReadInt32();
            for (int i = 0; i < AdditionalWinnerRolesCount; i++)
                AdditionalWinnerRoles.Add((CustomRoles)reader.ReadInt32());

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