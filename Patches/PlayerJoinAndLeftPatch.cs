using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using UnityEngine;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Neutral;
using static TownOfHostForE.Translator;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            ErrorText.Instance.Clear();
            BGMSettings.SetLobbyBGM();
            if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
            {
                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    class DisconnectInternalPatch
    {
        public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
        {

            Main.playerVersion.Clear();
            RPC.RpcVersionCheck();
            if (AmongUsClient.Instance.AmHost && GameStates.IsLobby)
                ChatCommands.StartButtonReset = true;

            Logger.Info($"切断(理由:{reason}:{stringReason}, ping:{__instance.Ping})", "Session");

            if (AmongUsClient.Instance.AmHost && GameStates.InGame)
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);

            //ホストであり、霊界投票が有効であれば切断時にCSVを保存する。
            if (AmongUsClient.Instance.AmHost && BetWinTeams.BetWinTeamMode.GetBool() && BetWinTeams.readedCSV)
            {
                BetWinTeams.WriteCSVPlayerData();
            }
            //if(BGMSettings.BGMMode.GetBool())
            BGMSettings.StopSound();


            CustomRoleManager.Dispose();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
            }
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
            RPC.RpcVersionCheck();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        static void Prefix([HarmonyArgument(0)] ClientData data)
        {
            if (CustomRoles.Executioner.IsPresent())
                Executioner.ChangeRoleByTarget(data.Character.PlayerId);
            if (CustomRoles.Lawyer.IsPresent())
                Lawyer.ChangeRoleByTarget(data.Character);
        }
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            //            main.RealNames.Remove(data.Character.PlayerId);
            if (GameStates.IsInGame)
            {
                if(data == null || data.Character == null)
                {
                    Logger.Info("切断者のデータがないため処理しない","disconnect");
                    return;
                }
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                {
                    var loversList = GetLoversList(data.Character);
                    byte ownerId = loversList[0].PlayerId;
                    foreach (var lovers in loversList.ToArray())
                    {
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.Lovers);
                    }
                    Main.LoversPlayersV2.Remove(ownerId);
                    Main.isLoversDeadV2[ownerId] = true;
                }
                var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                {
                    state.DeathReason = CustomDeathReason.Disconnected;
                    state.SetDead();
                }
                data.Character?.GetRoleClass()?.Dispose();
                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }
            Main.playerVersion.Remove(data.Character.PlayerId);
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
        }

        private static List<PlayerControl> GetLoversList(PlayerControl pc)
        {
            foreach (var list in Main.LoversPlayersV2)
            {
                if (list.Value.Contains(pc.PlayerId))
                {
                    List<PlayerControl> lovers = new ();

                    foreach (var id in list.Value)
                    {
                        lovers.Add(Utils.GetPlayerById(id));
                    }

                    return lovers;
                }
            }

            return null;
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                OptionItem.SyncAllOptions();
                _ = new LateTask(() =>
                {
                    if (client.Character == null) return;
                    if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceUsingTOH"), Main.PleviewPluginVersion), client.Character.PlayerId);
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                }, 3f, "Welcome Message");
                if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayLastRoles");
                }
                if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!GameStates.IsInGame && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowKillLog(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayKillLog");
                }
            }
        }
    }
}