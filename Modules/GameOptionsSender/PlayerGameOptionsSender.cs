using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

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
        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
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
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Madmate:
                    AURoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(true);
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            switch (role)
            {
                case CustomRoles.Terrorist:
                    AURoleOptions.EngineerCooldown = 0;
                    AURoleOptions.EngineerInVentMaxTime = 0;
                    break;
                case CustomRoles.ShapeMaster:
                    AURoleOptions.ShapeshifterCooldown = 0f;
                    AURoleOptions.ShapeshifterLeaveSkin = false;
                    AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                    break;
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
            }
            if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
            {
                AURoleOptions.KillCooldown = Mathf.Max(0f, killCooldown);
            }

            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
            {
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
            }

            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
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
                if (player.Is(CountTypes.Impostor))
                {
                    AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                }
            }
            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;

            return opt;
        }

        public override bool AmValid()
        {
            return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
        }
    }
}