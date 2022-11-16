using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using static TownOfHost.Translator;

namespace TownOfHost
{
    enum CustomRPC
    {
        VersionCheck = 60,
        RequestRetryVersionCheck = 61,
        SyncCustomSettings = 80,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        SetKillOrSpell,
        SetSheriffShotLimit,
        SetTimeThiefKillCount,
        SetDousedPlayer,
        AddNameColorData,
        RemoveNameColorData,
        ResetNameColorData,
        DoSpell,
        SniperSync,
        SetLoversPlayers,
        SetExecutionerTarget,
        RemoveExecutionerTarget,
        SendFireWorksState,
        SetCurrentDousingTarget,
        SetEvilTrackerTarget,
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
            Logger.Info($"{__instance?.Data?.PlayerId}({__instance?.Data?.PlayerName}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
            MessageReader subReader = MessageReader.Get(reader);
            switch (rpcType)
            {
                case RpcCalls.SetName: //SetNameRPC
                    string name = subReader.ReadString();
                    if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                    Logger.Info("名前変更:" + __instance.GetNameWithRole() + " => " + name, "SetName");
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
                && !(callId == (byte)CustomRPC.VersionCheck || callId == (byte)CustomRPC.RequestRetryVersionCheck)) //ホストではなく、CustomRPCで、VersionCheckではない
            {
                Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) ホスト以外から送信されたためキャンセルしました。", "CustomRPC");
                if (AmongUsClient.Instance.AmHost)
                {
                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                    Logger.Warn($"不正なRPCを受信したため{__instance?.Data?.PlayerName}をキックしました。", "Kick");
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
                    foreach (var co in CustomOption.Options)
                    {
                        //すべてのカスタムオプションについてインデックス値で受信
                        co.Selection = reader.ReadInt32();
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
                    byte playerId = reader.ReadByte();
                    bool KoS = reader.ReadBoolean();
                    Main.KillOrSpell[playerId] = KoS;
                    break;
                case CustomRPC.SetSheriffShotLimit:
                    Sheriff.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetTimeThiefKillCount:
                    TimeThief.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetDousedPlayer:
                    byte ArsonistId = reader.ReadByte();
                    byte DousedId = reader.ReadByte();
                    bool doused = reader.ReadBoolean();
                    Main.isDoused[(ArsonistId, DousedId)] = doused;
                    break;
                case CustomRPC.AddNameColorData:
                    byte addSeerId = reader.ReadByte();
                    byte addTargetId = reader.ReadByte();
                    string color = reader.ReadString();
                    RPC.AddNameColorData(addSeerId, addTargetId, color);
                    break;
                case CustomRPC.RemoveNameColorData:
                    byte removeSeerId = reader.ReadByte();
                    byte removeTargetId = reader.ReadByte();
                    RPC.RemoveNameColorData(removeSeerId, removeTargetId);
                    break;
                case CustomRPC.ResetNameColorData:
                    RPC.ResetNameColorData();
                    break;
                case CustomRPC.DoSpell:
                    Main.SpelledPlayer.Add(Utils.GetPlayerById(reader.ReadByte()));
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
                case CustomRPC.SetEvilTrackerTarget:
                    byte TrackerId = reader.ReadByte();
                    int TargetId = reader.ReadInt32();
                    EvilTracker.RPCSetTarget(TrackerId, TargetId);
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
            foreach (var co in CustomOption.Options)
            {
                //すべてのカスタムオプションについてインデックス値で送信
                writer.Write(co.GetSelection());
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
        public static void RpcSetRole(PlayerControl targetPlayer, PlayerControl sendTo, RoleTypes role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(targetPlayer.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, sendTo.GetClientId());
            writer.Write((byte)role);
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
            PlayerState.deathReasons[playerId] = deathReason;
            PlayerState.isDead[playerId] = true;
        }

        public static void EndGame(MessageReader reader)
        {
            try
            {
                CustomWinnerHolder.ReadFrom(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"正常にEndGameを行えませんでした。{ex}", "EndGame");
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
            if (role < CustomRoles.NoSubRoleAssigned)
            {
                Main.AllPlayerCustomRoles[targetId] = role;
            }
            else if (role >= CustomRoles.NoSubRoleAssigned)   //500:NoSubRole 501~:SubRole
            {
                Main.AllPlayerCustomSubRoles[targetId] = role;
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
                case CustomRoles.EvilHacker:
                    EvilHacker.Add(targetId);
                    break;

                case CustomRoles.Egoist:
                    Egoist.Add(targetId);
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    TeamEgoist.Add(targetId);
                    break;
                case CustomRoles.Executioner:
                    Executioner.Add(targetId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.Add(targetId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.Add(targetId);
                    break;
            }
            HudManager.Instance.SetHudActive(true);
        }
        public static void AddNameColorData(byte seerId, byte targetId, string color)
        {
            NameColorManager.Instance.Add(seerId, targetId, color);
        }
        public static void RemoveNameColorData(byte seerId, byte targetId)
        {
            NameColorManager.Instance.Remove(seerId, targetId);
        }
        public static void ResetNameColorData()
        {
            NameColorManager.Begin();
        }
        public static void RpcDoSpell(byte player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, Hazel.SendOption.Reliable, -1);
            writer.Write(player);
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
            if (!Main.AmDebugger.Value) return;
            string rpcName = GetRpcName(callId);
            string from = targetNetId.ToString();
            string target = targetClientId.ToString();
            try
            {
                target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
                from = PlayerControl.AllPlayerControls.ToArray().Where(c => c.NetId == targetNetId).FirstOrDefault()?.Data?.PlayerName;
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
        public static void ResetCurrentDousingTarget(byte arsonistId) => SetCurrentDousingTarget(arsonistId, 255);
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