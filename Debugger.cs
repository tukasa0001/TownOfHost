using System.Net.Http;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost
{
    class webhook
    {
        public static void send(string text)
        {
            if (main.WebhookURL.Value == "none") return;
            HttpClient httpClient = new HttpClient();
            Dictionary<string, string> strs = new Dictionary<string, string>()
                {
                    { "content", text },
                    { "username", "TownOfHost-Debugger" },
                    { "avatar_url", "https://cdn.discordapp.com/avatars/336095904320716800/95243b1468018a24f7ae03d7454fd5f2.webp?size=40" }
                };
            TaskAwaiter<HttpResponseMessage> awaiter = httpClient.PostAsync(
                main.WebhookURL.Value, new FormUrlEncodedContent(strs)).GetAwaiter();
            awaiter.GetResult();
        }
    }
    class Logger
    {
        public static void SendInGame(string text, bool isAlways = false)
        {
            DestroyableSingleton<HudManager>.Instance.Notifier.AddItem(text);
            SendToFile("<InGame>" + text);
        }
        public static void SendToFile(string text, LogLevel level = LogLevel.Normal)
        {
            var logger = main.Logger;
            switch (level)
            {
                case LogLevel.Normal:
                    logger.LogInfo(text);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(text);
                    break;
                case LogLevel.Error:
                    logger.LogError(text);
                    break;
                case LogLevel.Fatal:
                    logger.LogFatal(text);
                    break;
                case LogLevel.Message:
                    logger.LogMessage(text);
                    break;
                default:
                    logger.LogWarning("Error:Invalid LogLevel");
                    logger.LogInfo(text);
                    break;
            }
        }
    }
    public enum LogLevel
    {
        Normal = 0,
        Warning,
        Error,
        Fatal,
        Message
    }
}
