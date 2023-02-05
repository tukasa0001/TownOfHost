using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using VentLib.Localization;
using VentLib.Logging;


namespace TownOfHost;

[HarmonyPatch]
public class ModUpdater
{
    private static readonly string URL = "https://api.github.com/repos/music-discussion/TownOfHost-TheOtherRoles--TOH-TOR";
    public static bool hasUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = true;
    public static Version latestVersion = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static readonly bool ForceAccept = true;
    public static GenericPopup InfoPopup;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    [HarmonyPriority(2)]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        DeleteOldDLL();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "InfoPopup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
        if (ForceAccept)
        {
            isBroken = false;
            isChecked = true;
            hasUpdate = false;
        }
        if (!isChecked)
        {
            /*CheckRelease(TOHPlugin.BetaBuildURL.Value != "").GetAwaiter().GetResult();*/
        }
        if (ForceAccept)
        {
            isBroken = false;
            isChecked = true;
            hasUpdate = false;
        }
        MainMenuManagerPatch.updateButton.SetActive(hasUpdate);
        MainMenuManagerPatch.updateButton.transform.position = MainMenuManagerPatch.template.transform.position + new Vector3(0.25f, 0.75f);
        __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) =>
        {
            MainMenuManagerPatch.updateButton.transform
                .GetChild(0).GetComponent<TMPro.TMP_Text>()
                .SetText($"{Localizer.Get("ModUpdater.UpdateButton")}\n{latestTitle}");
        })));
        if (ForceAccept)
        {
            isBroken = false;
            isChecked = true;
            hasUpdate = false;
        }
    }
    /*public static async Task<bool> CheckRelease(bool beta = false)
    {
        string url = beta ? TOHPlugin.BetaBuildURL.Value : URL + "/releases/latest";
        try
        {
            string result;
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost Updater");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    VentLogger.Error($"ステータスコード: {response.StatusCode}", "CheckRelease");
                    return false;
                }
                result = await response.Content.ReadAsStringAsync();
            }
            JObject data = JObject.Parse(result);
            if (beta)
            {
                latestTitle = data["name"].ToString();
                downloadUrl = data["url"].ToString();
                hasUpdate = false; /*latestTitle != ThisAssembly.Git.Commit;#1#
            }
            else
            {
                latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                latestTitle = $"Ver. {latestVersion}";
                JArray assets = data["assets"].Cast<JArray>();
                for (int i = 0; i < assets.Count; i++)
                {
                    if (assets[i]["name"].ToString() == "TownOfHost_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TownOfHost_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TownOfHost.dll")
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                }
                /*hasUpdate = latestVersion.CompareTo(TOHPlugin) > 0;#1#
                hasUpdate = false;
            }
            if (downloadUrl == null)
            {
                VentLogger.Error("ダウンロードURLを取得できませんでした。", "CheckRelease");
                return false;
            }
            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            var flag = false;
            isBroken = true;
            if (ForceAccept)
            {
                flag = true;
                isBroken = false;
                isChecked = true;
                hasUpdate = false;
            }
            VentLogger.Error($"リリースのチェックに失敗しました。 / Release check failed. \n{ex}", "CheckRelease");
            return flag;
        }
        return true;
    }*/
    public static void StartUpdate(string url)
    {
        ShowPopup(Localizer.Get("ModUpdater.WaitMessage"));
        if (!BackupDLL())
        {
            ShowPopup(Localizer.Get("ModUpdater.UpdateManually"), true);
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
            VentLogger.Error("バックアップに失敗しました", "BackupDLL");
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
                VentLogger.Old($"{Path.GetFileName(path)}を削除", "DeleteOldDLL");
                File.Delete(path);
            }
        }
        catch
        {
            VentLogger.Error("削除に失敗しました", "DeleteOldDLL");
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
            while (client.IsBusy) await System.Threading.Tasks.Task.Delay(1);
            ShowPopup(Localizer.Get("ModUpater.UpdateRestart"), true);
        }
        catch (Exception ex)
        {
            VentLogger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL");
            ShowPopup(Localizer.Get("ModUpdater.UpdateManually"), true);
            return false;
        }
        return true;
    }
    private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
    {
        ShowPopup($"{Localizer.Get("ModUpdater.UpdateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
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