using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace TownOfHost
{
    enum CustomRPC
    {
        SyncCustomSettings = 80,
        JesterExiled,
        TerroristWin,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetBountyTarget,
        SetKillOrSpell,
    }
    public enum Sounds
    {
        KillSound
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCHandlerPatch {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)]byte callId, [HarmonyArgument(1)]MessageReader reader) {
            byte packetID = callId;
            switch (packetID)
            {
                case 6: //SetNameRPC
                    string name = reader.ReadString();
                    bool DontShowOnModdedClient = reader.ReadBoolean();
                    Logger.info("名前変更:" + __instance.name + " => " + name); //ログ
                    if(!DontShowOnModdedClient){
                        __instance.SetName(name);
                    }
                    return false;
            }
            return true;
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)]byte callId, [HarmonyArgument(1)]MessageReader reader) {
            byte packetID = callId;
            switch (packetID)
            {
                case (byte)CustomRPC.SyncCustomSettings:
                    int JesterCount = reader.ReadInt32();
                    int MadmateCount = reader.ReadInt32();
                    int BaitCount = reader.ReadInt32();
                    int TerroristCount = reader.ReadInt32();
                    int MafiaCount = reader.ReadInt32();
                    int VampireCount = reader.ReadInt32();
                    int SabotageMasterCount = reader.ReadInt32();
                    int MadGuardianCount = reader.ReadInt32();
                    int MayorCount = reader.ReadInt32();
                    int OpportunistCount = reader.ReadInt32();
                    int SnitchCount = reader.ReadInt32();
                    int SheriffCount = reader.ReadInt32();
                    int BountyHunterCount = reader.ReadInt32();
                    int WitchCount = reader.ReadInt32();
                    int FoxCount = reader.ReadInt32();
                    int TrollCount = reader.ReadInt32();

                    bool IsHideAndSeek = reader.ReadBoolean();
                    bool NoGameEnd = reader.ReadBoolean();
                    bool SwipeCardDisabled = reader.ReadBoolean();
                    bool SubmitScanDisabled = reader.ReadBoolean();
                    bool UnlockSafeDisabled = reader.ReadBoolean();
                    bool UploadDataDisabled = reader.ReadBoolean();
                    bool StartReactorDisabled = reader.ReadBoolean();
                    int VampireKillDelay = reader.ReadInt32();
                    int SabotageMasterSkillLimit = reader.ReadInt32();
                    bool SabotageMasterFixesDoors = reader.ReadBoolean();
                    bool SabotageMasterFixesReactors = reader.ReadBoolean();
                    bool SabotageMasterFixesOxygens = reader.ReadBoolean();
                    bool SabotageMasterFixesCommunications = reader.ReadBoolean();
                    bool SabotageMasterFixesElectrical = reader.ReadBoolean();
                    bool SheriffCanKillJester = reader.ReadBoolean();
                    bool SheriffCanKillTerrorist = reader.ReadBoolean();
                    bool SheriffCanKillOpportunist = reader.ReadBoolean();
                    bool SyncButtonMode = reader.ReadBoolean();
                    int SyncedButtonCount = reader.ReadInt32();
                    int whenSkipVote = reader.ReadInt32();
                    int whenNonVote = reader.ReadInt32();
                    bool canTerroristSuicideWin = reader.ReadBoolean();
                    bool AllowCloseDoors = reader.ReadBoolean();
                    int HaSKillDelay = reader.ReadInt32();
                    bool IgnoreVent = reader.ReadBoolean();
                    bool MadmateCanFixLightsOut = reader.ReadBoolean();
                    bool MadGuardianCanSeeBarrier = reader.ReadBoolean();
                    int MayorAdditionalVote = reader.ReadInt32();
                    RPCProcedure.SyncCustomSettings(
                        JesterCount,
                        MadmateCount,
                        BaitCount,
                        TerroristCount,
                        MafiaCount,
                        VampireCount,
                        SabotageMasterCount,
                        MadGuardianCount,
                        MayorCount,
                        OpportunistCount,
                        SnitchCount,
                        SheriffCount,
                        BountyHunterCount,
                        WitchCount,
                        FoxCount,
                        TrollCount,
                        IsHideAndSeek,
                        NoGameEnd,
                        SwipeCardDisabled,
                        SubmitScanDisabled,
                        UnlockSafeDisabled,
                        UploadDataDisabled,
                        StartReactorDisabled,
                        VampireKillDelay,
                        SabotageMasterSkillLimit,
                        SabotageMasterFixesDoors,
                        SabotageMasterFixesReactors,
                        SabotageMasterFixesOxygens,
                        SabotageMasterFixesCommunications,
                        SabotageMasterFixesElectrical,
                        SheriffCanKillJester,
                        SheriffCanKillTerrorist,
                        SheriffCanKillOpportunist,
                        SyncButtonMode,
                        SyncedButtonCount,
                        whenSkipVote,
                        whenNonVote,
                        canTerroristSuicideWin,
                        AllowCloseDoors,
                        HaSKillDelay,
                        IgnoreVent,
                        MadmateCanFixLightsOut,
                        MadGuardianCanSeeBarrier,
                        MayorAdditionalVote
                    );
                    break;
                case (byte)CustomRPC.JesterExiled:
                    byte exiledJester = reader.ReadByte();
                    RPCProcedure.JesterExiled(exiledJester);
                    break;
                case (byte)CustomRPC.TerroristWin:
                    byte wonTerrorist = reader.ReadByte();
                    RPCProcedure.TerroristWin(wonTerrorist);
                    break;
                case (byte)CustomRPC.EndGame:
                    RPCProcedure.EndGame();
                    break;
                case (byte)CustomRPC.PlaySound:
                    byte playerID = reader.ReadByte();
                    Sounds sound = (Sounds)reader.ReadByte();
                    RPCProcedure.PlaySound(playerID, sound);
                    break;
                case (byte)CustomRPC.SetCustomRole:
                    byte CustomRoleTargetId = reader.ReadByte();
                    CustomRoles role = (CustomRoles)reader.ReadByte();
                    RPCProcedure.SetCustomRole(CustomRoleTargetId, role);
                    break;
                case (byte)CustomRPC.SetBountyTarget:
                    byte HunterId = reader.ReadByte();
                    byte TargetId = reader.ReadByte();
                    var target = main.getPlayerById(TargetId);
                    if(target != null) main.BountyTargets[HunterId] = target;
                    break;
                case (byte)CustomRPC.SetKillOrSpell:
                    byte playerId = reader.ReadByte();
                    bool KoS = reader.ReadBoolean();
                    main.KillOrSpell[playerId] = KoS;
                    break;
            }
        }
    }
    static class RPCProcedure {
        public static void SyncCustomSettings(
                int JesterCount,
                int MadmateCount,
                int BaitCount,
                int TerroristCount,
                int MafiaCount,
                int VampireCount,
                int SabotageMasterCount,
                int MadGuardianCount,
                int MayorCount,
                int OpportunistCount,
                int SnitchCount,
                int SheriffCount,
                int BountyHunterCount,
                int WitchCount,
                int FoxCount,
                int TrollCount,
                bool isHideAndSeek,
                bool NoGameEnd,
                bool SwipeCardDisabled,
                bool SubmitScanDisabled,
                bool UnlockSafeDisabled,
                bool UploadDataDisabled,
                bool StartReactorDisabled,
                int VampireKillDelay,
                int SabotageMasterSkillLimit,
                bool SabotageMasterFixesDoors,
                bool SabotageMasterFixesReactors,
                bool SabotageMasterFixesOxygens,
                bool SabotageMasterFixesCommunications,
                bool SabotageMasterFixesElectrical,
                bool SheriffCanKillJester,
                bool SheriffCanKillTerrorist,
                bool SheriffCanKillOpportunist,
                bool SyncButtonMode,
                int SyncedButtonCount,
                int whenSkipVote,
                int whenNonVote,
                bool canTerroristSuicideWin,
                bool AllowCloseDoors,
                int HaSKillDelay,
                bool IgnoreVent,
                bool MadmateCanFixLightsOut,
                bool MadGuardianCanSeeBarrier,
                int MayorAdditionalVote
            ) {
            main.JesterCount = JesterCount;
            main.MadmateCount = MadmateCount;
            main.BaitCount = BaitCount;
            main.TerroristCount = TerroristCount;
            main.MafiaCount= MafiaCount;
            main.VampireCount= VampireCount;
            main.SabotageMasterCount= SabotageMasterCount;
            main.MadGuardianCount = MadGuardianCount;
            main.MayorCount = MayorCount;
            main.OpportunistCount= OpportunistCount;
            main.SnitchCount= SnitchCount;
            main.SheriffCount = SheriffCount;
            main.BountyHunterCount= BountyHunterCount;
            main.WitchCount = WitchCount;

            main.FoxCount = FoxCount;
            main.TrollCount = TrollCount;

            main.IsHideAndSeek = isHideAndSeek;
            main.NoGameEnd = NoGameEnd;

            main.DisableSwipeCard = SwipeCardDisabled;
            main.DisableSubmitScan = SubmitScanDisabled;
            main.DisableUnlockSafe = UnlockSafeDisabled;
            main.DisableUploadData = UploadDataDisabled;
            main.DisableStartReactor = StartReactorDisabled;

            main.currentWinner = CustomWinner.Default;
            main.CustomWinTrigger = false;

            main.VisibleTasksCount = true;

            main.VampireKillDelay = VampireKillDelay;

            main.SabotageMasterSkillLimit = SabotageMasterSkillLimit;
            main.SabotageMasterFixesDoors = SabotageMasterFixesDoors;
            main.SabotageMasterFixesReactors = SabotageMasterFixesReactors;
            main.SabotageMasterFixesOxygens = SabotageMasterFixesOxygens;
            main.SabotageMasterFixesCommunications = SabotageMasterFixesCommunications;
            main.SabotageMasterFixesElectrical = SabotageMasterFixesElectrical;

            main.SheriffCanKillJester = SheriffCanKillJester;
            main.SheriffCanKillTerrorist = SheriffCanKillTerrorist;
            main.SheriffCanKillOpportunist = SheriffCanKillOpportunist;

            main.SyncButtonMode = SyncButtonMode;
            main.SyncedButtonCount = SyncedButtonCount;

            main.whenSkipVote = (VoteMode)whenSkipVote;
            main.whenNonVote = (VoteMode)whenNonVote;
            main.canTerroristSuicideWin = canTerroristSuicideWin;

            main.AllowCloseDoors = AllowCloseDoors;
            main.HideAndSeekKillDelay = HaSKillDelay;
            main.IgnoreVent = IgnoreVent;

            main.MadmateCanFixLightsOut = MadmateCanFixLightsOut;
            main.MadGuardianCanSeeBarrier = MadGuardianCanSeeBarrier;

            main.MayorAdditionalVote = MayorAdditionalVote;
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
                Task task = Task.Run(() =>
                {
                    Thread.Sleep(100);
                    foreach (var imp in Impostors)
                    {
                        imp.RpcSetRole(RoleTypes.GuardianAngel);
                    }
                    Thread.Sleep(100);
                    main.CustomWinTrigger = true;
                });
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
                Thread.Sleep(100);
                main.CustomWinTrigger = true;
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
                }
            }
        }
        public static void SetCustomRole(byte targetId, CustomRoles role) {
            main.AllPlayerCustomRoles[targetId] = role;
            HudManager.Instance.SetHudActive(true);
        }
    }
}
