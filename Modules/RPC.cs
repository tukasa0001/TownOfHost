using System.Threading.Tasks;
using System;
using HarmonyLib;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    enum CustomRPC
    {
        VersionCheck = 60,
        SyncCustomSettings = 80,
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
        DoSpell
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
            byte packetID = callId;
            switch (packetID)
            {
                case 6: //SetNameRPC
                    string name = reader.ReadString();
                    bool DontShowOnModdedClient = reader.ReadBoolean();
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
            byte packetID = callId;
            switch (packetID)
            {
                case (byte)CustomRPC.VersionCheck:
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
                case (byte)CustomRPC.SyncCustomSettings:
                    foreach (var co in CustomOption.Options)
                    {
                        //すべてのカスタムオプションについてインデックス値で受信
                        co.Selection = reader.ReadInt32();
                    }
                    break;
                case (byte)CustomRPC.JesterExiled:
                    byte exiledJester = reader.ReadByte();
                    RPC.JesterExiled(exiledJester);
                    break;
                case (byte)CustomRPC.TerroristWin:
                    byte wonTerrorist = reader.ReadByte();
                    RPC.TerroristWin(wonTerrorist);
                    break;
                case (byte)CustomRPC.ArsonistWin:
                    byte wonArsonist = reader.ReadByte();
                    RPC.ArsonistWin(wonArsonist);
                    break;
                case (byte)CustomRPC.SchrodingerCatExiled:
                    byte exiledSchrodingerCat = reader.ReadByte();
                    break;
                case (byte)CustomRPC.EndGame:
                    RPC.EndGame();
                    break;
                case (byte)CustomRPC.PlaySound:
                    byte playerID = reader.ReadByte();
                    Sounds sound = (Sounds)reader.ReadByte();
                    RPC.PlaySound(playerID, sound);
                    break;
                case (byte)CustomRPC.SetCustomRole:
                    byte CustomRoleTargetId = reader.ReadByte();
                    CustomRoles role = (CustomRoles)reader.ReadByte();
                    RPC.SetCustomRole(CustomRoleTargetId, role);
                    break;
                case (byte)CustomRPC.SetBountyTarget:
                    byte HunterId = reader.ReadByte();
                    byte TargetId = reader.ReadByte();
                    var target = Utils.getPlayerById(TargetId);
                    if (target != null) main.BountyTargets[HunterId] = target;
                    break;
                case (byte)CustomRPC.SetKillOrSpell:
                    byte playerId = reader.ReadByte();
                    bool KoS = reader.ReadBoolean();
                    main.KillOrSpell[playerId] = KoS;
                    break;
                case (byte)CustomRPC.SetSheriffShotLimit:
                    byte SheriffId = reader.ReadByte();
                    float Limit = reader.ReadSingle();
                    if (main.SheriffShotLimit.ContainsKey(SheriffId))
                        main.SheriffShotLimit[SheriffId] = Limit;
                    else
                        main.SheriffShotLimit.Add(SheriffId, Options.SheriffShotLimit.GetFloat());
                    break;
                case (byte)CustomRPC.SetDousedPlayer:
                    byte ArsonistId = reader.ReadByte();
                    byte DousedId = reader.ReadByte();
                    bool doused = reader.ReadBoolean();
                    main.isDoused[(ArsonistId, DousedId)] = doused;
                    break;
                case (byte)CustomRPC.AddNameColorData:
                    byte addSeerId = reader.ReadByte();
                    byte addTargetId = reader.ReadByte();
                    string color = reader.ReadString();
                    RPC.AddNameColorData(addSeerId, addTargetId, color);
                    break;
                case (byte)CustomRPC.RemoveNameColorData:
                    byte removeSeerId = reader.ReadByte();
                    byte removeTargetId = reader.ReadByte();
                    RPC.RemoveNameColorData(removeSeerId, removeTargetId);
                    break;
                case (byte)CustomRPC.ResetNameColorData:
                    RPC.ResetNameColorData();
                    break;
                case (byte)CustomRPC.DoSpell:
                    main.SpelledPlayer.Add(Utils.getPlayerById(reader.ReadByte()));
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
    }
}
