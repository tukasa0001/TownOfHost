using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TownOfHost.Modules;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Main.existAntiAdminer = false;
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            Main.newLobby = true;
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            NameColorManager.Begin();
            ErrorText.Instance.Clear();
            if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
            {
                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
            ChatUpdatePatch.DoBlockChat = false;
            Main.OriginalName = new();
            Main.DevRole = new();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            if (Options.KickAndroidPlayer.GetBool())
            {
                if (client.PlatformData.Platform == Platforms.Android)
                {
                    new LateTask(() =>
                    {
                        Logger.Warn($"{client?.PlayerName} 因该房禁止安卓被踢出", "Android Kick");
                        Logger.SendInGame($"【{client?.PlayerName}】因该房禁止安卓被踢出");
                        AmongUsClient.Instance.KickPlayer(client.Id, false);
                    }, 3f, "Kick");
                }
            }
            if (!Main.OriginalName.ContainsKey(client.Id)) Main.OriginalName.Add(client.Id, client.PlayerName);
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();

            if (!AmongUsClient.Instance.AmHost) return;
            if (__instance.ClientId < 1) return;
            if (Main.LastRPC.ContainsKey(client.Id)) Main.LastRPC.Remove(client.Id);
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            if (Main.newLobby && Options.SendCodeToQQ.GetBool()) Cloud.SendCodeToQQ();
            if (AmongUsClient.Instance.AmHost) Utils.DevNameCheck(client);
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            //            main.RealNames.Remove(data.Character.PlayerId);
            if (GameStates.IsInGame)
            {
                if (data.Character.Is(CustomRoles.TimeThief))
                    data.Character.ResetVotingTime();
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.LoversPlayers.ToArray())
                    {
                        Main.isLoversDead = true;
                        Main.LoversPlayers.Remove(lovers);
                        Main.PlayerStates[lovers.PlayerId].RemoveSubRole(CustomRoles.Lovers);
                    }
                if (data.Character.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(data.Character.PlayerId))
                    Executioner.ChangeRole(data.Character);
                if (Executioner.Target.ContainsValue(data.Character.PlayerId))
                    Executioner.ChangeRoleByTarget(data.Character);
                if (Main.PlayerStates[data.Character.PlayerId].deathReason == PlayerState.DeathReason.etc) //死因が設定されていなかったら
                {
                    Main.PlayerStates[data.Character.PlayerId].deathReason = PlayerState.DeathReason.Disconnected;
                    Main.PlayerStates[data.Character.PlayerId].SetDead();
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    Logger.SendInGame($"{data.PlayerName} 被树懒超级厉害的反作弊踢出去啦~ QwQ");
                    break;
                case DisconnectReasons.Destroy:
                    Logger.SendInGame($"{data.PlayerName} 很不幸地遇到Bug卡退了~ QwQ");
                    break;
            }
            if (AmongUsClient.Instance.Ping > 700)
            {
                Logger.SendInGame($"{data.PlayerName} 在火星和你联机但是断了 (Ping:{AmongUsClient.Instance.Ping}) QwQ");
            }
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
            if (!AmongUsClient.Instance.AmHost) return;
            if (__instance.ClientId < 1) return;
            if (Main.OriginalName.ContainsKey(__instance.ClientId)) Main.OriginalName.Remove(__instance.ClientId);
            if (Main.LastRPC.ContainsKey(__instance.ClientId)) Main.LastRPC.Remove(__instance.ClientId);
            if (Main.SayStartTimes.ContainsKey(__instance.ClientId)) Main.SayStartTimes.Remove(__instance.ClientId);
            if (Main.SayBanwordsTimes.ContainsKey(__instance.ClientId)) Main.SayBanwordsTimes.Remove(__instance.ClientId);
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (Main.OverrideWelcomeMsg != "")
                {
                    new LateTask(() =>
                    {
                        Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
                    }, 3f, "Override Welcome Message");
                }
                else
                {
                    new LateTask(() =>
                    {
                        if (client.Character == null) return;
                        //if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceUsingTOH"), Main.PluginVersion), client.Character.PlayerId);
                        TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                    }, 3f, "Welcome Message");
                    if (Options.AutoDisplayLastResult.GetBool() && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                    {
                        new LateTask(() =>
                        {
                            if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                            {
                                Main.isChatCommand = true;
                                Utils.ShowLastResult(client.Character.PlayerId);
                            }
                        }, 3f, "DisplayLastRoles");
                    }
                }
            }
        }
    }
}