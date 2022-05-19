using System;
using HarmonyLib;
using System.Collections.Generic;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class changeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            PlayerState.Init();

            main.currentWinner = CustomWinner.Default;
            main.CustomWinTrigger = false;
            main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            main.AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            main.AllPlayerSpeed = new Dictionary<byte, float>();
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.SerialKillerTimer = new Dictionary<byte, float>();
            main.WarlockTimer = new Dictionary<byte, float>();
            main.BountyTimer = new Dictionary<byte, float>();
            main.isDoused = new Dictionary<(byte, byte), bool>();
            main.DousedPlayerCount = new Dictionary<byte, (int, int)>();
            main.isDeadDoused = new Dictionary<byte, bool>();
            main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            main.BountyTargets = new Dictionary<byte, PlayerControl>();
            main.isTargetKilled = new Dictionary<byte, bool>();
            main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            main.isCurseAndKill = new Dictionary<byte, bool>();
            main.AirshipMeetingTimer = new Dictionary<byte, float>();
            main.AirshipMeetingCheck = false;
            main.ExecutionerTarget = new Dictionary<byte, byte>();
            main.SKMadmateNowCount = 0;
            main.isCursed = false;
            main.PuppeteerList = new Dictionary<byte, byte>();

            main.IgnoreReportPlayers = new List<byte>();

            main.SheriffShotLimit = new Dictionary<byte, float>();
            main.TimeThiefKillCount = new Dictionary<byte, int>();

            main.SpelledPlayer = new List<PlayerControl>();
            main.witchMeeting = false;
            main.CheckShapeshift = new Dictionary<byte, bool>();
            main.SpeedBoostTarget = new Dictionary<byte, byte>();
            main.MayorUsedButtonCount = new Dictionary<byte, int>();
            main.targetArrows = new();

            Options.UsedButtonCount = 0;
            Options.SabotageMasterUsedSkillCount = 0;
            main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            main.RealNames = new Dictionary<byte, string>();
            main.BlockKilling = new Dictionary<byte, bool>();
            main.LastKiller = new();

            main.introDestroyed = false;

            main.DiscussionTime = main.RealOptionsData.DiscussionTime;
            main.VotingTime = main.RealOptionsData.VotingTime;

            NameColorManager.Instance.RpcReset();
            main.LastNotifyNames = new();
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                foreach (var seer in PlayerControl.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                main.AllPlayerSpeed[pc.PlayerId] = main.RealOptionsData.PlayerSpeedMod; //移動速度をデフォルトの移動速度に変更
                main.RealNames[pc.PlayerId] = pc.name;
                pc.nameText.text = pc.name;
            }
            main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                main.RefixCooldownDelay = 0;
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
            Options.SetWatcherTeam(Options.EvilWatcherChance.GetFloat());

            var rand = new System.Random();
            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                //役職の人数を指定
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                int AdditionalScientistNum = CustomRoles.Doctor.getCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + AdditionalScientistNum, AdditionalScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                int AdditionalEngineerNum = CustomRoles.Madmate.getCount() + CustomRoles.Terrorist.getCount();// - EngineerNum;

                if (Options.MayorHasPortableButton.GetBool())
                    AdditionalEngineerNum += CustomRoles.Mayor.getCount();

                if (Options.MadSnitchCanVent.GetBool())
                    AdditionalEngineerNum += CustomRoles.MadSnitch.getCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = CustomRoles.Mafia.getCount() + CustomRoles.SerialKiller.getCount() + CustomRoles.BountyHunter.getCount() + CustomRoles.Warlock.getCount() + CustomRoles.ShapeMaster.getCount() + CustomRoles.FireWorks.getCount() + CustomRoles.Sniper.getCount();//- ShapeshifterNum;
                if (main.RealOptionsData.NumImpostors > 1)
                    AdditionalShapeshifterNum += CustomRoles.Egoist.getCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));


                List<PlayerControl> AllPlayers = new List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (CustomRoles.Sheriff.isEnable())
                {
                    for (var i = 0; i < CustomRoles.Sheriff.getCount(); i++)
                    {
                        if (AllPlayers.Count <= 0) break;
                        var sheriff = AllPlayers[rand.Next(0, AllPlayers.Count)];
                        AllPlayers.Remove(sheriff);
                        main.AllPlayerCustomRoles[sheriff.PlayerId] = CustomRoles.Sheriff;
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
                if (CustomRoles.Arsonist.isEnable())
                {
                    for (var i = 0; i < CustomRoles.Arsonist.getCount(); i++)
                    {
                        if (AllPlayers.Count <= 0) break;
                        var arsonist = AllPlayers[rand.Next(0, AllPlayers.Count)];
                        AllPlayers.Remove(arsonist);
                        main.AllPlayerCustomRoles[arsonist.PlayerId] = CustomRoles.Arsonist;
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
            Logger.msg("SelectRolesPatch.Prefix.End");
        }
        public static void Postfix(RoleManager __instance)
        {
            Logger.msg("SelectRolesPatch.Postfix.Start");
            if (!AmongUsClient.Instance.AmHost) return;
            //Utils.ApplySuffix();

            var rand = new System.Random();
            main.KillOrSpell = new Dictionary<byte, bool>();

            List<PlayerControl> Crewmates = new List<PlayerControl>();
            List<PlayerControl> Impostors = new List<PlayerControl>();
            List<PlayerControl> Scientists = new List<PlayerControl>();
            List<PlayerControl> Engineers = new List<PlayerControl>();
            List<PlayerControl> GuardianAngels = new List<PlayerControl>();
            List<PlayerControl> Shapeshifters = new List<PlayerControl>();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (main.AllPlayerCustomRoles.ContainsKey(pc.PlayerId)) continue; //既にカスタム役職が割り当てられていればスキップ
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Crewmate);
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Impostor);
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Scientist);
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Engineer);
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.GuardianAngel);
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Shapeshifter);
                        break;
                    default:
                        Logger.SendInGame("エラー:役職設定中に無効な役職のプレイヤーを発見しました(" + pc.Data.PlayerName + ")");
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
                foreach (var pair in main.AllPlayerCustomRoles)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;

                //名前の記録
                main.AllPlayerNames = new();
                foreach (var pair in main.AllPlayerCustomRoles)
                {
                    main.AllPlayerNames.Add(pair.Key, main.RealNames[pair.Key]);
                }
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
                if (main.RealOptionsData.NumImpostors > 1)
                    AssignCustomRolesFromList(CustomRoles.Egoist, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Mare, Impostors);
                AssignCustomRolesFromList(CustomRoles.Doctor, Scientists);
                AssignCustomRolesFromList(CustomRoles.Puppeteer, Impostors);
                AssignCustomRolesFromList(CustomRoles.TimeThief, Impostors);
                AssignCustomRolesFromList(CustomRoles.GBomber, Impostors);

                //RPCによる同期
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Watcher) && Options.IsEvilWatcher)
                        main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.EvilWatcher;
                    if (pc.Is(CustomRoles.Watcher) && !Options.IsEvilWatcher)
                        main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.NiceWatcher;
                }
                foreach (var pair in main.AllPlayerCustomRoles)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }
                foreach (var pair in main.AllPlayerCustomSubRoles)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }

                //名前の記録
                main.AllPlayerNames = new();
                foreach (var pair in main.AllPlayerCustomRoles)
                {
                    main.AllPlayerNames.Add(pair.Key, main.RealNames[pair.Key]);
                }

                HudManager.Instance.SetHudActive(true);
                main.KillOrSpell = new Dictionary<byte, bool>();
                //BountyHunterのターゲットを初期化
                main.BountyTargets = new Dictionary<byte, PlayerControl>();
                main.BountyTimer = new Dictionary<byte, float>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    main.isDeadDoused[pc.PlayerId] = false;
                    if (pc.Is(CustomRoles.Sheriff))
                    {
                        main.SheriffShotLimit[pc.PlayerId] = Options.SheriffShotLimit.GetFloat();
                        pc.RpcSetSheriffShotLimit();
                        Logger.info($"{pc.getNameWithRole()} : 残り{main.SheriffShotLimit[pc.PlayerId]}発");
                    }
                    if (pc.Is(CustomRoles.BountyHunter))
                    {
                        pc.ResetBountyTarget();
                        main.isTargetKilled.Add(pc.PlayerId, false);
                        main.BountyTimer.Add(pc.PlayerId, 0f); //BountyTimerにBountyHunterのデータを入力
                    }
                    if (pc.Is(CustomRoles.Witch)) main.KillOrSpell.Add(pc.PlayerId, false);
                    if (pc.Is(CustomRoles.Warlock))
                    {
                        main.CursedPlayers.Add(pc.PlayerId, null);
                        main.isCurseAndKill.Add(pc.PlayerId, false);
                    }
                    if (pc.Is(CustomRoles.FireWorks)) FireWorks.Add(pc.PlayerId);
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) main.CheckShapeshift.Add(pc.PlayerId, false);
                    if (pc.Is(CustomRoles.Arsonist))
                    {
                        var targetPlayerCount = (PlayerControl.AllPlayerControls.Count - 1);
                        main.DousedPlayerCount[pc.PlayerId] = (0, targetPlayerCount);
                        pc.RpcSendDousedPlayerCount();
                        foreach (var ar in PlayerControl.AllPlayerControls)
                        {
                            main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        }
                    }
                    if (pc.Is(CustomRoles.TimeThief))
                    {
                        main.TimeThiefKillCount[pc.PlayerId] = 0;
                        pc.RpcSetTimeThiefKillCount();
                    }
                    //通常モードでかくれんぼをする人用
                    if (Options.StandardHAS.GetBool())
                    {
                        foreach (var seer in PlayerControl.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.getCustomRole().isImpostor() || pc.Is(CustomRoles.Egoist)) //変更対象がインポスター陣営orエゴイスト
                                NameColorManager.Instance.RpcAdd(seer.PlayerId, pc.PlayerId, $"{pc.getRoleColorCode()}");
                        }
                    }
                    if (pc.Is(CustomRoles.Sniper)) Sniper.Add(pc.PlayerId);
                    if (pc.Is(CustomRoles.Executioner))
                    {
                        List<PlayerControl> targetList = new List<PlayerControl>();
                        rand = new System.Random();
                        foreach (var target in PlayerControl.AllPlayerControls)
                        {
                            if (pc == target) continue;
                            else if (!Options.ExecutionerCanTargetImpostor.GetBool() && target.getCustomRole().isImpostor()) continue;

                            targetList.Add(target);
                        }
                        var Target = targetList[rand.Next(targetList.Count)];
                        main.ExecutionerTarget.Add(pc.PlayerId, Target.PlayerId);
                        RPC.SendExecutionerTarget(pc.PlayerId, Target.PlayerId);
                        Logger.info($"{pc.getNameWithRole()}:{Target.getNameWithRole()}", "Executioner");
                    }
                    if (pc.Is(CustomRoles.Mayor))
                        main.MayorUsedButtonCount[pc.PlayerId] = 0;
                }

                //役職の人数を戻す
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                ScientistNum -= CustomRoles.Doctor.getCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                EngineerNum -= CustomRoles.Madmate.getCount() + CustomRoles.Terrorist.getCount();

                if (Options.MayorHasPortableButton.GetBool())
                    EngineerNum -= CustomRoles.Mayor.getCount();

                if (Options.MadSnitchCanVent.GetBool())
                    EngineerNum -= CustomRoles.MadSnitch.getCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= CustomRoles.Mafia.getCount() + CustomRoles.SerialKiller.getCount() + CustomRoles.BountyHunter.getCount() + CustomRoles.Warlock.getCount() + CustomRoles.ShapeMaster.getCount() + CustomRoles.FireWorks.getCount() + CustomRoles.Sniper.getCount();
                if (main.RealOptionsData.NumImpostors > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.getCount();
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

            Logger.msg("SelectRolesPatch.Postfix.End");
        }
        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = new System.Random();
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.getCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new List<PlayerControl>();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                main.AllPlayerCustomRoles[player.PlayerId] = role;
                Logger.info("役職設定:" + player.Data.PlayerName + " = " + role.ToString(), "AssignRoles");

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
            if (CustomRoles.Lovers.isEnable())
            {
                //Loversを初期化
                main.LoversPlayers.Clear();
                main.isLoversDead = false;
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
            if (RawCount == -1) count = Math.Clamp(loversRole.getCount(), 0, allPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                main.LoversPlayers.Add(player);
                allPlayers.Remove(player);
                main.AllPlayerCustomSubRoles[player.PlayerId] = loversRole;
                Logger.info("役職設定:" + player.Data.PlayerName + " = " + player.getCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers();
        }
    }
}
