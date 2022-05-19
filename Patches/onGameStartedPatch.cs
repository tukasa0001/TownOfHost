using System;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            PlayerState.Init();

            Main.currentWinner = CustomWinner.Default;
            Main.CustomWinTrigger = false;
            Main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            Main.AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.SerialKillerTimer = new Dictionary<byte, float>();
            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.BountyTimer = new Dictionary<byte, float>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();
            Main.DousedPlayerCount = new Dictionary<byte, (int, int)>();
            Main.isDeadDoused = new Dictionary<byte, bool>();
            Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            Main.BountyTargets = new Dictionary<byte, PlayerControl>();
            Main.isTargetKilled = new Dictionary<byte, bool>();
            Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            Main.isCurseAndKill = new Dictionary<byte, bool>();
            Main.AirshipMeetingTimer = new Dictionary<byte, float>();
            Main.AirshipMeetingCheck = false;
            Main.ExecutionerTarget = new Dictionary<byte, byte>();
            Main.SKMadmateNowCount = 0;
            Main.isCursed = false;
            Main.PuppeteerList = new Dictionary<byte, byte>();

            Main.IgnoreReportPlayers = new List<byte>();

            Main.SheriffShotLimit = new Dictionary<byte, float>();
            Main.TimeThiefKillCount = new Dictionary<byte, int>();

            Main.SpelledPlayer = new List<PlayerControl>();
            Main.witchMeeting = false;
            Main.CheckShapeshift = new Dictionary<byte, bool>();
            Main.SpeedBoostTarget = new Dictionary<byte, byte>();
            Main.MayorUsedButtonCount = new Dictionary<byte, int>();
            Main.targetArrows = new();

            Options.UsedButtonCount = 0;
            Options.SabotageMasterUsedSkillCount = 0;
            Main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            Main.BlockKilling = new Dictionary<byte, bool>();
            Main.SelfGuard = new();

            Main.introDestroyed = false;

            Main.DiscussionTime = Main.RealOptionsData.DiscussionTime;
            Main.VotingTime = Main.RealOptionsData.VotingTime;

            NameColorManager.Instance.RpcReset();
            Main.LastNotifyNames = new();
            //名前の記録
            Main.AllPlayerNames = new();
            foreach (var p in PlayerControl.AllPlayerControls)
                Main.AllPlayerNames[p.PlayerId] = p?.Data?.PlayerName;

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
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.PlayerSpeedMod; //移動速度をデフォルトの移動速度に変更
                pc.nameText.text = pc.name;
                Main.SelfGuard[pc.PlayerId] = false;
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                    Options.HideAndSeekImpVisionMin = PlayerControl.GameOptions.ImpostorLightMod;
                }
            }
            FireWorks.Init();
            Sniper.Init();
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix(RoleManager __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            //ウォッチャーの陣営抽選
            Options.SetWatcherTeam();

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
                int AdditionalShapeshifterNum = CustomRoles.Mafia.GetCount() + CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.ShapeMaster.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount();//- ShapeshifterNum;
                if (Main.RealOptionsData.NumImpostors > 1)
                    AdditionalShapeshifterNum += CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));


                List<PlayerControl> AllPlayers = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (CustomRoles.Sheriff.IsEnable())
                {
                    for (var i = 0; i < CustomRoles.Sheriff.GetCount(); i++)
                    {
                        if (AllPlayers.Count <= 0) break;
                        var sheriff = AllPlayers[rand.Next(0, AllPlayers.Count)];
                        AllPlayers.Remove(sheriff);
                        Main.AllPlayerCustomRoles[sheriff.PlayerId] = CustomRoles.Sheriff;
                        //ここからDesyncが始まる
                        if (sheriff.PlayerId != 0)
                        {
                            //ただしホスト、お前はDesyncするな。
                            sheriff.RpcSetRoleDesync(RoleTypes.Impostor);
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc == sheriff) continue;
                                sheriff.RpcSetRoleDesync(RoleTypes.Scientist, pc);
                                pc.RpcSetRoleDesync(RoleTypes.Scientist, sheriff);
                            }
                        }
                        else
                        {
                            //ホストは代わりに普通のクルーにする
                            sheriff.RpcSetRole(RoleTypes.Crewmate);
                        }
                        sheriff.Data.IsDead = true;
                    }
                }
                if (CustomRoles.Arsonist.IsEnable())
                {
                    for (var i = 0; i < CustomRoles.Arsonist.GetCount(); i++)
                    {
                        if (AllPlayers.Count <= 0) break;
                        var arsonist = AllPlayers[rand.Next(0, AllPlayers.Count)];
                        AllPlayers.Remove(arsonist);
                        Main.AllPlayerCustomRoles[arsonist.PlayerId] = CustomRoles.Arsonist;
                        //ここからDesyncが始まる
                        if (arsonist.PlayerId != 0)
                        {
                            //ただしホスト、お前はDesyncするな。
                            arsonist.RpcSetRoleDesync(RoleTypes.Impostor);
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc == arsonist) continue;
                                arsonist.RpcSetRoleDesync(RoleTypes.Scientist, pc);
                                pc.RpcSetRoleDesync(RoleTypes.Scientist, arsonist);
                            }
                        }
                        else
                        {
                            //ホストは代わりに普通のクルーにする
                            arsonist.RpcSetRole(RoleTypes.Crewmate);
                        }
                        arsonist.Data.IsDead = true;
                    }
                }
            }
        }
        public static void Postfix(RoleManager __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
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
                        Logger.SendInGame("エラー:役職設定中に無効な役職のプレイヤーを発見しました(" + pc?.Data?.PlayerName + ")");
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
                AssignCustomRolesFromList(CustomRoles.Mafia, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Terrorist, Engineers);
                AssignCustomRolesFromList(CustomRoles.Executioner, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Vampire, Impostors);
                AssignCustomRolesFromList(CustomRoles.BountyHunter, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Witch, Impostors);
                AssignCustomRolesFromList(CustomRoles.ShapeMaster, Shapeshifters);
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
                //BountyHunterのターゲットを初期化
                Main.BountyTargets = new Dictionary<byte, PlayerControl>();
                Main.BountyTimer = new Dictionary<byte, float>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    Main.isDeadDoused[pc.PlayerId] = false;
                    pc.ResetKillCooldown();
                    if (pc.Is(CustomRoles.Sheriff))
                    {
                        Main.SheriffShotLimit[pc.PlayerId] = Options.SheriffShotLimit.GetFloat();
                        pc.RpcSetSheriffShotLimit();
                        Logger.Info($"{pc.GetNameWithRole()} : 残り{Main.SheriffShotLimit[pc.PlayerId]}発", "Sheriff");
                    }
                    if (pc.Is(CustomRoles.BountyHunter))
                    {
                        pc.ResetBountyTarget();
                        Main.isTargetKilled.Add(pc.PlayerId, false);
                        Main.BountyTimer.Add(pc.PlayerId, 0f); //BountyTimerにBountyHunterのデータを入力
                    }
                    if (pc.Is(CustomRoles.Witch)) Main.KillOrSpell.Add(pc.PlayerId, false);
                    if (pc.Is(CustomRoles.Warlock))
                    {
                        Main.CursedPlayers.Add(pc.PlayerId, null);
                        Main.isCurseAndKill.Add(pc.PlayerId, false);
                    }
                    if (pc.Is(CustomRoles.FireWorks)) FireWorks.Add(pc.PlayerId);
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                    if (pc.Is(CustomRoles.Arsonist))
                    {
                        var targetPlayerCount = PlayerControl.AllPlayerControls.Count - 1;
                        Main.DousedPlayerCount[pc.PlayerId] = (0, targetPlayerCount);
                        pc.RpcSendDousedPlayerCount();
                        foreach (var ar in PlayerControl.AllPlayerControls)
                        {
                            Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        }
                    }
                    if (pc.Is(CustomRoles.TimeThief))
                    {
                        Main.TimeThiefKillCount[pc.PlayerId] = 0;
                        pc.RpcSetTimeThiefKillCount();
                    }
                    //通常モードでかくれんぼをする人用
                    if (Options.StandardHAS.GetBool())
                    {
                        foreach (var seer in PlayerControl.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.Egoist)) //変更対象がインポスター陣営orエゴイスト
                                NameColorManager.Instance.RpcAdd(seer.PlayerId, pc.PlayerId, $"{pc.GetRoleColorCode()}");
                        }
                    }
                    if (pc.Is(CustomRoles.Sniper)) Sniper.Add(pc.PlayerId);
                    if (pc.Is(CustomRoles.Executioner))
                    {
                        List<PlayerControl> targetList = new();
                        rand = new System.Random();
                        foreach (var target in PlayerControl.AllPlayerControls)
                        {
                            if (pc == target) continue;
                            else if (!Options.ExecutionerCanTargetImpostor.GetBool() && target.GetCustomRole().IsImpostor()) continue;

                            targetList.Add(target);
                        }
                        var Target = targetList[rand.Next(targetList.Count)];
                        Main.ExecutionerTarget.Add(pc.PlayerId, Target.PlayerId);
                        RPC.SendExecutionerTarget(pc.PlayerId, Target.PlayerId);
                        Logger.Info($"{pc.GetNameWithRole()}:{Target.GetNameWithRole()}", "Executioner");
                    }
                    if (pc.Is(CustomRoles.Mayor))
                        Main.MayorUsedButtonCount[pc.PlayerId] = 0;
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
                ShapeshifterNum -= CustomRoles.Mafia.GetCount() + CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.ShapeMaster.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount();
                if (Main.RealOptionsData.NumImpostors > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));
            }

            //サーバーの役職判定をだます
            new LateTask(() =>
            {
                if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        pc.RpcSetRole(RoleTypes.Shapeshifter);
                    }
            }, 3f, "SetImpostorForServer");
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
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
            foreach (var player in PlayerControl.AllPlayerControls) allPlayers.Add(player);
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
    }
}