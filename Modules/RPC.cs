using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Modules;
using static TownOfHostForE.Translator;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.AddOns.Common;
using TownOfHostForE.GameMode;

namespace TownOfHostForE
{
    public enum CustomRPC
    {
        VersionCheck = 80,
        RequestRetryVersionCheck = 81,
        SyncCustomSettings = 100,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        //SetBountyTarget,
        //WitchSync,
        //SetSheriffShotLimit,
        SetSSheriffShotLimit,
        SetHunterShotLimit,
        //SetDousedPlayer,
        SetNameColorData,
        //SniperSync,
        SetLoversPlayers,
        //SetExecutionerTarget,
        //SetCurrentDousingTarget,
        //SetEvilTrackerTarget,
        SetRealKiller,
        CustomRoleSync,
        SyncPuppet,
        SetOppoKillerShotLimit,
        SetCursedWolfSpellCount,
        SetNBakryPoison,
        BakryChangeSync,
        SetBlinderVisionPlayer,
        SetEvilDiviner,
        SetLawyerTarget,
        LawTrackerSync,
        Guess,
        GuessKill,
        ShowPopUp,
        PlayCustomSound,
        SetPrincessShotLimit,
        SetCinderellaTarget,
        SendChu2Attack,
        JapPupkillTargetSync,
        OniichanTargetSync,
        OniichanStateSync,
        GizokuShotLimitSync,
        TeleportTargetSync,
        GreatDetectiveSync,
        BadgerSync,
        VultureSync,
        SetGrudgeSheriffShotLimit,
        SetPsychicCount,
        SetFortuneTellerTarget,
        SetDuelistTarget,
        SetTotocalcioTarget,
        LoveCuttorSync,
        SendingSync,
        EraserSync,
        SetSchrodingerCatTeam,
        StealthDarken,
        EvilHackerCreateMurderNotify,
        PenguinSync,
        MareSync,
        SyncPlagueDoctor,
        SyncMetaton,
        RedPandaSync,
        SyncBetwinShogo,
        PlaySoundSERPC,
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
                case RpcCalls.SetRole: //SetNameRPC
                    var role = (RoleTypes)subReader.ReadUInt16();
                    Logger.Info("役職:" + __instance.GetRealName() + " => " + role, "SetRole");
                    break;
                case RpcCalls.SendChat:
                    var text = subReader.ReadString();
                    Logger.Info($"{__instance.GetNameWithRole()}:{text}", "SendChat");
                    ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                    if (!GameStates.IsLobby)
                    {
                        var cRole = __instance.GetCustomRole();
                        if(!cRole.IsNotAssignRoles())__instance.GetRoleClass().OnReceiveChat(__instance, text);
                        Ojou.OjouOnReceiveChat(__instance, text);
                        Chu2Byo.Chu2OnReceiveChat(__instance, text);
                        WordLimit.OnReceiveChat(__instance,text);
                    }
                    BetWinTeams.BetOnReceiveChat(__instance, text);
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
            //CustomRPC以外は処理しない
            if (callId < (byte)CustomRPC.VersionCheck) return;

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
                        _ = new LateTask(() =>
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
                        co.SetValue(reader.ReadPackedInt32());
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
                case CustomRPC.SetNameColorData:
                    NameColorManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetLoversPlayers:
                    Main.LoversPlayersV2.Clear();
                    List<byte> lovers = new();
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        byte id = reader.ReadByte();
                        if (id != byte.MaxValue)
                        {
                            lovers.Add(id);
                        }
                        else
                        {
                            //リストに保管 最初のindexを親とする。
                            Main.LoversPlayersV2.Add(lovers[0], lovers);
                            Main.isLoversLeaders.Add(lovers[0]);
                            //リセット
                            lovers.Clear();
                        }
                    }
                    if (AmongUsClient.Instance.AmHost) break;
                    foreach (var list in Main.LoversPlayersV2)
                    {
                        foreach (var loversId in list.Value)
                        {
                            var lovePc = Utils.GetPlayerById(loversId);
                            lovePc.RpcSetCustomRole(CustomRoles.Lovers);
                        }
                    }
                    break;
                case CustomRPC.SetRealKiller:
                    byte targetId = reader.ReadByte();
                    byte killerId = reader.ReadByte();
                    RPC.SetRealKiller(targetId, killerId);
                    break;
                case CustomRPC.ShowPopUp:
                    string msg = reader.ReadString();
                    HudManager.Instance.ShowPopUp(msg);
                    break;
                case CustomRPC.Guess:
                    GuessManager.ReceiveRPC(reader, __instance);
                    break;
                case CustomRPC.GuessKill:
                    GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.PlayCustomSound:
                    CustomSoundsManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.SendingSync:
                    Sending.ReceiveRPC(reader);
                    break;
                case CustomRPC.SyncBetwinShogo:
                    BetWinTeams.ReceiveRPC(reader);
                    break;
                case CustomRPC.PlaySoundSERPC:
                    BGMSettings.ReceiveRPC(reader);
                    break;
                case CustomRPC.CustomRoleSync:
                    CustomRoleManager.DispatchRpc(reader);
                    break;
                default:
                    CustomRoleManager.DispatchRpc(reader, rpcType);
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
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, -1);
            foreach (var co in OptionItem.AllOptions)
            {
                //すべてのカスタムオプションについてインデックス値で送信
                writer.WritePacked(co.GetValue());
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
                RPC.PlaySound(PlayerID, sound);
            if (!AmongUsClient.Instance.AmHost) return;
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
        public static void SendDeathReason(byte playerId, CustomDeathReason deathReason)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)deathReason);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GetDeathReason(MessageReader reader)
        {
            var playerId = reader.ReadByte();
            var deathReason = (CustomDeathReason)reader.ReadInt32();
            var state = PlayerState.GetByPlayerId(playerId);
            state.DeathReason = deathReason;
            state.IsDead = true;
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
                var roleClass = CustomRoleManager.GetByPlayerId(targetId);
                if (roleClass != null)
                {
                    var player = roleClass.Player;
                    roleClass.Dispose();
                    CustomRoleManager.CreateInstance(role, player);
                }

                PlayerState.GetByPlayerId(targetId).SetMainRole(role);
                CustomRoleManager.CreateInstance(role, Utils.GetPlayerById(targetId));
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                PlayerState.GetByPlayerId(targetId).SetSubRole(role);
            }

            HudManager.Instance.SetHudActive(true);
            if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
        }

        //最初とbyteMaxを受け取った後は切り替え式
        public static void SyncLoversPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, Hazel.SendOption.Reliable, -1);

            List<byte> sendData = LoversAllCount();

            writer.Write(sendData.Count);
            foreach (var id in sendData)
            {
                writer.Write(id);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        private static List<byte> LoversAllCount()
        {
            List<byte> sendData = new();

            foreach (var list in Main.LoversPlayersV2)
            {
                foreach (var id in list.Value)
                {
                    sendData.Add(id);
                }
                //切り分け判定
                sendData.Add(byte.MaxValue);
            }

            return sendData;
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
        public static void SetRealKiller(byte targetId, byte killerId)
        {
            var state = PlayerState.GetByPlayerId(targetId);
            state.RealKiller.Item1 = DateTime.Now;
            state.RealKiller.Item2 = killerId;

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, Hazel.SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReportDeadBodyForced(this PlayerControl player, GameData.PlayerInfo target)
        {
            //PlayerControl.ReportDeadBodyと同様の処理
            if (!AmongUsClient.Instance.AmHost) return;
            //if (AmongUsClient.Instance.IsGameOver || (bool)MeetingHud.Instance || (target == null && LocalPlayer.myTasks.Any(PlayerTask.TaskIsEmergency)) || Data.IsDead)
            //    return;

            MeetingRoomManager.Instance.AssignSelf(player, target);
            //if (AmongUsClient.Instance.AmHost && !ShipStatus.Instance.   .CheckTaskCompletion())
            if (AmongUsClient.Instance.AmHost)
            {
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
                player.RpcStartMeeting(target);
            }
        }
        public static void ShowPopUp(this PlayerControl pc, string msg)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
            writer.Write(msg);
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