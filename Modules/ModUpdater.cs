using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Twitch;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class ModUpdaterButton
    {
        private static void Prefix(MainMenuManager __instance)
        {
            ModUpdater.LaunchUpdater();
            var template = GameObject.Find("ExitGameButton");
            if (template == null) return;
            //Discordボタンを生成
            var discordButton = UnityEngine.Object.Instantiate(template, null);
            discordButton.transform.localPosition = new Vector3(discordButton.transform.localPosition.x, discordButton.transform.localPosition.y + 0.6f, discordButton.transform.localPosition.z);

            PassiveButton passiveDiscordButton = discordButton.GetComponent<PassiveButton>();
            passiveDiscordButton.OnClick = new Button.ButtonClickedEvent();
            passiveDiscordButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => Application.OpenURL("https://discord.gg/W5ug6hXB9V")));

            var discordText = discordButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) =>
            {
                discordText.SetText("Discord");
            })));

            SpriteRenderer buttonSpriteDiscord = discordButton.GetComponent<SpriteRenderer>();
            Color discordColor = new Color32(88, 101, 242, byte.MaxValue);
            buttonSpriteDiscord.color = discordText.color = discordColor;
            passiveDiscordButton.OnMouseOut.AddListener((System.Action)delegate
            {
                buttonSpriteDiscord.color = discordText.color = discordColor;
            });
            //以下アップデートがあれば実行
            if (!ModUpdater.hasUpdate) return;
            //アップデートボタンを生成
            var updateButton = UnityEngine.Object.Instantiate(template, null);
            updateButton.transform.localPosition = new Vector3(updateButton.transform.localPosition.x, updateButton.transform.localPosition.y + 1.2f, updateButton.transform.localPosition.z);

            PassiveButton passiveUpdateButton = updateButton.GetComponent<PassiveButton>();
            passiveUpdateButton.OnClick = new Button.ButtonClickedEvent();
            passiveUpdateButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                ModUpdater.ExecuteUpdate();
                updateButton.SetActive(false);
            }));

            var updateText = updateButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) =>
            {
                updateText.SetText(getString("updateButton"));
            })));

            SpriteRenderer buttonSpriteUpdate = updateButton.GetComponent<SpriteRenderer>();
            Color updateColor = new Color32(0, 191, 255, byte.MaxValue);
            buttonSpriteUpdate.color = updateText.color = updateColor;
            passiveUpdateButton.OnMouseOut.AddListener((System.Action)delegate
            {
                buttonSpriteUpdate.color = updateText.color = updateColor;
            });

            TwitchManager man = DestroyableSingleton<TwitchManager>.Instance;
            ModUpdater.InfoPopup = UnityEngine.Object.Instantiate<GenericPopup>(man.TwitchPopup);
            ModUpdater.InfoPopup.TextAreaTMP.fontSize *= 0.7f;
            ModUpdater.InfoPopup.TextAreaTMP.enableAutoSizing = false;
        }
    }

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
    public static class Announcement
    {
        public static bool Prefix(AnnouncementPopUp __instance)
        {
            var text = __instance.AnnounceTextMeshPro;
            text.text = ModUpdater.announcement;
            return false;
        }
    }

    public class ModUpdater
    {
        public static bool running = false;
        public static bool hasUpdate = false;
        public static string updateURI = null;
        private static Task updateTask = null;
        public static string announcement = "";
        public static GenericPopup InfoPopup;

        public static void LaunchUpdater()
        {
            if (running) return;
            running = true;
            checkForUpdate().GetAwaiter().GetResult();
            clearOldVersions();
            if (hasUpdate || main.ShowPopUpVersion.Value != main.PluginVersion)
            {
                DestroyableSingleton<MainMenuManager>.Instance.Announcement.gameObject.SetActive(true);
                main.ShowPopUpVersion.Value = main.PluginVersion;
            }
        }

        public static void ExecuteUpdate()
        {
            string info = getString("updatePleaseWait");
            ModUpdater.InfoPopup.Show(info);
            if (updateTask == null)
            {
                if (updateURI != null)
                {
                    updateTask = downloadUpdate();
                }
                else
                {
                    info = getString("updateManually");
                }
            }
            else
            {
                info = getString("updateInProgress");
            }
            ModUpdater.InfoPopup.StartCoroutine(Effects.Lerp(0.01f, new System.Action<float>((p) => { ModUpdater.setPopupText(info); })));
        }

        public static void clearOldVersions()
        {
            try
            {
                DirectoryInfo d = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath) + @"\BepInEx\plugins");
                string[] files = d.GetFiles("*.old").Select(x => x.FullName).ToArray();
                foreach (string f in files)
                    File.Delete(f);
            }
            catch (System.Exception e)
            {
                Logger.error("Exception occurred when clearing old versions:\n" + e, "ModUpdater");
            }
        }

        public static async Task<bool> checkForUpdate()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            try
            {
                HttpClient http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "TownOfHost Updater");
                var response = await http.GetAsync(new System.Uri("https://api.github.com/repos/tukasa0001/TownOfHost/releases/latest"), HttpCompletionOption.ResponseContentRead);
                if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
                {
                    Logger.error("Server returned no data: " + response.StatusCode.ToString(), "ModUpdater");
                    return false;
                }
                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);

                string tagname = data["tag_name"]?.ToString();
                if (tagname == null)
                {
                    return false;
                }

                string changeLog = data["body"]?.ToString();
                if (changeLog != null) announcement = changeLog;

                System.Version ver = System.Version.Parse(tagname.Replace("v", ""));
                int diff = main.version.CompareTo(ver);
                if (diff < 0)
                {
                    hasUpdate = true;
                    announcement = string.Format(getString("announcementUpdate"), ver, announcement);

                    JToken assets = data["assets"];
                    if (!assets.HasValues)
                        return false;

                    for (JToken current = assets.First; current != null; current = current.Next)
                    {
                        string browser_download_url = current["browser_download_url"]?.ToString();
                        if (browser_download_url != null && current["content_type"] != null)
                        {
                            if (current["content_type"].ToString().Equals("application/x-msdownload") &&
                                browser_download_url.EndsWith(".dll"))
                            {
                                updateURI = browser_download_url;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    announcement = string.Format(getString("announcementChangelog"), ver, announcement);
                }
            }
            catch (System.Exception ex)
            {
                Logger.error(ex.ToString(), "ModUpdater");
            }
            return false;
        }

        public static async Task<bool> downloadUpdate()
        {
            try
            {
                HttpClient http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "TownOfHost Updater");
                var response = await http.GetAsync(new System.Uri(updateURI), HttpCompletionOption.ResponseContentRead);
                if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
                {
                    Logger.error("Server returned no data: " + response.StatusCode.ToString(), "ModUpdater");
                    return false;
                }
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                System.UriBuilder uri = new System.UriBuilder(codeBase);
                string fullname = System.Uri.UnescapeDataString(uri.Path);
                if (File.Exists(fullname + ".old"))
                    File.Delete(fullname + ".old");

                File.Move(fullname, fullname + ".old");

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    using (var fileStream = File.Create(fullname))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                }
                showPopup(getString("updateRestart"));
                return true;
            }
            catch (System.Exception ex)
            {
                Logger.error(ex.ToString(), "ModUpdater");
            }
            showPopup(getString("updateFailed"));
            return false;
        }
        private static void showPopup(string message)
        {
            setPopupText(message);
            InfoPopup.gameObject.SetActive(true);
        }

        public static void setPopupText(string message)
        {
            if (InfoPopup == null)
                return;
            if (InfoPopup.TextAreaTMP != null)
            {
                InfoPopup.TextAreaTMP.text = message;
            }
        }
    }
}