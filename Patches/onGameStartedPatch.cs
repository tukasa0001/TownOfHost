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
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            Main.PlayerStates = new();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();

            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();
            Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
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

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            NameColorManager.Instance.RpcReset();
            Main.LastNotifyNames = new();

            Main.currentDousingTarget = 255;
            Main.PlayerColors = new();
            //名前の記録
            Main.AllPlayerNames = new();

            Camouflage.Init();

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
            EvilTracker.Init();
            EvilHacker.Init();
            Snitch.Init();
            SchrodingerCat.Init();
            Vampire.Init();
            TimeManager.Init();
            LastImpostor.Init();
            TargetArrow.Init();
            DoubleTrigger.Init();
            Workhorse.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

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
                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    int additionalNum = GetAdditionalRoleTypesCount(roleTypes);
                    roleOpt.SetRoleRate(roleTypes, roleOpt.GetNumPerGame(roleTypes) + additionalNum, additionalNum > 0 ? 100 : roleOpt.GetChancePerGame(roleTypes));
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
                AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Jackal, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
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
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x < CustomRoles.NotAssigned))
                {
                    if (role.IsVanilla()) continue;
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role is CustomRoles.Sheriff or CustomRoles.Arsonist or CustomRoles.Jackal) continue;
                    if (role == CustomRoles.Egoist && Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
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
                AssignLoversRoles();

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
                        case CustomRoles.Vampire:
                            Vampire.Add(pc.PlayerId);
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
                        case CustomRoles.SabotageMaster:
                            SabotageMaster.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilTracker:
                            EvilTracker.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilHacker:
                            EvilHacker.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Snitch:
                            Snitch.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SchrodingerCat:
                            SchrodingerCat.Add(pc.PlayerId);
                            break;
                        case CustomRoles.TimeManager:
                            TimeManager.Add(pc.PlayerId);
                            break;
                    }
                    foreach (var subRole in pc.GetCustomSubRoles())
                    {
                        switch (subRole)
                        {
                            // ここに属性のAddを追加
                            default:
                                break;
                        }
                    }
                    HudManager.Instance.SetHudActive(true);
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

                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    roleOpt.SetRoleRate(roleTypes, roleOpt.GetNumPerGame(roleTypes) - GetAdditionalRoleTypesCount(roleTypes), roleOpt.GetChancePerGame(roleTypes));
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

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist).Select(p => p.PlayerId));
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

        private static void AssignCustomSubRolesFromList(CustomRoles role, int RawCount = -1)
        {
            if (!role.IsEnable()) return;
            var allPlayers = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
                if (IsAssignTarget(pc, role))
                    allPlayers.Add(pc);

            if (RawCount == -1) RawCount = role.GetCount();
            int count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (count <= 0) return;

            var rand = IRandom.Instance;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(allPlayers.Count)];
                allPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
            }
        }
        //属性ごとの割り当て条件
        private static bool IsAssignTarget(PlayerControl player, CustomRoles subrole)
        {
            if (player.Is(CustomRoles.GM)) return false;
            return subrole switch
            {
                _ => true,
            };
        }
        private static void AssignLoversRoles(int RawCount = -1)
        {
            if (!CustomRoles.Lovers.IsEnable()) return;
            //Loversを初期化
            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            var allPlayers = new List<PlayerControl>();
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.GM)) continue;
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
        public static int GetAdditionalRoleTypesCount(RoleTypes roleTypes)
        {
            int count = 0;
            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x < CustomRoles.NotAssigned))
            {
                if (role.IsVanilla()) continue;
                if (role is CustomRoles.Sheriff or CustomRoles.Arsonist or CustomRoles.Jackal) continue;
                if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
                if (role.GetRoleTypes() == roleTypes)
                    count += role.GetCount();
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