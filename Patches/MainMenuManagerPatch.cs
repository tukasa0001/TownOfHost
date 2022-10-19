using System;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch]
    public class MainMenuManagerPatch
    {
        public static GameObject template;
        public static GameObject discordButton;
        public static GameObject updateButton;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
        public static void Start_Prefix(MainMenuManager __instance)
        {
            if (template == null) template = GameObject.Find("/MainUI/ExitGameButton");
            if (template == null) return;
            //Discordボタンを生成
            if (discordButton == null) discordButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
            discordButton.name = "DiscordButton";
            discordButton.transform.position = Vector3.Reflect(template.transform.position, Vector3.left);

            var discordText = discordButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Color discordColor = new Color32(88, 101, 242, byte.MaxValue);
            PassiveButton discordPassiveButton = discordButton.GetComponent<PassiveButton>();
            SpriteRenderer discordButtonSprite = discordButton.GetComponent<SpriteRenderer>();
            discordPassiveButton.OnClick = new();
            discordPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL(Main.DiscordInviteUrl)));
            discordPassiveButton.OnMouseOut.AddListener((Action)(() => discordButtonSprite.color = discordText.color = discordColor));
            __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => discordText.SetText("Discord"))));
            discordButtonSprite.color = discordText.color = discordColor;
            discordButton.gameObject.SetActive(Main.ShowDiscordButton);

            //Updateボタンを生成
            if (updateButton == null) updateButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
            updateButton.name = "UpdateButton";
            updateButton.transform.position = template.transform.position + new Vector3(0.25f, 0.75f);
            updateButton.transform.GetChild(0).GetComponent<RectTransform>().localScale *= 1.5f;

            var updateText = updateButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Color updateColor = new Color32(128, 255, 255, byte.MaxValue);
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
                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.OpenURL("https://github.com/tukasa0001/TownOfHost")));
                __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => freeplayButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().SetText("GitHub"))));
            }
#endif
        }
    }
}