using BepInEx.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

//参考：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/LogoAndStampPatch.cs

[HarmonyPatch]
public static class CredentialsPatch
{
    public static GenericPopup popup;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class LogoPatch
    {
        static IEnumerator ViewBoosterCoro(MainMenuManager __instance)
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (__instance != null)
                {
                    ViewBoosterPatch(__instance);
                }
                break;
            }
        }
        public static string SponsersData = "";
        public static string DevsData = "";
        public static string TransData = "";

        public static void InitCreditData()
        {
            SponsersData = "";
            DevsData = "";
            TransData = "";

            DevsData += $"<color={Main.ModColor}>♡咔皮呆</color> - <size=75%>{GetString("MainDev")}</size>";
            DevsData += $"\n<color={Main.ModColor}>♡IRIDESCENT</color> - <size=75%>{GetString("Art")}</size>";
            DevsData += $"\nNCSIMON - <size=75%>{GetString("RoleDev")}</size>";
            DevsData += $"\n天寸梦初 - <size=75%>{GetString("RoleDev")}&{GetString("TechSup")}</size>";
            DevsData += $"\nCommandf1 - <size=75%>{GetString("RoleDev")}&{GetString("FeatureDev")}</size>";
            DevsData += $"\n喜 - <size=75%>{GetString("RoleDev")}</size>";
            DevsData += $"\nSHAAARKY - <size=75%>{GetString("RoleDev")}</size>";

            TransData += $"Tommy-XL - <size=75%>{GetString("TranEN")}&{GetString("TranRU")}</size>";
            TransData += $"\nTem - <size=75%>{GetString("TranRU")}</size>";

            SponsersData += $"罗寄";
            SponsersData += $"\n屑人";
            SponsersData += $"\n小叨院长";
            SponsersData += $"\n波奇酱";
            SponsersData += $"\n法师";
            SponsersData += $"\n沐煊";
            SponsersData += $"\n林林林";
            SponsersData += $"\n撒币";
            SponsersData += $"\nltemten";
            SponsersData += $"\nSlok";
            SponsersData += $"\n辣鸡";
            SponsersData += $"\n湛蓝色";
            SponsersData += $"\n<size=60%>({GetString("OnlyShowPart")})</size>";
        }

        public static GameObject CreditsPopup;
        static void ViewBoosterPatch(MainMenuManager __instance)
        {
            var template = __instance.transform.FindChild("StatsPopup");
            var obj = Object.Instantiate(template, template.transform.parent).gameObject;
            CreditsPopup = obj;
            Object.Destroy(obj.GetComponent<StatsPopup>());
            var devtitletext = obj.transform.FindChild("StatNumsText_TMP");
            devtitletext.GetComponent<TextMeshPro>().text = GetString("Developer");
            devtitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Left;
            devtitletext.localPosition = new Vector3(-2.75f, 1.73f, -2f);
            devtitletext.localScale = new Vector3(1f, 1f, 1f);
            var devtext = obj.transform.FindChild("StatsText_TMP");
            devtext.localPosition = new Vector3(-2f, -0.35f, -2f);
            devtext.localScale = new Vector3(0.7f, 0.7f, 1f);
            devtext.GetComponent<TextMeshPro>().text = DevsData;

            var boostertitletext = Object.Instantiate(devtitletext, obj.transform);
            boostertitletext.GetComponent<TextMeshPro>().text = GetString("Sponsor");
            boostertitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Left;
            boostertitletext.localPosition = new Vector3(0.7f, 1.45f, -2f);
            boostertitletext.localScale = new Vector3(1f, 1f, 1f);

            var boostertext = Object.Instantiate(devtext, obj.transform);
            boostertext.localPosition = new Vector3(1.5f, -0.65f, -2f);
            boostertext.localScale = new Vector3(0.7f, 0.7f, 1f);
            boostertext.GetComponent<TextMeshPro>().text = SponsersData;

            var transtitletext = Object.Instantiate(devtitletext, obj.transform);
            transtitletext.GetComponent<TextMeshPro>().text = GetString("Translator");
            transtitletext.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Left;
            transtitletext.localPosition = new Vector3(-2.75f, -1.1f, -2f);
            transtitletext.localScale = new Vector3(1f, 1f, 1f);

            var transtext = Object.Instantiate(devtext, obj.transform);
            transtext.localPosition = new Vector3(-2f, -3.2f, -2f);
            transtext.localScale = new Vector3(0.7f, 0.7f, 1f);
            transtext.GetComponent<TextMeshPro>().text = TransData;

            var textobj = obj.transform.FindChild("Title_TMP");
            Object.Destroy(textobj.GetComponent<TextTranslatorTMP>());
            textobj.GetComponent<TextMeshPro>().text = GetString("DevAndSpnTitle");
            textobj.localScale = new Vector3(1.2f, 1.2f, 1f);
            obj.transform.FindChild("Background").localScale = new Vector3(1.5f, 1f, 1f);
            obj.transform.FindChild("CloseButton").localPosition = new Vector3(-3.75f, 2.65f, 0);
        }
        public static MainMenuManager instance;
        public static void Postfix(MainMenuManager __instance)
        {
            InitCreditData();
            instance = __instance;
            AmongUsClient.Instance.StartCoroutine(ViewBoosterCoro(__instance));
        }
    }
}