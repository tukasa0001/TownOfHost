using TOHTOR.Extensions;
using TOHTOR.Roles;
using UnityEngine;
using VentLib.Localization;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Utilities.Collections;

namespace TOHTOR.Options;

// TODO: This whole class needs to be looked over and refactored, a lot of this is old TOH-TOR code and a lot of it is new TOH code
// TODO: Ideally all of TOH "static" options should end up in here with the use of the new options system
[Localized("StaticOptions")]
public static class StaticOptions
{
    public static bool EnableGM;
    public static bool FixFirstKillCooldown = true;
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
    public static bool CustomServerMode = false;

    //////////////////////////////////////

    public static bool AllowCustomizeCommands = true;
    public static object WhichDisableAdmin { get; set; }
    public static int SyncedButtonCount = 1;
    public static int UsedButtonCount = 0;
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
    public static string WhenTieVote = "";
    public static bool CanTerroristSuicideWin = false;
    public static bool LadderDeath = false;
    public static int LadderDeathChance = 0;
    public static bool DisableTaskWin = false;

    public static bool ColorNameMode = false;
    public static bool TOuRArso = false;

    public static bool STIgnoreVent = false;
    public static float AdditionalEmergencyCooldownTime = 0f;
    public static float AllAliveMeetingTime = 0f;

    public static bool JuggerCanVent = false;
    public static bool AutoDisplayLastResult = false;
    public static bool AddedTheSkeld = false;

    public static bool AddedMiraHQ = false;


    public static bool AddedPolus = false;
    public static bool AddedTheAirShip = false;
    public static bool PestiCanVent = false;
    public static bool ResetKillCooldown = false;
    public static bool RandomSpawn = false;
    public static bool BKcanVent = false;
    public static bool MarksmanCanVent = false;
    public static string SuffixStr = "";
    public static int MayorAdditionalVote = 1;

    public static string Suffix = "";
    public static string VoteModeStr = "";

    public static bool DisableAirshipGapRoomLightsPanel = false;
    public static bool DisableAirshipCargoLightsPanel = false;
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
    public static float KillFlashDuration = 10.0f;
    public static float DemoSuicideTime = 10.0f;
    public static float EvilWatcherChance = 10.0f;
    public static bool AllAliveMeeting = false;

    public static bool AllowMultipleSubroles;

    public static bool ShowHistoryTimestamp;
    public static bool NoGameEnd = true;
    public static bool MayhemOptions;
    public static bool DebugOptions;
    public static bool AllRolesCanVent;
    public static bool LogAllActions;


    public static void AddStaticOptions()
    {
        new GameOptionBuilder()
            .LocaleName("StaticOptions.EnableGM")
			.Key("EnableGM")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => EnableGM = v)
            .AddOnOffValues(false)
            .Color(CustomRoleManager.Special.GM.RoleColor)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.AutoKick")
			.Key("AutoKick")
			.Key("AutoKick")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => AutoKick = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .Name("Ban instead of Kick")
                .BindBool(v => AutoBan = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.KillFlashDuration")
			.Key("KillFlashDuration")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindFloat(v => KillFlashDuration = v)
            .AddFloatRange(0.1f, 0.45f, 0.05f)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.SabotageTimeControl.Enable")
			.Key("Enable Sabotage Time Control")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => SabotageTimeControl = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.SabotageTimeControl.PolusReactorTime")
			    .Key("PolusReactorTime")
                .AddFloatRange(1f, 60f, 1f)
                .BindFloat(v => PolusReactorTimeLimit = v)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.SabotageTimeControl.AirshipReactorTime")
			    .Key("AirshipReactorTime")
                .AddFloatRange(1f, 90f, 1f)
                .BindFloat(v => AirshipReactorTimeLimit = v)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.TOSOptions.Enable")
			.Key("Enable TOS Options")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .Color(Color.yellow)
            .BindBool(v => TosOptions = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.AttackDefense")
			    .Key("AttackDefense")
                .BindBool(v => AttackDefenseValues = v)
                .AddOnOffValues(false)
                .ShowSubOptionPredicate(v => (bool)v)
                .SubOption(sub2 => sub2
                .LocaleName("StaticOptions.TOSOptions.ResetKillcooldown")
			    .Key("ResetKillcooldown")
                .BindBool(v => ResetKillCooldown = v)
                .AddOnOffValues(false)
                .Build())
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.SkKillsRB")
			    .Key("SkKillsRB")
                .BindBool(v => SKkillsRoleblockers = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.AutoGameProgress")
			    .Key("AutoGameProgress")
                .BindBool(v => GameProgression = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.RoundReview")
			    .Key("RoundReview")
                .BindBool(v => RoundReview = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.AmnesiacRememberAnnouncement")
			    .Key("AmnesiacRememberAnnouncement")
                .BindBool(v => AmneRemember = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.TOSOptions.GAVoteImmunity")
			    .Key("GAVoteImmunity")
                .BindBool(v => GuardianAngelVoteImmunity = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.LightsPanelSettings.Enable")
			.Key("Enable Lights Panel Settings")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => LightsOutSpecialSettings = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.LightsPanelSettings.AirshipViewingDeck")
			    .Key("AirshipViewingDeck")
                .BindBool(v => DisableAirshipViewingDeckLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.LightsPanelSettings.AirshipGapRoom")
			    .Key("AirshipGapRoom")
                .BindBool(v => DisableAirshipGapRoomLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.LightsPanelSettings.AirshipCargoRoom")
			    .Key("AirshipCargoRoom")
                .BindBool(v => DisableAirshipCargoLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
           .LocaleName("StaticOptions.PlayerAppearanceCommands")
           .Key("PlayerAppearanceCommands")
           .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
           .BindBool(v => AllowCustomizeCommands = v)
           .AddOnOffValues(false)
           .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.DisableTasks.Enable")
			.Key("Disable Tasks")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.CardSwipe")
			    .Key("CardSwipe")
                .BindBool(v => DisableSwipeCard = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.CardScan")
			    .Key("CardScan")
                .BindBool(v => DisableSubmitScan = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.UnlockSafe")
			    .Key("UnlockSafe")
                .BindBool(v => DisableUnlockSafe = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.UploadData")
			    .Key("UploadData")
                .BindBool(v => DisableUploadData = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.StartReactor")
			    .Key("StartReactor")
                .BindBool(v => DisableStartReactor = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.DisableTasks.ResetBreaker")
			    .Key("ResetBreaker")
                .BindBool(v => DisableResetBreaker = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        // TODO: DISABLE DEVICES CODE

        new GameOptionBuilder()
            .LocaleName("StaticOptions.RandomMap.Enable")
			.Key("Enable Random Maps")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.RandomMap.Skeld")
			    .Key("Skeld")
                .BindBool(v => AddedTheSkeld = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.RandomMap.Mira")
			    .Key("Mira")
                .BindBool(v => AddedMiraHQ = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.RandomMap.Polus")
			    .Key("Polus")
                .BindBool(v => AddedPolus = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.RandomMap.Airship")
			    .Key("Airship")
                .BindBool(v => AddedTheAirShip = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.RandomSpawn.Enable")
			.Key("Enable Random Spawn")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => RandomSpawn = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.RandomSpawn.AirshipExtraSpawn")
			    .Key("Airship Extra Spawn")
                .BindBool(v => AirshipAdditionalSpawn = v)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.SyncButton.Enable")
			.Key("Enable Sync Button Mode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => SyncButtonMode = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.SyncButton.Count")
			    .Key("Count")
                .AddIntRange(0, 100, 1)
                .BindInt(v => SyncedButtonCount = v)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.VoteMode.Enable")
			.Key("Enable Custom Vote Mode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => VoteMode = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.SkipMode")
		    	.Key("SkipMode")
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Default")).Value("Default").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Suicide")).Value("Suicide").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.SelfVote")).Value("Self Vote").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Skip")).Value("Skip").Build())
                .Bind(v => VoteModeStr = (string)v)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.SkipFirstMeeting")
			    .Key("SkipFirstMeeting")
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.IgnoreNoBody")
			    .Key("IgnoreNoBody")
                .BindBool(v => WhenSkipVoteIgnoreNoDeadBody = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.IgnoreEmergencyMeeting")
		    	.Key("IgnoreEmergencyMeeting")
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.NonVote")
		    	.Key("NonVote")
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Default")).Value("Default").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Suicide")).Value("Suicide").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.SelfVote")).Value("Self Vote").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Skip")).Value("Skip").Build())
                .Bind(v => WhenNonVote = (string)v)
                .Build())
            .SubOption(sub => sub
                .LocaleName("StaticOptions.VoteMode.Tie")
		    	.Key("Tie")
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.Default")).Value("Default").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.All")).Value("All").Build())
                .Value(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.Random")).Value("Random").Build())
                .Bind(v => WhenTieVote = (string)v)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.AllAliveMeeting.Enable")
			.Key("Enable All Alive Meeting")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => AllAliveMeeting = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.AllAliveMeeting.Time")
			    .Key("Time")
                .AddFloatRange(1f, 300f, 1f)
                .BindFloat(v => AllAliveMeetingTime = v)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.AdditionalEmergencyCooldown.Enable")
			.Key("Enable Additional Emergency Cooldown")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => AdditionalEmergencyCooldown = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .Name("Additional Emergency Cooldown Threshold")
                .AddIntRange(1, 15, 1)
                .BindInt(v => AdditionalEmergencyCooldownThreshold = v)
                .Build())
            .SubOption(sub => sub
                .Name("Additional Emergency Cooldown Time")
                .AddFloatRange(1f, 60f, 1f)
                .BindFloat(v => AdditionalEmergencyCooldownTime = v)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.LadderDeath.Enable")
			.Key("Enable Ladder Death Customizations")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => LadderDeath = v)
            .ShowSubOptionPredicate(v => (bool)v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.LadderDeath.Chance")
		    	.Key("Chance")
                .AddIntRange(0, 100, 10)
                .BindInt(v => LadderDeathChance = v)
                .Build())
            .BuildAndRegister();

        // TODO: STANDARDHAS STUFF

        new GameOptionBuilder()
            .LocaleName("StaticOptions.FixFirstCooldown")
			.Key("FixFirstCooldown")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => FixFirstKillCooldown = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.DisableTaskWin")
			.Key("DisableTaskWin")
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => DisableTaskWin = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        // MIN / MAX STUFF //
        new GameOptionBuilder()
            .LocaleName("StaticOptions.MinMax.MinNeutralKiller")
			.Key("MinNeutralKiller")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRange(0, 11, 1)
            .BindInt(v => MinNK = v)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MinMax.MaxNeutralKiller")
			.Key("MaxNeutralKiller")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRange(0, 11, 1)
            .BindInt(v => MaxNK = v)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MinMax.MinNeutralNonKiller")
			.Key("MinNeutralNonKiller")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRange(0, 11, 1)
            .BindInt(v => MinNK = v)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MinMax.MaxNeutralNonKiller")
			.Key("MaxNeutralNonKiller")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRange(0, 11, 1)
            .BindInt(v => MaxNonNK = v)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MadMates.MinMadmates")
			.Key("MinMadmates")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRange(0, 4, 1)
            .BindInt(v => MinMadmates = v)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MadMates.MaxMadmates")
			.Key("MaxMadmates")
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRange(0, 4, 1)
            .BindInt(v => MaxMadmates = v)
            .BuildAndRegister();
        // DONE //
        new GameOptionBuilder()
            .LocaleName("StaticOptions.NoGameEnd")
			.Key("NoGameEnd")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v =>
            {
                // TODO: fix
                NoGameEnd = true;
            })
            .AddOnOffValues(false)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.GhostsCanSeeRoles")
			.Key("GhostsCanSeeRoles")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherRoles = v)
            .AddOnOffValues(true)
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.GhostsCanSeeVotes")
			.Key("GhostsCanSeeVotes")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherVotes = v)
            .AddOnOffValues()
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.GhostIgnoreTasks")
			.Key("GhostIgnoreTasks")
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostIgnoreTasks = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        new GameOptionBuilder()
           .LocaleName("StaticOptions.CamoComms")
			.Key("CamoComms")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => CamoComms = v)
           .AddOnOffValues(false)
           .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.AutoDisplayResult")
			.Key("AutoDisplayResult")
            .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
            .BindBool(v => AutoDisplayLastResult = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        new GameOptionBuilder()
           .LocaleName("StaticOptions.SuffixMode")
			.Key("SuffixMode")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .Values(suffixModes)
           .Bind(v => SuffixStr = (string)v)
           .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.ColorNameMode")
			.Key("ColorNameMode")
            .Tab(DefaultTabs.GeneralTab)
            //    .IsHeader(true)
            .BindBool(v => ColorNameMode = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        new GameOptionBuilder()
            .Name("Players Can Have Multiple Modifiers")
            // .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => AllowMultipleSubroles = v)
            .AddOnOffValues(false)
            .BuildAndRegister();

        // Another option I hate that exists. I have seen multiple complaints about this option.
        new GameOptionBuilder()
           .LocaleName("StaticOptions.ChangeNameToRoleInfo")
			.Key("ChangeNameToRoleInfo")
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => ChangeNameToRoleInfo = v)
           .AddOnOffValues(false)
           .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.ShowHistoryTimestamp")
			.Key("ShowHistoryTimestamp")
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => ShowHistoryTimestamp = v)
            .AddOnOffValues()
            .BuildAndRegister();

        new GameOptionBuilder()
            .LocaleName("StaticOptions.MayhemOptions.Enable")
			.Key("Enable Mayhem Options")
            .IsHeader(false)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => MayhemOptions = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .LocaleName("StaticOptions.MayhemOptions.AllRolesVent")
		    	.Key("AllRolesVent")
                .BindBool(v => AllRolesCanVent = v && MayhemOptions)
                .AddOnOffValues(false)
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
            .Name("Debug Options")
            .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .ShowSubOptionPredicate(v => (bool)v)
            .BindBool(v => DebugOptions = v)
            .AddOnOffValues(false)
            .SubOption(sub => sub
                .Name("Debug All Actions")
                .BindBool(v => LogAllActions = v && DebugOptions)
                .AddOnOffValues()
                .Build())
            .BuildAndRegister();

        new GameOptionBuilder()
               .LocaleName("StaticOptions.CustomServerMode")
		    	.Key("CustomServerMode")
               .Tab(DefaultTabs.GeneralTab)
               .IsHeader(true)
               .BindBool(v => CustomServerMode = v)
               .AddOnOffValues(false)
               .BuildAndRegister();
    }


    private static bool sending;
    private static BatchList<Option> changedOptions = new();


    // Keys
    [Localized("PetOptions")]
    private static class PetOptionsNames
    {
        [Localized("Enabled")]
        public static string PetOptions => "Override";
        [Localized("ShowPetAnimation")]
        private static string ShowPetAnimation => "Show Pet Animation";
        [Localized("AssignedPet")]
        private static string AssignedPet => "Assigned Pet";

        static PetOptionsNames()
        {
            FrozenContext fc = new("SS");
            VentLogger.Fatal(fc.Resolve());
        }
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