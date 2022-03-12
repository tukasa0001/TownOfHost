using System;
using HarmonyLib;
using System.Collections.Generic;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.StartGame))]
    class changeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {//注:この時点では役職は設定されていません。
            main.currentWinner = CustomWinner.Default;
            main.CustomWinTrigger = false;
            main.OptionControllerIsEnable = false;
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.SerialKillerTimer = new Dictionary<byte, float>();
            main.BountyTimer = new Dictionary<byte, float>();
            main.BountyTargets = new Dictionary<byte, PlayerControl>();
            main.isTargetKilled = new Dictionary<byte, bool>();
            main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            main.CursedPlayerDie = new List<PlayerControl>();
            main.FirstCursedCheck = new Dictionary<byte, bool>();
            main.SKMadmateNowCount = 0;

            main.IgnoreReportPlayers = new List<byte>();

            main.ps = new PlayerState();

            main.SpelledPlayer = new List<PlayerControl>();
            main.witchMeeting = false;
            main.isBountyKillSuccess = false;
            main.BountyTimerCheck = false;
            main.BountyMeetingCheck = false;
            main.CheckShapeshift = new Dictionary<byte, bool>();

            main.UsedButtonCount = 0;
            main.SabotageMasterUsedSkillCount = 0;
            main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            main.RealNames = new Dictionary<byte, string>();
            main.BlockKilling = new Dictionary<byte, bool>();

            NameColorManager.Instance.RpcReset();
            foreach(var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info($"{pc.PlayerId}:{pc.name}:{pc.nameText.text}");
                main.RealNames[pc.PlayerId] = pc.name;
                pc.nameText.text = pc.name; 
            }
            if (__instance.AmHost)
            {

                main.VisibleTasksCount = true;

                main.SyncCustomSettingsRPC();
                main.RefixCooldownDelay = 0;
                if(main.IsHideAndSeek) {
                    main.HideAndSeekKillDelayTimer = main.HideAndSeekKillDelay;
                    main.HideAndSeekImpVisionMin = PlayerControl.GameOptions.ImpostorLightMod;
                }
            }
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch {
        public static void Prefix(RoleManager __instance) {
            if(!AmongUsClient.Instance.AmHost) return;
            main.AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            var rand = new System.Random();
            if(!main.IsHideAndSeek) {
                //役職の人数を指定
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);
                int AdditionalEngineerNum = main.MadmateCount + main.TerroristCount;// - EngineerNum;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = main.MafiaCount + main.SerialKillerCount + main.BountyHunterCount + main.WarlockCount + main.ShapeMasterCount;//- ShapeshifterNum;
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

                
                List<PlayerControl> AllPlayers = new List<PlayerControl>();
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    AllPlayers.Add(pc);
                }

                if(main.SheriffCount > 0)
                for(var i = 0; i < main.SheriffCount; i++) {
                    if(AllPlayers.Count <= 0) break;
                    var sheriff = AllPlayers[rand.Next(0, AllPlayers.Count)];
                    AllPlayers.Remove(sheriff);
                    main.AllPlayerCustomRoles[sheriff.PlayerId] = CustomRoles.Sheriff;
                    //ここからDesyncが始まる
                    if(sheriff.PlayerId != 0) {
                        //ただしホスト、お前はDesyncするな。
                        sheriff.RpcSetRoleDesync(RoleTypes.Impostor);
                        foreach(var pc in PlayerControl.AllPlayerControls) {
                            if(pc == sheriff) continue;
                            sheriff.RpcSetRoleDesync(RoleTypes.Scientist, pc);
                            pc.RpcSetRoleDesync(RoleTypes.Scientist, sheriff);
                        }
                    } else {
                        //ホストは代わりに普通のクルーにする
                        sheriff.RpcSetRole(RoleTypes.Crewmate);
                    }
                    sheriff.Data.IsDead = true;
                }
            }
            Logger.msg("SelectRolesPatch.Prefix.End");
        }
        public static void Postfix(RoleManager __instance) {
            Logger.msg("SelectRolesPatch.Postfix.Start");
            if(!AmongUsClient.Instance.AmHost) return;
            //main.ApplySuffix();

            var rand = new System.Random();
            main.KillOrSpell = new Dictionary<byte, bool>();

            if(main.IsHideAndSeek) {
                rand = new System.Random();
                SetColorPatch.IsAntiGlitchDisabled = true;

                //Hide And Seek時の処理
                List<PlayerControl> Impostors = new List<PlayerControl>();
                List<PlayerControl> Crewmates = new List<PlayerControl>();
                //リスト作成兼色設定処理
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    main.AllPlayerCustomRoles.Add(pc.PlayerId,CustomRoles.Crewmate);
                    if(pc.Data.Role.IsImpostor) {
                        Impostors.Add(pc);
                        pc.RpcSetColor(0);
                    } else {
                        Crewmates.Add(pc);
                        pc.RpcSetColor(1);
                    }
                    if(main.IgnoreCosmetics) {
                        pc.RpcSetHat("");
                        pc.RpcSetSkin("");
                    }
                }
                //FoxCountとTrollCountを適切に修正する
                int FixedFoxCount = Math.Clamp(main.FoxCount,0,Crewmates.Count);
                int FixedTrollCount = Math.Clamp(main.TrollCount,0,Crewmates.Count - FixedFoxCount);
                List<PlayerControl> FoxList = new List<PlayerControl>();
                List<PlayerControl> TrollList = new List<PlayerControl>();
                //役職設定処理
                for(var i = 0; i < FixedFoxCount; i++) {
                    var id = rand.Next(Crewmates.Count);
                    FoxList.Add(Crewmates[id]);
                    main.AllPlayerCustomRoles[Crewmates[id].PlayerId] = CustomRoles.Fox;
                    Crewmates[id].RpcSetColor(3);
                    Crewmates[id].RpcSetCustomRole(CustomRoles.Fox);
                    Crewmates.RemoveAt(id);
                }
                for(var i = 0; i < FixedTrollCount; i++) {
                    var id = rand.Next(Crewmates.Count);
                    TrollList.Add(Crewmates[id]);
                    main.AllPlayerCustomRoles[Crewmates[id].PlayerId] = CustomRoles.Troll;
                    Crewmates[id].RpcSetColor(2);
                    Crewmates[id].RpcSetCustomRole(CustomRoles.Troll);
                    Crewmates.RemoveAt(id);
                }
                //通常クルー・インポスター用RPC
                foreach(var pc in Crewmates) pc.RpcSetCustomRole(CustomRoles.Crewmate);
                foreach(var pc in Impostors) pc.RpcSetCustomRole(CustomRoles.Crewmate);
            } else {
                List<PlayerControl> Crewmates = new List<PlayerControl>();
                List<PlayerControl> Impostors = new List<PlayerControl>();
                List<PlayerControl> Scientists = new List<PlayerControl>();
                List<PlayerControl> Engineers = new List<PlayerControl>();
                List<PlayerControl> GuardianAngels = new List<PlayerControl>();
                List<PlayerControl> Shapeshifters = new List<PlayerControl>();

                foreach(var pc in PlayerControl.AllPlayerControls) {
                    pc.Data.IsDead = false; //プレイヤーの死を解除する
                    if(main.AllPlayerCustomRoles.ContainsKey(pc.PlayerId)) continue; //既にカスタム役職が割り当てられていればスキップ
                    switch(pc.Data.Role.Role) {
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

                //RPCによる同期
                foreach(var pair in main.AllPlayerCustomRoles) {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }

                //名前の記録
                main.AllPlayerNames = new ();
                foreach (var pair in main.AllPlayerCustomRoles)
                {
                    main.AllPlayerNames.Add(pair.Key,main.RealNames[pair.Key]);
                }

                HudManager.Instance.SetHudActive(true);
                main.KillOrSpell = new Dictionary<byte, bool>();
                foreach (var pc in PlayerControl.AllPlayerControls){
                    if(pc.isWitch())main.KillOrSpell.Add(pc.PlayerId,false);
                }

                //BountyHunterのターゲットを初期化
                main.BountyTargets = new Dictionary<byte, PlayerControl>();
                main.BountyTimer = new Dictionary<byte, float>();
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.isBountyHunter()){
                        pc.ResetBountyTarget();
                        main.isTargetKilled.Add(pc.PlayerId, false);
                        main.BountyTimer.Add(pc.PlayerId, 0f); //BountyTimerにBountyHunterのデータを入力
                        }
                    if(pc.isWarlock())main.FirstCursedCheck.Add(pc.PlayerId, false);
                    if(pc.Data.Role.Role == RoleTypes.Shapeshifter)main.CheckShapeshift.Add(pc.PlayerId, false);
                }

                //役職の人数を戻す
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);
                EngineerNum -= main.MadmateCount + main.TerroristCount;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= main.MafiaCount + main.SerialKillerCount + main.BountyHunterCount + main.WarlockCount;
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

                //サーバーの役職判定をだます
                new LateTask(() => {
                    if(AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                    foreach(var pc in PlayerControl.AllPlayerControls) {
                        pc.RpcSetRole(RoleTypes.Shapeshifter);
                    }
                }, 3f, "SetImpostorForServer");
            }
            main.CustomSyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;

            Logger.msg("SelectRolesPatch.Postfix.End");
        }
        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1) {
            if(players == null || players.Count <= 0) return null;
            var rand = new System.Random();
            var count = Math.Clamp(RawCount, 0, players.Count);
            if(RawCount == -1) count = Math.Clamp(main.GetCountFromRole(role), 0, players.Count);
            if(count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new List<PlayerControl>();
            for(var i = 0; i < count; i++) {
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
