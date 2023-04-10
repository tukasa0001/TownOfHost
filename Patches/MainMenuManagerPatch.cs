using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.IL2CPP.Utils.Collections;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnhollowerBaseLib;
using UnityEngine;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch]
public class MainMenuManagerPatch
{
    public static GameObject template;
    public static GameObject qqButton;
    public static GameObject discordButton;
    public static GameObject updateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        if (template == null) template = GameObject.Find("/MainUI/ExitGameButton");
        if (template == null) return;

        if (CultureInfo.CurrentCulture.Name == "zh-CN")
        {
            //生成QQ群按钮
            if (qqButton == null) qqButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
            qqButton.name = "qqButton";
            qqButton.transform.position = Vector3.Reflect(template.transform.position, Vector3.left);

            var qqText = qqButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Color qqColor = new Color32(0, 164, 255, byte.MaxValue);
            PassiveButton qqPassiveButton = qqButton.GetComponent<PassiveButton>();
            SpriteRenderer qqButtonSprite = qqButton.GetComponent<SpriteRenderer>();
            qqPassiveButton.OnClick = new();
            qqPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL(Main.QQInviteUrl)));
            qqPassiveButton.OnMouseOut.AddListener((Action)(() => qqButtonSprite.color = qqText.color = qqColor));
            __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => qqText.SetText("QQ群"))));
            qqButtonSprite.color = qqText.color = qqColor;
            qqButton.gameObject.SetActive(Main.ShowQQButton && !Main.IsAprilFools);
        }
        else
        {
            //Discordボタンを生成
            if (discordButton == null) discordButton = Object.Instantiate(template, template.transform.parent);
            discordButton.name = "DiscordButton";
            discordButton.transform.position = Vector3.Reflect(template.transform.position, Vector3.left);

            var discordText = discordButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Color discordColor = new Color32(86, 98, 246, byte.MaxValue);
            PassiveButton discordPassiveButton = discordButton.GetComponent<PassiveButton>();
            SpriteRenderer discordButtonSprite = discordButton.GetComponent<SpriteRenderer>();
            discordPassiveButton.OnClick = new();
            discordPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL(Main.DiscordInviteUrl)));
            discordPassiveButton.OnMouseOut.AddListener((Action)(() => discordButtonSprite.color = discordText.color = discordColor));
            __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => discordText.SetText("Discord"))));
            discordButtonSprite.color = discordText.color = discordColor;
            discordButton.gameObject.SetActive(Main.ShowDiscordButton && !Main.IsAprilFools);
        }

        //Updateボタンを生成
        if (updateButton == null) updateButton = Object.Instantiate(template, template.transform.parent);
        updateButton.name = "UpdateButton";
        updateButton.transform.position = template.transform.position + new Vector3(0.25f, 0.75f);
        updateButton.transform.GetChild(0).GetComponent<RectTransform>().localScale *= 1.5f;

        var updateText = updateButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
        Color updateColor = new Color32(247, 56, 23, byte.MaxValue);
        PassiveButton updatePassiveButton = updateButton.GetComponent<PassiveButton>();
        SpriteRenderer updateButtonSprite = updateButton.GetComponent<SpriteRenderer>();
        updatePassiveButton.OnClick = new();
        updatePassiveButton.OnClick.AddListener((Action)(() =>
        {
            updateButton.SetActive(false);
            ModUpdater.StartUpdate(ModUpdater.downloadUrl);
        }));
        updatePassiveButton.OnMouseOut.AddListener((Action)(() => updateButtonSprite.color = updateText.color = updateColor));
        updateButtonSprite.color = updateText.color = updateColor;
        updateButtonSprite.size *= 1.5f;
        updateButton.SetActive(false);

#if RELEASE
            //フリープレイの無効化
            var freeplayButton = GameObject.Find("/MainUI/FreePlayButton");
            if (freeplayButton != null)
            {
                freeplayButton.GetComponent<PassiveButton>().OnClick = new();
                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.OpenURL("https://github.com/KARPED1EM/TownOfHostEdited")));
                __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => freeplayButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().SetText("GitHub"))));
            }
#endif

        if (Main.IsAprilFools) return;

        var bottomTemplate = GameObject.Find("InventoryButton");
        if (bottomTemplate == null) return;

        var HorseButton = Object.Instantiate(bottomTemplate, bottomTemplate.transform.parent);
        var passiveHorseButton = HorseButton.GetComponent<PassiveButton>();
        var spriteHorseButton = HorseButton.GetComponent<SpriteRenderer>();
        if (HorseModePatch.isHorseMode) spriteHorseButton.transform.localScale *= -1;

        spriteHorseButton.sprite = Utils.LoadSprite($"TOHE.Resources.Images.HorseButton.png", 75f);
        passiveHorseButton.OnClick = new ButtonClickedEvent();
        passiveHorseButton.OnClick.AddListener((Action)(() =>
        {
            spriteHorseButton.transform.localScale *= -1;
            HorseModePatch.isHorseMode = !HorseModePatch.isHorseMode;
            var particles = Object.FindObjectOfType<PlayerParticles>();
            if (particles != null)
            {
                particles.pool.ReclaimAll();
                particles.Start();
            }
        }));

        var CreditsButton = Object.Instantiate(bottomTemplate, bottomTemplate.transform.parent);
        var passiveCreditsButton = CreditsButton.GetComponent<PassiveButton>();
        var spriteCreditsButton = CreditsButton.GetComponent<SpriteRenderer>();

        spriteCreditsButton.sprite = Utils.LoadSprite($"TOHE.Resources.Images.CreditsButton.png", 75f);
        passiveCreditsButton.OnClick = new ButtonClickedEvent();
        passiveCreditsButton.OnClick.AddListener((Action)(() =>
        {
            CredentialsPatch.LogoPatch.CreditsPopup?.SetActive(true);
        }));

        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
    }
}

// 来源：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/HorseModePatch.cs
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldHorseAround))]
public static class HorseModePatch
{
    public static bool isHorseMode = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isHorseMode;
        return false;
    }
}

// 参考：https://github.com/Yumenopai/TownOfHost_Y
public class ModNews
{
    public int Number;
    public uint Lang;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,
            Language = Lang,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
}

[HarmonyPatch]
public class ModNewsHistory
{
    public static List<ModNews> AllModNews = new();

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
    public static void Initialize(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        static IEnumerator GetEnumerator()
        {
            while (AnnouncementPopUp.UpdateState == AnnouncementPopUp.AnnounceState.Fetching) yield return null;
            if (AnnouncementPopUp.UpdateState > AnnouncementPopUp.AnnounceState.Fetching && DataManager.Player.Announcements.AllAnnouncements.Count > 0) yield break;

            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.Fetching;
            AllModNews.Clear();

            var lang = DataManager.Settings.Language.CurrentLanguage.ToString();
            if (!Assembly.GetExecutingAssembly().GetManifestResourceNames().Any(x => x.StartsWith($"TOHE.Resources.ModNews.{lang}.")))
                lang = SupportedLangs.English.ToString();

            var fileNames = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith($"TOHE.Resources.ModNews.{lang}."));
            foreach (var file in fileNames)
                AllModNews.Add(GetContentFromRes(file));

            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
        }

        __result = Effects.Sequence(GetEnumerator().WrapToIl2Cpp(), __result);
    }

    public static ModNews GetContentFromRes(string path)
    {
        ModNews mn = new();
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        string text = "";
        uint langId = (uint)DataManager.Settings.Language.CurrentLanguage;
        //uint langId = (uint)SupportedLangs.SChinese;
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (line.StartsWith("#Number:")) mn.Number = int.Parse(line.Replace("#Number:", string.Empty));
            else if (line.StartsWith("#LangId:")) langId = uint.Parse(line.Replace("#LangId:", string.Empty));
            else if (line.StartsWith("#Title:")) mn.Title = line.Replace("#Title:", string.Empty);
            else if (line.StartsWith("#SubTitle:")) mn.SubTitle = line.Replace("#SubTitle:", string.Empty);
            else if (line.StartsWith("#ShortTitle:")) mn.ShortTitle = line.Replace("#ShortTitle:", string.Empty);
            else if (line.StartsWith("#Date:")) mn.Date = line.Replace("#Date:", string.Empty);
            else if (line.StartsWith("#---")) continue;
            else
            {
                if (line.StartsWith("## ")) line = line.Replace("## ", "<b>") + "</b>";
                else if (line.StartsWith("- ")) line = line.Replace("- ", "・");
                text += $"\n{line}";
            }
        }
        mn.Lang = langId;
        mn.Text = text;
        Logger.Info($"Number:{mn.Number}", "ModNews");
        Logger.Info($"Title:{mn.Title}", "ModNews");
        Logger.Info($"SubTitle:{mn.SubTitle}", "ModNews");
        Logger.Info($"ShortTitle:{mn.ShortTitle}", "ModNews");
        Logger.Info($"Date:{mn.Date}", "ModNews");
        return mn;
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] Il2CppReferenceArray<Announcement> aRange)
    {
        List<Announcement> list = new();
        foreach (var a in aRange) list.Add(a);
        foreach (var m in AllModNews) list.Add(m.ToAnnouncement());
        list.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        __instance.allAnnouncements = new Il2CppSystem.Collections.Generic.List<Announcement>();
        foreach (var a in list) __instance.allAnnouncements.Add(a);


        __instance.HandleChange();
        __instance.OnAddAnnouncement?.Invoke();

        return false;
    }
}
