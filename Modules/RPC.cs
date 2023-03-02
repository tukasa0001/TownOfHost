using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE
{
    enum CustomRPC
    {
        VersionCheck = 60,
        RequestRetryVersionCheck = 61,
        AntiBlack = 62,
        SyncCustomSettings = 80,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        SetKillOrSpell,
        SetSheriffShotLimit,
        SetDousedPlayer,
        SetDrawPlayer,
        SetNameColorData,
        DoSpell,
        SniperSync,
        SetLoversPlayers,
        SetExecutionerTarget,
        RemoveExecutionerTarget,
        SendFireWorksState,
        SetCurrentDousingTarget,
        SetCurrentDrawTarget,
        SetEvilTrackerTarget,
        SetRealKiller,
    }
    public enum Sounds
    {
        KillSound,
        TaskComplete
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCHandlerPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            var rpcType = (RpcCalls)callId;
            MessageReader subReader = MessageReader.Get(reader);
            if (EAC.Receive(__instance, callId, reader)) return false;
            Logger.Info($"{__instance?.Data?.PlayerId}({__instance?.Data?.PlayerName}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
            switch (rpcType)
            {
                case RpcCalls.SetName: //SetNameRPC
                    string name = subReader.ReadString();
                    if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                    Logger.Info("名称修改:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                    break;
                case RpcCalls.SetRole: //SetNameRPC
                    var role = (RoleTypes)subReader.ReadUInt16();
                    Logger.Info("设置角色:" + __instance.GetRealName() + " => " + role, "SetRole");
                    break;
                case RpcCalls.SendChat:
                    var text = subReader.ReadString();
                    Logger.Info($"{__instance.GetNameWithRole()}:{text}", "SendChat");
                    ChatCommands.OnReceiveChat(__instance, text);
                    break;
                case RpcCalls.StartMeeting:
                    var p = Utils.GetPlayerById(subReader.ReadByte());
                    Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                    break;
            }
            if (__instance.PlayerId != 0
                && Enum.IsDefined(typeof(CustomRPC), (int)callId)
                && !(callId == (byte)CustomRPC.VersionCheck || callId == (byte)CustomRPC.RequestRetryVersionCheck || callId == (byte)CustomRPC.AntiBlack)) //ホストではなく、CustomRPCで、VersionCheckではない
            {
                Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) 已取消，因为它是由主机以外的其他人发送的。", "CustomRPC");
                if (AmongUsClient.Instance.AmHost)
                {
                    if (Main.LastRPC.ContainsKey(__instance.PlayerId))
                    {
                        if (Main.LastRPC[__instance.PlayerId] == byte.MaxValue) return false; //已处理
                        string text = "";
                        if (Main.LastRPC[__instance.PlayerId] == callId && EAC.CheckAUM(callId, ref text))
                        {
                            EAC.Report(__instance, "AUM");
                            switch (Options.CheatResponses.GetInt())
                            {
                                case 0:
                                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), true);
                                    Logger.Warn($"检测到 {__instance?.Data?.PlayerName} 正在使用作弊程序，因此将其踢出：{text}", "Kick");
                                    Logger.SendInGame(string.Format($"封禁 {__instance?.Data?.PlayerName}，理由：{text}", __instance?.Data?.PlayerName));
                                    break;
                                case 1:
                                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                                    Logger.Warn($"检测到 {__instance?.Data?.PlayerName} 正在使用作弊程序，因此将其踢出：{text}", "Kick");
                                    Logger.SendInGame(string.Format($"踢出 {__instance?.Data?.PlayerName}，理由：{text}", __instance?.Data?.PlayerName));
                                    break;
                                case 2:
                                    Utils.SendMessage($"检测到 {__instance?.Data?.PlayerName}：{text}", PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "【 ★ 作弊检测 ★ 】"));
                                    break;
                                case 3:
                                    foreach (var pc in PlayerControl.AllPlayerControls)
                                    {
                                        if (pc != null && pc.PlayerId != __instance?.Data?.PlayerId)
                                        {
                                            Utils.SendMessage($"检测到 {__instance?.Data?.PlayerName}：{text}", pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "【 ★ 作弊检测 ★ 】"));
                                        }
                                    }
                                    break;
                            }
                            Main.LastRPC[__instance.PlayerId] = byte.MaxValue;
                            return false;
                        }
                    }
                    else
                    {
                        Main.LastRPC.Add(__instance.PlayerId, callId);
                        return false;
                    }
                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                    Logger.Warn($"多次收到来自 {__instance?.Data?.PlayerName} 的不受信用的RPC，因此将其踢出。", "Kick");
                    Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
                }
                return false;
            }
            return true;
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {

            var rpcType = (CustomRPC)callId;
            switch (rpcType)
            {
                case CustomRPC.AntiBlack:
                    if (Options.EndWhenPlayerBug.GetBool())
                    {
                        Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                        ChatUpdatePatch.DoBlockChat = true;
                        Main.OverrideWelcomeMsg = $"由于玩家【{__instance?.Data?.PlayerName}】发生未知错误，已终止游戏防止卡房。若您不希望在其他玩家发生错误时终止游戏，请在设置关闭【{GetString("EndWhenPlayerBug")}】";
                        new LateTask(() =>
                        {
                            Logger.SendInGame($"【{__instance?.Data?.PlayerName}】发生未知错误，将终止游戏以防止黑屏", true);
                        }, 3f, "Anti-Black Msg SendInGame");
                        new LateTask(() =>
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                            GameManager.Instance.LogicFlow.CheckEndCriteria();
                            RPC.ForceEndGame(CustomWinner.Error);
                        }, 5.5f, "Anti-Black End Game");
                    }
                    else
                    {
                        Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                        new LateTask(() =>
                        {
                            Logger.SendInGame($"【{__instance?.Data?.PlayerName}】发生未知错误，根据房主设置将忽略该玩家", true);
                        }, 3f, "Anti-Black Msg SendInGame");
                    }
                    break;
                case CustomRPC.VersionCheck:
                    try
                    {
                        Version version = Version.Parse(reader.ReadString());
                        string tag = reader.ReadString();
                        string forkId = 3 <= version.Major ? reader.ReadString() : Main.OriginalForkId;
                        Main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag, forkId);
                    }
                    catch
                    {
                        Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                        new LateTask(() =>
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }, 1f, "Retry Version Check Task");
                    }
                    break;
                case CustomRPC.RequestRetryVersionCheck:
                    RPC.RpcVersionCheck();
                    break;
                case CustomRPC.SyncCustomSettings:
                    foreach (var co in OptionItem.AllOptions)
                    {
                        //すべてのカスタムオプションについてインデックス値で受信
                        co.SetValue(reader.ReadInt32());
                    }
                    break;
                case CustomRPC.SetDeathReason:
                    RPC.GetDeathReason(reader);
                    break;
                case CustomRPC.EndGame:
                    RPC.EndGame(reader);
                    break;
                case CustomRPC.PlaySound:
                    byte playerID = reader.ReadByte();
                    Sounds sound = (Sounds)reader.ReadByte();
                    RPC.PlaySound(playerID, sound);
                    break;
                case CustomRPC.SetCustomRole:
                    byte CustomRoleTargetId = reader.ReadByte();
                    CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                    RPC.SetCustomRole(CustomRoleTargetId, role);
                    break;
                case CustomRPC.SetBountyTarget:
                    BountyHunter.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetKillOrSpell:
                    Witch.ReceiveRPC(reader, false);
                    break;
                case CustomRPC.SetSheriffShotLimit:
                    Sheriff.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetDousedPlayer:
                    byte ArsonistId = reader.ReadByte();
                    byte DousedId = reader.ReadByte();
                    bool doused = reader.ReadBoolean();
                    Main.isDoused[(ArsonistId, DousedId)] = doused;
                    break;
                case CustomRPC.SetNameColorData:
                    NameColorManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.DoSpell:
                    Witch.ReceiveRPC(reader, true);
                    break;
                case CustomRPC.SniperSync:
                    Sniper.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetLoversPlayers:
                    Main.LoversPlayers.Clear();
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                        Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SetExecutionerTarget:
                    Executioner.ReceiveRPC(reader, SetTarget: true);
                    break;
                case CustomRPC.RemoveExecutionerTarget:
                    Executioner.ReceiveRPC(reader, SetTarget: false);
                    break;
                case CustomRPC.SendFireWorksState:
                    FireWorks.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetCurrentDousingTarget:
                    byte arsonistId = reader.ReadByte();
                    byte dousingTargetId = reader.ReadByte();
                    if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
                        Main.currentDousingTarget = dousingTargetId;
                    break;
                case CustomRPC.SetCurrentDrawTarget:
                    byte arsonistId1 = reader.ReadByte();
                    byte doTargetId = reader.ReadByte();
                    if (PlayerControl.LocalPlayer.PlayerId == arsonistId1)
                        Main.currentDrawTarget = doTargetId;
                    break;
                case CustomRPC.SetEvilTrackerTarget:
                    EvilTracker.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetRealKiller:
                    byte targetId = reader.ReadByte();
                    byte killerId = reader.ReadByte();
                    RPC.SetRealKiller(targetId, killerId);
                    break;
            }
        }
    }
    static class RPC
    {
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            foreach (var co in OptionItem.AllOptions)
            {
                //すべてのカスタムオプションについてインデックス値で送信
                writer.Write(co.GetValue());
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                RPC.PlaySound(PlayerID, sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ExileAsync(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            player.Exiled();
        }
        public static async void RpcVersionCheck()
        {
            while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
            writer.Write(Main.PluginVersion);
            writer.Write($"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(Main.ForkId);
            writer.EndMessage();
            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
        public static void SendDeathReason(byte playerId, PlayerState.DeathReason deathReason)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)deathReason);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GetDeathReason(MessageReader reader)
        {
            var playerId = reader.ReadByte();
            var deathReason = (PlayerState.DeathReason)reader.ReadInt32();
            Main.PlayerStates[playerId].deathReason = deathReason;
            Main.PlayerStates[playerId].IsDead = true;
        }
        public static void ForceEndGame(CustomWinner win)
        {
            if (ShipStatus.Instance == null) return;
            try { CustomWinnerHolder.ResetAndSetWinner(win); }
            catch { }
            if (AmongUsClient.Instance.AmHost)
            {
                ShipStatus.Instance.enabled = false;
                try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
                catch { }
                try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
                catch { }
            }
        }
        public static void EndGame(MessageReader reader)
        {
            try
            {
                CustomWinnerHolder.ReadFrom(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
            }
        }
        public static void PlaySound(byte playerID, Sounds sound)
        {
            if (PlayerControl.LocalPlayer.PlayerId == playerID)
            {
                switch (sound)
                {
                    case Sounds.KillSound:
                        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                        break;
                    case Sounds.TaskComplete:
                        SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 0.8f);
                        break;
                }
            }
        }
        public static void SetCustomRole(byte targetId, CustomRoles role)
        {
            if (role < CustomRoles.NotAssigned)
            {
                Main.PlayerStates[targetId].SetMainRole(role);
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                Main.PlayerStates[targetId].SetSubRole(role);
            }
            switch (role)
            {
                case CustomRoles.BountyHunter:
                    BountyHunter.Add(targetId);
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.Add(targetId);
                    break;
                case CustomRoles.FireWorks:
                    FireWorks.Add(targetId);
                    break;
                case CustomRoles.TimeThief:
                    TimeThief.Add(targetId);
                    break;
                case CustomRoles.Sniper:
                    Sniper.Add(targetId);
                    break;
                case CustomRoles.Mare:
                    Mare.Add(targetId);
                    break;
                case CustomRoles.EvilTracker:
                    EvilTracker.Add(targetId);
                    break;
                case CustomRoles.Witch:
                    Witch.Add(targetId);
                    break;
                case CustomRoles.Vampire:
                    Vampire.Add(targetId);
                    break;
                case CustomRoles.Executioner:
                    Executioner.Add(targetId);
                    break;
                case CustomRoles.Jackal:
                    Jackal.Add(targetId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.Add(targetId);
                    break;
                case CustomRoles.ChivalrousExpert:
                    ChivalrousExpert.Add(targetId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.Add(targetId);
                    break;
                case CustomRoles.Snitch:
                    Snitch.Add(targetId);
                    break;
                case CustomRoles.AntiAdminer:
                    AntiAdminer.Add(targetId);
                    break;
                case CustomRoles.LastImpostor:
                    LastImpostor.Add(targetId);
                    break;
                case CustomRoles.TimeManager:
                    TimeManager.Add(targetId);
                    break;
                case CustomRoles.Workhorse:
                    Workhorse.Add(targetId);
                    break;
                case CustomRoles.Pelican:
                    Pelican.Add(targetId);
                    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.Add(targetId);
                    break;
                case CustomRoles.Gangster:
                    Gangster.Add(targetId);
                    break;
            }
            HudManager.Instance.SetHudActive(true);
            if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
        }
        public static void RpcDoSpell(byte targetId, byte killerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SyncLoversPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, Hazel.SendOption.Reliable, -1);
            writer.Write(Main.LoversPlayers.Count);
            foreach (var lp in Main.LoversPlayers)
            {
                writer.Write(lp.PlayerId);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
        {
            if (!DebugModeManager.AmDebugger) return;
            string rpcName = GetRpcName(callId);
            string from = targetNetId.ToString();
            string target = targetClientId.ToString();
            try
            {
                target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
                from = Main.AllPlayerControls.Where(c => c.NetId == targetNetId).FirstOrDefault()?.Data?.PlayerName;
            }
            catch { }
            Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
        }
        public static string GetRpcName(byte callId)
        {
            string rpcName;
            if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
            else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
            else rpcName = callId.ToString();
            return rpcName;
        }
        public static void SetCurrentDousingTarget(byte arsonistId, byte targetId)
        {
            if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
            {
                Main.currentDousingTarget = targetId;
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDousingTarget, Hazel.SendOption.Reliable, -1);
                writer.Write(arsonistId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void SetCurrentDrawTarget(byte arsonistId, byte targetId)
        {
            if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
            {
                Main.currentDrawTarget = targetId;
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDrawTarget, Hazel.SendOption.Reliable, -1);
                writer.Write(arsonistId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void ResetCurrentDousingTarget(byte arsonistId) => SetCurrentDousingTarget(arsonistId, 255);
        public static void ResetCurrentDrawTarget(byte arsonistId) => SetCurrentDrawTarget(arsonistId, 255);
        public static void SetRealKiller(byte targetId, byte killerId)
        {
            var state = Main.PlayerStates[targetId];
            state.RealKiller.Item1 = DateTime.Now;
            state.RealKiller.Item2 = killerId;

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
    class StartRpcPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
        {
            RPC.SendRpcLogger(targetNetId, callId);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
        {
            RPC.SendRpcLogger(targetNetId, callId, targetClientId);
        }
    }
}