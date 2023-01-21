using TownOfHost.Roles;
using UnityEngine;
using VentLib.Localization;
using VentLib.Localization.Attributes;
using VentLib.Logging;

namespace TownOfHost.Options;

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

    public static bool Customise = false;
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
    public static bool NoGameEnd;
    public static bool MayhemOptions;
    public static bool DebugOptions;
    public static bool AllRolesCanVent;
    public static bool LogAllActions;


    public static void AddStaticOptions()
    {
        OptionManager manager = TOHPlugin.OptionManager;

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.EnableGM"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => EnableGM = v)
            .AddOnOffValues(false)
            .Color(CustomRoleManager.Special.GM.RoleColor)
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.AutoKick"))
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
            .Name(Localizer.Get("StaticOptions.KillFlashDuration"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindFloat(v => KillFlashDuration = v)
            .AddFloatRangeValues(0.1f, 0.45f, 0.05f)
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.SabotageTimeControl.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => SabotageTimeControl = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.SabotageTimeControl.PolusReactorTime"))
                .AddFloatRangeValues(1f, 60f, 1f)
                .BindFloat(v => PolusReactorTimeLimit = v)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.SabotageTimeControl.AirshipReactorTime"))
                .AddFloatRangeValues(1f, 90f, 1f)
                .BindFloat(v => AirshipReactorTimeLimit = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.TOSOptions.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .Color(Color.yellow)
            .BindBool(v => TosOptions = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.AttackDefense"))
                .BindBool(v => AttackDefenseValues = v)
                .AddOnOffValues(false)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddSubOption(sub2 => sub2
                .Name(Localizer.Get("StaticOptions.TOSOptions.ResetKillcooldown"))
                .BindBool(v => ResetKillCooldown = v)
                .AddOnOffValues(false)
                .Build())
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.SkKillsRB"))
                .BindBool(v => SKkillsRoleblockers = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.AutoGameProgress"))
                .BindBool(v => GameProgression = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.RoundReview"))
                .BindBool(v => RoundReview = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.AmnesiacRememberAnnouncement"))
                .BindBool(v => AmneRemember = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.TOSOptions.GAVoteImmunity"))
                .BindBool(v => GuardianAngelVoteImmunity = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.LightsPanelSettings.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => LightsOutSpecialSettings = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.LightsPanelSettings.AirshipViewingDeck"))
                .BindBool(v => DisableAirshipViewingDeckLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.LightsPanelSettings.AirshipGapRoom"))
                .BindBool(v => DisableAirshipGapRoomLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.LightsPanelSettings.AirshipCargoRoom"))
                .BindBool(v => DisableAirshipCargoLightsPanel = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
           .Name(Localizer.Get("StaticOptions.PlayerAppearanceCommands"))
           .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
           .BindBool(v => Customise = v)
           .AddOnOffValues(false)
           .Build()
       );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.DisableTasks.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.CardSwipe"))
                .BindBool(v => DisableSwipeCard = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.CardScan"))
                .BindBool(v => DisableSubmitScan = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.UnlockSafe"))
                .BindBool(v => DisableUnlockSafe = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.UploadData"))
                .BindBool(v => DisableUploadData = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.StartReactor"))
                .BindBool(v => DisableStartReactor = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.DisableTasks.ResetBreaker"))
                .BindBool(v => DisableResetBreaker = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        // TODO: DISABLE DEVICES CODE

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.RandomMap.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => DisableTasks = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.RandomMap.Skeld"))
                .BindBool(v => AddedTheSkeld = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.RandomMap.Mira"))
                .BindBool(v => AddedMiraHQ = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.RandomMap.Polus"))
                .BindBool(v => AddedPolus = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.RandomMap.Airship"))
                .BindBool(v => AddedTheAirShip = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.RandomSpawn.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => RandomSpawn = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.RandomSpawn.AirshipExtraSpawn"))
                .BindBool(v => AirshipAdditionalSpawn = v)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.SyncButton.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => SyncButtonMode = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.SyncButton.Count"))
                .AddIntRangeValues(0, 100, 1)
                .BindInt(v => SyncedButtonCount = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.VoteMode.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => VoteMode = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.SkipMode"))
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Default")).Value("Default").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Suicide")).Value("Suicide").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.SelfVote")).Value("Self Vote").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Skip")).Value("Skip").Build())
                .Bind(v => VoteModeStr = (string)v)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.SkipFirstMeeting"))
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.IgnoreNoBody"))
                .BindBool(v => WhenSkipVoteIgnoreNoDeadBody = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.IgnoreEmergencyMeeting"))
                .BindBool(v => WhenSkipVoteIgnoreFirstMeeting = v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.NonVote"))
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Default")).Value("Default").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Suicide")).Value("Suicide").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.SelfVote")).Value("Self Vote").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.Mode.Skip")).Value("Skip").Build())
                .Bind(v => WhenNonVote = (string)v)
                .Build())
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.VoteMode.Tie"))
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.Default")).Value("Default").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.All")).Value("All").Build())
                .AddValue(v => v.Text(Localizer.Get("StaticOptions.VoteMode.TieMode.Random")).Value("Random").Build())
                .Bind(v => WhenTieVote = (string)v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.AllAliveMeeting.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => AllAliveMeeting = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.AllAliveMeeting.Time"))
                .AddFloatRangeValues(1f, 300f, 1f)
                .BindFloat(v => AllAliveMeetingTime = v)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.AdditionalEmergencyCooldown.Enable"))
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
            .Name(Localizer.Get("StaticOptions.LadderDeath.Enable"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => LadderDeath = v)
            .ShowSubOptionsWhen(v => (bool)v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.LadderDeath.Chance"))
                .AddIntRangeValues(0, 100, 10)
                .BindInt(v => LadderDeathChance = v)
                .Build())
            .Build()
        );

        // TODO: STANDARDHAS STUFF

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.FixFirstCooldown"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v => FixFirstKillCooldown = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.DisableTaskWin"))
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => DisableTaskWin = v)
            .AddOnOffValues(false)
            .Build()
        );

        // MIN / MAX STUFF //
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MinMax.MinNeutralKiller"))
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MinNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MinMax.MaxNeutralKiller"))
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MaxNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MinMax.MinNeutralNonKiller"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MinNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MinMax.MaxNeutralNonKiller"))
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 11, 1)
            .BindInt(v => MaxNonNK = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MadMates.MinMadmates"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .AddIntRangeValues(0, 4, 1)
            .BindInt(v => MinMadmates = v)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MadMates.MaxMadmates"))
            .Tab(DefaultTabs.GeneralTab)
            .AddIntRangeValues(0, 4, 1)
            .BindInt(v => MaxMadmates = v)
            .Build()
        );
        // DONE //
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.NoGameEnd"))
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindBool(v =>
            {
                NoGameEnd = v;
            })
            .AddOnOffValues(false)
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.GhostsCanSeeRoles"))
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherRoles = v)
            .AddOnOffValues(true)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.GhostsCanSeeVotes"))
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostsCanSeeOtherVotes = v)
            .AddOnOffValues(true)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.GhostIgnoreTasks"))
            .Tab(DefaultTabs.GeneralTab)
        //    .IsHeader(true)
            .BindBool(v => GhostIgnoreTasks = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
           .Name(Localizer.Get("StaticOptions.CamoComms"))
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => CamoComms = v)
           .AddOnOffValues(false)
           .Build()
       );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.AutoDisplayResult"))
            .Tab(DefaultTabs.GeneralTab)
           .IsHeader(true)
            .BindBool(v => AutoDisplayLastResult = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
           .Name(Localizer.Get("StaticOptions.SuffixMode"))
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .AddValues(-1, suffixModes)
           .Bind(v => SuffixStr = (string)v)
           .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.ColorNameMode"))
            .Tab(DefaultTabs.GeneralTab)
            //    .IsHeader(true)
            .BindBool(v => ColorNameMode = v)
            .AddOnOffValues(false)
            .Build()
        );
        manager.Add(new SmartOptionBuilder()
            .Name("Players Can Have Multiple Modifiers")
            // .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => AllowMultipleSubroles = v)
            .AddOnOffValues(false)
            .Build());

        // Another option I hate that exists. I have seen multiple complaints about this option.
        manager.Add(new SmartOptionBuilder()
           .Name(Localizer.Get("StaticOptions.ChangeNameToRoleInfo"))
           .Tab(DefaultTabs.GeneralTab)
           //    .IsHeader(true)
           .BindBool(v => ChangeNameToRoleInfo = v)
           .AddOnOffValues(false)
           .Build()
       );

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.ShowHistoryTimestamp"))
            .Tab(DefaultTabs.GeneralTab)
            .BindBool(v => ShowHistoryTimestamp = v)
            .AddOnOffValues()
            .Build());

        manager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.MayhemOptions.Enable"))
            .IsHeader(false)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => MayhemOptions = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name(Localizer.Get("StaticOptions.MayhemOptions.AllRolesVent"))
                .BindBool(v => AllRolesCanVent = v && MayhemOptions)
                .AddOnOffValues(false)
                .Build())
            .Build()
        );

        manager.Add(new SmartOptionBuilder()
            .Name("Debug Options")
            .IsHeader(true)
            .Tab(DefaultTabs.GeneralTab)
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => DebugOptions = v)
            .AddOnOffValues(false)
            .AddSubOption(sub => sub
                .Name("Debug All Actions")
                .BindBool(v => LogAllActions = v && DebugOptions)
                .AddOnOffValues()
                .Build())
            .Build());

        manager.Add(new SmartOptionBuilder()
               .Name(Localizer.Get("StaticOptions.CustomServerMode"))
               .Tab(DefaultTabs.GeneralTab)
               .IsHeader(true)
               .BindBool(v => CustomServerMode = v)
               .AddOnOffValues(false)
               .Build()
           );
    }


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