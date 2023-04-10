using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        if (!Main.VersionCheat.Value) RPC.RpcVersionCheck();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

        if (GameStates.IsModHost)
            Main.HostClientId = Utils.GetPlayerById(0)?.GetClientId() ?? -1;

        ChatUpdatePatch.DoBlockChat = false;
        GameStates.InGame = false;
        ErrorText.Instance.Clear();

        if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
        {
            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            Main.DoBlockNameChange = false;
            Main.newLobby = true;
            Main.DevRole = new();
            EAC.DeNum = new();
            Main.AllPlayerNames = new();

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
        if (!AmongUsClient.Instance.AmHost) return;

        if (client.Id != AmongUsClient.Instance.ClientId)
        {
            new LateTask(() =>
            {
                if (client.Character == null) return;
                if (client.Character.Data.PlayerLevel != 0 && client.Character.Data.PlayerLevel < Options.KickLowLevelPlayer.GetInt())
                {
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    string msg = string.Format(GetString("KickBecauseLowLevel"), client?.PlayerName);
                    Logger.SendInGame(msg);
                    Logger.Info(msg, "LowLevel Kick");
                }
            }, 1f, "LowLevel Kick Check");
        }

        Logger.Msg($"创建玩家数据：ID{client.Character.PlayerId}: {client.PlayerName}", "CreatePlayer");

        //规范昵称
        var name = client.PlayerName;
        if (Options.FormatNameMode.GetInt() == 2 && client.Id != AmongUsClient.Instance.ClientId)
            name = Main.Get_TName_Snacks;
        else
        {
            name = name.RemoveHtmlTags().Replace(@"\", string.Empty).Replace("/", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);
            if (Options.DisableEmojiName.GetBool())
                name = Regex.Replace(name, @"\p{Cs}", string.Empty);
            if (name.Length > 10) name = name[..10];
            if (name.Length < 1) name = Main.Get_TName_Snacks;
        }
        Main.AllPlayerNames.Remove(client.Character.PlayerId);
        Main.AllPlayerNames.TryAdd(client.Character.PlayerId, name);
        if (!name.Equals(client.PlayerName))
        {
            new LateTask(() =>
            {
                if (client.Character == null) return;
                Logger.Warn($"规范昵称：{client.PlayerName} => {name}", "Name Format");
                client.Character.RpcSetName(name);
            }, 1f, "Name Format");
        }

        new LateTask(() => { if (client.Character == null || !GameStates.IsLobby) return; OptionItem.SyncAllOptions(client.Id); }, 3f, "Sync All Options For New Player");

        new LateTask(() =>
        {
            if (client.Character == null) return;
            if (Main.OverrideWelcomeMsg != "") Utils.SendMessage(Main.OverrideWelcomeMsg, client.Character.PlayerId);
            else TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
        }, 3f, "Welcome Message");
        if (Main.OverrideWelcomeMsg == "" && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
        {
            if (Options.AutoDisplayKillLog.GetBool() && Main.PlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
            {
                new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowKillLog(client.Character.PlayerId);
                    }
                }, 3f, "DisplayKillLog");
            }
            if (Options.AutoDisplayLastRoles.GetBool())
            {
                new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowLastRoles(client.Character.PlayerId);
                    }
                }, 3.1f, "DisplayLastRoles");
            }
            if (Options.AutoDisplayLastResult.GetBool())
            {
                new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.ShowLastResult(client.Character.PlayerId);
                    }
                }, 3.2f, "DisplayLastResult");
            }
            if (PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp && Options.EnableUpMode.GetBool())
            {
                new LateTask(() =>
                {
                    if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                    {
                        Main.isChatCommand = true;
                        Utils.SendMessage($"提示：该房间启用了【创作者素材保护计划】，房主可以指定自己的职业。\n该功能仅允许创作者用于获取视频素材，如遇滥用情况，请退出游戏或举报。\n当前创作者认证：{PlayerControl.LocalPlayer.FriendCode.GetDevUser().UpName}", client.Character.PlayerId);
                    }
                }, 3.3f, "DisplayUpWarnning");
            }
        }
    }
}