using System.Collections.Generic;
using System;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using static TownOfHost.Translator;

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
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static TMPro.TextMeshPro HideName;
            public static void Postfix(GameStartManager __instance)
            {
                __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                // Reset lobby countdown timer
                timer = 600f;

                HideName = UnityEngine.Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
                HideName.text = ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                        ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                        : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";

                if (!AmongUsClient.Instance.AmHost) return;

                // Make Public Button
                if (ModUpdater.isBroken || ModUpdater.hasUpdate || !Main.AllowPublicRoom)
                {
                    __instance.MakePublicButton.color = Palette.DisabledClear;
                    __instance.privatePublicText.color = Palette.DisabledClear;
                }

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            private static bool update = false;
            private static string currentText = "";
            private static float exitTimer = 0f;
            public static void Prefix(GameStartManager __instance)
            {
                // Lobby code
                if (DataManager.Settings.Gameplay.StreamerMode)
                {
                    __instance.GameRoomNameCode.color = new(255, 255, 255, 0);
                    GameStartManagerStartPatch.HideName.enabled = true;
                }
                else
                {
                    __instance.GameRoomNameCode.color = new(255, 255, 255, 255);
                    GameStartManagerStartPatch.HideName.enabled = false;
                }
                if (!AmongUsClient.Instance.AmHost || !GameData.Instance || AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame) return; // Not host or no instance or LocalGame
                update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
            }
            public static void Postfix(GameStartManager __instance)
            {
                if (!AmongUsClient.Instance) return;

                string warningMessage = "";
                if (AmongUsClient.Instance.AmHost)
                {
                    bool canStartGame = true;
                    List<string> mismatchedPlayerNameList = new();
                    foreach (var client in AmongUsClient.Instance.allClients.ToArray())
                    {
                        if (client.Character == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;
                        if (!MatchVersions(client.Character.PlayerId, true))
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }
                    }
                    if (!canStartGame)
                    {
                        __instance.StartButton.gameObject.SetActive(false);
                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.MismatchedVersion"), String.Join(" ", mismatchedPlayerNameList), $"<color={Main.ModColor}>{Main.ModName}</color>"));
                    }
                }
                else
                {
                    if (MatchVersions(0))
                        exitTimer = 0;
                    else
                    {
                        exitTimer += Time.deltaTime;
                        if (exitTimer > 10)
                        {
                            exitTimer = 0;
                            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                            SceneChanger.ChangeScene("MainMenu");
                        }

                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.AutoExitAtMismatchedVersion"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - exitTimer).ToString()));
                    }
                }
                if (warningMessage != "")
                {
                    __instance.GameStartText.text = warningMessage;
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 2;
                }
                else
                {
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                    if (!GameStates.IsCountDown)
                        __instance.GameStartText.text = "";
                }

                // Lobby timer
                if (!AmongUsClient.Instance.AmHost || !GameData.Instance) return;

                if (update) currentText = __instance.PlayerCounter.text;

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string suffix = $" ({minutes:00}:{seconds:00})";
                if (timer <= 60) suffix = Utils.ColorString(Color.red, suffix);

                __instance.PlayerCounter.text = currentText + suffix;
                __instance.PlayerCounter.autoSizeTextContainer = true;
            }
            private static bool MatchVersions(byte playerId, bool acceptVanilla = false)
            {
                if (!Main.playerVersion.TryGetValue(playerId, out var version)) return acceptVanilla;
                return Main.ForkId == version.forkId
                    && Main.version.CompareTo(version.version) == 0
                    && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
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
        public static bool Prefix(GameStartManager __instance)
        {
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Count() != 0)
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                return false;
            }

            Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
            Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
            Main.NormalOptions.KillCooldown = 0f;

            var opt = Main.NormalOptions.Cast<IGameOptions>();
            AURoleOptions.SetOpt(opt);
            Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
            AURoleOptions.ShapeshifterCooldown = 0f;

            PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt));

            __instance.ReallyBegin(false);
            return false;
        }
        public static bool Prefix(GameStartRandomMap __instance)
        {
            bool continueStart = true;
            if (Options.RandomMapsMode.GetBool())
            {
                var rand = IRandom.Instance;
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
                Main.NormalOptions.MapId = MapsId;

            }
            return continueStart;
        }
    }
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
    class ResetStartStatePatch
    {
        public static void Prefix()
        {
            if (GameStates.IsCountDown)
            {
                Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions));
            }
        }
    }
    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    class UnrestrictedNumImpostorsPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = Main.NormalOptions.NumImpostors;
            return false;
        }
    }
}