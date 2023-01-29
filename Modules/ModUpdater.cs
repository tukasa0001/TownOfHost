using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using GooglePlayGames;
using HarmonyLib;
using Il2CppSystem.Diagnostics.Tracing;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public class ModUpdater
    {
        private static readonly string URL = "http://api.2018k.cn";
        public static bool hasUpdate = false;
        public static bool forceUpdate = true;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static Version latestVersion = null;
        public static string latestTitle = null;
        public static string downloadUrl = null;
        public static GenericPopup InfoPopup;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
        [HarmonyPriority(2)]
        public static void Start_Prefix(MainMenuManager __instance)
        {
            DeleteOldDLL();
            InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
            InfoPopup.name = "InfoPopup";
            InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
            if (!isChecked)
            {
                CheckRelease().GetAwaiter().GetResult();
            }
            MainMenuManagerPatch.updateButton.SetActive(hasUpdate);
            MainMenuManagerPatch.updateButton.transform.position = MainMenuManagerPatch.template.transform.position + new Vector3(0.25f, 0.75f);
            __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) =>
            {
                MainMenuManagerPatch.updateButton.transform
                    .GetChild(0).GetComponent<TMPro.TMP_Text>()
                    .SetText($"{GetString("updateButton")}\n{latestTitle}");
            })));
        }

        public static string UrlSetId(string url) => url + "?id=05ACF8DE5AE047D59372E733D5B3172B";
        public static string UrlSetCheck(string url) => url + "/checkVersion";
        public static string UrlSetInfo(string url) => url + "/getExample";
        public static string UrlSetToday(string url) => url + "/today";

        public static string Get(string url)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            try
            {
                //获取内容
                using StreamReader reader = new(stream);
                result = reader.ReadToEnd();
            }
            finally
            {
                stream.Close();
            }
            return result;
        }

        public static Task<bool> CheckRelease()
        {
            string url =  UrlSetId(UrlSetCheck(URL)) + "&version=" + Main.PluginVersion;
            try
            {
                string res = Get(url);
                string[] info = res.Split("|");
                hasUpdate = false;
                forceUpdate = info[1] == "true";
                downloadUrl = info[3];
                latestVersion = new(info[4]);
                latestTitle = new("TOHE");

                string[] num = info[4].Split(".");
                string[] inum = Main.PluginVersion.Split(".");
                if (num.Length > inum.Length) inum.AddItem("0");
                for (int i = 0; i < num.Length; i++)
                {
                    int c = int.Parse(num[i]);
                    int m = int.Parse(inum[i]);
                    if (c > m)hasUpdate= true;
                    if (c != m) break;
                }

#if DEBUG
                if (!hasUpdate && Main.PluginVersion == info[4])
                {
                    hasUpdate = true;
                    forceUpdate = false;
                }
#endif

                if (downloadUrl == null && downloadUrl == "")
                {
                    Logger.Error("获取下载地址失败", "CheckRelease");
                    return Task.FromResult(false);
                }
                isChecked = true;
                isBroken = false;
            }
            catch (Exception ex)
            {
                isChecked = false;
                isBroken = true;
                Logger.Error($"检查更新时发生错误\n{ex}", "CheckRelease", false);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        public static void StartUpdate(string url)
        {
            ShowPopup(GetString("updatePleaseWait"));
            if (!BackupDLL())
            {
                ShowPopup(GetString("updateManually"), true);
                return;
            }
            _ = DownloadDLL(url);
            return;
        }
        public static bool BackupDLL()
        {
            try
            {
                File.Move(Assembly.GetExecutingAssembly().Location, Assembly.GetExecutingAssembly().Location + ".bak");
            }
            catch
            {
                Logger.Error("バックアップに失敗しました", "BackupDLL");
                return false;
            }
            return true;
        }
        public static void DeleteOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.bak"))
                {
                    Logger.Info($"{Path.GetFileName(path)}を削除", "DeleteOldDLL");
                    File.Delete(path);
                }
            }
            catch
            {
                Logger.Error("削除に失敗しました", "DeleteOldDLL");
            }
            return;
        }
        public static async Task<bool> DownloadDLL(string url)
        {
            try
            {
                using WebClient client = new();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadCallBack);
                client.DownloadFileAsync(new Uri(url), "BepInEx/plugins/TownOfHost.dll");
                while (client.IsBusy) await Task.Delay(1);
                ShowPopup(GetString("updateRestart"), true);
            }
            catch (Exception ex)
            {
                Logger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL", false);
                ShowPopup(GetString("updateManually"), true);
                return false;
            }
            return true;
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false)
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show(message);
                var button = InfoPopup.transform.FindChild("ExitGame");
                if (button != null)
                {
                    button.gameObject.SetActive(showButton);
                    button.GetChild(0).GetComponent<TextTranslatorTMP>().TargetText = StringNames.QuitLabel;
                    button.GetComponent<PassiveButton>().OnClick = new();
                    button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.Quit()));
                }
            }
        }
    }
}