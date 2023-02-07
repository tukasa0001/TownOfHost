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
                    if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                    {
                        Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                    }
                    if (Options.IsStandardHAS)
                    {
                        Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                    }
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
                Egoist.Init();
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
                if (AmongUsClient.Instance.AmHost)
                {
                    ChatUpdatePatch.DoBlockChat = true;
                    Logger.Fatal("Change Role Setting Postfix 错误，触发防黑屏措施", "Anti-black");
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    Utils.SendMessage("由于未知错误发生，已终止游戏以防止黑屏\n若您是房主，如果可以的话请发送/dump并将桌面上的文件发送给咔皮呆，非常感谢您的贡献！");
                    RPC.ForceEndGame();
                }
                else
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Destroy);
                    Logger.Fatal("Change Role Setting Postfix 错误", "Anti-black");
                    Logger.SendInGame("很不幸，您似乎触发了TOH古老的半屏Bug\n记住，这100%是原生TOH的锅哈~", true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
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

                //ウォッチャーの陣営抽選
                Options.SetWatcherTeam(Options.EvilWatcherChance.GetFloat());

                if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
                {
                    //役職の人数を指定
                    var rd = Utils.RandomSeedByGuid();
                    var roleOpt = Main.NormalOptions.roleOptions;
                    int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
                    int AdditionalScientistNum = CustomRoles.Doctor.GetCount();
                    if (Options.TrueRandomeRoles.GetBool())
                    {
                        AdditionalScientistNum = rd.Next(0, AdditionalScientistNum + 1);
                        if (AdditionalScientistNum > 0 && rd.Next(0, 100) > 30) AdditionalScientistNum--;
                    }
                    roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + AdditionalScientistNum, AdditionalScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));

                    int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);

                    int AdditionalEngineerNum = CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount() + CustomRoles.Paranoia.GetCount() + CustomRoles.Plumber.GetCount() + CustomRoles.Mario.GetCount();// - EngineerNum;

                    if (Options.MayorHasPortableButton.GetBool())
                        AdditionalEngineerNum += CustomRoles.Mayor.GetCount();

                    if (Options.MadSnitchCanVent.GetBool())
                        AdditionalEngineerNum += CustomRoles.MadSnitch.GetCount();

                    if (Options.TrueRandomeRoles.GetBool())
                    {
                        AdditionalEngineerNum = rd.Next(0, AdditionalEngineerNum + 1);
                        if (AdditionalEngineerNum > 0 && rd.Next(0, 100) > 35) AdditionalEngineerNum -= rd.Next(0, AdditionalEngineerNum);
                    }
                    roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                    int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                    int AdditionalShapeshifterNum = CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.Assassin.GetCount() + CustomRoles.Miner.GetCount() + CustomRoles.Escapee.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount() + CustomRoles.Bomber.GetCount();//- ShapeshifterNum;
                    if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1)
                        AdditionalShapeshifterNum += CustomRoles.Egoist.GetCount();
                    if (Options.TrueRandomeRoles.GetBool()) ShapeshifterNum = rd.Next(0, ShapeshifterNum + 1);
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
                    if (Options.TrueRandomeRoles.GetBool())
                    {
                        if (rd.Next(0, 100) < 20) AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        if (rd.Next(0, 100) < 30) AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        if (rd.Next(0, 100) < 30) AssignDesyncRole(CustomRoles.Jackal, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        if (rd.Next(0, 100) < 20) AssignDesyncRole(CustomRoles.ChivalrousExpert, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        if (rd.Next(0, 100) < 30) AssignDesyncRole(CustomRoles.OpportunistKiller, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                    }
                    else
                    {
                        AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        AssignDesyncRole(CustomRoles.Jackal, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        AssignDesyncRole(CustomRoles.ChivalrousExpert, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                        AssignDesyncRole(CustomRoles.OpportunistKiller, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                    }
                    MakeDesyncSender(senders, rolesMap);
                }
            }
            catch
            {
                ChatUpdatePatch.DoBlockChat = true;
                Logger.Fatal("Select Roles Prefix 错误，触发防黑屏措施", "Anti-black");
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                Utils.SendMessage("由于未知错误发生，已终止游戏以防止黑屏\n若您是房主，如果可以的话请发送/dump并将桌面上的文件发送给咔皮呆，非常感谢您的贡献！");
                RPC.ForceEndGame();
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

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(RoleType.Impostor))
                        pc.RpcSetColor(0);
                    else if (pc.Is(RoleType.Crewmate))
                        pc.RpcSetColor(1);
                }

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
                foreach (var pair in Main.PlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;

                GameEndChecker.SetPredicateToHideAndSeek();
            }
            else
            {
                List<int> funList = new();
                for (int i = 0; i <= 55; i++)
                {
                    funList.Add(i);
                }

                Random rd = new();
                int index = 0;
                int temp;
                for (int i = 0; i < funList.Count; i++)
                {
                    index = rd.Next(0, funList.Count - 1);
                    if (index != i)
                    {
                        temp = funList[i];
                        funList[i] = funList[index];
                        funList[index] = temp;
                    }
                }

                funList.Remove(0);
                if (rd.Next(0, 100) <= Options.LoverSpawnChances.GetInt()) funList.Insert(0, 0);
                funList.Remove(55);
                if (rd.Next(0, 100) <= Options.NtrSpawnChances.GetInt()) funList.Insert(0, 55);

                foreach (int i in funList)
                {
                    switch (i)
                    {
                        case 0: AssignLoversRolesFromList(); break;
                        case 1: AssignCustomRolesFromList(CustomRoles.Sniper, Shapeshifters); break;
                        case 2: AssignCustomRolesFromList(CustomRoles.Jester, Crewmates); break;
                        case 3: AssignCustomRolesFromList(CustomRoles.Madmate, Engineers); break;
                        case 4: AssignCustomRolesFromList(CustomRoles.Bait, Crewmates); break;
                        case 5: AssignCustomRolesFromList(CustomRoles.MadGuardian, Crewmates); break;
                        case 6: AssignCustomRolesFromList(CustomRoles.MadSnitch, Options.MadSnitchCanVent.GetBool() ? Engineers : Crewmates); break;
                        case 7: AssignCustomRolesFromList(CustomRoles.Mayor, Options.MayorHasPortableButton.GetBool() ? Engineers : Crewmates); break;
                        case 8: AssignCustomRolesFromList(CustomRoles.Opportunist, Crewmates); break;
                        case 9: AssignCustomRolesFromList(CustomRoles.Snitch, Crewmates); break;
                        case 10: AssignCustomRolesFromList(CustomRoles.SabotageMaster, Crewmates); break;
                        case 11: AssignCustomRolesFromList(CustomRoles.Mafia, Impostors); break;
                        case 12: AssignCustomRolesFromList(CustomRoles.Terrorist, Engineers); break;
                        case 13: AssignCustomRolesFromList(CustomRoles.Executioner, Crewmates); break;
                        case 14: AssignCustomRolesFromList(CustomRoles.Vampire, Impostors); break;
                        case 15: AssignCustomRolesFromList(CustomRoles.BountyHunter, Shapeshifters); break;
                        case 16: AssignCustomRolesFromList(CustomRoles.Witch, Impostors); break;
                        case 17: AssignCustomRolesFromList(CustomRoles.Warlock, Shapeshifters); break;
                        case 18: AssignCustomRolesFromList(CustomRoles.SerialKiller, Shapeshifters); break;
                        case 19: AssignCustomRolesFromList(CustomRoles.Lighter, Crewmates); break;
                        case 20: AssignCustomRolesFromList(CustomRoles.FireWorks, Shapeshifters); break;
                        case 21: AssignCustomRolesFromList(CustomRoles.SpeedBooster, Crewmates); break;
                        case 22: AssignCustomRolesFromList(CustomRoles.Trapper, Crewmates); break;
                        case 23: AssignCustomRolesFromList(CustomRoles.Dictator, Crewmates); break;
                        case 24: AssignCustomRolesFromList(CustomRoles.SchrodingerCat, Crewmates); break;
                        case 25: AssignCustomRolesFromList(CustomRoles.Watcher, Options.IsEvilWatcher ? Impostors : Crewmates); break;
                        case 26: if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1) AssignCustomRolesFromList(CustomRoles.Egoist, Shapeshifters); break;
                        case 27: AssignCustomRolesFromList(CustomRoles.Mare, Impostors); break;
                        case 28: AssignCustomRolesFromList(CustomRoles.Doctor, Scientists); break;
                        case 29: AssignCustomRolesFromList(CustomRoles.Puppeteer, Impostors); break;
                        case 30: AssignCustomRolesFromList(CustomRoles.TimeThief, Impostors); break;
                        case 31: AssignCustomRolesFromList(CustomRoles.EvilTracker, Shapeshifters); break;
                        case 32: AssignCustomRolesFromList(CustomRoles.Seer, Crewmates); break;
                        case 33: AssignCustomRolesFromList(CustomRoles.Paranoia, Engineers); break;
                        case 34: AssignCustomRolesFromList(CustomRoles.Miner, Shapeshifters); break;
                        case 35: AssignCustomRolesFromList(CustomRoles.Psychic, Crewmates); break;
                        case 36: AssignCustomRolesFromList(CustomRoles.Plumber, Engineers); break;
                        case 37: AssignCustomRolesFromList(CustomRoles.Needy, Crewmates); break;
                        case 38: AssignCustomRolesFromList(CustomRoles.SuperStar, Crewmates); break;
                        case 39: AssignCustomRolesFromList(CustomRoles.Hacker, Impostors); break;
                        case 40: AssignCustomRolesFromList(CustomRoles.Assassin, Shapeshifters); break;
                        case 41: AssignCustomRolesFromList(CustomRoles.Luckey, Crewmates); break;
                        case 42: AssignCustomRolesFromList(CustomRoles.CyberStar, Crewmates); break;
                        case 43: AssignCustomRolesFromList(CustomRoles.Escapee, Shapeshifters); break;
                        case 44: AssignCustomRolesFromList(CustomRoles.NiceGuesser, Crewmates); break;
                        case 45: AssignCustomRolesFromList(CustomRoles.EvilGuesser, Impostors); break;
                        case 46: AssignCustomRolesFromList(CustomRoles.Detective, Crewmates); break;
                        case 47: AssignCustomRolesFromList(CustomRoles.Minimalism, Impostors); break;
                        case 48: AssignCustomRolesFromList(CustomRoles.God, Crewmates); break;
                        case 49: AssignCustomRolesFromList(CustomRoles.Zombie, Impostors); break;
                        case 50: AssignCustomRolesFromList(CustomRoles.Mario, Engineers); break;
                        case 51: AssignCustomRolesFromList(CustomRoles.AntiAdminer, Impostors); Main.existAntiAdminer = true; break;
                        case 52: AssignCustomRolesFromList(CustomRoles.Sans, Impostors); break;
                        case 53: AssignCustomRolesFromList(CustomRoles.Bomber, Shapeshifters); break;
                        case 54: AssignCustomRolesFromList(CustomRoles.BoobyTrap, Impostors); break;
                        case 55: AssignNtrRoles(); break;
                    }
                }

                //RPCによる同期
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Watcher))
                        Main.PlayerStates[pc.PlayerId].MainRole = Options.IsEvilWatcher ? CustomRoles.EvilWatcher : CustomRoles.NiceWatcher;
                }
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
                        case CustomRoles.Egoist:
                            Egoist.Add(pc.PlayerId);
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

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Instance.RpcAdd(seer.PlayerId, pc.PlayerId, $"{pc.GetRoleColorCode()}");
                        }
                    }
                }

                //役職の人数を戻す
                var roleOpt = Main.NormalOptions.roleOptions;
                int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
                ScientistNum -= CustomRoles.Doctor.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);

                EngineerNum -= CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount() + CustomRoles.Paranoia.GetCount() + CustomRoles.Plumber.GetCount() + CustomRoles.Mario.GetCount();

                if (Options.MayorHasPortableButton.GetBool())
                    EngineerNum -= CustomRoles.Mayor.GetCount();

                if (Options.MadSnitchCanVent.GetBool())
                    EngineerNum -= CustomRoles.MadSnitch.GetCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.Assassin.GetCount() + CustomRoles.Miner.GetCount() + CustomRoles.Escapee.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount() + CustomRoles.Bomber.GetCount();
                if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));
                GameEndChecker.SetPredicateToNormal();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }
            }

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist).Select(p => p.PlayerId));
            /*
            //インポスターのゴーストロールがクルーメイトになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */
            Utils.CountAliveImpostors();
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsEnable()) return;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;

            for (var i = 0; i < role.GetCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;

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

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;
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
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.GM) || player.Is(CustomRoles.Ntr) || player.Is(CustomRoles.Needy)) continue;
                allPlayers.Add(player);
            }
            var loversRole = CustomRoles.Lovers;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                Main.LoversPlayers.Add(player);
                allPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers();
        }
        private static void AssignNtrRoles()
        {
            var allPlayers = new List<PlayerControl>();
            foreach (var ntrPc in Main.AllPlayerControls)
            {
                if (ntrPc.Is(CustomRoles.GM) || ntrPc.Is(CustomRoles.Lovers) || ntrPc.Is(CustomRoles.Needy)) continue;
                allPlayers.Add(ntrPc);
            }
            var ntrRole = CustomRoles.Ntr;
            var rd = Utils.RandomSeedByGuid();
            var player = allPlayers[rd.Next(0, allPlayers.Count)];
            Main.PlayerStates[player.PlayerId].SetSubRole(ntrRole);
            Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + ntrRole.ToString(), "AssignLovers");
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