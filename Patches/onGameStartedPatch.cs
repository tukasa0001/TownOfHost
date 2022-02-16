using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;

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
            main.BountyTargets = new Dictionary<byte, PlayerControl>();

            main.ps = new PlayerState();

            main.SpelledPlayer = new List<PlayerControl>();
            main.witchMeeting = false;

            main.UsedButtonCount = 0;
            main.SabotageMasterUsedSkillCount = 0;
            main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            main.RealNames = new Dictionary<byte, string>();
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
                int AdditionalShapeshifterNum = main.MafiaCount;// - ShapeshifterNum;
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
                    main.AllPlayerCustomRoles.Add(pc.PlayerId,CustomRoles.Default);
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
                foreach(var pc in Crewmates) pc.RpcSetCustomRole(CustomRoles.Default);
                foreach(var pc in Impostors) pc.RpcSetCustomRole(CustomRoles.Default);
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
                            main.AllPlayerCustomRoles.Add(pc.PlayerId, CustomRoles.Default);
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
                AssignCustomRolesFromList(CustomRoles.Mayor, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Opportunist, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Snitch, Crewmates);
                AssignCustomRolesFromList(CustomRoles.SabotageMaster, Crewmates);
                AssignCustomRolesFromList(CustomRoles.Mafia, Shapeshifters);
                AssignCustomRolesFromList(CustomRoles.Terrorist, Engineers);
                AssignCustomRolesFromList(CustomRoles.Vampire, Impostors);
                AssignCustomRolesFromList(CustomRoles.BountyHunter, Impostors);
                AssignCustomRolesFromList(CustomRoles.Witch, Impostors);

                //RPCによる同期
                foreach(var pair in main.AllPlayerCustomRoles) {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value);
                }
                main.lastAllPlayerCustomRoles = main.AllPlayerCustomRoles;

                HudManager.Instance.SetHudActive(true);
                main.KillOrSpell = new Dictionary<byte, bool>();
                foreach (var pc in PlayerControl.AllPlayerControls){
                    if(pc.isWitch())main.KillOrSpell.Add(pc.PlayerId,false);
                }

                //BountyHunterのターゲットを初期化
                main.BountyTargets = new Dictionary<byte, PlayerControl>();
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.isBountyHunter()) pc.ResetBountyTarget();
                }

                //役職の人数を戻す
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);
                EngineerNum -= main.MadmateCount + main.TerroristCount;
                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= main.MafiaCount;
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

                //サーバーの役職判定をだます
                new LateTask(() => {
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
                var player = players[rand.Next(0, players.Count - 1)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                main.AllPlayerCustomRoles[player.PlayerId] = role;
                Logger.info("役職設定:" + player.name + " = " + role.ToString());
            }
            return AssignedPlayers;
        }
    }
}
