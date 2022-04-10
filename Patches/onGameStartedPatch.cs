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
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.SerialKillerTimer = new Dictionary<byte, float>();
            main.WarlockTimer = new Dictionary<byte, float>();
            main.BountyTimer = new Dictionary<byte, float>();
            main.isDoused = new Dictionary<(byte, byte), bool>();
            main.DousedPlayerCount = new Dictionary<byte, int>();
            main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            main.ArsonistKillCooldownCheck = true;
            main.BountyTargets = new Dictionary<byte, PlayerControl>();
            main.isTargetKilled = new Dictionary<byte, bool>();
            main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            main.isCurseAndKill = new Dictionary<byte, bool>();
            main.SKMadmateNowCount = 0;
            main.isCursed = false;

            main.IgnoreReportPlayers = new List<byte>();

            main.SheriffShotLimit = new Dictionary<byte, float>();

            main.SpelledPlayer = new List<PlayerControl>();
            main.witchMeeting = false;
            main.isBountyKillSuccess = false;
            main.BountyTimerCheck = false;
            main.BountyMeetingCheck = false;
            main.CheckShapeshift = new Dictionary<byte, bool>();
            main.SpeedBoostTarget = new Dictionary<byte, byte>();

            Options.UsedButtonCount = 0;
            Options.SabotageMasterUsedSkillCount = 0;
            main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            main.RealNames = new Dictionary<byte, string>();
            main.BlockKilling = new Dictionary<byte, bool>();

            NameColorManager.Instance.RpcReset();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info($"{pc.PlayerId}:{pc.name}:{pc.nameText.text}");
                main.RealNames[pc.PlayerId] = pc.name;
                pc.nameText.text = pc.name;

                if (!__instance.AmHost || pc.isSheriff())
                {
                    main.SheriffShotLimit[pc.PlayerId] = Options.SheriffShotLimit.GetFloat();
                    pc.RpcSetSheriffShotLimit();
                    Logger.info($"{pc.getRealName()} : 残り{main.SheriffShotLimit[pc.PlayerId]}発");
                }
            }
            if (__instance.AmHost)
            {

                main.VisibleTasksCount = true;

                RPC.SyncCustomSettingsRPC();
                main.RefixCooldownDelay = 0;
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                    Options.HideAndSeekImpVisionMin = PlayerControl.GameOptions.ImpostorLightMod;
                }
            }
            else
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.isSheriff())
                    {
                        main.SheriffShotLimit[pc.PlayerId] = Options.SheriffShotLimit.GetFloat();
                        pc.RpcSetSheriffShotLimit();
                        Logger.info($"{pc.getRealName()} : 残り{main.SheriffShotLimit[pc.PlayerId]}発");
                    }
                }
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

            main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            main.AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            var rand = new System.Random();
            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                //役職の人数を指定
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);
                int AdditionalEngineerNum = CustomRoles.Madmate.getCount() + CustomRoles.Terrorist.getCount();// - EngineerNum;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = CustomRoles.Mafia.getCount() + CustomRoles.SerialKiller.getCount() + CustomRoles.BountyHunter.getCount() + CustomRoles.Warlock.getCount() + CustomRoles.ShapeMaster.getCount();//- ShapeshifterNum;
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

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                rand = new System.Random();
                SetColorPatch.IsAntiGlitchDisabled = true;

                //Hide And Seek時の処理
                List<PlayerControl> Impostors = new List<PlayerControl>();
                List<PlayerControl> Crewmates = new List<PlayerControl>();
                //リスト作成兼色設定処理
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Crewmate);
                    if (pc.Data.Role.IsImpostor)
                    {
                        Impostors.Add(pc);
                        pc.RpcSetColor(0);
                    }
                    else
                    {
                        Crewmates.Add(pc);
                        pc.RpcSetColor(1);
                    }
                    if (Options.IgnoreCosmetics.GetBool())
                    {
                        pc.RpcSetHat("");
                        pc.RpcSetSkin("");
                    }
                }
                //FoxCountとTrollCountを適切に修正する
                int FixedFoxCount = Math.Clamp(CustomRoles.Fox.getCount(), 0, Crewmates.Count);
                int FixedTrollCount = Math.Clamp(CustomRoles.Troll.getCount(), 0, Crewmates.Count - FixedFoxCount);
                List<PlayerControl> FoxList = new List<PlayerControl>();
                List<PlayerControl> TrollList = new List<PlayerControl>();
                //役職設定処理
                for (var i = 0; i < FixedFoxCount; i++)
                {
                    var id = rand.Next(Crewmates.Count);
                    FoxList.Add(Crewmates[id]);
                    main.AllPlayerCustomRoles[Crewmates[id].PlayerId] = CustomRoles.Fox;
                    Crewmates[id].RpcSetColor(3);
                    Crewmates[id].RpcSetCustomRole(CustomRoles.Fox);
                    Crewmates.RemoveAt(id);
                }
                for (var i = 0; i < FixedTrollCount; i++)
                {
                    var id = rand.Next(Crewmates.Count);
                    TrollList.Add(Crewmates[id]);
                    main.AllPlayerCustomRoles[Crewmates[id].PlayerId] = CustomRoles.Troll;
                    Crewmates[id].RpcSetColor(2);
                    Crewmates[id].RpcSetCustomRole(CustomRoles.Troll);
                    Crewmates.RemoveAt(id);
                }
                //通常クルー・インポスター用RPC
                foreach (var pc in Crewmates) pc.RpcSetCustomRole(CustomRoles.Crewmate);
                foreach (var pc in Impostors) pc.RpcSetCustomRole(CustomRoles.Crewmate);
            }
            else
            {
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
                            Logger.SendInGame("エラー:役職設定中に無効な役職のプレイヤーを発見しました(" + pc.name + ")");
                            break;
                    }
                }

                AssignCustomRolesFromList(CustomRoles.Jester, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Madmate, Engineers);
                AssignCustomRolesFromList(CustomRoles.Bait, Crewmates);
                AssignCustomRolesFromList(CustomRoles.MadGuardian, Crewmates);
                AssignCustomRolesFromList(CustomRoles.MadSnitch, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Mayor, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Opportunist, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Snitch, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SabotageMaster, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Mafia, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Terrorist, Engineers);
                AssignCustomRolesFromList(CustomRoles.Vampire, Impostors);
                AssignCustomRolesFromList(CustomRoles.BountyHunter, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Witch, Impostors);
                AssignCustomRolesFromList(CustomRoles.ShapeMaster, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Warlock, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.SerialKiller, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Lighter, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SpeedBooster, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SchrodingerCat, Crewmates);
                if (Options.IsEvilWatcher) AssignCustomRolesFromList(CustomRoles.Watcher, Impostors);
                else AssignCustomRolesFromList(CustomRoles.Watcher, Crewmates);
                if (main.RealOptionsData.NumImpostors > 1)
                    AssignCustomRolesFromList(CustomRoles.Egoist, Shapeshifters);

                //RPCによる同期
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.isWatcher() && Options.IsEvilWatcher)
                        main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.EvilWatcher;
                    if (pc.isWatcher() && !Options.IsEvilWatcher)
                        main.AllPlayerCustomRoles[pc.PlayerId] = CustomRoles.NiceWatcher;
                }
                foreach (var pair in main.AllPlayerCustomRoles)
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
                    if (pc.isSheriff())
                    {
                        main.SheriffShotLimit[pc.PlayerId] = Options.SheriffShotLimit.GetFloat();
                        pc.RpcSetSheriffShotLimit();
                        Logger.info($"{pc.getRealName()} : 残り{main.SheriffShotLimit[pc.PlayerId]}発");
                    }
                    if (pc.isBountyHunter())
                    {
                        pc.ResetBountyTarget();
                        main.isTargetKilled.Add(pc.PlayerId, false);
                        main.BountyTimer.Add(pc.PlayerId, 0f); //BountyTimerにBountyHunterのデータを入力
                    }
                    if (pc.isWitch()) main.KillOrSpell.Add(pc.PlayerId, false);
                    if (pc.isWarlock())
                    {
                        main.CursedPlayers.Add(pc.PlayerId, null);
                        main.isCurseAndKill.Add(pc.PlayerId, false);
                    }
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) main.CheckShapeshift.Add(pc.PlayerId, false);
                    if (pc.isArsonist())
                    {
                        main.DousedPlayerCount.Add(pc.PlayerId, PlayerControl.AllPlayerControls.Count - 1);
                        foreach (var ar in PlayerControl.AllPlayerControls)
                        {
                            main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        }
                    }
                }

                //役職の人数を戻す
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);
                EngineerNum -= CustomRoles.Madmate.getCount() + CustomRoles.Terrorist.getCount();
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= CustomRoles.Mafia.getCount() + CustomRoles.SerialKiller.getCount() + CustomRoles.BountyHunter.getCount() + CustomRoles.Warlock.getCount() + CustomRoles.ShapeMaster.getCount();
                if (main.RealOptionsData.NumImpostors > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.getCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

                //サーバーの役職判定をだます
                new LateTask(() =>
                {
                    if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            pc.RpcSetRole(RoleTypes.Shapeshifter);
                        }
                }, 3f, "SetImpostorForServer");
            }
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
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                main.AllPlayerCustomRoles[player.PlayerId] = role;
                Logger.info("役職設定:" + player.name + " = " + role.ToString());
            }
            return AssignedPlayers;
        }
    }
}
