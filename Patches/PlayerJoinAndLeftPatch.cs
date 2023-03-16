using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
internal class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        Cloud.StartConnect();
        Main.existAntiAdminer = false;
        GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Main.newLobby = true;
        Logger.Info($"{__instance.GameId} 创建房间", "OnGameJoined");
        Main.playerVersion = new Dictionary<byte, PlayerVersion>();
        RPC.RpcVersionCheck();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
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
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
internal class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        ShowDisconnectPopupPatch.Reason = reason;
        ShowDisconnectPopupPatch.StringReason = stringReason;
        Logger.Info($"断开连接(理由:{reason}:{stringReason}, ping:{__instance.Ping})", "Session");
        ErrorText.Instance.CheatDetected = false;
        ErrorText.Instance.SBDetected = false;
        ErrorText.Instance.Clear();
        Cloud.StopConnect();
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
internal class OnPlayerJoinedPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        Logger.Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}) 加入房间", "Session");
        if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
        {
            AmongUsClient.Instance.KickPlayer(client.Id, false);
            Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
            Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
        }
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

        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.LastRPC.ContainsKey(client.Id)) Main.LastRPC.Remove(client.Id);
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            if (Main.newLobby && Options.SendCodeToQQ.GetBool()) Cloud.SendCodeToQQ();
            if (AmongUsClient.Instance.AmHost) Utils.DevNameCheck(client);
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
internal class OnPlayerLeftPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
        //            main.RealNames.Remove(data.Character.PlayerId);
        if (GameStates.IsInGame)
        {
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
            if (data.Character.Is(CustomRoles.Pelican))
                Pelican.OnPelicanDied(data.Character.PlayerId);
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
        Logger.Info($"{data.PlayerName}(ClientID:{data.Id}/FriendCode:{data.FriendCode})断开连接(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.OriginalName.ContainsKey(__instance.ClientId)) Main.OriginalName.Remove(__instance.ClientId);
            if (Main.LastRPC.ContainsKey(__instance.ClientId)) Main.LastRPC.Remove(__instance.ClientId);
            if (Main.SayStartTimes.ContainsKey(__instance.ClientId)) Main.SayStartTimes.Remove(__instance.ClientId);
            if (Main.SayBanwordsTimes.ContainsKey(__instance.ClientId)) Main.SayBanwordsTimes.Remove(__instance.ClientId);
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
internal class CreatePlayerPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            OptionItem.SyncAllOptions();
            new LateTask(() =>
            {
                if (client.Character == null) return;
                if (Main.OverrideWelcomeMsg != "") Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
                else TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
            }, 3f, "Welcome Message");
            if (Options.AutoDisplayLastResult.GetBool() && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id) && Main.OverrideWelcomeMsg == "")
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