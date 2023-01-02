using System;
using TownOfHost.Options;

namespace TownOfHost.ReduxOptions;

// TODO: This whole class needs to be looked over and refactored, a lot of this is old TOH-TOR code and a lot of it is new TOH code
// TODO: Ideally all of TOH "static" options should end up in here with the use of the new options system
public static class StaticOptions
{
    public static string EnableGM = "";

    public static bool RolesLikeTOU = true;

    //StaticOptions.VultureArrow
    public static bool VultureArrow = false;

    //StaticOptions.MediumArrow
    public static bool MediumArrow = false;

    //StaticOptions.AmnesiacArrow
    public static bool AmnesiacArrow = false;

    //StaticOptions.GhostsCanSeeOtherRoles
    public static bool GhostsCanSeeOtherRoles = false;

    //StaticOptions.ImpostorKnowsRolesOfTeam
    public static bool ImpostorKnowsRolesOfTeam = false;

    //StaticOptions.CovenKnowsRolesOfTeam
    public static bool CovenKnowsRolesOfTeam = false;

    //StaticOptions.ChildKnown
    public static bool ChildKnown = false;

    //StaticOptions.EnableLastImpostor
    public static bool EnableLastImpostor = false;


    public static string GameModeString = "Classic";
    public static string GameModeName = "Classic";
    public static int LastImpostorKillCooldown = 1;
    public static int CanMakeMadmateCount = 1;

    public static bool TargetKnowsGA = false;

    public static bool DisableDevices = false;
    public static bool SyncButtonMode = true;
    public static bool SabotageTimeControl = false;
    public static bool RandomMapsMode = false;
    public static bool CamoComms = false;

    public static int MinNK = 0;


    public static int MaxNK = 10;


    public static int MinNonNK = 0;


    public static int MaxNonNK = 10;


    public static bool Customise = false;
    public static object WhichDisableAdmin { get; set; }
    public static int SyncedButtonCount = 1;
    public static bool VoteMode = false;
    public static float PolusReactorTimeLimit = 10.0f;
    public static float AirshipReactorTimeLimit = 10.0f;
    public static string WhenSkipVote = "O";
    public static string WhenNonVote = ":O";
    public static bool CanTerroristSuicideWin = false;
    public static bool LadderDeath = false;
    public static object LadderDeathChance { get; set; }
    public static bool DisableTaskWin = false;

    public static bool StandardHAS = false;


    public static bool ExeTargetShowsEvil = false;
    public static bool GAdependsOnTaregtRole = false;
    public static bool CkshowEvil = false;
    public static bool NBshowEvil = false;
    public static bool NEshowEvil = false;
    public static bool MadmatesAreEvil = false;
    public static bool ColorNameMode = false;
    public static int BodiesAmount = 1;

    public static bool TOuRArso = false;


    public static bool FreeForAllOn = false;

    public static bool STIgnoreVent = false;


    public static bool JuggerCanVent = false;
    public static bool DisableAdmin = false;
    public static bool SidekickGetsPromoted = false;
    public static bool SchrodingerCatExiledTeamChanges = false;
    public static bool MayorHasPortableButton = false;
    public static float SwooperCooldown = 10.0f;
    public static bool ExecutionerCanTargetImpostor = false;

    public static float RampageCD = 10.0f;


    public static float StoneCD = 10.0f;


    public static bool SheriffCorrupted = false;

    public static bool TraitorCanSpawnIfNK = false;


    public static bool TraitorCanSpawnIfCoven = false;
    public static int PlayersForTraitor = 1;
    public static bool AutoDisplayLastResult = false;
    public static bool AddedTheSkeld = false;

    public static bool AddedMiraHQ = false;


    public static bool AddedPolus = false;
    public static bool AddedTheAirShip = false;
    public static bool JackalCanUseSabotage = false;
    public static bool PestiCanVent = false;
    public static bool BKcanVent = false;
    public static bool MarksmanCanVent = false;
    public static bool HitmanCanVent = false;
    public static bool GrenadierCanVent = false;
    public static int MayorAdditionalVote = 1;
    public static bool MayorVotesAppearBlack = false;
    public static int ExecutionerChangeRolesAfterTargetKilled = 1;
    public static int WhenGaTargetDies = 1;
    public static bool LoversDieTogether = false;
    public static bool GAknowsRole = false;
    public static bool LoversKnowRoleOfOtherLover = false;
    public static int NumOfTransports = 1;
    public static float KillDelay = 10.0f;

    public static float StandardHASWaitingTime = 5.0f;

    public static bool JackalHasSidekick = false;
    public static bool InfectionSkip = false;
    public static bool VampireDitchesOn = false;
    public static bool HexMasterOn = false;

    public static bool MedusaOn = false;


    public static int CovenMeetings = 1;
    public static bool ModifierRestrict = false;
    public static bool CanBeforeSchrodingerCatWinTheCrewmate = false;
    public static bool HitmanCanWinWithExeJes = false;
    public static bool ResetToYinYang = false;
    public static bool JesterCanVent = false;
    public static double VampireKillDelay = 10.0;
    public static bool VampireBuff = false;
    public static double ArsonistDouseTime = 10.0;
    public static int ArsonistCooldown = 1;
    public static int InfectCooldown = 1;
    public static bool VultureCanVent = false;
    public static bool SplatoonOn = false;
    public static bool GlitchCanVent = true;
    public static bool EscortPreventsVent = false;
    public static bool IgnoreVent = false;
    public static int MayorNumOfUseButton = 1;
    public static int NumOfVets = 1;
    public static float SwooperDuration = 10.0f;
    public static bool SwooperCanVentInvis = false;
    public static int NumOfProtects = 1;
    public static bool BastionVentsRemoveOnBomb = false;
    public static bool VentWhileRampaged = false;
    public static float RampageDur = 10.0f;
    public static string GlobalRoleBlockDuration { get; set; }
    public static bool MadSnitchCanAlsoBeExposedToImpostor { get; set; }
    public static bool MadmateRevengeCrewmate { get; set; }
    public static bool WhenSkipVoteIgnoreFirstMeeting { get; set; }
    public static bool WhenSkipVoteIgnoreNoDeadBody { get; set; }
    public static bool WhenSkipVoteIgnoreEmergency { get; set; }
    public static bool WhenTie { get; set; }
    public static bool MadmateCanSeeDeathReason { get; set; }
    public static bool GhostCanSeeDeathReason { get; set; }
    public static bool AmDebugger { get; set; }
    public static bool MadmateCanSeeKillFlash { get; set; }
    public static bool GhostIgnoreTasks { get; set; }
    public static bool IsStandardHAS { get; set; }


    public static bool ChangeNameToRoleInfo { get; set; }

    public static bool MadSnitchCanVent = false;
    public static int TasksRemainingForPhantomClicked = 1;
    public static int TasksRemaningForPhantomAlert = 1;
    public static int SaboAmount = 1;
    public static bool MadmateCanFixLightsOut = false;
    public static bool MadmateCanFixComms = false;
    public static bool AllowCloseDoors = false;
    public static bool DisableTasks = false;
    public static bool DisableSwipeCard = false;
    public static bool DisableSubmitScan = false;
    public static bool DisableUnlockSafe = false;
    public static bool DisableUploadData = false;
    public static bool DisableStartReactor = false;
    public static bool DisableResetBreaker = false;
    public static bool DisableFixWiring = false;
    public static bool NecroCanUseSheriff = false;
    public static float StoneReport = 10.0f;
    public static float TrapperBlockMoveTime = 10.0f;
    public static float KillFlashDuration = 10.0f;
    public static float DemoSuicideTime = 10.0f;
    public static float VetDuration = 10.0f;
    public static int NumOfVests = 1;
    public static float VestDuration = 10.0f;
    public static float StoneDuration = 10.0f;
    public static float SpeedBoosterUpSpeed = 10.0f;
    public static float EvilWatcherChance = 10.0f;

    public static bool mayhemOptions;
    public static bool debugOptions;
    public static bool allRolesCanVent;
    public static bool logAllActions;

    public static void AddStaticOptions()
    {
        OptionManager manager = Main.OptionManager;

        manager.Add(new SmartOptionBuilder()
            .Name("NoGameEnd")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => Main.NoGameEnd = v)
            .AddOnOffValues(false)
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Mayhem Options")
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => mayhemOptions = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Most Roles Can Vent")
                .BindBool(v => allRolesCanVent = v && mayhemOptions)
                .AddOnOffValues(false)
                .Build())
            .Build());

        manager.Add(new SmartOptionBuilder()
            .Name("Debug Options")
            .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => debugOptions = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Debug All Actions")
                .BindBool(v => logAllActions = v && debugOptions)
                .AddOnOffValues()
                .Build())
            .Build());

        manager.Add(new SmartOptionBuilder()
            .Name("Nested Options Test")
            .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Nested Options Test")
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues(false)
                .AddSubOption(sub2 => sub2
                    .Name("Nested Options Test")
                    .ShowSubOptionsWhen(v => (bool)v)
                    .AddOnOffValues(false)
                    .AddSubOption(sub3 => sub3
                        .Name("Nested Options Test")
                        .ShowSubOptionsWhen(v => (bool)v)
                        .AddOnOffValues(false)
                        .AddSubOption(sub4 => sub4
                            .Name("Nested Options Test")
                            .ShowSubOptionsWhen(v => (bool)v)
                            .AddOnOffValues(false)
                            .AddSubOption(sub5 => sub5
                                .Name("Nested Options Test")
                                .AddOnOffValues(false)
                                .Build())
                            .Build())
                        .Build())
                    .Build())
                .Build())
            .Build());
    }

}


    //Options.JesterCanVent.GetBool()
    //Options.VultureCanVent.GetBool()
    //Options.MadSnitchCanVent.GetBool()
    //Options.MayorHasPortableButton.GetBool()
    //Options.TrapperBlockMoveTime.GetFloat()
    //Options.KillFlashDuration.GetFloat()
    //Options.DemoSuicideTime.GetFloat()
    //Options.VetDuration.GetFloat()
    //Options.NumOfVests.GetInt()
    //Options.VestDuration.GetFloat()
    //Options.VetDuration.GetFloat()
    //Options.StoneCD.GetFloat()
    //Options.StoneDuration.GetFloat()
    //Options.TOuRArso.GetBool()
    //Options.JuggerCanVent.GetBool()
    //Options.JackalCanVent.GetBool()
    //Options.MarksmanCanVent.GetBool()
    //Options.PestiCanVent.GetBool()
    //Options.STIgnoreVent.GetBool()
    //Options.STIgnoreVent.GetBool()
    //Options.CanMakeMadmateCount.GetInt()
    //Options.SpeedBoosterUpSpeed.GetFloat()
    //Options.EvilWatcherChance.GetFloat()
    //Options.GameMode.GetName()
    //Options.EnableLastImpostor.GetString()
    //Options.EnableLastImpostor.GetString()
    //Options.LastImpostorKillCooldown.GetString()
    //Options.GlobalRoleBlockDuration.GetString()
    //Options.TasksRemainingForPhantomClicked.GetInt()
    //Options.TasksRemaningForPhantomAlert.GetInt()
    //Options.CanMakeMadmateCount.GetInt()
    //Options.TargetKnowsGA.GetBool()
    //Options.DisableDevices.GetBool()
    //Options.SyncButtonMode.GetBool()
    //Options.SabotageTimeControl.GetBool()
    //Options.RandomMapsMode.GetBool()
    //Options.CamoComms.GetBool()
    //Options.CamoComms.GetBool()
    //Options.MinNK.GetString()
    //Options.MaxNK.GetString()
    //Options.MinNonNK.GetString()
    //Options.MaxNonNK.GetString()
    //Options.ImpostorKnowsRolesOfTeam.GetString()
    //Options.CovenKnowsRolesOfTeam.GetString()
    //Options.GameMode.GetString()
    //Options.Customise.GetBool()
    //Options.LastImpostorKillCooldown.GetString()
    //Options.DisableDevices.GetBool()
    //Options.DisableDevices.GetBool()
    //Options.WhichDisableAdmin.GetString()
    //Options.AirshipReactorTimeLimit.GetString()
    //Options.PolusReactorTimeLimit.GetString()
    //Options.VoteMode.GetBool()
    //Options.GetWhenSkipVote()
    //Options.CanTerroristSuicideWin.GetBool()
    //Options.LadderDeath.GetBool()
    //Options.LadderDeath.GetBool()
    //Options.LadderDeathChance.GetString()
    //Options.StandardHAS.GetBool()
    //Options.ExeTargetShowsEvil.GetBool()
    //Options.DisableTaskWin.GetBool()
    //Options.GAdependsOnTaregtRole.GetBool()
    //Options.CkshowEvil.GetBool()
    //Options.NBshowEvil.GetBool()
    //Options.MadmatesAreEvil.GetBool()
    //Options.ColorNameMode.GetBool()
    //Options.GetSuffixMode()
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //