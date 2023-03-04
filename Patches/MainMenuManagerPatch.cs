using HarmonyLib;
using System;
using UnityEngine;

namespace TOHE;

[HarmonyPatch]
public class MainMenuManagerPatch
{
    public static GameObject template;
    public static GameObject qqButton;
    public static GameObject updateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        if (template == null) template = GameObject.Find("/MainUI/ExitGameButton");
        if (template == null) return;
        //Discordボタンを生成
        if (qqButton == null) qqButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
        qqButton.name = "qqButton";
        qqButton.transform.position = Vector3.Reflect(template.transform.position, Vector3.left);

        var discordText = qqButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
        Color qqColor = new Color32(255, 192, 203, byte.MaxValue);
        PassiveButton qqPassiveButton = qqButton.GetComponent<PassiveButton>();
        SpriteRenderer qqButtonSprite = qqButton.GetComponent<SpriteRenderer>();
        qqPassiveButton.OnClick = new();
        qqPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL(Main.QQInviteUrl)));
        qqPassiveButton.OnMouseOut.AddListener((Action)(() => qqButtonSprite.color = discordText.color = qqColor));
        __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => discordText.SetText("QQ群"))));
        qqButtonSprite.color = discordText.color = qqColor;
        qqButton.gameObject.SetActive(Main.ShowQQButton);

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
        /*
#if RELEASE
        //フリープレイの無効化
        var freeplayButton = GameObject.Find("/MainUI/FreePlayButton");
        if (freeplayButton != null)
        {
            freeplayButton.GetComponent<PassiveButton>().OnClick = new();
            freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Application.OpenURL("https://jq.qq.com/?_wv=1027&k=2RpigaN6")));
            __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => freeplayButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().SetText("QQ群"))));
        }
#endif
        */
    }
}