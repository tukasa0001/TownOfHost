using System.Globalization;
using HarmonyLib;
using TownOfHost.Patches.Actions;
using UnityEngine;
using TownOfHost.Roles;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerUpdatePatch
    {
        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            __instance.text.text += TOHPlugin.credentialsText;
            if (TOHPlugin.NoGameEnd) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("NoGameEnd"));
            if (OldOptions.IsStandardHAS) __instance.text.text += $"\r\n" + Utils.ColorString(Color.yellow, GetString("StandardHAS"));
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("HideAndSeek"));
            if (DebugModeManager.IsDebugMode) __instance.text.text += "\r\n" + Utils.ColorString(Color.green, "デバッグモード");

            var offset_x = 1.2f; //右端からのオフセット
            if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
            if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
            __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

            if (!GameStates.IsLobby) return;
            if (OldOptions.IsStandardHAS && !CustomRoleManager.Static.Sheriff.IsEnable() && !CustomRoleManager.Static.SerialKiller.IsEnable() && CustomRoleManager.Static.Egoist.IsEnable()) // Egoist
                __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin"));
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerStartPatch
    {
        static TMPro.TextMeshPro SpecialEventText;
        static void Postfix(VersionShower __instance)
        {
            TOHPlugin.credentialsText = $"\r\n<color={TOHPlugin.ModColor}>{TOHPlugin.ModName}</color> v{TOHPlugin.PluginVersion}" + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "");
#if DEBUG
            TOHPlugin.credentialsText += $"\r\n<color={TOHPlugin.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
            var credentials = Object.Instantiate(__instance.text);
            credentials.text = TOHPlugin.credentialsText;
            credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
            credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

            ErrorText.Create(__instance.text);
            if (TOHPlugin.hasArgumentException && ErrorText.Instance != null)
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
            if (TOHPlugin.IsInitialRelease)
            {
                SpecialEventText.text = $"Happy Birthday to {TOHPlugin.ModName}!";
                ColorUtility.TryParseHtmlString(TOHPlugin.ModColor, out var col);
                SpecialEventText.color = col;
            }
            if (TOHPlugin.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
            {
                //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
                //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
                SpecialEventText.text = "何とは言いませんが、特別な日ですね。\n<size=15%>\n\n末永く爆発しろ</size>";
                SpecialEventText.color = Utils.GetRoleColor(Lovers.Ref<Lovers>());
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    class TitleLogoPatch
    {
        public static GameObject amongUsLogo;
        static void Postfix(MainMenuManager __instance)
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
            renderer.sprite = Utils.LoadSprite("TownOfHost.assets.tohtor-logo-rold.png", 300f);
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModManagerLateUpdatePatch
    {
        public static void Prefix(ModManager __instance)
        {
            __instance.ShowModStamp();

            DTask.Update(Time.deltaTime);
            MurderPatches.CheckMurderPatch.Update();
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
