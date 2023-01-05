using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using TownOfHost.Addons;
using TownOfHost.Extensions;
using static TownOfHost.Translator;
using TownOfHost.Roles;
using TownOfHost.ReduxOptions;
using TownOfHost.RPC;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!OldOptions.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            TOHPlugin.playerVersion = new Dictionary<byte, PlayerVersion>();
            OldRPC.RpcVersionCheck();
            SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);

            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            ErrorText.Instance.Clear();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
            TOHPlugin.playerVersion = new Dictionary<byte, PlayerVersion>();
            OldRPC.RpcVersionCheck();
            DTask.Schedule(() => HostRpc.RpcSendOptions(TOHPlugin.OptionManager.Options()), 0.5f);
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
                /*if (data.Character.Is(CustomRoles.TimeThief))
                    data.Character.ResetVotingTime();*/
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in TOHPlugin.LoversPlayers.ToArray())
                    {
                        TOHPlugin.isLoversDead = true;
                        TOHPlugin.LoversPlayers.Remove(lovers);
                        TOHPlugin.PlayerStates[lovers.PlayerId].RemoveSubRole(CustomRoles.Lovers);
                    }
                if (TOHPlugin.PlayerStates[data.Character.PlayerId].deathReason == PlayerStateOLD.DeathReason.etc) //死因が設定されていなかったら
                {
                    TOHPlugin.PlayerStates[data.Character.PlayerId].deathReason = PlayerStateOLD.DeathReason.Disconnected;
                    TOHPlugin.PlayerStates[data.Character.PlayerId].SetDead();
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
            }
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                new DTask(() =>
                {
                    if (client.Character == null) return;
                    if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceUsingTOH"), TOHPlugin.PluginVersion + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "")), client.Character.PlayerId);
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                }, 3f, "Welcome Message");
                if (StaticOptions.AutoDisplayLastResult && TOHPlugin.PlayerStates.Count != 0 && TOHPlugin.clientIdList.Contains(client.Id))
                {
                    new DTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            TOHPlugin.isChatCommand = true;
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayLastRoles");
                }
            }
        }
    }
}