using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class GameStartManagerUpdatePatch
    {
        public static void Prefix(GameStartManager __instance)
        {
            __instance.MinPlayers = 1;
        }
    }
    //タイマーとコード隠し
    public class GameStartManagerPatch
    {
        private static float timer = 600f;
        private static string lobbyCodehide = "";
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static void Postfix(GameStartManager __instance)
            {
                // Reset lobby countdown timer
                timer = 600f;

                if (AmongUsClient.Instance.AmHost && Options.AutoDisplayLastResult.GetBool() && Main.AllPlayerCustomRoles.Count != 0)
                {
                    new LateTask(() =>
                    {
                        Main.isChatCommand = true;
                        Utils.ShowLastRoles();
                    }
                        , 5f, "DisplayLastRoles");
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            private static bool update = false;
            private static string currentText = "";
            public static void Prefix(GameStartManager __instance)
            {
                if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.GameMode == GameModes.LocalGame) return; // Not host or no instance or LocalGame
                update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
            }
            public static void Postfix(GameStartManager __instance)
            {
                // Lobby code
                string htmlValue = Main.HideColor.Value;
                lobbyCodehide = Main.HideCodes.Value
                    ? ColorUtility.TryParseHtmlString(htmlValue, out _)
                        ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                        : $"<color={Main.modColor}>{Main.HideName.Value}</color>"
                    : $"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoomCode, new Il2CppReferenceArray<Il2CppSystem.Object>(0)) + "\r\n" + InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId)}";
                __instance.GameRoomName.text = lobbyCodehide;
                // Lobby timer
                if (!AmongUsClient.Instance.AmHost || !GameData.Instance) return;

                if (update) currentText = __instance.PlayerCounter.text;

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string suffix = $" ({minutes:00}:{seconds:00})";
                if (timer <= 60) suffix = "<color=#ff0000>" + suffix + "</color>";

                __instance.PlayerCounter.text = currentText + suffix;
                __instance.PlayerCounter.autoSizeTextContainer = true;
            }
        }
        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
        public static class HiddenTextPatch
        {
            private static void Postfix(TextBoxTMP __instance)
            {
                if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
            }
        }
    }
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public class GameStartRandomMap
    {
        public static bool Prefix(GameStartRandomMap __instance)
        {
            bool continueStart = true;
            if (Options.RandomMapsMode.GetBool())
            {
                var rand = new System.Random();
                System.Collections.Generic.List<byte> RandomMaps = new();
                /*TheSkeld   = 0
                MIRAHQ     = 1
                Polus      = 2
                Dleks      = 3
                TheAirShip = 4*/
                if (Options.AddedTheSkeld.GetBool()) RandomMaps.Add(0);
                if (Options.AddedMiraHQ.GetBool()) RandomMaps.Add(1);
                if (Options.AddedPolus.GetBool()) RandomMaps.Add(2);
                // if (Options.AddedDleks.GetBool()) RandomMaps.Add(3);
                if (Options.AddedTheAirShip.GetBool()) RandomMaps.Add(4);

                if (RandomMaps.Count <= 0) return true;
                var MapsId = RandomMaps[rand.Next(RandomMaps.Count)];
                PlayerControl.GameOptions.MapId = MapsId;

            }
            return continueStart;
        }
    }
    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.GetAdjustedNumImpostors))]
    class UnrestrictedNumImpostorsPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = PlayerControl.GameOptions.NumImpostors;
            return false;
        }
    }
}