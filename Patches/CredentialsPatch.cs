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
            if (DebugModeManager.IsDebugMode) __instance.text.text += "\r\n" + Utils.ColorString(Color.green, "デバッグモード");

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
        static void Postfix(VersionShower __instance)
        {
            Main.credentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
#if DEBUG
            Main.credentialsText += $"\r\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
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
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    class TitleLogoPatch
    {
        static void Postfix(MainMenuManager __instance)
        {
            var amongUsLogo = GameObject.Find("bannerLogo_AmongUs");
            if (amongUsLogo != null)
            {
                amongUsLogo.transform.localScale *= 0.4f;
                amongUsLogo.transform.position += Vector3.up * 0.25f;
            }

            var tohLogo = new GameObject("titleLogo_TOH");
            tohLogo.transform.position = Vector3.up;
            tohLogo.transform.localScale *= 1.2f;
            var renderer = tohLogo.AddComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite("TownOfHost.Resources.TownOfHost-Logo.png", 300f);
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
            var offset_y = HudManager.InstanceExists && HudManager._instance.MapButton.isVisible ? 1.6f : 0.9f;
            __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
        }
    }
}
