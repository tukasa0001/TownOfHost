using System.Linq;
using Il2CppSystem.Linq;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;
using Mathf = UnityEngine.Mathf;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .Where(sender => sender.player.PlayerId == playerId)
            .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else base.SendGameOptions();
        }

        public override void SendOptionsArray(byte[] optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }

        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            AURoleOptions.SetOpt(opt);
            var state = Main.PlayerStates[player.PlayerId];
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            RoleType roleType = role.GetRoleType();
            switch (roleType)
            {
                case RoleType.Impostor:
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case RoleType.Madmate:
                    AURoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(true);
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            switch (player.GetCustomRole())
            {
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                // case CustomRoles.ShapeMaster:
                //     roleOpt.ShapeshifterCooldown = 0.1f;
                //     roleOpt.ShapeshifterLeaveSkin = false;
                //     roleOpt.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                //     break;
                case CustomRoles.Warlock:
                    AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyGameOptions(player);
                    break;
                case CustomRoles.BountyHunter:
                    BountyHunter.ApplyGameOptions();
                    break;
                case CustomRoles.EvilWatcher:
                case CustomRoles.NiceWatcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    opt.SetVision(false);
                    break;
                case CustomRoles.Lighter:
                    if (player.GetPlayerTaskState().IsTaskFinished)
                    {
                        opt.SetFloat(
                            FloatOptionNames.CrewLightMod,
                            Options.LighterTaskCompletedVision.GetFloat());
                        if (Utils.IsActive(SystemTypes.Electrical) && Options.LighterTaskCompletedDisableLightOut.GetBool())
                        {
                            opt.SetFloat(
                            FloatOptionNames.CrewLightMod,
                            opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
                        }
                    }
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    opt.SetVision(true);
                    break;
                case CustomRoles.Doctor:
                    AURoleOptions.ScientistCooldown = 0f;
                    AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                    break;
                case CustomRoles.Mayor:
                    AURoleOptions.EngineerCooldown =
                        Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt()
                        ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                        : 300f;
                    AURoleOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.EvilTracker:
                    EvilTracker.ApplyGameOptions(player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.JSchrodingerCat:
                    Jackal.ApplyGameOptions(opt);
                    break;


                InfinityVent:
                    AURoleOptions.EngineerCooldown = 0;
                    AURoleOptions.EngineerInVentMaxTime = 0;
                    break;
            }
            if (Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
            {
                foreach (var kc in Main.AllPlayerKillCooldown)
                {
                    if (kc.Key == player.PlayerId)
                    {
                        opt.SetFloat(
                            FloatOptionNames.KillCooldown,
                            kc.Value > 0 ? kc.Value : 0.01f);
                    }
                }
            }
            if (Main.AllPlayerSpeed.ContainsKey(player.PlayerId))
            {
                foreach (var speed in Main.AllPlayerSpeed)
                {
                    if (speed.Key == player.PlayerId)
                    {
                        opt.SetFloat(
                            FloatOptionNames.PlayerSpeedMod,
                            Mathf.Clamp(speed.Value, Main.MinSpeed, 3f));
                    }
                }
            }
            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead))
            {
                opt.SetInt(
                    Int32OptionNames.EmergencyCooldown,
                    Options.AdditionalEmergencyCooldownTime.GetInt());
            }
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
            }
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Egoist))
                {
                    opt.SetFloat(FloatOptionNames.PlayerSpeedMod, Main.MinSpeed);
                }
            }
            opt.SetInt(Int32OptionNames.DiscussionTime, Mathf.Clamp(Main.DiscussionTime, 0, 300));

            opt.SetInt(
                Int32OptionNames.VotingTime,
                Mathf.Clamp(Main.VotingTime, TimeThief.LowerLimitVotingTime.GetInt(), 300));

            if (Options.AllAliveMeeting.GetBool() && GameData.Instance.AllPlayers.ToArray().Where(x => !x.Object.Is(CustomRoles.GM)).All(x => !x.IsDead))
            {
                opt.SetInt(Int32OptionNames.DiscussionTime, 0);
                opt.SetInt(
                Int32OptionNames.VotingTime,
                Options.AllAliveMeetingTime.GetInt());
            }

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;

            return opt;
        }
    }
}