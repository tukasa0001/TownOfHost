using System.Data;
using System;
using HarmonyLib;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    enum CustomRPC
    {
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
        ResetNameColorData
    }
    public enum Sounds
    {
        KillSound
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
                case (byte)CustomRPC.SyncCustomSettings:
                    foreach (var kvp in Options.CustomRoleSpawnChances)
                    {
                        kvp.Value.Selection = reader.ReadInt32();
                        kvp.Key.setCount(reader.ReadInt32());
                    }
                    int CurrentGameMode = reader.ReadInt32();
                    bool NoGameEnd = reader.ReadBoolean();
                    bool SwipeCardDisabled = reader.ReadBoolean();
                    bool SubmitScanDisabled = reader.ReadBoolean();
                    bool UnlockSafeDisabled = reader.ReadBoolean();
                    bool UploadDataDisabled = reader.ReadBoolean();
                    bool StartReactorDisabled = reader.ReadBoolean();
                    bool ResetBreakerDisabled = reader.ReadBoolean();
                    int VampireKillDelay = reader.ReadInt32();
                    int EvilWatcherChance = reader.ReadInt32();
                    int SabotageMasterSkillLimit = reader.ReadInt32();
                    bool SabotageMasterFixesDoors = reader.ReadBoolean();
                    bool SabotageMasterFixesReactors = reader.ReadBoolean();
                    bool SabotageMasterFixesOxygens = reader.ReadBoolean();
                    bool SabotageMasterFixesCommunications = reader.ReadBoolean();
                    bool SabotageMasterFixesElectrical = reader.ReadBoolean();
                    int SheriffKillCooldown = reader.ReadInt32();
                    bool SheriffCanKillArsonist = reader.ReadBoolean();
                    bool SheriffCanKillJester = reader.ReadBoolean();
                    bool SheriffCanKillTerrorist = reader.ReadBoolean();
                    bool SheriffCanKillOpportunist = reader.ReadBoolean();
                    bool SheriffCanKillMadmate = reader.ReadBoolean();
                    bool SheriffCanKillCrewmatesAsIt = reader.ReadBoolean();
                    int SheriffShotLimit = reader.ReadInt32();
                    bool SyncButtonMode = reader.ReadBoolean();
                    int SyncedButtonCount = reader.ReadInt32();
                    int whenSkipVote = reader.ReadInt32();
                    int whenNonVote = reader.ReadInt32();
                    int ArsonistDouseTime = reader.ReadInt32();
                    int ArsonistCooldown = reader.ReadInt32();
                    bool canTerroristSuicideWin = reader.ReadBoolean();
                    bool RandomMapsMode = reader.ReadBoolean();
                    bool AddedTheSkeld = reader.ReadBoolean();
                    bool AddedMiraHQ = reader.ReadBoolean();
                    bool AddedPolus = reader.ReadBoolean();
                    bool AddedTheAirShip = reader.ReadBoolean();
                    bool AllowCloseDoors = reader.ReadBoolean();
                    int HaSKillDelay = reader.ReadInt32();
                    bool IgnoreVent = reader.ReadBoolean();
                    bool IgnoreCosmetics = reader.ReadBoolean();
                    bool MadmateCanFixLightsOut = reader.ReadBoolean();
                    bool MadmateCanFixComms = reader.ReadBoolean();
                    bool MadmateHasImpostorVision = reader.ReadBoolean();
                    int CanMakeMadmateCount = reader.ReadInt32();
                    bool MadGuardianCanSeeBarrier = reader.ReadBoolean();
                    int MadSnitchTasks = reader.ReadInt32();
                    int MayorAdditionalVote = reader.ReadInt32();
                    int SerialKillerCooldown = reader.ReadInt32();
                    int SerialKillerLimit = reader.ReadInt32();
                    int BountyTargetChangeTime = reader.ReadInt32();
                    int BountySuccessKillCooldown = reader.ReadInt32();
                    int BountyFailureKillCooldown = reader.ReadInt32();
                    int BHDefaultKillCooldown = reader.ReadInt32();
                    int DefaultShapeshiftCooldown = reader.ReadInt32();
                    int ShapeMasterShapeshiftDuration = reader.ReadInt32();
                    bool CanBeforeSchrodingerCatWinTheCrewmate = reader.ReadBoolean();
                    bool SchrodingerCatExiledTeamChanges = reader.ReadBoolean();
                    bool AutoDisplayLastResult = reader.ReadBoolean();
                    bool EnableLastImpostor = reader.ReadBoolean();
                    int LastImpostorKillCooldown = reader.ReadInt32();
                    RPC.SyncCustomSettings(
                        Options.roleCounts,
                        CurrentGameMode,
                        NoGameEnd,
                        SwipeCardDisabled,
                        SubmitScanDisabled,
                        UnlockSafeDisabled,
                        UploadDataDisabled,
                        StartReactorDisabled,
                        ResetBreakerDisabled,
                        VampireKillDelay,
                        SabotageMasterSkillLimit,
                        SabotageMasterFixesDoors,
                        SabotageMasterFixesReactors,
                        SabotageMasterFixesOxygens,
                        SabotageMasterFixesCommunications,
                        SabotageMasterFixesElectrical,
                        SheriffKillCooldown,
                        SheriffCanKillArsonist,
                        SheriffCanKillJester,
                        SheriffCanKillTerrorist,
                        SheriffCanKillOpportunist,
                        SheriffCanKillMadmate,
                        SheriffCanKillCrewmatesAsIt,
                        SheriffShotLimit,
                        SerialKillerCooldown,
                        SerialKillerLimit,
                        BountyTargetChangeTime,
                        BountySuccessKillCooldown,
                        BountyFailureKillCooldown,
                        BHDefaultKillCooldown,
                        DefaultShapeshiftCooldown,
                        ShapeMasterShapeshiftDuration,
                        EvilWatcherChance,
                        EnableLastImpostor,
                        LastImpostorKillCooldown,
                        SyncButtonMode,
                        SyncedButtonCount,
                        whenSkipVote,
                        whenNonVote,
                        ArsonistDouseTime,
                        ArsonistCooldown,
                        canTerroristSuicideWin,
                        RandomMapsMode,
                        AddedTheSkeld,
                        AddedMiraHQ,
                        AddedPolus,
                        AddedTheAirShip,
                        AllowCloseDoors,
                        HaSKillDelay,
                        IgnoreVent,
                        IgnoreCosmetics,
                        MadmateCanFixLightsOut,
                        MadmateCanFixComms,
                        MadmateHasImpostorVision,
                        CanMakeMadmateCount,
                        MadGuardianCanSeeBarrier,
                        MadSnitchTasks,
                        MayorAdditionalVote,
                        CanBeforeSchrodingerCatWinTheCrewmate,
                        SchrodingerCatExiledTeamChanges,
                        AutoDisplayLastResult
                    );
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
            }
        }
    }
    static class RPC
    {
        public static void SyncCustomSettings(
                Dictionary<CustomRoles, int> roleCounts,
                int CurrentGameMode,
                bool NoGameEnd,
                bool SwipeCardDisabled,
                bool SubmitScanDisabled,
                bool UnlockSafeDisabled,
                bool UploadDataDisabled,
                bool StartReactorDisabled,
                bool ResetBreakerDisabled,
                int VampireKillDelay,
                int SabotageMasterSkillLimit,
                bool SabotageMasterFixesDoors,
                bool SabotageMasterFixesReactors,
                bool SabotageMasterFixesOxygens,
                bool SabotageMasterFixesCommunications,
                bool SabotageMasterFixesElectrical,
                int SheriffKillCooldown,
                bool SheriffCanKillArsonist,
                bool SheriffCanKillJester,
                bool SheriffCanKillTerrorist,
                bool SheriffCanKillOpportunist,
                bool SheriffCanKillMadmate,
                bool SheriffCanKillCrewmatesAsIt,
                int SheriffShotLimit,
                int EvilWatcherChance,
                int SerialKillerCooldown,
                int SerialKillerLimit,
                int BountyTargetChangeTime,
                int BountySuccessKillCooldown,
                int BountyFailureKillCooldown,
                int BHDefaultKillCooldown,
                int DefaultShapeshiftCooldown,
                int ShapeMasterShapeshiftDuration,
                bool EnableLastImpostor,
                int LastImpostorKillCooldown,
                bool SyncButtonMode,
                int SyncedButtonCount,
                int whenSkipVote,
                int whenNonVote,
                int ArsonistDouseTime,
                int ArsonistCooldown,
                bool canTerroristSuicideWin,
                bool RandomMapsMode,
                bool AddedTheSkeld,
                bool AddedMiraHQ,
                bool AddedPolus,
                bool AddedTheAirShip,
                bool AllowCloseDoors,
                int HaSKillDelay,
                bool IgnoreVent,
                bool IgnoreCosmetics,
                bool MadmateCanFixLightsOut,
                bool MadmateCanFixComms,
                bool MadmateHasImpostorVision,
                int CanMakeMadmateCount,
                bool MadGuardianCanSeeBarrier,
                int MadSnitchTasks,
                int MayorAdditionalVote,
                bool CanBeforeSchrodingerCatWinTheCrewmate,
                bool SchrodingerCatExiledTeamChanges,
                bool AutoDisplayLastResult
            )
        {
            Options.roleCounts = roleCounts;

            Options.GameMode.UpdateSelection(CurrentGameMode);
            Options.NoGameEnd.UpdateSelection(NoGameEnd);

            Options.DisableSwipeCard.UpdateSelection(SwipeCardDisabled);
            Options.DisableSubmitScan.UpdateSelection(SubmitScanDisabled);
            Options.DisableUnlockSafe.UpdateSelection(UnlockSafeDisabled);
            Options.DisableUploadData.UpdateSelection(UploadDataDisabled);

            Options.DisableStartReactor.UpdateSelection(StartReactorDisabled);
            Options.DisableResetBreaker.UpdateSelection(ResetBreakerDisabled);

            main.currentWinner = CustomWinner.Default;
            main.CustomWinTrigger = false;

            main.VisibleTasksCount = true;

            Options.VampireKillDelay.UpdateSelection(VampireKillDelay);

            Options.SabotageMasterSkillLimit.UpdateSelection(SabotageMasterSkillLimit);
            Options.SabotageMasterFixesDoors.UpdateSelection(SabotageMasterFixesDoors);
            Options.SabotageMasterFixesReactors.UpdateSelection(SabotageMasterFixesReactors);
            Options.SabotageMasterFixesOxygens.UpdateSelection(SabotageMasterFixesOxygens);
            Options.SabotageMasterFixesComms.UpdateSelection(SabotageMasterFixesCommunications);
            Options.SabotageMasterFixesElectrical.UpdateSelection(SabotageMasterFixesElectrical);

            Options.SheriffKillCooldown.UpdateSelection(SheriffKillCooldown);
            Options.SheriffCanKillArsonist.UpdateSelection(SheriffCanKillArsonist);
            Options.SheriffCanKillJester.UpdateSelection(SheriffCanKillJester);
            Options.SheriffCanKillTerrorist.UpdateSelection(SheriffCanKillTerrorist);
            Options.SheriffCanKillOpportunist.UpdateSelection(SheriffCanKillOpportunist);
            Options.SheriffCanKillMadmate.UpdateSelection(SheriffCanKillMadmate);
            Options.SheriffCanKillCrewmatesAsIt.UpdateSelection(SheriffCanKillCrewmatesAsIt);
            Options.SheriffShotLimit.UpdateSelection(SheriffShotLimit);

            Options.EvilWatcherChance.UpdateSelection(EvilWatcherChance);

            Options.SerialKillerCooldown.UpdateSelection(SerialKillerCooldown);
            Options.SerialKillerLimit.UpdateSelection(SerialKillerLimit);
            Options.BountyTargetChangeTime.UpdateSelection(BountyTargetChangeTime);
            Options.BountySuccessKillCooldown.UpdateSelection(BountySuccessKillCooldown);
            Options.BountyFailureKillCooldown.UpdateSelection(BountyFailureKillCooldown);
            Options.BHDefaultKillCooldown.UpdateSelection(BHDefaultKillCooldown);
            Options.ShapeMasterShapeshiftDuration.UpdateSelection(ShapeMasterShapeshiftDuration);
            Options.DefaultShapeshiftCooldown.UpdateSelection(DefaultShapeshiftCooldown);
            Options.EnableLastImpostor.UpdateSelection(EnableLastImpostor);
            Options.LastImpostorKillCooldown.UpdateSelection(LastImpostorKillCooldown);

            Options.SyncButtonMode.UpdateSelection(SyncButtonMode);
            Options.SyncedButtonCount.UpdateSelection(SyncedButtonCount);

            Options.WhenSkipVote.UpdateSelection(whenSkipVote);
            Options.WhenNonVote.UpdateSelection(whenNonVote);
            Options.ArsonistDouseTime.UpdateSelection(ArsonistDouseTime);
            Options.ArsonistCooldown.UpdateSelection(ArsonistCooldown);
            Options.CanTerroristSuicideWin.UpdateSelection(canTerroristSuicideWin);

            Options.RandomMapsMode.UpdateSelection(RandomMapsMode);
            Options.AddedTheSkeld.UpdateSelection(AddedTheSkeld);
            Options.AddedMiraHQ.UpdateSelection(AddedMiraHQ);
            Options.AddedPolus.UpdateSelection(AddedPolus);
            Options.AddedTheAirShip.UpdateSelection(AddedTheAirShip);

            Options.AllowCloseDoors.UpdateSelection(AllowCloseDoors);
            Options.KillDelay.UpdateSelection(HaSKillDelay);
            Options.IgnoreVent.UpdateSelection(IgnoreVent);
            Options.IgnoreCosmetics.UpdateSelection(IgnoreCosmetics);

            Options.MadmateCanFixLightsOut.UpdateSelection(MadmateCanFixLightsOut);
            Options.MadmateCanFixComms.UpdateSelection(MadmateCanFixComms);
            Options.MadmateHasImpostorVision.UpdateSelection(MadmateHasImpostorVision);
            Options.MadGuardianCanSeeWhoTriedToKill.UpdateSelection(MadGuardianCanSeeBarrier);
            Options.CanMakeMadmateCount.UpdateSelection(CanMakeMadmateCount);
            Options.MadSnitchTasks.UpdateSelection(MadSnitchTasks);

            Options.MayorAdditionalVote.UpdateSelection(MayorAdditionalVote);

            Options.CanBeforeSchrodingerCatWinTheCrewmate.UpdateSelection(CanBeforeSchrodingerCatWinTheCrewmate);
            Options.SchrodingerCatExiledTeamChanges.UpdateSelection(SchrodingerCatExiledTeamChanges);

            Options.AutoDisplayLastResult.UpdateSelection(AutoDisplayLastResult);
        }
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            foreach (var kvp in Options.CustomRoleSpawnChances)
            {
                writer.Write(kvp.Value.GetSelection());
                writer.Write(kvp.Key.getCount());
            }

            writer.Write(Options.GameMode.Selection);
            writer.Write(Options.NoGameEnd.GetBool());
            writer.Write(Options.DisableSwipeCard.GetBool());
            writer.Write(Options.DisableSubmitScan.GetBool());
            writer.Write(Options.DisableUnlockSafe.GetBool());
            writer.Write(Options.DisableUploadData.GetBool());
            writer.Write(Options.DisableStartReactor.GetBool());
            writer.Write(Options.DisableResetBreaker.GetBool());
            writer.Write(Options.VampireKillDelay.GetSelection());
            writer.Write(Options.SabotageMasterSkillLimit.GetSelection());
            writer.Write(Options.SabotageMasterFixesDoors.GetBool());
            writer.Write(Options.SabotageMasterFixesReactors.GetBool());
            writer.Write(Options.SabotageMasterFixesOxygens.GetBool());
            writer.Write(Options.SabotageMasterFixesComms.GetBool());
            writer.Write(Options.SabotageMasterFixesElectrical.GetBool());
            writer.Write(Options.SheriffKillCooldown.GetSelection());
            writer.Write(Options.SheriffCanKillArsonist.GetBool());
            writer.Write(Options.SheriffCanKillJester.GetBool());
            writer.Write(Options.SheriffCanKillTerrorist.GetBool());
            writer.Write(Options.SheriffCanKillOpportunist.GetBool());
            writer.Write(Options.SheriffCanKillMadmate.GetBool());
            writer.Write(Options.SheriffCanKillCrewmatesAsIt.GetBool());
            writer.Write(Options.SheriffShotLimit.GetSelection());
            writer.Write(Options.SyncButtonMode.GetBool());
            writer.Write(Options.SyncedButtonCount.GetSelection());
            writer.Write((int)Options.WhenSkipVote.GetSelection());
            writer.Write((int)Options.WhenNonVote.GetSelection());
            writer.Write(Options.ArsonistDouseTime.GetSelection());
            writer.Write(Options.ArsonistCooldown.GetSelection());
            writer.Write(Options.CanTerroristSuicideWin.GetBool());
            writer.Write(Options.RandomMapsMode.GetBool());
            writer.Write(Options.AddedTheSkeld.GetBool());
            writer.Write(Options.AddedMiraHQ.GetBool());
            writer.Write(Options.AddedPolus.GetBool());
            writer.Write(Options.AddedTheAirShip.GetBool());
            writer.Write(Options.AllowCloseDoors.GetBool());
            writer.Write(Options.KillDelay.GetSelection());
            writer.Write(Options.IgnoreVent.GetBool());
            writer.Write(Options.IgnoreCosmetics.GetBool());
            writer.Write(Options.MadmateCanFixLightsOut.GetBool());
            writer.Write(Options.MadmateCanFixComms.GetBool());
            writer.Write(Options.MadmateHasImpostorVision.GetBool());
            writer.Write(Options.CanMakeMadmateCount.GetSelection());
            writer.Write(Options.MadGuardianCanSeeWhoTriedToKill.GetBool());
            writer.Write(Options.MadSnitchTasks.GetSelection());
            writer.Write(Options.MayorAdditionalVote.GetSelection());
            writer.Write(Options.EvilWatcherChance.GetSelection());
            writer.Write(Options.SerialKillerCooldown.GetSelection());
            writer.Write(Options.SerialKillerLimit.GetSelection());
            writer.Write(Options.BountyTargetChangeTime.GetSelection());
            writer.Write(Options.BountySuccessKillCooldown.GetSelection());
            writer.Write(Options.BountyFailureKillCooldown.GetSelection());
            writer.Write(Options.BHDefaultKillCooldown.GetSelection());
            writer.Write(Options.ShapeMasterShapeshiftDuration.GetSelection());
            writer.Write(Options.EnableLastImpostor.GetBool());
            writer.Write(Options.LastImpostorKillCooldown.GetSelection());
            writer.Write(Options.DefaultShapeshiftCooldown.GetSelection());
            writer.Write(Options.CanBeforeSchrodingerCatWinTheCrewmate.GetSelection());
            writer.Write(Options.SchrodingerCatExiledTeamChanges.GetSelection());
            writer.Write(Options.AutoDisplayLastResult.GetBool());
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
    }
}
