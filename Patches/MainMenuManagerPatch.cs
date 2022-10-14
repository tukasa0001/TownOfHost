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

        }
    }
}