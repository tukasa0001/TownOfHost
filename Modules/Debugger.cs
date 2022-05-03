using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

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
        public static bool isEnable;
        public static List<string> disableList = new List<string>();
        public static List<string> sendToGameList = new List<string>();
        public static List<string> sendToWebhookList = new List<string>();
        public static bool isDetail = false;
        public static void enable() => isEnable = true;
        public static void disable() => isEnable = false;
        public static void enable(string tag, bool toGame = false, bool toWebhook = false)
        {
            disableList.Remove(tag);
            if (toGame)
            {
                if (!sendToGameList.Contains(tag)) sendToGameList.Add(tag);
            }
            else
            {
                sendToGameList.Remove(tag);
            }
            if (toWebhook)
            {
                if (!sendToWebhookList.Contains(tag)) sendToWebhookList.Add(tag);
            }
            else
            {
                sendToWebhookList.Remove(tag);
            }
        }
        public static void disable(string tag) { if (!disableList.Contains(tag)) disableList.Add(tag); }
        public static void SendInGame(string text, bool isAlways = false)
        {
            if (!isEnable) return;
            if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddItem(text);
            //SendToFile("<InGame>" + text);
        }
        public static void SendToFile(string text, LogLevel level = LogLevel.Normal, string tag = "")
        {
            if (!isEnable || disableList.Contains(tag)) return;
            string t = DateTime.Now.ToString("HH:mm:ss");
            if (sendToGameList.Contains(tag)) SendInGame($"[{tag}]{text}");
            if (sendToWebhookList.Contains(tag)) webhook.send($"[{t}][{tag}]{text}");
            string log_text = $"[{t}][{tag}]{text}";
            if (isDetail && main.AmDebugger.Value)
            {
                StackFrame stack = new StackFrame(2);
                string class_name = stack.GetMethod().ReflectedType.Name;
                string method_name = stack.GetMethod().Name;
                log_text = $"[{t}][{class_name}.{method_name}][{tag}]{text}";
            }
            var logger = main.Logger;
            switch (level)
            {
                case LogLevel.Normal:
                    logger.LogInfo(log_text);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(log_text);
                    break;
                case LogLevel.Error:
                    logger.LogError(log_text);
                    break;
                case LogLevel.Fatal:
                    logger.LogFatal(log_text);
                    break;
                case LogLevel.Message:
                    logger.LogMessage(log_text);
                    break;
                default:
                    logger.LogWarning("Error:Invalid LogLevel");
                    logger.LogInfo(log_text);
                    break;
            }
        }
        public static void info(string text, string tag = "") => SendToFile(text, LogLevel.Normal, tag);
        public static void warn(string text, string tag = "") => SendToFile(text, LogLevel.Warning, tag);
        public static void error(string text, string tag = "") => SendToFile(text, LogLevel.Error, tag);
        public static void fatal(string text, string tag = "") => SendToFile(text, LogLevel.Fatal, tag);
        public static void msg(string text, string tag = "") => SendToFile(text, LogLevel.Message, tag);
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
