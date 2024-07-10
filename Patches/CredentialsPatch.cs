using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public static SpriteRenderer TohLogo { get; private set; }
        private static TextMeshPro pingTrackerCredential = null;
        private static AspectPosition pingTrackerCredentialAspectPos = null;

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        class PingTrackerUpdatePatch
        {
            static StringBuilder sb = new();
            static void Postfix(PingTracker __instance)
            {
                if (pingTrackerCredential == null)
                {
                    var uselessPingTracker = Object.Instantiate(__instance, __instance.transform.parent);
                    pingTrackerCredential = uselessPingTracker.GetComponent<TextMeshPro>();
                    Object.Destroy(uselessPingTracker);
                    pingTrackerCredential.alignment = TextAlignmentOptions.TopRight;
                    pingTrackerCredential.color = new(1f, 1f, 1f, 0.7f);
                    pingTrackerCredential.rectTransform.pivot = new(1f, 1f);  // 中心を右上角に設定
                    pingTrackerCredentialAspectPos = pingTrackerCredential.GetComponent<AspectPosition>();
                    pingTrackerCredentialAspectPos.Alignment = AspectPosition.EdgeAlignments.RightTop;
                }
                if (pingTrackerCredentialAspectPos)
                {
                    pingTrackerCredentialAspectPos.DistanceFromEdge = DestroyableSingleton<HudManager>.InstanceExists && DestroyableSingleton<HudManager>.Instance.Chat.chatButton.gameObject.active
                        ? new(2.5f, 0f, -800f)
                        : new(1.8f, 0f, -800f);
                }

                sb.Clear();
                sb.Append(Main.credentialsText);
                if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
                if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("HideAndSeek")));
                if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
                if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, "デバッグモード"));

                if (GameStates.IsLobby)
                {
                    if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                        sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
                }

                pingTrackerCredential.text = sb.ToString();
            }
        }
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerStartPatch
        {
            static TextMeshPro SpecialEventText;
            static void Postfix(VersionShower __instance)
            {
                TMPTemplate.SetBase(__instance.text);
                Main.credentialsText = $"<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}";
                if (Main.IsPrerelease)
                {
                    Main.credentialsText += $"\r\n<#F39C12><size=120%>{GetString("Prerelease")}</size></color>";
                }
#if DEBUG
                Main.credentialsText += $"\r\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
                var credentials = TMPTemplate.Create(
                    "TOHCredentialsText",
                    Main.credentialsText,
                    fontSize: 2f,
                    alignment: TextAlignmentOptions.Right,
                    setActive: true);
                credentials.transform.position = new Vector3(1f, 2.65f, -2f);

                ErrorText.Create(__instance.text);
                if (Main.hasArgumentException && ErrorText.Instance != null)
                {
                    ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
                }

                VersionChecker.Check();
#if DEBUG
                if (OptionItem.IdDuplicated)
                {
                    ErrorText.Instance.AddError(ErrorCode.OptionIDDuplicate);
                }
#endif

                if (SpecialEventText == null && TohLogo != null)
                {
                    SpecialEventText = TMPTemplate.Create(
                        "SpecialEventText",
                        "",
                        Color.white,
                        alignment: TextAlignmentOptions.Center,
                        parent: TohLogo.transform);
                    SpecialEventText.name = "SpecialEventText";
                    SpecialEventText.fontSizeMin = 3f;
                    SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                }
                if (SpecialEventText != null)
                {
                    SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
                    SpecialEventText.gameObject.SetActive(true);
                }
                if (Main.IsInitialRelease)
                {
                    SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                    if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                    {
                        SpecialEventText.color = col;
                    }
                }
                if (Main.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
                {
                    //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
                    //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
                    SpecialEventText.text = "何とは言いませんが、特別な日ですね。\n<size=15%>\n\n末永く爆発しろ</size>";
                    SpecialEventText.color = Utils.GetRoleColor(CustomRoles.Lovers);
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class TitleLogoPatch
        {
            public static GameObject amongUsLogo;

            [HarmonyPriority(Priority.VeryHigh)]
            static void Postfix(MainMenuManager __instance)
            {
                amongUsLogo = GameObject.Find("LOGO-AU");

                var rightpanel = __instance.gameModeButtons.transform.parent;
                var logoObject = new GameObject("titleLogo_TOH");
                var logoTransform = logoObject.transform;
                TohLogo = logoObject.AddComponent<SpriteRenderer>();
                logoTransform.parent = rightpanel;
                logoTransform.localPosition = new(0f, 0.15f, 1f);
                logoTransform.localScale *= 1.2f;
                TohLogo.sprite = Utils.LoadSprite("TownOfHost.Resources.TownOfHost-Logo.png", 300f);
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
}
