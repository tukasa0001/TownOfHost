using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using TOHTOR.Addons;
using TOHTOR.API;
using TOHTOR.Gamemodes;
using TOHTOR.Managers;
using VentLib.Logging;
using VentLib.Utilities;
using VentLib.Version;
using GameStates = TOHTOR.API.GameStates;


namespace TOHTOR.Patches.Network;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{

    public static void Postfix(AmongUsClient __instance)
    {
        /*while (!OldOptions.IsLoaded) System.Threading.Tasks.Task.Delay(1);*/
        VentLogger.Old($"{__instance.GameId}に参加", "OnGameJoined");
        TOHPlugin.PlayerVersion = new Dictionary<byte, Version>();
        SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);
        /*ChatCommands.ChatHistoryDictionary = new();*/

        /*ChatUpdatePatch.DoBlockChat = false;*/
        GameStates.InGame = false;
        Async.Schedule(() => AddonManager.VerifyClientAddons(AddonManager.Addons.Select(AddonInfo.From).ToList()), NetUtils.DeriveDelay(0.5f));
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
class OnPlayerJoinedPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        VentLogger.Old($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, true);
            VentLogger.Old($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
        }
        BanManager.CheckBanPlayer(client);
        BanManager.CheckDenyNamePlayer(client);
        TOHPlugin.PlayerVersion = new Dictionary<byte, Version>();
        Game.CurrentGamemode.Trigger(GameAction.GameJoin, client);
    }
}