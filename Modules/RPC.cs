using System;
using System.Threading.Tasks;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace TownOfHost
{
    enum CustomRPC
    {
        VersionCheck = 60,
        SyncCustomSettings = 80,
        SetDeathReason,
        JesterExiled,
        TerroristWin,
        ArsonistWin,
        SchrodingerCatExiled,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        SetKillOrSpell,
        SetSheriffShotLimit,
        SetDousedPlayer,
        AddNameColorData,
        RemoveNameColorData,
        ResetNameColorData,
        DoSpell,
        SniperSync,
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
            switch (rpcType)
            {
                case RpcCalls.SetName: //SetNameRPC
                    string name = reader.ReadString();
                    bool DontShowOnModdedClient = false;
                    if (reader.BytesRemaining > 0)
                    {
                        DontShowOnModdedClient = reader.ReadBoolean();
                    }
                    Logger.info("名前変更:" + __instance.name + " => " + name); //ログ
                    if (!DontShowOnModdedClient)
                    {
                        __instance.SetName(name);
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
                        string version = reader.ReadString();
                        string tag = reader.ReadString();
                        main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag);
                    }
                    catch
                    {
                        Logger.info($"{__instance.getRealName()}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                    }
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
                case CustomRPC.JesterExiled:
                    byte exiledJester = reader.ReadByte();
                    RPC.JesterExiled(exiledJester);
                    break;
                case CustomRPC.TerroristWin:
                    byte wonTerrorist = reader.ReadByte();
                    RPC.TerroristWin(wonTerrorist);
                    break;
                case CustomRPC.ArsonistWin:
                    byte wonArsonist = reader.ReadByte();
                    RPC.ArsonistWin(wonArsonist);
                    break;
                case CustomRPC.SchrodingerCatExiled:
                    byte exiledSchrodingerCat = reader.ReadByte();
                    break;
                case CustomRPC.EndGame:
                    RPC.EndGame();
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
                    byte HunterId = reader.ReadByte();
                    byte TargetId = reader.ReadByte();
                    var target = Utils.getPlayerById(TargetId);
                    if (target != null) main.BountyTargets[HunterId] = target;
                    break;
                case CustomRPC.SetKillOrSpell:
                    byte playerId = reader.ReadByte();
                    bool KoS = reader.ReadBoolean();
                    main.KillOrSpell[playerId] = KoS;
                    break;
                case CustomRPC.SetSheriffShotLimit:
                    byte SheriffId = reader.ReadByte();
                    float Limit = reader.ReadSingle();
                    if (main.SheriffShotLimit.ContainsKey(SheriffId))
                        main.SheriffShotLimit[SheriffId] = Limit;
                    else
                        main.SheriffShotLimit.Add(SheriffId, Options.SheriffShotLimit.GetFloat());
                    break;
                case CustomRPC.SetDousedPlayer:
                    byte ArsonistId = reader.ReadByte();
                    byte DousedId = reader.ReadByte();
                    bool doused = reader.ReadBoolean();
                    main.isDoused[(ArsonistId, DousedId)] = doused;
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
                    main.SpelledPlayer.Add(Utils.getPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SniperSync:
                    Sniper.RecieveRPC(reader);
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
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(targetPlayer.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, sendTo.getClientId());
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
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, Hazel.SendOption.Reliable);
            writer.Write(main.PluginVersion);
            writer.Write($"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
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

        public static void JesterExiled(byte jesterID)
        {
            main.ExiledJesterID = jesterID;
            main.currentWinner = CustomWinner.Jester;
            PlayerControl Jester = null;
            PlayerControl Imp = null;
            List<PlayerControl> Impostors = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == jesterID) Jester = p;
                if (p.Data.Role.IsImpostor)
                {
                    if (Imp == null) Imp = p;
                    Impostors.Add(p);
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var imp in Impostors)
                {
                    imp.RpcSetRole(RoleTypes.GuardianAngel);
                }
                new LateTask(() => main.CustomWinTrigger = true,
                0.2f, "Custom Win Trigger Task");
            }
        }
        public static void TerroristWin(byte terroristID)
        {
            main.WonTerroristID = terroristID;
            main.currentWinner = CustomWinner.Terrorist;
            PlayerControl Terrorist = null;
            List<PlayerControl> Impostors = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == terroristID) Terrorist = p;
                if (p.Data.Role.IsImpostor)
                {
                    Impostors.Add(p);
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var imp in Impostors)
                {
                    imp.RpcSetRole(RoleTypes.GuardianAngel);
                }
                new LateTask(() => main.CustomWinTrigger = true,
                0.2f, "Custom Win Trigger Task");
            }
        }
        public static void ArsonistWin(byte arsonistID)
        {
            main.WonArsonistID = arsonistID;
            main.currentWinner = CustomWinner.Arsonist;
            PlayerControl Arsonist = null;
            PlayerControl Imp = null;
            List<PlayerControl> Impostors = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == arsonistID) Arsonist = p;
                if (p.Data.Role.IsImpostor)
                {
                    if (Imp == null) Imp = p;
                    Impostors.Add(p);
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var imp in Impostors)
                {
                    imp.RpcSetRole(RoleTypes.GuardianAngel);
                }
                new LateTask(() => main.CustomWinTrigger = true,
                0.2f, "Custom Win Trigger Task");
            }
        }
        public static void EndGame()
        {
            if (ShipStatus.Instance == null) return;
            main.currentWinner = CustomWinner.Draw;
            if (AmongUsClient.Instance.AmHost)
            {
                ShipStatus.Instance.enabled = false;
                ShipStatus.RpcEndGame(GameOverReason.ImpostorByKill, false);
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
            main.AllPlayerCustomRoles[targetId] = role;
            if (role == CustomRoles.Sniper) Sniper.Add(targetId);
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
        public static void sendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
        {
            if (!main.AmDebugger.Value) return;
            string rpcName;
            string from = targetNetId.ToString();
            string target = targetClientId.ToString();
            if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
            else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
            else rpcName = callId.ToString();
            try
            {
                target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
                from = PlayerControl.AllPlayerControls.ToArray().Where(c => c.NetId == targetNetId).FirstOrDefault().Data.PlayerName;
            }
            catch { }
            Logger.info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
    class StartRpcPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
        {
            RPC.sendRpcLogger(targetNetId, callId);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    class StartRpcImmediatelyPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
        {
            RPC.sendRpcLogger(targetNetId, callId, targetClientId);
        }
    }
}
