using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            PlayerState.Init();

            Main.CustomWinTrigger = false;
            Main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            Main.AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();
            Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            Main.isCurseAndKill = new Dictionary<byte, bool>();
            Main.AirshipMeetingTimer = new Dictionary<byte, float>();
            Main.SKMadmateNowCount = 0;
            Main.isCursed = false;
            Main.PuppeteerList = new Dictionary<byte, byte>();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();

            Main.SpelledPlayer = new List<PlayerControl>();
            Main.witchMeeting = false;
            Main.CheckShapeshift = new Dictionary<byte, bool>();
            Main.SpeedBoostTarget = new Dictionary<byte, byte>();
            Main.MayorUsedButtonCount = new Dictionary<byte, int>();
            Main.targetArrows = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            Main.DiscussionTime = Main.RealOptionsData.DiscussionTime;
            Main.VotingTime = Main.RealOptionsData.VotingTime;
            Main.DefaultCrewmateVision = Main.RealOptionsData.CrewLightMod;
            Main.DefaultImpostorVision = Main.RealOptionsData.ImpostorLightMod;

            NameColorManager.Instance.RpcReset();
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
                if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(pc.Data.DefaultOutfit.ColorId));
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.PlayerSpeedMod; //移動速度をデフォルトの移動速度に変更
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
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
            Egoist.Init();
            Executioner.Init();
            Sheriff.Init();
            EvilTracker.Init();
            EvilHacker.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();

            GameStates.MeetingCalled = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            CustomRpcSender sender = CustomRpcSender.Create("SelectRoles Sender", SendOption.Reliable);
            RpcSetRoleReplacer.StartReplace(sender);

            //ウォッチャーの陣営抽選
            Options.SetWatcherTeam(Options.EvilWatcherChance.GetFloat());

            var rand = new System.Random();
            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                //役職の人数を指定
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                int AdditionalScientistNum = CustomRoles.Doctor.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + AdditionalScientistNum, AdditionalScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                int AdditionalEngineerNum = CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount();// - EngineerNum;

                if (Options.MayorHasPortableButton.GetBool())
                    AdditionalEngineerNum += CustomRoles.Mayor.GetCount();

                if (Options.MadSnitchCanVent.GetBool())
                    AdditionalEngineerNum += CustomRoles.MadSnitch.GetCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount()/* + CustomRoles.ShapeMaster.GetCount()*/ + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount();//- ShapeshifterNum;
                if (Main.RealOptionsData.NumImpostors > 1)
                    AdditionalShapeshifterNum += CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));


                List<PlayerControl> AllPlayers = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
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

                AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, sender, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, sender, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Jackal, AllPlayers, sender, BaseRole: RoleTypes.Impostor);
            }
            if (sender.CurrentState == CustomRpcSender.State.InRootMessage) sender.EndMessage();
            //以下、バニラ側の役職割り当てが入る
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.sender.SendMessage();

            //Utils.ApplySuffix();

            var rand = new System.Random();
            Main.KillOrSpell = new Dictionary<byte, bool>();

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (Main.AllPlayerCustomRoles.ContainsKey(pc.PlayerId)) continue; //既にカスタム役職が割り当てられていればスキップ
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Crewmate);
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Impostor);
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Scientist);
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Engineer);
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.GuardianAngel);
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        Main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Shapeshifter);
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(RoleType.Impostor))
                        pc.RpcSetColor(0);
                    else if (pc.Is(RoleType.Crewmate))
                        pc.RpcSetColor(1);
                }

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
                foreach (var pair in Main.AllPlayerCustomRoles)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;
            }
            else
            {

                AssignCustomRolesFromList(CustomRoles.FireWorks, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Sniper, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Jester, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Madmate, Engineers);
                AssignCustomRolesFromList(CustomRoles.Bait, Crewmates);
                AssignCustomRolesFromList(CustomRoles.MadGuardian, Crewmates);
                AssignCustomRolesFromList(CustomRoles.MadSnitch, Options.MadSnitchCanVent.GetBool() ? Engineers : Crewmates);
                AssignCustomRolesFromList(CustomRoles.Mayor, Options.MayorHasPortableButton.GetBool() ? Engineers : Crewmates);
                AssignCustomRolesFromList(CustomRoles.Opportunist, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Snitch, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SabotageMaster, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Mafia, Impostors);
                AssignCustomRolesFromList(CustomRoles.Terrorist, Engineers);
                AssignCustomRolesFromList(CustomRoles.Executioner, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Vampire, Impostors);
                AssignCustomRolesFromList(CustomRoles.BountyHunter, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Witch, Impostors);
                //AssignCustomRolesFromList(CustomRoles.ShapeMaster, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Warlock, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.SerialKiller, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Lighter, Crewmates);
                AssignLoversRolesFromList();
                AssignCustomRolesFromList(CustomRoles.SpeedBooster, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Trapper, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Dictator, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SchrodingerCat, Crewmates);
                if (Options.IsEvilWatcher) AssignCustomRolesFromList(CustomRoles.Watcher, Impostors);
                else AssignCustomRolesFromList(CustomRoles.Watcher, Crewmates);
                if (Main.RealOptionsData.NumImpostors > 1)
                    AssignCustomRolesFromList(CustomRoles.Egoist, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Mare, Impostors);
                AssignCustomRolesFromList(CustomRoles.Doctor, Scientists);
                AssignCustomRolesFromList(CustomRoles.Puppeteer, Impostors);
                AssignCustomRolesFromList(CustomRoles.TimeThief, Impostors);
                AssignCustomRolesFromList(CustomRoles.EvilTracker, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Seer, Crewmates);
                AssignCustomRolesFromList(CustomRoles.EvilHacker, Impostors);

                //RPCによる同期
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Watcher) && Options.IsEvilWatcher)
                        Main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.EvilWatcher;
                    if (pc.Is(CustomRoles.Watcher) && !Options.IsEvilWatcher)
                        Main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.NiceWatcher;
                }
                foreach (var pair in Main.AllPlayerCustomRoles)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }
                foreach (var pair in Main.AllPlayerCustomSubRoles)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }

                HudManager.Instance.SetHudActive(true);
                Main.KillOrSpell = new Dictionary<byte, bool>();
                foreach (var pc in PlayerControl.AllPlayerControls)
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
                            Main.KillOrSpell.Add(pc.PlayerId, false);
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

                        case CustomRoles.Arsonist:
                            foreach (var ar in PlayerControl.AllPlayerControls)
                                Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                            break;
                        case CustomRoles.Executioner:
                            Executioner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Egoist:
                            Egoist.Add(pc.PlayerId);
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
                    }
                    pc.ResetKillCooldown();

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in PlayerControl.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Instance.RpcAdd(seer.PlayerId, pc.PlayerId, $"{pc.GetRoleColorCode()}");
                        }
                    }
                }

                //役職の人数を戻す
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                ScientistNum -= CustomRoles.Doctor.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                EngineerNum -= CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount();

                if (Options.MayorHasPortableButton.GetBool())
                    EngineerNum -= CustomRoles.Mayor.GetCount();

                if (Options.MadSnitchCanVent.GetBool())
                    EngineerNum -= CustomRoles.MadSnitch.GetCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount()/* + CustomRoles.ShapeMaster.GetCount()*/ + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount();
                if (Main.RealOptionsData.NumImpostors > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));
            }

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(PlayerControl.AllPlayerControls.ToArray().Where(p => p.GetCustomRole() is CustomRoles.Arsonist or CustomRoles.Jackal).Select(p => p.PlayerId));
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, CustomRpcSender sender, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsEnable()) return;

            for (var i = 0; i < role.GetCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var rand = new Random();
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                Main.AllPlayerCustomRoles[player.PlayerId] = role;
                //ここからDesyncが始まる
                if (player.PlayerId != 0)
                {
                    int playerCID = player.GetClientId();
                    sender.RpcSetRole(player, BaseRole, playerCID);
                    //Desyncする人視点で他プレイヤーを科学者にするループ
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == player) continue;
                        sender.RpcSetRole(pc, RoleTypes.Scientist, playerCID);
                    }
                    //他視点でDesyncする人の役職を科学者にする
                    player.SetRole(RoleTypes.Scientist); //ホスト視点用
                    sender.RpcSetRole(player, RoleTypes.Scientist);
                }
                else
                {
                    //ホストは別の役職にする
                    player.SetRole(hostBaseRole); //ホスト視点用
                    sender.RpcSetRole(player, hostBaseRole);
                }
                player.Data.IsDead = true;
            }
        }
        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = new System.Random();
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
                Main.AllPlayerCustomRoles[player.PlayerId] = role;
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
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Is(CustomRoles.GM)) continue;
                allPlayers.Add(player);
            }
            var loversRole = CustomRoles.Lovers;
            var rand = new System.Random();
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                Main.LoversPlayers.Add(player);
                allPlayers.Remove(player);
                Main.AllPlayerCustomSubRoles[player.PlayerId] = loversRole;
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
        {
            public static bool doReplace = false;
            public static CustomRpcSender sender;
            public static List<(PlayerControl, RoleTypes)> StoragedData = new();
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
            {
                if (doReplace && sender != null)
                {
                    StoragedData.Add((__instance, roleType));
                    return false;
                }
                else return true;
            }
            public static void Release()
            {
                sender.StartMessage(-1);
                foreach (var pair in StoragedData)
                {
                    pair.Item1.SetRole(pair.Item2);
                    sender.StartRpc(pair.Item1.NetId, RpcCalls.SetRole)
                        .Write((ushort)pair.Item2)
                        .EndRpc();
                }
                sender.EndMessage();
                doReplace = false;
            }
            public static void StartReplace(CustomRpcSender sender)
            {
                RpcSetRoleReplacer.sender = sender;
                StoragedData = new();
                doReplace = true;
            }
        }
    }
}