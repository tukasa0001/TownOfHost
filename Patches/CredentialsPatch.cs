using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;

namespace TownOfHost
{
    //From The Other Roles source
    //https://github.com/Eisbison/TheOtherRoles/blob/main/TheOtherRoles/Patches/CredentialsPatch.cs
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerPatch
    {
        private static GameObject modStamp;
        static void Prefix(PingTracker __instance)
        {
            if (modStamp == null)
            {
                modStamp = new GameObject("ModStamp");
                var rend = modStamp.AddComponent<SpriteRenderer>();
                rend.color = new Color(1, 1, 1, 0.5f);
                modStamp.transform.parent = __instance.transform.parent;
                modStamp.transform.localScale *= 0.6f;
            }
            float offset = (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started) ? 0.75f : 0f;
            modStamp.transform.position = HudManager.Instance.MapButton.transform.position + Vector3.down * offset;
        }

        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            __instance.text.text += main.credentialsText;
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
            {
                if (PlayerControl.LocalPlayer.Data.IsDead)
                {
                    __instance.transform.localPosition = new Vector3(3.45f, __instance.transform.localPosition.y, __instance.transform.localPosition.z);
                } else {
                    __instance.transform.localPosition = new Vector3(4.2f, __instance.transform.localPosition.y, __instance.transform.localPosition.z);
                }
            } else {
                __instance.transform.localPosition = new Vector3(3.5f, __instance.transform.localPosition.y, __instance.transform.localPosition.z);
            }
        }
    }
    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    class VersionShowerPatch
    {
        private static TMPro.TextMeshPro ErrorText;
        static void Postfix(VersionShower __instance)
        {
            main.credentialsText = "\r\n<color=" + main.modColor + ">Town Of Host</color> v" + main.PluginVersion + main.VersionSuffix;
            if(main.PluginVersionType == VersionTypes.Beta) main.credentialsText += $"\r\n{main.BetaName}\r\n{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
            var credentials = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(__instance.text);
            credentials.text = main.credentialsText;
            credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
            credentials.transform.position = new Vector3(4.3f,__instance.transform.localPosition.y+0.3f,0);

            if(main.hasArgumentException && !main.ExceptionMessageIsShown) {
                main.ExceptionMessageIsShown = true;
                ErrorText = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(__instance.text);
                ErrorText.transform.position = new Vector3(0, 0.5f, 50f);
                ErrorText.alignment = TMPro.TextAlignmentOptions.Center;
                ErrorText.text = $"エラー:Lang系DictionaryにKeyの重複が発生しています!\r\n{main.ExceptionMessage}";
                ErrorText.color = Color.red;
            }
        }
    }
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class AwakePatch
    {
        public static void Prefix(ModManager __instance)
        {
            __instance.ShowModStamp();
            LateTask.Update(Time.deltaTime);
        }
    }
}
