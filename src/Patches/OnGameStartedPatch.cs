using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Patches.Actions;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Game.Setup();
            GameOptionsManager.Instance.CurrentGameOptions = GameOptionsManager.Instance.normalGameHostOptions.Cast<IGameOptions>();

            //注:この時点では役職は設定されていません。
            Main.PlayerStates = new();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            Main.isCurseAndKill = new Dictionary<byte, bool>();
            Main.SKMadmateNowCount = 0;
            Main.isCursed = false;
            Main.PuppeteerList = new Dictionary<byte, byte>();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();
            Main.SpeedBoostTarget = new Dictionary<byte, byte>();
            Main.MayorUsedButtonCount = new Dictionary<byte, int>();
            Main.targetArrows = new();

            ReportDeadBodyPatch.CanReport = new();

            OldOptions.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            Main.DiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
            Main.VotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.currentDousingTarget = 255;
            Main.PlayerColors = new();
            //名前の記録
            Main.AllPlayerNames = new();

            foreach (var target in PlayerControl.AllPlayerControls)
            {
                foreach (var seer in PlayerControl.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                Main.RefixCooldownDelay = 0;
                if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    OldOptions.HideAndSeekKillDelayTimer = OldOptions.KillDelay.GetFloat();
                }
                if (OldOptions.IsStandardHAS)
                {
                    OldOptions.HideAndSeekKillDelayTimer = OldOptions.StandardHASWaitingTime.GetFloat();
                }
            }
            FallFromLadder.Reset();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            /*IRandom.SetInstanceById(OldOptions.RoleAssigningAlgorithm.GetValue());*/

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Game.State = GameState.InIntro;

            DesyncOptions.OriginalHostOptions = GameOptionsManager.Instance.CurrentGameOptions;

            List<PlayerControl> AllPlayers = new();
            foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
            {
                AllPlayers.Add(playerControl);
                CustomRoleManager.PlayersCustomRolesRedux[playerControl.PlayerId] = CustomRoleManager.Default;
            }

            List<PlayerControl> unassignedPlayers = Game.GetAllPlayers().ToList();

            bool hunterSpawn = false;
            int impostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;

            List<CustomRole> impostorRoles =
                RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.Factions.IsImpostor())), 0, impostors);


            while (impostorRoles.Count < impostors)
                impostorRoles.Add(CustomRoleManager.Static.Impostor);

            List<CustomRole> neutralKillingRoles =
                RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.SpecialType is SpecialType.NeutralKilling)), StaticOptions.MinNK, StaticOptions.MaxNK);

            List<CustomRole> neutralPassiveRoles =
                RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.SpecialType is SpecialType.Neutral)), StaticOptions.MinNonNK, StaticOptions.MaxNK);

            List<CustomRole> crewMateRoles =
                RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.Factions.IsCrewmate())), 0, ModConstants.MaxPlayers);

            List<CustomRole> joinedRoleSelection = new(impostorRoles);
            joinedRoleSelection.AddRange(neutralKillingRoles);
            joinedRoleSelection.AddRange(neutralPassiveRoles);
            joinedRoleSelection.AddRange(crewMateRoles);

            joinedRoleSelection.PrettyString().DebugLog("Remaining Roles: ");
            List<Tuple<PlayerControl, CustomRole>> assignments = new();

            int i = 0;
            while (i < unassignedPlayers.Count)
            {
                PlayerControl player = unassignedPlayers[i];
                CustomRole role = CustomRoleManager.Roles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(player.GetRawName()?.ToLower() ?? "HEHXD"));
                if (role != null && role.GetType() != typeof(Crewmate))
                {
                    role = CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
                    role.SyncOptions();
                    assignments.Add(new Tuple<PlayerControl, CustomRole>(player, role));
                    unassignedPlayers.Pop(i);
                }
                else i++;
            }


            while (unassignedPlayers.Count > 0 && joinedRoleSelection.Count > 0)
            {
                PlayerControl assignedPlayer = unassignedPlayers.PopRandom();
                CustomRole role = joinedRoleSelection.Pop(0);
                //CustomRoleManager.PlayersCustomRolesRedux[assignedPlayer.PlayerId] = role;

                // We have to initialize the role past its "static" phase
                role = CustomRoleManager.PlayersCustomRolesRedux[assignedPlayer.PlayerId] = role.Instantiate(assignedPlayer);
                role.SyncOptions();
                assignments.Add(new Tuple<PlayerControl, CustomRole>(assignedPlayer, role));

            }

            while (unassignedPlayers.Count > 0)
            {
                PlayerControl unassigned = unassignedPlayers.Pop(0);
                CustomRole role = CustomRoleManager.PlayersCustomRolesRedux[unassigned.PlayerId] = CustomRoleManager.Static.Crewmate.Instantiate(unassigned);
                role.SyncOptions();
                assignments.Add(new System.Tuple<PlayerControl, CustomRole>(unassigned, role));
            }

            List<Subrole> subroles = CustomRoleManager.Roles.OfType<Subrole>().ToList();
            while (subroles.Count > 0)
            {
                Subrole subrole = subroles.PopRandom();
                bool hasSubrole = subrole.Chance > UnityEngine.Random.RandomRange(0, 100);
                if (!hasSubrole) continue;
                List<PlayerControl> victims = Game.GetAllPlayers().Where(p => p.GetSubrole() == null).ToList();
                if (victims.Count == 0) break;
                PlayerControl victim = victims.GetRandom();
                CustomRoleManager.AddPlayerSubrole(victim.PlayerId, (Subrole)subrole.Instantiate(victim));
            }
            Game.GetAllPlayers().Do(p => p.GetCustomRole().Assign());
        }


        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            Game.GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions());

            List<Tuple<string, CustomRole>> debugList = CustomRoleManager.PlayersCustomRolesRedux
                .Select(kvp => new Tuple<string, CustomRole>(Utils.GetPlayerById(kvp.Key).name, kvp.Value))
                .ToList();

            Logger.Info($"Assignments: {String.Join(", ", debugList)}", "");

            Main.ResetCamPlayerList.AddRange(Game.GetAllPlayers().Where(p => p.GetCustomRole() is Arsonist).Select(p => p.PlayerId));
            Utils.CountAliveImpostors();
            Game.RenderAllForAll(state: GameState.InIntro);
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
    }
}