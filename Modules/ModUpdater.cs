using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE
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
        public static string MD5 = null;
        public static GenericPopup InfoPopup;
        public static int visit = 0;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
        [HarmonyPriority(2)]
        public static void Start_Prefix(MainMenuManager __instance)
        {
            NewVersionCheck();
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

        public static string UrlSetId(string url) => url + "?id=2494EF351D6844E496596C3B78F93519";
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
            string url = UrlSetId(UrlSetCheck(URL)) + "&version=" + Main.PluginVersion;
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
                    if (c > m) hasUpdate = true;
                    if (c != m) break;
                }

                url = UrlSetId(UrlSetInfo(URL)) + "&data=remark|notice|md5|visit";
                string[] data = Get(url).Split("|");
                int create = int.Parse(data[0]);
                MD5 = data[2];
                visit = int.TryParse(data[3], out int x) ? x : 0;
                visit += 25457;
                if (create > Main.PluginCreate)
                {
                    hasUpdate = true;
                    forceUpdate = true;
                }

#if DEBUG
                if (!hasUpdate && Main.PluginVersion == info[4])
                {
                    hasUpdate = true;
                    forceUpdate = false;
                }
#endif
                if (!Main.AlreadyShowMsgBox || create == 0)
                {
                    Main.AlreadyShowMsgBox = true;
                    ShowPopup(data[1], true, create == 0);
                }

                Logger.Info("hasupdate: " + info[0], "2018k");
                Logger.Info("forceupdate: " + info[1], "2018k");
                Logger.Info("downloadUrl: " + info[3], "2018k");
                Logger.Info("latestVersionl: " + info[4], "2018k");
                Logger.Info("remark: " + data[0], "2018k");
                Logger.Info("notice: " + data[1], "2018k");
                Logger.Info("MD5: " + data[2], "2018k");
                Logger.Info("Visit: " + data[3], "2018k");

                if (downloadUrl == null && downloadUrl == "")
                {
                    Logger.Error("获取下载地址失败", "CheckRelease");
                    return Task.FromResult(false);
                }

                isChecked = true;

                if (GetMD5HashFromFile("BepInEx/plugins/TOHE.dll") != MD5 && Main.Dev) isBroken = true;
                else isBroken = false;

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
        public static bool NewVersionCheck()
        {
            try
            {
                var fileName = Assembly.GetExecutingAssembly().Location;
                if (fileName.Contains("TOHE.dll"))
                {
                    var newFileName = Directory.GetParent(fileName).FullName + @"\TOHE.dll";
                    File.Move(fileName, newFileName);
                    Logger.Warn("更名自文件为：TOHE.dll", "NewVersionCheck");
                }
                if (Directory.Exists("TOH_DATA") && File.Exists(@"./TOHE_DATA/BanWords.txt"))
                {
                    DirectoryInfo di = new("TOH_DATA");
                    di.Delete(true);
                    Logger.Warn("删除旧数据：TOH_DATA", "NewVersionCheck");
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "NewVersionCheck");
                return false;
            }
            return true;
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
        public static bool BackOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
                {
                    Logger.Info($"{Path.GetFileName(path)}を削除", "BackOldDLL");
                    File.Delete(path);
                }
                File.Move(Assembly.GetExecutingAssembly().Location + ".bak", Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                Logger.Error("回退老版本失败", "BackOldDLL");
                return false;
            }
            return true;
        }
        public static void DeleteOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.*"))
                {
                    if (path.EndsWith(Path.GetFileName(Assembly.GetExecutingAssembly().Location))) continue;
                    if (path.EndsWith("TOHE.dll")) continue;
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
                client.DownloadFileAsync(new Uri(url), "BepInEx/plugins/TOHE.dll");
                while (client.IsBusy) await Task.Delay(1);
                if (GetMD5HashFromFile("BepInEx/plugins/TOHE.dll") != MD5)
                {
                    BackOldDLL();
                    ShowPopup(GetString("downloadFailed"), true, false);
                    MainMenuManagerPatch.updateButton.SetActive(true);
                    MainMenuManagerPatch.updateButton.transform.position = MainMenuManagerPatch.template.transform.position + new Vector3(0.25f, 0.75f);
                }
                else
                {
                    ShowPopup(GetString("updateRestart"), true);
                }

            }
            catch (Exception ex)
            {
                Logger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL", false);
                ShowPopup(GetString("updateManually"), true);
                return false;
            }
            return true;
        }
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false, bool buttonIsExit = true)
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show(message);
                var button = InfoPopup.transform.FindChild("ExitGame");
                if (button != null)
                {
                    button.gameObject.SetActive(showButton);
                    button.GetChild(0).GetComponent<TextTranslatorTMP>().TargetText = buttonIsExit ? StringNames.QuitLabel : StringNames.OK;
                    button.GetComponent<PassiveButton>().OnClick = new();
                    if (buttonIsExit) button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.Quit()));
                    else button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => InfoPopup.Close()));
                }
            }
        }
    }
}