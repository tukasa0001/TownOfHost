using System;
using TownOfHost.Roles;
using TownOfHost.Options;

namespace TownOfHost.ReduxOptions;

// TODO: This whole class needs to be looked over and refactored, a lot of this is old TOH-TOR code and a lot of it is new TOH code
// TODO: Ideally all of TOH "static" options should end up in here with the use of the new options system
public static class StaticOptions
{
    public static bool EnableGM = false;
    public static bool FixFirstKillCooldown = false;
    public static bool AutoKick = false;
    public static bool AutoBan = false;

    public static bool RolesLikeTOU = true;

    //StaticOptions.VultureArrow
    public static bool VultureArrow = false;

    //StaticOptions.MediumArrow
    public static bool MediumArrow = false;

    //StaticOptions.AmnesiacArrow
    public static bool AmnesiacArrow = false;

    //StaticOptions.GhostsCanSeeOtherRoles
    public static bool GhostsCanSeeOtherRoles = true;
    public static bool GhostsCanSeeOtherVotes = true;

    //StaticOptions.ImpostorKnowsRolesOfTeam
    public static bool ImpostorKnowsRolesOfTeam = false;

    //StaticOptions.CovenKnowsRolesOfTeam
    public static bool CovenKnowsRolesOfTeam = false;

    //StaticOptions.ChildKnown
    public static bool ChildKnown = false;

    //StaticOptions.EnableLastImpostor
    public static bool EnableLastImpostor = false;

    public static bool GuardianAngelVoteImmunity = false;
    public static bool TosOptions = false;
    public static bool RoundReview = false;
    public static bool AttackDefenseValues = false;
    public static bool SKkillsRoleblockers = false;
    public static bool GameProgression = false;
    public static bool AmneRemember = false;

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
    // MIN/MAX STUFF
    public static int MinNK = 0;
    public static int MaxNK = 10;
    public static int MinNonNK = 0;
    public static int MaxNonNK = 10;
    public static int MinMadmates = 0;
    public static int MaxMadmates = 4;

    //////////////////////////////////////

    public static bool Customise = false;
    public static object WhichDisableAdmin { get; set; }
    public static int SyncedButtonCount = 1;
    public static bool VoteMode = false;
    public static float PolusReactorTimeLimit = 10.0f;
    public static float AirshipReactorTimeLimit = 10.0f;
    public static string WhenSkipVote = "O";
    public static readonly string[] voteModes =
        {
            "Default", "Suicide", "Self Vote", "Skip"
        };
    public static readonly string[] tieModes =
    {
        "Default", "All", "Random"
    };
    public static readonly string[] suffixModes =
    {
            "None",
            "Version",
            "Streaming",
            "Recording",
            "Room Host",
            "Original Name"
        };
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
    public static float AdditionalEmergencyCooldownTime = 0f;
    public static float AllAliveMeetingTime = 0f;

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
    public static bool RandomSpawn = false;
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
    public static bool DisableAirshipGapRoomLightsPanel = false;
    public static bool DisableAirshipCargoLightsPanel = false;
    public static bool JesterCanVent = false;
    public static double VampireKillDelay = 10.0;
    public static bool VampireBuff = false;
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

    public static bool AdditionalEmergencyCooldown = false;
    public static int AdditionalEmergencyCooldownThreshold = 0;

    public static bool MadSnitchCanVent = false;
    public static int TasksRemainingForPhantomClicked = 1;
    public static int TasksRemaningForPhantomAlert = 1;
    public static int SaboAmount = 1;
    public static bool MadmateCanFixLightsOut = false;
    public static bool LightsOutSpecialSettings = false;
    public static bool DisableAirshipViewingDeckLightsPanel = false;
    public static bool AirshipAdditionalSpawn = false;
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
    public static bool AllAliveMeeting = false;

    public static bool mayhemOptions;
    public static bool debugOptions;
    public static bool allRolesCanVent;
    public static bool logAllActions;

    public static void AddStaticOptions()
    {
        OptionManager manager = Main.OptionManager;

        manager.Add(new SmartOptionBuilder()
            .Name("GM")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => EnableGM = v)
            .AddOnOffValues(false)
            .Color(Utils.GetRoleColor(GM.Ref<GM>()))
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("AutoKick")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => AutoKick = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Ban instead of Kick")
                .BindBool(v => AutoBan = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
            );

        manager.Add(new SmartOptionBuilder()
            .Name("Kill Flash Duration")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindFloat(v => KillFlashDuration = v)
            .AddFloatRangeValues(0.1f, 0.45f, 0.05f)
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Sabotage Time Control")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => SabotageTimeControl = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Polus Reactor Time Limit")
                .AddFloatRangeValues(1f, 60f, 1f)
                .BindFloat(v => PolusReactorTimeLimit = v)
                .Build())
            .AddSubOption(sub => sub
                .Name("AirShip Reactor Time Limit")
                .AddFloatRangeValues(1f, 90f, 1f)
                .BindFloat(v => AirshipReactorTimeLimit = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Tos Options")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => TosOptions = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Attack and Defense Values")
                .BindBool(v => AttackDefenseValues = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Serial Killer kills RoleBlockers")
                .BindBool(v => SKkillsRoleblockers = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Auto Game Progression")
                .BindBool(v => GameProgression = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Round Review")
                .BindBool(v => RoundReview = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Amnesiac Remember Announcement")
                .BindBool(v => AmneRemember = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Guardian Angel Vote Immunity ")
                .BindBool(v => GuardianAngelVoteImmunity = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Lights Out Special Settings")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => LightsOutSpecialSettings = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Disable Airship Viewing Deck Lights Panel")
                .BindBool(v => DisableAirshipViewingDeckLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Airship Gap Room Lights Panel")
                .BindBool(v => DisableAirshipGapRoomLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Airship Cargo Lights Panel")
                .BindBool(v => DisableAirshipCargoLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
           .Name("Players have access to /name, /color, and /level")
           .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
           .BindBool(v => Customise = v)
           .AddOnOffValues(false)
           .Build()
       );

        manager.Add(new SmartOptionBuilder()
            .Name("Roles look like ToUR")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => RolesLikeTOU = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Disable Tasks")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Disable Swipe Card Task")
                .BindBool(v => DisableSwipeCard = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Submit Sacan")
                .BindBool(v => DisableSubmitScan = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Unlock Safe")
                .BindBool(v => DisableUnlockSafe = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Upload Data")
                .BindBool(v => DisableUploadData = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Start Reactor")
                .BindBool(v => DisableStartReactor = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Disable Reset Breaker")
                .BindBool(v => DisableResetBreaker = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        // TODO: DISABLE DEVICES CODE

        manager.Add(new SmartOptionBuilder()
            .Name("Random Maps Mode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Add The Skeld")
                .BindBool(v => AddedTheSkeld = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Add Mira HQ")
                .BindBool(v => AddedMiraHQ = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Add Polus")
                .BindBool(v => AddedPolus = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Add The AirShip")
                .BindBool(v => AddedTheAirShip = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Random Spawn")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => RandomSpawn = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Airship Additional Spawn")
                .BindBool(v => AirshipAdditionalSpawn = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Sync Button Mode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => SyncButtonMode = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Synced Button Count")
                .AddIntRangeValues(0, 100, 1)
                .BindInt(v => SyncedButtonCount = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Vote Mode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => VoteMode = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("When Skip Vote")
                .AddValues(-1, voteModes[0..3])
                .Build())
            .AddSubOption(sub => sub
                .Name("When SkipVote Ignore First Meeting")
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("When SkipVote Ignore No Dead Body")
                .BindBool(v => WhenSkipVoteIgnoreNoDeadBody = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("When SkipVote Ignore Emergency")
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("When Non Vote")
                .AddValues(-1, voteModes)
                .Build())
            .AddSubOption(sub => sub
                .Name("When Tie")
                .AddValues(-1, tieModes)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("All Alive Meeting")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => AllAliveMeeting = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("All Alive Meeting Time")
                .AddFloatRangeValues(1f, 300f, 1f)
                .BindFloat(v => AllAliveMeetingTime = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Additional Emergency Cooldown")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => AdditionalEmergencyCooldown = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Additional Emergency Cooldown Threshold")
                .AddIntRangeValues(1, 15, 1)
                .BindInt(v => AdditionalEmergencyCooldownThreshold = v)
                .Build())
            .AddSubOption(sub => sub
                .Name("Additional Emergency Cooldown Time")
                .AddFloatRangeValues(1f, 60f, 1f)
                .BindFloat(v => AdditionalEmergencyCooldownTime = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Ladder Death")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => LadderDeath = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Ladder Death Chance")
                .AddIntRangeValues(0, 100, 10)
                .BindInt(v => LadderDeathChance = v)
                .Build())
            .Build()
        );

        // TODO: STANDARDHAS STUFF

        manager.Add(new SmartOptionBuilder()
            .Name("Fix First Kill Cooldown")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => FixFirstKillCooldown = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Disable Task Win")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => DisableTaskWin = v)
            .AddOnOffValues(false)
            .Build()
        );
        // MIN / MAX STUFF //
        manager.Add(new SmartOptionBuilder()
            .Name("Min Neutral Killer")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MinNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Max Neutral Killer")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MaxNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Min Non-Killing Neutral")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MinNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Max Non-Killing Neutral")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MaxNonNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Min Madmates")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRangeValues(0, 4, 1)
            .BindInt(v => MinMadmates = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Max Madmates")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 4, 1)
            .BindInt(v => MaxMadmates = v)
            .Build()
        );
        // DONE //
        manager.Add(new SmartOptionBuilder()
            .Name("NoGameEnd")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => Main.NoGameEnd = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Ghost Can See Other Roles")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherRoles = v)
            .AddOnOffValues(true)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Ghost Can See Other Votes")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherVotes = v)
            .AddOnOffValues(true)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Ghosts Ignore Tasks")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostIgnoreTasks = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
           .Name("Camouflage Comms")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => CamoComms = v)
           .AddOnOffValues(false)
           .Build()
       );

        manager.Add(new SmartOptionBuilder()
            .Name("Auto Display Last Result")
            .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
            .BindBool(v => AutoDisplayLastResult = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
           .Name("Suffix Mode")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .AddValues(-1, suffixModes)
           .Build()
        );

        // I hate the fact this option even exists. Players SHOULD know the Lobby Options.
        /*
        manager.Add(new SmartOptionBuilder()
           .Name("Hide Game Settings")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => HideGameSettings = v)
           .AddOnOffValues(true)
           .Build()
       );
       */
        manager.Add(new SmartOptionBuilder()
            .Name("Color Name Mode")
            .Tab(DefaultTabs.GeneralTab)
            //    .IsHeader(true)
            .BindBool(v => ColorNameMode = v)
            .AddOnOffValues(false)
            .Build()
        );

        // Another option I hate that exists. I have seen multiple complaints about this option.
        manager.Add(new SmartOptionBuilder()
           .Name("Change Name to Role Info")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => ChangeNameToRoleInfo = v)
           .AddOnOffValues(false)
           .Build()
       );
        manager.Add(new SmartOptionBuilder()
            .Name("Mayhem Options")
            .IsHeader(false)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => mayhemOptions = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Most Roles Can Vent")
                .BindBool(v => allRolesCanVent = v && mayhemOptions)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

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