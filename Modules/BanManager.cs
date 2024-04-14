using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using HarmonyLib;
using static TownOfHostForE.Translator;
using TownOfHostForE.Attributes;
using InnerNet;

namespace TownOfHostForE
{
    public static class BanManager
    {
        private static readonly string DENY_NAME_LIST_PATH = @"./TOH_DATA/DenyName.txt";
        private static readonly string BAN_LIST_PATH = @"./TOH_DATA/BanList.txt";

        [PluginModuleInitializer]
        public static void Init()
        {
            Directory.CreateDirectory("TOH_DATA");
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
        }
        public static string GetHashedPuid(this ClientData player)
        {
            if (player == null) return "";
            string puid = player.ProductUserId;
            using SHA256 sha256 = SHA256.Create();

            // get sha-256 hash
            byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
            string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

            // pick front 5 and last 4
            return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
        }
        public static void AddBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || player == null) return;
            if (!CheckBanList(player?.FriendCode, player?.GetHashedPuid()))
            {
                if (player?.GetHashedPuid() != "" && player?.GetHashedPuid() != null && player?.GetHashedPuid() != "e3b0cb855")
                {
                    var additionalInfo = "";
                    File.AppendAllText(BAN_LIST_PATH, $"{player?.FriendCode},{player?.GetHashedPuid()},{player.PlayerName.RemoveHtmlTags()}{additionalInfo}\n");
                    Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
                }
                else Logger.Info($"Failed to add player {player?.PlayerName.RemoveHtmlTags()}/{player?.FriendCode}/{player?.GetHashedPuid()} to ban list!", "AddBanPlayer");
            }
            //if (!CheckBanList(player) && player.FriendCode != "")
            //{
            //    File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.PlayerName}\n");
            //    Logger.SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
            //}
        }
        public static void CheckDenyNamePlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return;
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
            if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
            if (CheckBanList(player?.FriendCode, player?.GetHashedPuid()))
            {
                AmongUsClient.Instance.KickPlayer(player.Id, true);
                Logger.SendInGame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
                Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
                return;
            }
        }
        public static bool CheckBanList(string code, string hashedpuid = "")
        {
            bool OnlyCheckPuid = false;
            if (code == "" && hashedpuid != "") OnlyCheckPuid = true;
            else if (code == "") return false;
            try
            {
                Directory.CreateDirectory("TOH_DATA");
                if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
                using StreamReader sr = new(BAN_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (!OnlyCheckPuid)
                        if (line.Contains(code)) return true;
                    if (line.Contains(hashedpuid)) return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckBanList");
            }
            return false;
        }
        //public static bool CheckBanList(InnerNet.ClientData player)
        //{
        //    if (player == null || player?.FriendCode == "") return false;
        //    try
        //    {
        //        Directory.CreateDirectory("TOH_DATA");
        //        if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
        //        using StreamReader sr = new(BAN_LIST_PATH);
        //        string line;
        //        while ((line = sr.ReadLine()) != null)
        //        {
        //            if (line == "") continue;
        //            if (player.FriendCode == line.Split(",")[0]) return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Exception(ex, "CheckBanList");
        //    }
        //    return false;
        //}
    }
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
    class BanMenuSelectPatch
    {
        public static void Postfix(BanMenu __instance, int clientId)
        {
            InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
            if (recentClient == null) return;
            if (!BanManager.CheckBanList(recentClient?.FriendCode, recentClient?.GetHashedPuid())) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
        }
    }
}