using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using LogLevel = BepInEx.Logging.LogLevel;

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
        public static bool isDetail = false;
        public static bool isAlsoInGame = false;
        public static void enable() => isEnable = true;
        public static void disable() => isEnable = false;
        public static void enable(string tag, bool toGame = false)
        {
            disableList.Remove(tag);
            if (toGame && !sendToGameList.Contains(tag)) sendToGameList.Add(tag);
            else sendToGameList.Remove(tag);
        }
        public static void disable(string tag) { if (!disableList.Contains(tag)) disableList.Add(tag); }
        public static void SendInGame(string text, bool isAlways = false)
        {
            if (!isEnable) return;
            if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddItem(text);
        }
        private static void SendToFile(string text, LogLevel level = LogLevel.Info, string tag = "", int lineNumber = 0, string fileName = "")
        {
            if (!isEnable || disableList.Contains(tag)) return;
            var logger = main.Logger;
            string t = DateTime.Now.ToString("HH:mm:ss");
            if (sendToGameList.Contains(tag) || isAlsoInGame) SendInGame($"[{tag}]{text}");
            text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            string log_text = $"[{t}][{tag}]{text}";
            if (isDetail && main.AmDebugger.Value)
            {
                StackFrame stack = new StackFrame(2);
                string className = stack.GetMethod().ReflectedType.Name;
                string memberName = stack.GetMethod().Name;
                log_text = $"[{t}][{className}.{memberName}({Path.GetFileName(fileName)}:{lineNumber})][{tag}]{text}";
            }
            switch (level)
            {
                case LogLevel.Info:
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
        public static void info(string text, string tag = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Info, tag, lineNumber, fileName);
        public static void warn(string text, string tag = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Warning, tag, lineNumber, fileName);
        public static void error(string text, string tag = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Error, tag, lineNumber, fileName);
        public static void fatal(string text, string tag = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Fatal, tag, lineNumber, fileName);
        public static void msg(string text, string tag = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Message, tag, lineNumber, fileName);
        public static void currentMethod([CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
        {
            StackFrame stack = new StackFrame(1);
            Logger.msg($"\"{stack.GetMethod().ReflectedType.Name}.{stack.GetMethod().Name}\" Called in \"{Path.GetFileName(fileName)}({lineNumber})\"", "Method");
        }
    }
}
