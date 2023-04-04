using HarmonyLib;
using System.Text;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
internal class PingTrackerUpdatePatch
{
    private static readonly StringBuilder sb = new();

    private static void Postfix(PingTracker __instance)
    {
        __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;

        sb.Clear();

        sb.Append(Main.credentialsText);

        if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
        if (Options.AllowConsole.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("AllowConsole")));
        if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
        if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, GetString("DebugMode")));

        var offset_x = 1.2f; //右端からのオフセット
        if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
        if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

        __instance.text.text += sb.ToString();
    }
}
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
internal class VersionShowerStartPatch
{
    public static GameObject OVersionShower;
    private static TMPro.TextMeshPro SpecialEventText;
    private static TMPro.TextMeshPro VisitText;

    private static void Postfix(VersionShower __instance)
    {

        Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
        if (Main.IsAprilFools) Main.credentialsText = $"\r\n<color=#00bfff>Town Of Host</color> v11.45.14";
#if DEBUG
        Main.credentialsText += $"\r\n<color={Main.ModColor}>内测({ThisAssembly.Git.Commit})</color>";
#endif

#if RELEASE
        string additionalCredentials = GetString("TextBelowVersionText");
        if (additionalCredentials != null && additionalCredentials != "*TextBelowVersionText")
        {
            Main.credentialsText += $"\n{additionalCredentials}";
        }
#endif
        var credentials = Object.Instantiate(__instance.text);
        credentials.text = Main.credentialsText;
        credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
        credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

        ErrorText.Create(__instance.text);
        if (Main.hasArgumentException && ErrorText.Instance != null)
            ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);

        if (SpecialEventText == null)
        {
            SpecialEventText = Object.Instantiate(__instance.text);
            SpecialEventText.text = "";
            SpecialEventText.color = Color.white;
            SpecialEventText.fontSize += 2.5f;
            SpecialEventText.alignment = TMPro.TextAlignmentOptions.Top;
            SpecialEventText.transform.position = new Vector3(0, 0.5f, 0);
        }
        SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
        if (Main.IsInitialRelease)
        {
            SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
            ColorUtility.TryParseHtmlString(Main.ModColor, out var col);
            SpecialEventText.color = col;
        }
        else if (!Main.IsAprilFools)
        {
            SpecialEventText.text = $"{Main.MainMenuText}";
            SpecialEventText.fontSize = 0.9f;
            SpecialEventText.color = Color.white;
            SpecialEventText.alignment = TMPro.TextAlignmentOptions.TopRight;
            SpecialEventText.transform.position = new Vector3(4.6f, 2.725f, 0);
        }

        if ((OVersionShower = GameObject.Find("VersionShower")) != null && !Main.IsAprilFools)
        {
            OVersionShower.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            OVersionShower.transform.position = new Vector3(-5.3f, 2.9f, 0f);
            if (TitleLogoPatch.amongUsLogo != null)
            {
                if (VisitText == null)
                {
                    VisitText = Object.Instantiate(__instance.text);
                    VisitText.text = string.Format(GetString("TOHEVisitorCount"), Main.ModColor, ModUpdater.visit);
                    VisitText.color = Color.white;
                    VisitText.fontSize = 1.2f;
                    //VisitText.alignment = TMPro.TextAlignmentOptions.Top;
                    OVersionShower.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                    VisitText.transform.position = new Vector3(-5.3f, 2.75f, 0f);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
internal class TitleLogoPatch
{
    public static GameObject Ambience;
    public static GameObject amongUsLogo;
    public static GameObject PlayLocalButton;
    public static GameObject PlayOnlineButton;
    public static GameObject HowToPlayButton;
    public static GameObject FreePlayButton;
    public static GameObject BottomButtons;

    private static void Postfix(MainMenuManager __instance)
    {
        if (Main.IsAprilFools)
        {
            if ((amongUsLogo = GameObject.Find("bannerLogo_AmongUs")) != null)
            {
                amongUsLogo.transform.localScale *= 0.4f;
                amongUsLogo.transform.position += Vector3.up * 0.25f;
            }

            var tohLogo = new GameObject("titleLogo_TOH");
            tohLogo.transform.position = Vector3.up;
            tohLogo.transform.localScale *= 1.2f;
            var renderer = tohLogo.AddComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite("TOHE.Resources.TownOfHost-Logo.png", 300f);

            return;
        }

        if ((amongUsLogo = GameObject.Find("bannerLogo_AmongUs")) != null)
        {
            amongUsLogo.transform.localScale *= 0.4f;
            amongUsLogo.transform.position += Vector3.up * 0.25f;
        }

        if ((PlayLocalButton = GameObject.Find("PlayLocalButton")) != null)
        {
            PlayLocalButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            PlayLocalButton.transform.position = new Vector3(-0.76f, -2.1f, 0f);
        }

        if ((PlayOnlineButton = GameObject.Find("PlayOnlineButton")) != null)
        {
            PlayOnlineButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            PlayOnlineButton.transform.position = new Vector3(0.725f, -2.1f, 0f);
        }

        if ((HowToPlayButton = GameObject.Find("HowToPlayButton")) != null)
        {
            HowToPlayButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            HowToPlayButton.transform.position = new Vector3(-2.225f, -2.175f, 0f);
        }

        if ((FreePlayButton = GameObject.Find("FreePlayButton")) != null)
        {
            FreePlayButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            FreePlayButton.transform.position = new Vector3(2.1941f, -2.175f, 0f);
        }

        if ((BottomButtons = GameObject.Find("BottomButtons")) != null)
        {
            BottomButtons.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            BottomButtons.transform.position = new Vector3(0f, -2.71f, 0f);
        }

        if ((Ambience = GameObject.Find("Ambience")) != null)
        {
            Ambience.SetActive(false);
            var CustomBG = new GameObject("CustomBG");
            CustomBG.transform.position = new Vector3(0, 0, 520f);
            var bgRenderer = CustomBG.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = Utils.LoadSprite("TOHE.Resources.TOHE-BG.jpg", 179f);
        }
    }
}
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
internal class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

        LateTask.Update(Time.deltaTime);
        CheckMurderPatch.Update();
    }
    public static void Postfix(ModManager __instance)
    {
        var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
    }
}