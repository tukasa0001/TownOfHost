using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils.Collections;

using Object = UnityEngine.Object;
using TownOfHostForE.Modules;
using static TownOfHostForE.Roles.Crewmate.Tiikawa;
using Epic.OnlineServices.Presence;
using TownOfHostForE.Roles.Core;
//using Il2CppSystem.Reflection;
using System.Linq;
using System.IO;
using System.Reflection;
using TownOfHostForE.Templates;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class BlacklistPatch
    {
        public static void Postfix(MainMenuManager __instance)
        {
            __instance.StartCoroutine(Blacklist.FetchBlacklist().WrapToIl2Cpp());
        }
    }

    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static PassiveButton template;
        private static PassiveButton discordButton;
        private static PassiveButton twitterButton;
        private static PassiveButton YouTubeButton;
        private static PassiveButton gitHubButton;
        public static SimpleButton UpdateButton { get; private set; }

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            SimpleButton.SetBase(__instance.quitButton);
            if (template == null) template = __instance.quitButton;
            if (template == null) return;

            //Updateボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton))
            {
                UpdateButton = CreateButton(
                    "UpdateButton",
                    new(0f, -1.3f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () =>
                    {
                        UpdateButton.Button.gameObject.SetActive(false);
                        ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                    },
                    $"{Translator.GetString("updateButton")}\n{ModUpdater.latestTitle}",
                    new(2.5f, 1f),
                    isActive: false);
            }

            //Discordボタンを生成
            if (discordButton == null)
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(0.85f, -2f, 0f),
                    new(88, 101, 242, byte.MaxValue),
                    new(173, 179, 244, byte.MaxValue),
                    () => Application.OpenURL(Main.DiscordInviteUrl),
                    "Discord",
                    new(1.9f, 0.725f));
            }
            discordButton.gameObject.SetActive(Main.ShowDiscordButton);

            // Twitterボタンを生成
            if (twitterButton == null)
            {
                twitterButton = CreateButton(
                    "TwitterButton",
                    new(-0.85f, -2.6f, 0f),
                    new(29, 160, 241, byte.MaxValue),
                    new(169, 215, 242, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/TOH4E_AmongUs"),
                    "Twitter",
                    new(1.9f, 0.725f));
            }
            // WIKIWIKIボタンを生成
            if (YouTubeButton == null)
            {
                YouTubeButton = CreateButton(
                    "YouTubeButton",
                    new(0.85f, -2.6f, 0),
                    new(196, 48, 43, byte.MaxValue),
                    new(200, 50, 50, byte.MaxValue),
                    () => Application.OpenURL("https://www.youtube.com/channel/UCJDcUf0KOwLFGwVmj1m6Fag"),
                    "YouTube",
                    new(1.9f, 0.725f));
            }
            // GitHubボタンを生成
            if (gitHubButton == null)
            {
                gitHubButton = CreateButton(
                    "GitHubButton",
                    new(-0.85f, -2f, 0),
                    new(161, 161, 161, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL("https://github.com/AsumuAkaguma/TownOfHost_ForE"),
                    "GitHub",
                    new(1.9f, 0.725f));
            }

#if RELEASE
            // フリープレイの無効化
            var howToPlayButton = __instance.howToPlayButton;
            var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
            if (freeplayButton != null)
            {
                freeplayButton.gameObject.SetActive(false);
            }
            // フリープレイが消えるのでHowToPlayをセンタリング
            howToPlayButton.transform.SetLocalX(0);

            //__instance.StartCoroutine(Blacklist.FetchBlacklist().WrapToIl2Cpp());

#endif
        }

        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        private static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, Action action, string label, Vector2? scale = null)
        {
            var button = Object.Instantiate(template, CredentialsPatch.TohLogo.transform);
            button.name = name;
            Object.Destroy(button.GetComponent<AspectPosition>());
            button.transform.localPosition = localPosition;

            button.OnClick = new();
            button.OnClick.AddListener(action);

            var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
            buttonText.DestroyTranslator();
            buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 4.2f;//3.7f;
            buttonText.enableWordWrapping = false;
            buttonText.text = label;
            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = normalColor;
            hoverSprite.color = hoverColor;

            // ラベルをセンタリング
            var container = buttonText.transform.parent;
            Object.Destroy(container.GetComponent<AspectPosition>());
            Object.Destroy(buttonText.GetComponent<AspectPosition>());
            container.SetLocalX(0f);
            buttonText.transform.SetLocalX(0f);
            buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var buttonCollider = button.GetComponent<BoxCollider2D>();
            if (scale.HasValue)
            {
                normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
            }
            // 当たり判定のズレを直す
            buttonCollider.offset = new(0f, 0f);

            return button;
        }


        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        private static SimpleButton CreateButton(
            string name,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            Action action,
            string label,
            Vector2? scale = null,
            bool isActive = true)
        {
            var button = new SimpleButton(CredentialsPatch.TohLogo.transform, name, localPosition, normalColor, hoverColor, action, label, isActive);
            if (scale.HasValue)
            {
                button.Scale = scale.Value;
            }
            return button;
        }

        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
        [HarmonyPostfix]
        public static void OpenMenuPostfix()
        {
            if (CredentialsPatch.TohLogo != null)
            {
                CredentialsPatch.TohLogo.gameObject.SetActive(false);
            }
        }
        [HarmonyPatch(nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
        public static void Postfix()
        {
            //if (Input.GetKeyDown(KeyCode.LeftShift))
            //{
            //    BGMSettings.PlaySoundSE("SuisoSound");
            //}

            if (state == TiikawaInitState.Finished) return;
            if (Input.anyKeyDown)
            {
                KeyCode keyPressed = Event.current.keyCode; // 押されたキーの種類を取得する
                if (keyPressed == KeyCode.UpArrow)
                {
                    Logger.Info("state:" + state, "state");
                    if (state == TiikawaInitState.Init)
                    {
                        state = TiikawaInitState.UE1;
                    }
                    else if (state == TiikawaInitState.UE1)
                    {
                        state = TiikawaInitState.UE2;
                    }
                }
                else if (keyPressed == KeyCode.DownArrow)
                {
                    Logger.Info("state:" + state, "state");
                    if (state == TiikawaInitState.UE2)
                    {
                        state = TiikawaInitState.SITA1;
                    }
                    else if (state == TiikawaInitState.SITA1)
                    {
                        state = TiikawaInitState.SITA2;
                    }
                }
                else if (keyPressed == KeyCode.LeftArrow)
                {
                    Logger.Info("state:" + state, "state");
                    if (state == TiikawaInitState.SITA2)
                    {
                        state = TiikawaInitState.HIDARI1;
                    }
                    else if (state == TiikawaInitState.MIGI1)
                    {
                        state = TiikawaInitState.HIDARI2;
                    }
                }
                else if (keyPressed == KeyCode.RightArrow)
                {
                    Logger.Info("state:" + state, "state");
                    if (state == TiikawaInitState.HIDARI1)
                    {
                        state = TiikawaInitState.MIGI1;
                    }
                    else if (state == TiikawaInitState.HIDARI2)
                    {
                        state = TiikawaInitState.MIGI2;
                    }
                }
                else if (keyPressed == KeyCode.LeftShift && state == TiikawaInitState.MIGI2)
                {
                    Logger.Info("state:" + state, "state");
                    state = TiikawaInitState.B;
                }
                else if (keyPressed == KeyCode.RightShift && state == TiikawaInitState.B)
                {
                    state = TiikawaInitState.Finished;
                    var tiikawaInfo = CustomRoleManager.AllRolesInfo[CustomRoles.Tiikawa];
                    Options.SetupSingleRoleOptions(tiikawaInfo.ConfigId, tiikawaInfo.Tab, tiikawaInfo.RoleName, 1);
                    tiikawaInfo.OptionCreator?.Invoke();
                    var metatonInfo = CustomRoleManager.AllRolesInfo[CustomRoles.Metaton];
                    Options.SetupSingleRoleOptions(metatonInfo.ConfigId, metatonInfo.Tab, metatonInfo.RoleName, 1);
                    metatonInfo.OptionCreator?.Invoke();
                    Logger.Info("エクストラ役職参戦！","Extra");
                    BGMSettings.PlaySoundSE("Boom");
                }
                //CustomSoundsManager.PlayBGMForWav("sbp");
            }
        }

        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TohLogo != null)
            {
                CredentialsPatch.TohLogo.gameObject.SetActive(true);
            }
        }
    }
}