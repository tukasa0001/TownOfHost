using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Modules;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Main.OverrideWelcomeMsg = "";
            try
            {
                //注:この時点では役職は設定されていません。
                Main.PlayerStates = new();

                Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
                Main.AllPlayerSpeed = new Dictionary<byte, float>();
                Main.BitPlayers = new Dictionary<byte, (byte, float)>();
                Main.WarlockTimer = new Dictionary<byte, float>();
                Main.AssassinTimer = new Dictionary<byte, float>();
                Main.isDoused = new Dictionary<(byte, byte), bool>();
                Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
                Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
                Main.isMarkAndKill = new Dictionary<byte, bool>();
                Main.MarkedPlayers = new Dictionary<byte, PlayerControl>();
                Main.MafiaRevenged = new Dictionary<byte, int>();
                Main.isCurseAndKill = new Dictionary<byte, bool>();
                Main.SKMadmateNowCount = 0;
                Main.isCursed = false;
                Main.isMarked = false;
                Main.existAntiAdminer = false;
                Main.PuppeteerList = new Dictionary<byte, byte>();
                Main.DetectiveNotify = new Dictionary<byte, string>();
                Main.HackerUsedCount = new Dictionary<byte, int>();
                Main.CyberStarDead = new List<byte>();
                Main.BoobyTrapBody = new List<byte>();
                Main.KillerOfBoobyTrapBody = new Dictionary<byte, byte>();

                Main.LastEnteredVent = new Dictionary<byte, Vent>();
                Main.LastEnteredVentLocation = new Dictionary<byte, UnityEngine.Vector2>();
                Main.EscapeeLocation = new Dictionary<byte, UnityEngine.Vector2>();

                Main.AfterMeetingDeathPlayers = new();
                Main.ResetCamPlayerList = new();
                Main.clientIdList = new();

                Main.SansKillCooldown = new();
                Main.CheckShapeshift = new();
                Main.ShapeshiftTarget = new();
                Main.SpeedBoostTarget = new Dictionary<byte, byte>();
                Main.MayorUsedButtonCount = new Dictionary<byte, int>();
                Main.ParaUsedButtonCount = new Dictionary<byte, int>();
                Main.MarioVentCount = new Dictionary<byte, int>();
                Main.targetArrows = new();

                ReportDeadBodyPatch.CanReport = new();

                Options.UsedButtonCount = 0;

                GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

                Main.introDestroyed = false;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

                Main.DiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
                Main.VotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
                Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
                Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

                NameColorManager.Instance.RpcReset();
                Main.LastNotifyNames = new();

                Main.currentDousingTarget = 255;
                Main.PlayerColors = new();
                //名前の記録
                Main.AllPlayerNames = new();

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
                    if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(pc.Data.DefaultOutfit.ColorId));
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
                    RPC.SyncCustomSettingsRPC();
                    Main.RefixCooldownDelay = 0;
                }
                FallFromLadder.Reset();
                BountyHunter.Init();
                SerialKiller.Init();
                FireWorks.Init();
                Sniper.Init();
                TimeThief.Init();
                Mare.Init();
                Witch.Init();
                SabotageMaster.Init();
                Executioner.Init();
                Jackal.Init();
                Sheriff.Init();
                ChivalrousExpert.Init();
                EvilTracker.Init();
                AntiAdminer.Init();
                LastImpostor.Init();
                CustomWinnerHolder.Reset();
                AntiBlackout.Reset();
                IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

                MeetingStates.MeetingCalled = false;
                MeetingStates.FirstMeeting = true;
                GameStates.AlreadyDied = false;
            }
            catch
            {
                Utils.ErrorEnd("Change Role Setting Postfix");
            }
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {

        public static Dictionary<byte, CustomRoles> devRoleToAssign = new();
        public static List<CustomRoles> addRoleList = new();
        public static List<CustomRoles> rolesToAssign = new();
        public static int addScientistNum = 0;
        public static int addEngineerNum = 0;
        public static int addShapeshifterNum = 0;

        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            try
            {
                //CustomRpcSenderとRpcSetRoleReplacerの初期化
                Dictionary<byte, CustomRpcSender> senders = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                            .StartMessage(pc.GetClientId());
                }
                RpcSetRoleReplacer.StartReplace(senders);

                // 开始职业抽取
                var rd = IRandom.Instance;
                int playerCount = Options.EnableGM.GetBool() ? PlayerControl.AllPlayerControls.Count - 1 : PlayerControl.AllPlayerControls.Count;
                int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
                int optNeutralNum = 0;
                if (Options.NeutralRolesMaxPlayer.GetInt() > 0 && Options.NeutralRolesMaxPlayer.GetInt() >= Options.NeutralRolesMinPlayer.GetInt())
                    optNeutralNum = rd.Next(Options.NeutralRolesMinPlayer.GetInt(), Options.NeutralRolesMaxPlayer.GetInt() + 1);
                int readyRoleNum = 0;
                int readyNeutralNum = 0;

                devRoleToAssign = new();
                rolesToAssign = new();
                addRoleList = new();

                List<CustomRoles> roleList = new();
                List<CustomRoles> roleOnList = new();
                List<CustomRoles> ImpOnList = new();
                List<CustomRoles> NeutralOnList = new();
                List<CustomRoles> roleRateList = new();
                List<CustomRoles> ImpRateList = new();
                List<CustomRoles> NeutralRateList = new();

                foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
                {
                    CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
                    if (CustomRolesHelper.IsAdditionRole(role))
                    {
                        if (role is CustomRoles.LastImpostor or CustomRoles.Lovers) continue;
                        addRoleList.Add(role);
                        continue;
                    }
                    if (CustomRolesHelper.IsVanilla(role)) continue;
                    if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
                    roleList.Add(role);
                }

                // 职业设置为：优先
                foreach (var role in roleList) if (role.GetMode() == 2)
                    {
                        if (role.IsImpostor()) ImpOnList.Add(role);
                        else if (role.IsNeutral()) NeutralOnList.Add(role);
                        else roleOnList.Add(role);
                    }
                // 职业设置为：启用
                foreach (var role in roleList) if (role.GetMode() == 1)
                    {
                        if (role.IsImpostor()) ImpRateList.Add(role);
                        else if (role.IsNeutral()) NeutralRateList.Add(role);
                        else roleRateList.Add(role);
                    }

                // 抽取优先职业（内鬼）
                while (ImpOnList.Count > 0)
                {
                    var select = ImpOnList[rd.Next(0, ImpOnList.Count)];
                    try { foreach (var dr in Main.DevRole) foreach (var role in roleRateList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } }  catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                    ImpOnList.Remove(select);
                    rolesToAssign.Add(select);
                    readyRoleNum += select.GetCount();
                    Logger.Info(select.ToString() + " 加入内鬼职业待选列表（优先）", "Role Assign Function");
                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                    if (readyRoleNum >= optImpNum) break;
                }

                // 优先职业不足以分配，开始分配启用的职业（内鬼）
                if (readyRoleNum < playerCount && readyRoleNum < optImpNum)
                {
                    while (ImpRateList.Count > 0)
                    {
                        var select = ImpRateList[rd.Next(0, ImpRateList.Count)];
                        try { foreach (var dr in Main.DevRole) foreach (var role in ImpRateList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } } catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                        ImpRateList.Remove(select);
                        rolesToAssign.Add(select);
                        readyRoleNum += select.GetCount();
                        Logger.Info(select.ToString() + " 加入内鬼职业待选列表", "Role Assign Function");
                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyRoleNum >= optImpNum) break;
                    }
                }

                // 抽取优先职业（中立）
                while (NeutralOnList.Count > 0 && optNeutralNum > 0)
                {
                    var select = NeutralOnList[rd.Next(0, NeutralOnList.Count)];
                    try { foreach (var dr in Main.DevRole) foreach (var role in NeutralOnList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } } catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                    NeutralOnList.Remove(select);
                    rolesToAssign.Add(select);
                    readyRoleNum += select.GetCount();
                    readyNeutralNum += select.GetCount();
                    Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "Role Assign Function");
                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                    if (readyNeutralNum >= optNeutralNum) break;
                }

                // 优先职业不足以分配，开始分配启用的职业（中立）
                if (readyRoleNum < playerCount && readyNeutralNum < optNeutralNum)
                {
                    while (NeutralRateList.Count > 0 && optNeutralNum > 0)
                    {
                        var select = NeutralRateList[rd.Next(0, NeutralRateList.Count)];
                        try { foreach (var dr in Main.DevRole) foreach (var role in NeutralRateList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } } catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                        NeutralRateList.Remove(select);
                        rolesToAssign.Add(select);
                        readyRoleNum += select.GetCount();
                        readyNeutralNum += select.GetCount();
                        Logger.Info(select.ToString() + " 加入中立职业待选列表", "Role Assign Function");
                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                        if (readyNeutralNum >= optNeutralNum) break;
                    }
                }

                // 抽取优先职业
                while (roleOnList.Count > 0)
                {
                    var select = roleOnList[rd.Next(0, roleOnList.Count)];
                    try { foreach (var dr in Main.DevRole) foreach (var role in roleOnList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } } catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                    roleOnList.Remove(select);
                    rolesToAssign.Add(select);
                    readyRoleNum += select.GetCount();
                    Logger.Info(select.ToString() + " 加入船员职业待选列表（优先）", "Role Assign Function");
                    if (readyRoleNum >= playerCount) goto EndOfAssign;
                }
                // 优先职业不足以分配，开始分配启用的职业
                if (readyRoleNum < playerCount)
                {
                    while (roleRateList.Count > 0)
                    {
                        var select = roleRateList[rd.Next(0, roleRateList.Count)];
                        try { foreach (var dr in Main.DevRole) foreach (var role in roleRateList) if (dr.Value == role && !rolesToAssign.Contains(role)) { select = role; devRoleToAssign.Add(dr.Key, dr.Value); Main.DevRole.Remove(dr.Key); } } catch (Exception e) { Logger.Fatal(e.Message, "Dev Role"); }
                        roleRateList.Remove(select);
                        rolesToAssign.Add(select);
                        readyRoleNum += select.GetCount();
                        Logger.Info(select.ToString() + " 加入船员职业待选列表", "Role Assign Function");
                        if (readyRoleNum >= playerCount) goto EndOfAssign;
                    }
                }

            // 职业抽取结束
            EndOfAssign:

                // 计算原版特殊职业数量
                addEngineerNum = 0;
                addScientistNum = 0;
                addShapeshifterNum = 0;
                foreach (var role in rolesToAssign)
                {
                    switch (CustomRolesHelper.GetVNRole(role))
                    {
                        case CustomRoles.Scientist: addScientistNum++; break;
                        case CustomRoles.Engineer: addEngineerNum++; break;
                        case CustomRoles.Shapeshifter: addShapeshifterNum++; break;
                    }
                }

                //指定原版特殊职业数量
                var roleOpt = Main.NormalOptions.roleOptions;

                int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
                int AdditionalScientistNum = addScientistNum;
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + AdditionalScientistNum, AdditionalScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
                int AdditionalEngineerNum = addEngineerNum;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = addShapeshifterNum;
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

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

                // 注册反职业
                foreach (var role in rolesToAssign.Where(x => x.IsDesyncRole()))
                {
                    byte player = byte.MaxValue;
                    foreach (var dr in devRoleToAssign)
                    {
                        if (dr.Value == role) player = dr.Key; break;
                    }
                    AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: role.GetDYRole(), devPlayer: player == byte.MaxValue ? null : Utils.GetPlayerById(player));
                }

                MakeDesyncSender(senders, rolesMap);
            }
            catch (Exception e)
            {
                Utils.ErrorEnd("Select Role Prefix");
                Logger.Fatal(e.Message, "Select Role Prefix");
            }
            //以下、バニラ側の役職割り当てが入る
        }

        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            try
            {
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

                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.Data.IsDead = false; //プレイヤーの死を解除する
                    if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
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
                    Main.PlayerStates[pc.PlayerId].MainRole = role;
                }

                var rd = IRandom.Instance;
                if (CustomRoles.Lovers.IsEnable() && rd.Next(1, 100) <= Options.LoverSpawnChances.GetInt()) AssignLoversRolesFromList();
                foreach (var role in addRoleList)
                {
                    if (rd.Next(1, 100) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(role, out var sc) ? sc.GetFloat() : 0))
                        if (role.IsEnable()) AssignSubRoles(role);
                }

                foreach (var role in rolesToAssign)
                {
                    if (role.IsDesyncRole()) continue;
                    List<PlayerControl> playerList = new();
                    switch (CustomRolesHelper.GetVNRole(role))
                    {
                        case CustomRoles.Crewmate:
                            playerList = Crewmates;
                            break;
                        case CustomRoles.Engineer:
                            playerList = Engineers;
                            break;
                        case CustomRoles.Scientist:
                            playerList = Scientists;
                            break;
                        case CustomRoles.Impostor:
                            playerList = Impostors;
                            break;
                        case CustomRoles.Shapeshifter:
                            playerList = Shapeshifters;
                            break;
                        default:
                            Logger.Error(role.ToString() + " 存在于列表但没有被注册", "Assign Roles In List");
                            break;
                    }
                    byte player = byte.MaxValue;
                    foreach (var dr in devRoleToAssign)
                    {
                        if (dr.Value == role) player = dr.Key; break;
                    }
                        AssignCustomRolesFromList(role, playerList, devPlayer: player == byte.MaxValue ? null : Utils.GetPlayerById(player));
                }

                //RPCによる同期
                foreach (var pair in Main.PlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                HudManager.Instance.SetHudActive(true);
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                    switch (pc.GetCustomRole())
                    {
                        case CustomRoles.BountyHunter:
                            BountyHunter.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SerialKiller:
                            SerialKiller.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Witch:
                            Witch.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Warlock:
                            Main.CursedPlayers.Add(pc.PlayerId, null);
                            Main.isCurseAndKill.Add(pc.PlayerId, false);
                            break;
                        case CustomRoles.Assassin:
                            Main.MarkedPlayers.Add(pc.PlayerId, null);
                            Main.isMarkAndKill.Add(pc.PlayerId, false);
                            break;
                        case CustomRoles.FireWorks:
                            FireWorks.Add(pc.PlayerId);
                            break;
                        case CustomRoles.TimeThief:
                            TimeThief.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Sniper:
                            Sniper.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mare:
                            Mare.Add(pc.PlayerId);
                            break;
                        case CustomRoles.ChivalrousExpert:
                            ChivalrousExpert.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Hacker:
                            Main.HackerUsedCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.Arsonist:
                            foreach (var ar in Main.AllPlayerControls)
                                Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                            break;
                        case CustomRoles.Executioner:
                            Executioner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Jackal:
                            Jackal.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Sheriff:
                            Sheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mayor:
                            Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.Paranoia:
                            Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.SabotageMaster:
                            SabotageMaster.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilTracker:
                            EvilTracker.Add(pc.PlayerId);
                            break;
                        case CustomRoles.AntiAdminer:
                            AntiAdminer.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Psychic:
                            Main.PsychicTarget.Clear();
                            break;
                        case CustomRoles.Mario:
                            Main.MarioVentCount[pc.PlayerId] = 0;
                            break;
                    }
                    pc.ResetKillCooldown();
                }

                //役職の人数を戻す
                var roleOpt = Main.NormalOptions.roleOptions;

                int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
                ScientistNum -= addScientistNum;
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
                EngineerNum -= addEngineerNum;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= addShapeshifterNum;
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

                GameEndChecker.SetPredicateToNormal();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }

                // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
                Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist).Select(p => p.PlayerId));
                Utils.CountAliveImpostors();
                Utils.SyncAllSettings();
                SetColorPatch.IsAntiGlitchDisabled = false;
            }
            catch (Exception e)
            {
                Utils.ErrorEnd("Select Role Postfix");
                Logger.Fatal(e.Message, "Select Role Prefix");
            }
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate, PlayerControl devPlayer = null)
        {
            if (!role.IsEnable()) return;
            if (AllPlayers == null || AllPlayers.Count <= 0) return;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rd = IRandom.Instance;

            var count = Math.Clamp(-1, 0, AllPlayers.Count);
            count = Math.Clamp(role.GetCount(), 0, AllPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < role.GetCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = devPlayer ?? AllPlayers[rd.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;

                var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                //Desync役職視点
                foreach (var target in Main.AllPlayerControls)
                {
                    if (player.PlayerId != target.PlayerId) rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                    else rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
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
            }
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

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1, PlayerControl devPlayer = null)
        {
            if (players == null || players.Count <= 0) return null;
            var rd = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = devPlayer ?? players[rd.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
            if (role == CustomRoles.AntiAdminer) Main.existAntiAdminer = true;
            return AssignedPlayers;
        }

        private static void AssignLoversRolesFromList()
        {
            if (CustomRoles.Lovers.IsEnable())
            {
                //Loversを初期化
                Main.LoversPlayers.Clear();
                Main.isLoversDead = false;
                //ランダムに2人選出
                AssignLoversRoles(2);
            }
        }
        private static void AssignLoversRoles(int RawCount = -1)
        {
            var allPlayers = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.GM) || (pc.HasSubRole() && !Options.NoLimitAddonsNum.GetBool()) || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Ntr)) continue;
                allPlayers.Add(pc);
            }
            var role = CustomRoles.Lovers;
            var rd = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rd.Next(0, allPlayers.Count)];
                Main.LoversPlayers.Add(player);
                allPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers();
        }
        private static void AssignSubRoles(CustomRoles role, int RawCount = -1)
        {
            var allPlayers = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.GM) || (pc.HasSubRole() && !Options.NoLimitAddonsNum.GetBool()) || pc.Is(CustomRoles.Needy)) continue;
                if ((role is CustomRoles.Madmate or CustomRoles.Lighter) && !pc.GetCustomRole().IsCrewmate()) continue;
                if (role is CustomRoles.Bewilder && pc.GetCustomRole().IsImpostor()) continue;
                if (role is CustomRoles.Ntr && pc.Is(CustomRoles.Lovers)) continue;
                allPlayers.Add(pc);
            }
            var rd = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rd.Next(0, allPlayers.Count)];
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "Assign " + role.ToString());
            }
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