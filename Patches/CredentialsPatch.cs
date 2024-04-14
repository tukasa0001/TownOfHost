using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Templates;
using static TownOfHostForE.Translator;

namespace TownOfHostForE
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public static SpriteRenderer TohLogo { get; private set; }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        class PingTrackerUpdatePatch
        {
            static StringBuilder sb = new();
            static void Postfix(PingTracker __instance)
            {
                __instance.text.alignment = TextAlignmentOptions.TopRight;

                sb.Clear();

                sb.Append("\r\n").Append(Main.credentialsText);

                if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
                if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("HideAndSeek")));
                if (Options.CurrentGameMode == CustomGameMode.SuperBombParty) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("SuperBombParty")));
                if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
                if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, "デバッグモード"));

                var offset_x = 1.2f; //右端からのオフセット
                if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
                if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
                __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

                if (GameStates.IsLobby)
                {
                    if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                        sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
                }

                __instance.text.text += sb.ToString();
            }
        }
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerStartPatch
        {
            //static TextMeshPro SpecialEventText;
            static void Postfix(VersionShower __instance)
            {
                TMPTemplate.SetBase(__instance.text);
                Main.credentialsText = $"<color={Main.ModColor}>{Main.ModName}</color> v{Main.PleviewPluginVersion}";
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

                //if (SpecialEventText == null && TohLogo != null)
                //{
                //    SpecialEventText = Object.Instantiate(__instance.text, TohLogo.transform);
                //    SpecialEventText.name = "SpecialEventText";
                //    SpecialEventText.text = "";
                //    SpecialEventText.color = Color.white;
                //    SpecialEventText.fontSizeMin = 3f;
                //    SpecialEventText.alignment = TextAlignmentOptions.Center;
                //    SpecialEventText.transform.localPosition = new Vector3(0f, -1.2f, 0f);
                //}
                //if (SpecialEventText != null)
                //{
                //    SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
                //}
                //if (Main.IsInitialRelease)
                //{
                //    SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                //    if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                //    {
                //        SpecialEventText.color = col;
                //    }
                //}
                //if (Main.IsOneNightRelease && CultureInfo.CurrentCulture.Name == "ja-JP")
                //{
                //    SpecialEventText.text = "TOH_ForE(制限版)へようこそ！" +
                //        "\n<size=55%>6/22のAmongUs内部的サイレント更新のため、" +
                //        "\nホスト系MODの役職に不具合が発生しております。" +
                //        "\nしばらくはこのTOH_ForEをご利用ください。\n</size><size=40%>\nTOH_ForEのＳはSimpleのＳです。</size>";
                //    SpecialEventText.color = Color.yellow;
                //}
                ////if (Main.IsValentine)
                ////{
                ////    SpecialEventText.text = "♥happy Valentine♥";
                ////    if (CultureInfo.CurrentCulture.Name == "ja-JP")
                ////        SpecialEventText.text += "<size=60%>\n<color=#b58428>チョコレート屋で遊んでみてね。</size></color>";
                ////    SpecialEventText.color = Utils.GetRoleColor(CustomRoles.Lovers);
                ////}
                //if (Main.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
                //{
                //    SpecialEventText.text = "★Merry Christmas★\n<size=15%>\n\nTOH_ForEからのプレゼントはありません。</size>";
                //    SpecialEventText.color = Color.yellow;
                //}
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
                logoTransform.localPosition = new(0f, 0.18f, 1f);
                //logoTransform.localScale *= 1f;
                if (Main.IsForEPreRelease)
                {
                    TohLogo.sprite = Utils.LoadSprite("TownOfHost_ForE.Resources.TownOfHost4E_Debug-Logo.png", 300f);
                }
                else
                {
                    TohLogo.sprite = Utils.LoadSprite("TownOfHost_ForE.Resources.TownOfHost-Logo.png", 300f);
                }
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
                //var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
                __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                    __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                    new Vector3(0.4f, 1.6f, __instance.localCamera.nearClipPlane + 0.1f));
            }
        }
    }
}
