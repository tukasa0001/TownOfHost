using System;
using System.IO;
using System.Text.RegularExpressions;
using static TownOfHost.Translator;
namespace TownOfHost
{
    public static class BanManager
    {
        private static readonly string DENY_NAME_LIST_PATH = @"./TOH_DATA/DenyName.txt";
        private static readonly string BAN_LIST_PATH = @"./TOH_DATA/BanList.txt";

        public static void Init()
        {
            Directory.CreateDirectory("TOH_DATA");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
        }
        public static void AddBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!CheckBanList(player))
            {
                File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.PlayerName}\n");
            }
        }
        public static void CheckDenyNamePlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
                using StreamReader sr = new(DENY_NAME_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (Regex.IsMatch(player.PlayerName, line))
                    {
                        AmongUsClient.Instance.KickPlayer(player.Id, false);
                        Logger.SendInGame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                        Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckDenyNamePlayer");
            }
        }
        public static void CheckBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (CheckBanList(player))
            {
                AmongUsClient.Instance.KickPlayer(player.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
                Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
                return;
            }
        }
        public static bool CheckBanList(InnerNet.ClientData player)
        {
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
                using StreamReader sr = new(BAN_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (player.FriendCode == line.Split(",")[0]) return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckBanList");
            }
            return false;
        }

    }
}