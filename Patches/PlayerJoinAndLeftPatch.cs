using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
        Logger.Info($"{__instance.GameId} 加入房间", "OnGameJoined");
        Main.playerVersion = new Dictionary<byte, PlayerVersion>();
        RPC.RpcVersionCheck();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        if (GameStates.IsModHost)
            Main.HostClientId = Utils.GetPlayerById(0)?.GetClientId() ?? -1;

        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();
        EAC.DeNum = new();
        if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
        {
            Main.newLobby = true;
            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.OriginalName = new();
            Main.DevRole = new();

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
        ShowDisconnectPopupPatch.Reason = reason;
        ShowDisconnectPopupPatch.StringReason = stringReason;
        Logger.Info($"断开连接(理由:{reason}:{stringReason}，Ping:{__instance.Ping})", "Session");
        ErrorText.Instance.CheatDetected = false;
        ErrorText.Instance.SBDetected = false;
        ErrorText.Instance.Clear();
        Cloud.StopConnect();
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
class OnPlayerJoinedPatch
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
        if (AmongUsClient.Instance.AmHost && client.PlatformData.Platform == Platforms.Android && Options.KickAndroidPlayer.GetBool())
        {
            AmongUsClient.Instance.KickPlayer(client.Id, false);
            string msg = string.Format(GetString("KickAndriodPlayer"), client?.PlayerName);
            Logger.SendInGame(msg);
            Logger.Info(msg, "Android Kick");
        }
        if (!Main.OriginalName.ContainsKey(client.Id)) Main.OriginalName.Add(client.Id, client.PlayerName);
        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
        {
            AmongUsClient.Instance.KickPlayer(client.Id, true);
            Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
        }
        BanManager.CheckBanPlayer(client);
        BanManager.CheckDenyNamePlayer(client);
        RPC.RpcVersionCheck();

        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.SayStartTimes.ContainsKey(client.Id)) Main.SayStartTimes.Remove(client.Id);
            if (Main.SayBanwordsTimes.ContainsKey(client.Id)) Main.SayBanwordsTimes.Remove(client.Id);
            if (Main.newLobby && Options.SendCodeToQQ.GetBool()) Cloud.SendCodeToQQ();
            Utils.DevNameCheck(client);
        }

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

        if (Main.HostClientId == __instance.ClientId)
        {
            var clientId = -1;
            var player = PlayerControl.LocalPlayer;
            var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            var name = player?.Data?.PlayerName;
            var msg = "";
            if (GameStates.IsInGame)
            {
                Utils.ErrorEnd("房主退出游戏");
                msg = GetString("Message.HostLeftGameInGame");
            }
            else if (GameStates.IsLobby)
                msg = GetString("Message.HostLeftGameInLobby");

            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            player.SetName(name);

            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }

        // 附加描述掉线原因
        switch (reason)
        {
            case DisconnectReasons.Hacking:
                Logger.SendInGame(string.Format(GetString("PlayerLeftByAU-Anticheat"), data?.PlayerName));
                break;
        }

        Logger.Info($"{data?.PlayerName}(ClientID:{data?.Id}/FriendCode:{data?.FriendCode})断开连接(理由:{reason}，Ping:{AmongUsClient.Instance.Ping})", "Session");

        if (AmongUsClient.Instance.AmHost)
        {
            Main.OriginalName.Remove(__instance.ClientId);
            Main.SayStartTimes.Remove(__instance.ClientId);
            Main.SayBanwordsTimes.Remove(__instance.ClientId);
            Main.playerVersion.Remove(data?.Character?.PlayerId ?? byte.MaxValue);
        }
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