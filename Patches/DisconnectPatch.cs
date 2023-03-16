using HarmonyLib;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
internal class OnDisconnectedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.VisibleTasksCount = false;
    }
}

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
internal class ShowDisconnectPopupPatch
{
    public static DisconnectReasons Reason;
    public static string StringReason;
    public static void Postfix(DisconnectPopup __instance)
    {
        new LateTask(() =>
        {
            switch (Reason)
            {
                case DisconnectReasons.Hacking:
                    __instance.SetText(GetString("DCNotify.Hacking"));
                    break;
                case DisconnectReasons.Banned:
                case DisconnectReasons.IncorrectGame:
                    __instance.SetText(GetString("DCNotify.Banned"));
                    break;
                case DisconnectReasons.Kicked:
                    __instance.SetText(GetString("DCNotify.Kicked"));
                    break;
                case DisconnectReasons.GameNotFound:
                    __instance.SetText(GetString("DCNotify.GameNotFound"));
                    break;
                case DisconnectReasons.GameStarted:
                    __instance.SetText(GetString("DCNotify.GameStarted"));
                    break;
                case DisconnectReasons.GameFull:
                    __instance.SetText(GetString("DCNotify.GameFull"));
                    break;
                case DisconnectReasons.IncorrectVersion:
                    __instance.SetText(GetString("DCNotify.IncorrectVersion"));
                    break;
                case DisconnectReasons.Error:
                    //if (StringReason.Contains("Couldn't find self")) __instance.SetText(GetString("DCNotify.DCFromServer"));
                    if (StringReason.Contains("Failed to send message")) __instance.SetText(GetString("DCNotify.DCFromServer"));
                    break;
                case DisconnectReasons.Custom:
                    if (StringReason.Contains("Reliable packet")) __instance.SetText(GetString("DCNotify.DCFromServer"));
                    else if (StringReason.Contains("remote has not responded to")) __instance.SetText(GetString("DCNotify.DCFromServer"));
                    break;
            }
        }, 0.01f, "Override Disconnect Text");
    }
}