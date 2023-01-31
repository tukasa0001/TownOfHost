using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            __instance.text.text += Main.credentialsText;
            if (Options.NoGameEnd.GetBool()) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("NoGameEnd"));
            if (Options.IsStandardHAS) __instance.text.text += $"\r\n" + Utils.ColorString(Color.yellow, GetString("StandardHAS"));
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("HideAndSeek"));
            if (!GameStates.IsModHost) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("Warning.NoModHost"));
            if (DebugModeManager.IsDebugMode) __instance.text.text += "\r\n" + Utils.ColorString(Color.green, GetString("DebugMode"));

            var offset_x = 1.2f; //右端からのオフセット
            if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            if (!GameStates.IsLobby) return;
            if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin"));
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerStartPatch
    {
        public static GameObject OVersionShower;
        static TMPro.TextMeshPro SpecialEventText;
        static void Postfix(VersionShower __instance)
        {

            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
#if DEBUG
            Main.credentialsText += $"\r\n<color={Main.ModColor}>内测({ThisAssembly.Git.Commit})</color>";
#endif

#if RELEASE
            string additionalCredentials = GetString("TextBelowVersionText");
            if (additionalCredentials != null && additionalCredentials != "空")
            {
                Main.credentialsText += $"\n<color=#569bc2><size=1.4>{additionalCredentials}</size></color>";
            }
#endif
            var credentials = Object.Instantiate(__instance.text);
            credentials.text = Main.credentialsText;
            credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
            credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

            ErrorText.Create(__instance.text);
            if (Main.hasArgumentException && ErrorText.Instance != null)
            {
                ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
            }

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
            else
            {
                SpecialEventText.text = $"{Main.MainMenuText}";
                SpecialEventText.fontSize = 0.9f;
                SpecialEventText.color = Color.white;
                SpecialEventText.alignment = TMPro.TextAlignmentOptions.TopRight;
                SpecialEventText.transform.position = new Vector3(4.6f, 2.725f, 0);
            }

            if ((OVersionShower = GameObject.Find("VersionShower")) != null)
            {
                OVersionShower.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                OVersionShower.transform.position = new Vector3(-5.3f, 2.9f, 0f);
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    class TitleLogoPatch
    {
        public static GameObject amongUsLogo;
        public static GameObject PlayLocalButton;
        public static GameObject PlayOnlineButton;
        public static GameObject HowToPlayButton;
        public static GameObject FreePlayButton;
        public static GameObject BottomButtons;

        static void Postfix(MainMenuManager __instance)
        {
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

            var tohLogo = new GameObject("titleLogo_TOH");
            tohLogo.transform.position = Vector3.up;
            tohLogo.transform.position -= Vector3.up * 0.20f;
            tohLogo.transform.localScale *= 1.3f;
            var renderer = tohLogo.AddComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite("TownOfHost.Resources.TownOfHost-Logo.png", 300f);
            
            var bq = new GameObject("title_BQ");
            bq.transform.position = new Vector3(4.1f, -2f, 0f);
            bq.transform.localScale *= 1.8f;
            var bqRenderer = bq.AddComponent<SpriteRenderer>();
            bqRenderer.sprite = Utils.LoadSprite("TownOfHost.Resources.BQ.png", 300f);
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModManagerLateUpdatePatch
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
}