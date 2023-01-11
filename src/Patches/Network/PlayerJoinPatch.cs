using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using TownOfHost.Addons;
using TownOfHost.Managers;
using TownOfHost.RPC;
using VentLib;
using static TownOfHost.Translator;

namespace TownOfHost.Patches.Network;

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
        ChatPatch.ChatHistory = new();

        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();
        DTask.Schedule(() => AddonManager.VerifyClientAddons(AddonManager.Addons.Select(AddonInfo.From).ToList()), GameStats.DeriveDelay(0.4f));
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
        DTask.Schedule(() => VentFramework.FindRPC((uint)ModCalls.SendOptionPreview)!.Send(new[] { client.Id }, TOHPlugin.OptionManager.Options()), GameStats.DeriveDelay(0.4f));
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
class CreatePlayerPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        DTask.Schedule(() =>
        {
            if (client.Character == null) return;
            if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceUsingTOH"), TOHPlugin.PluginVersion + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "")), client.Character.PlayerId);
            TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
        }, 3f);
    }
}