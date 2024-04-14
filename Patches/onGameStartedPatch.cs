using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHostForE.Attributes;
using TownOfHostForE.GameMode;
using TownOfHostForE.Modules;
using TownOfHostForE.Patches;
using TownOfHostForE.Roles;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.Neutral;
using static TownOfHostForE.Translator;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static int ImpostorSetNum = 2;
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            ImpostorSetNum = Main.NormalOptions.NumImpostors;

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();

            Main.SKMadmateNowCount = 0;

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            Main.NotCrewAssignCount = new();

            ReportDeadBodyPatch.CanReport = new();
            ReportDeadBodyPatch.CanReportByDeadBody = new();
            MeetingHudPatch.RevengeTargetPlayer = new();
            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();
            //名前の記録
            Main.AllPlayerNames = new();

            //ラバーズ系
            Main.LoversPlayersV2 = new();
            Main.isLoversDeadV2 = new();
            Main.isLoversLeaders = new();

            //キルカウントリセット
            Main.killCount = new();

            //デスゲーム判定をリセット
            DarkGameMaster.IsDeathGameTime = false;

            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            GameModuleInitializerAttribute.InitializeAll();

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.GetNameChangeModes() == NameChange.Color)
                {
                    if(pc.Is(CustomRoles.Rainbow)) pc.RpcSetName(GetString("RainbowColor"));
                    else pc.RpcSetName(Palette.GetColorName(colorId));
                }
                PlayerState.Create(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.CanReportByDeadBody[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                }
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        private static bool AssignedStrayWolf = false;
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            RoleAssignManager.SelectAssignRoles();

            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    int numRoleTypes = GetRoleTypesCount(roleTypes);
                    roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
                }

                List<PlayerControl> AllPlayers = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (Options.EnableGM.GetBool())
                {
                    AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    PlayerControl.LocalPlayer.Data.IsDead = true;
                }
                Dictionary<(byte, byte), RoleTypes> rolesMap = new();

                foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                {
                    if (info.IsDesyncImpostor)
                    {
                        switch (role)
                        {
                            case CustomRoles.StrayWolf:
                                AssignedStrayWolf = AssignDesyncRole(CustomRoles.StrayWolf, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor, IsImpostorRole: true);
                                continue;
                            case CustomRoles.Opportunist:
                                if (!Opportunist.OptionCanKill.GetBool()) continue;
                                break;
                        }
                        AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: info.BaseRoleType.Invoke());
                    }
                }

                MakeDesyncSender(senders, rolesMap);
            }
            //以下、バニラ側の役職割り当てが入る
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            var rand = IRandom.Instance;

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();

            List<PlayerControl> allPlayersbySub = new();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する

                if (!pc.Is(CustomRoles.GM)) allPlayersbySub.Add(pc);

                var state = PlayerState.GetByPlayerId(pc.PlayerId);
                if (state.MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                state.SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoleTypes.Impostor))
                        pc.RpcSetColor(0);
                    else if (pc.Is(CustomRoleTypes.Crewmate))
                        pc.RpcSetColor(1);
                }

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;

                GameEndChecker.SetPredicateToHideAndSeek();
            }
            else if (Options.CurrentGameMode == CustomGameMode.SuperBombParty)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;

                //役職設定処理
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    HudManager.Instance.SetHudActive(true);
                    //Main.AllPlayerKillCooldown[pc.PlayerId] = SuperBakuretsuBros.SBPKillCool.GetInt();
                    Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown;
                }
                GameEndChecker.SetPredicateToSuperBombParty();

                SuperBakuretsuBros.Add();
            }
            else
            {
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsVanilla()) continue;
                    //if (role is CustomRoles.HASTroll or CustomRoles.HASFox or CustomRoles.BAKURETSUKI) continue;
                    //if (CustomRoleManager.GetRoleInfo(role) is SimpleRoleInfo info && info.RequireResetCam) continue;
                    if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                    if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
                    if (role == CustomRoles.StrayWolf && AssignedStrayWolf) continue;
                    if (role is not CustomRoles.Opportunist and not CustomRoles.StrayWolf and not CustomRoles.Egoist &&
                        CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;

                    var baseRoleTypes = role.GetRoleTypes() switch
                    {
                        RoleTypes.Impostor => Impostors,
                        RoleTypes.Shapeshifter => Shapeshifters,
                        RoleTypes.Scientist => Scientists,
                        RoleTypes.Engineer => Engineers,
                        RoleTypes.GuardianAngel => GuardianAngels,
                        _ => Crewmates,
                    };
                    AssignCustomRolesFromList(role, baseRoleTypes);
                }

                // Random-Addon
                AssignCustomSubRolesFromListfromNotCrew(CustomRoles.Gambler);
                //if (!CustomRoles.PlatonicLover.IsEnable() || !CustomRoles.OtakuPrincess.IsEnable()) AssignLoversRolesFromList(allPlayersbySub);
                AssignLoversRolesFromList(allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddWatch, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Sunglasses, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddLight, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddSeer, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Autopsy, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.VIP, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Clumsy, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Revenger, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Management, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.InfoPoor, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Sending, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.TieBreaker, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.NonReport, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.PlusVote, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Guarding, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.AddBait, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Refusing, allPlayersbySub);
                AssignCustomSubRolesFromList(CustomRoles.Chu2Byo, allPlayersbySub);

                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                CustomRoleManager.CreateInstance();

                ImposterChat.Add();

                foreach (var pc in Main.AllPlayerControls)
                {
                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();

                    // DirectAssign-Addon
                    if (pc.GetCustomRole().IsAddAddOn()
                        && (Options.AddOnBuffAssign[pc.GetCustomRole()].GetBool() || Options.AddOnDebuffAssign[pc.GetCustomRole()].GetBool()))
                    {
                        foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                        {
                            if (Options.AddOnRoleOptions.TryGetValue((pc.GetCustomRole(), Addon), out var option) && option.GetBool())
                            {
                                pc.RpcSetCustomRole(Addon);
                                CustomRoleManager.subRoleAdd(pc.PlayerId, Addon);
                            }
                        }
                    }

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller() || pc.IsAnimalsKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                        }
                    }
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        if (seer == pc) continue;
                        if (pc.Is(CustomRoles.GM)
                            || (pc.Is(CustomRoles.Workaholic) && Workaholic.Seen)
                            || pc.Is(CustomRoles.Rainbow))
                            NameColorManager.Add(seer.PlayerId, pc.PlayerId, pc.GetRoleColorCode());
                    }
                }

                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    roleOpt.SetRoleRate(roleTypes, 0, 0);
                }
                GameEndChecker.SetPredicateToNormal();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }
            }

            /*
            //インポスターのゴーストロールがクルーになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */

            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }

        private static bool AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate,bool IsImpostorRole = false)
        {
            if (!role.IsPresent()) return false;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;
            var realAssigned = 0;

            if (IsImpostorRole)
            {
                var impostorNum = Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors);
                if (impostorNum == role.GetRealCount()) return false;
                if (Main.tempImpostorNum == 0)
                    Main.tempImpostorNum = impostorNum;
            }

            for (var i = 0; i < role.GetRealCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                if (role.IsImpostor() ||
                    role.IsNeutral() ||
                    role.IsAnimals())
                {
                    Logger.Info("非クルーカウント:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
                    Main.NotCrewAssignCount.Add(player);
                }

                PetSettings.PetRoleCheck(role);
                AllPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

                var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                //Desync役職視点
                foreach (var target in Main.AllPlayerControls)
                {
                    if (player.PlayerId != target.PlayerId)
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                    }
                    else
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                    }
                }

                //他者視点
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (player.PlayerId != seer.PlayerId)
                    {
                        rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                    }
                }
                RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
                //ホスト視点はロール決定
                player.SetRole(othersRole);
                player.Data.IsDead = true;
                realAssigned++;

                Logger.Info("役職設定(desync):" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
            }

            if (IsImpostorRole) Main.NormalOptions.NumImpostors -= realAssigned;

            return realAssigned > 0;
        }
        public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
        {
            var hostId = PlayerControl.LocalPlayer.PlayerId;
            foreach (var seer in Main.AllPlayerControls)
            {
                var sender = senders[seer.PlayerId];
                foreach (var target in Main.AllPlayerControls)
                {
                    if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                    {
                        sender.RpcSetRole(seer, role, target.GetClientId());
                    }
                }
            }
        }

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetRealCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                if (role.IsImpostor() ||
                    role.IsNeutral() ||
                    role.IsAnimals())
                {
                    Logger.Info("非クルーカウント:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
                    Main.NotCrewAssignCount.Add(player);
                }
                PetSettings.PetRoleCheck(role);
                players.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (player.Is(CustomRoles.HASTroll))
                        player.RpcSetColor(2);
                    else if (player.Is(CustomRoles.HASFox))
                        player.RpcSetColor(3);
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
            return AssignedPlayers;
        }

        private static List<PlayerControl> AssignCustomSubRolesFromList(CustomRoles role, List<PlayerControl> allPlayersbySub, int RawCount = -1)
        {
            if (allPlayersbySub == null || allPlayersbySub.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, allPlayersbySub.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayersbySub.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();

            List<byte> Lovers = new();

            for (var i = 0; i < count; i++)
            {
                var player = allPlayersbySub[rand.Next(0, allPlayersbySub.Count)];
                AssignedPlayers.Add(player);
                if (role == CustomRoles.Lovers)
                {
                    //Main.LoversPlayers.Add(player);
                    Lovers.Add(player.PlayerId);
                }
                allPlayersbySub.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
                Logger.Info("属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignSubRoles");
            }
            if (role == CustomRoles.Lovers)
            {
                //先頭の人を親に
                Main.LoversPlayersV2.Add(Lovers[0], Lovers);
                Main.isLoversDeadV2.Add(Lovers[0],false);
                Main.isLoversLeaders.Add(Lovers[0]);
                RPC.SyncLoversPlayers();
            }

            return AssignedPlayers;
        }
        private static List<PlayerControl> AssignCustomSubRolesFromListfromNotCrew(CustomRoles role, int RawCount = -1)
        {
            Logger.Info("カウント数:" + Main.NotCrewAssignCount.Count, "AssignRoles");
            if (Main.NotCrewAssignCount == null || Main.NotCrewAssignCount.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, Main.NotCrewAssignCount.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, Main.NotCrewAssignCount.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();

            for (var i = 0; i < count; i++)
            {
                var player = Main.NotCrewAssignCount[rand.Next(0, Main.NotCrewAssignCount.Count)];
                AssignedPlayers.Add(player);
                Main.NotCrewAssignCount.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
                Logger.Info("非クルー向け属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignSubRoles");
            }
            if (role == CustomRoles.Lovers) RPC.SyncLoversPlayers();

            return AssignedPlayers;
        }

        private static List<PlayerControl> AssignLoversRolesFromList(List<PlayerControl> allPlayersbySub)
        {
            if (!CustomRoles.Lovers.IsEnable()) return null;
                //Loversを初期化
                Main.LoversPlayersV2.Clear();
                Main.isLoversDeadV2.Clear();
                //ランダムに2人選出
                //AssignLoversRoles(2);
                return AssignCustomSubRolesFromList(CustomRoles.Lovers, allPlayersbySub, 2);
        }

        public static int GetRoleTypesCount(RoleTypes roleTypes)
        {
            int count = 0;
            foreach (var role in CustomRolesHelper.AllRoles)
            {
                if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
	            if (role is not CustomRoles.Opportunist &&
                    CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
	            if (role.GetRoleTypes() == roleTypes)
	                count += role.GetRealCount();
            }
            return count;
        }


        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
        {
            public static bool doReplace = false;
            public static Dictionary<byte, CustomRpcSender> senders;
            public static List<(PlayerControl, RoleTypes)> StoragedData = new();
            // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
            public static List<CustomRpcSender> OverriddenSenderList;
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
            {
                if (doReplace && senders != null)
                {
                    StoragedData.Add((__instance, roleType));
                    return false;
                }
                else return true;
            }
            public static void Release()
            {
                foreach (var sender in senders)
                {
                    if (OverriddenSenderList.Contains(sender.Value)) continue;
                    if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                        throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                    foreach (var pair in StoragedData)
                    {
                        pair.Item1.SetRole(pair.Item2);
                        sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                            .Write((ushort)pair.Item2)
                            .EndRpc();
                    }
                    sender.Value.EndMessage();
                }
                doReplace = false;
            }
            public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
            {
                RpcSetRoleReplacer.senders = senders;
                StoragedData = new();
                OverriddenSenderList = new();
                doReplace = true;
            }
        }
    }
}