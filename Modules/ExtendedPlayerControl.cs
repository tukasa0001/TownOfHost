using System.Collections.Generic;
using Hazel;
using System;
using System.Linq;
using InnerNet;
using static TownOfHost.Translator;

namespace TownOfHost
{
    static class ExtendedPlayerControl
    {
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
        {
            if (role < CustomRoles.NoSubRoleAssigned)
            {
                main.AllPlayerCustomRoles[player.PlayerId] = role;
            }
            else if (role >= CustomRoles.NoSubRoleAssigned)   //500:NoSubRole 501~:SubRole
            {
                main.AllPlayerCustomSubRoles[player.PlayerId] = role;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void SetCustomRole(this PlayerControl player, CustomRoles role)
        {
            main.AllPlayerCustomRoles[player.PlayerId] = role;
        }

        public static void RpcExile(this PlayerControl player)
        {
            RPC.ExileAsync(player);
        }
        public static InnerNet.ClientData getClient(this PlayerControl player)
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        public static int getClientId(this PlayerControl player)
        {
            var client = player.getClient();
            if (client == null) return -1;
            return client.Id;
        }
        public static CustomRoles getCustomRole(this GameData.PlayerInfo player)
        {
            if (player == null || player.Object == null) return CustomRoles.Crewmate;
            return player.Object.getCustomRole();
        }

        public static CustomRoles getCustomRole(this PlayerControl player)
        {
            var cRole = CustomRoles.Crewmate;
            if (player == null)
            {
                Logger.warn("CustomRoleを取得しようとしましたが、対象がnullでした。", "getCustomRole");
                return cRole;
            }
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(player.PlayerId, out cRole);
            if (cRoleFound || player.Data.Role == null) return cRole;

            switch (player.Data.Role.Role)
            {
                case RoleTypes.Crewmate: return CustomRoles.Crewmate;
                case RoleTypes.Engineer: return CustomRoles.Engineer;
                case RoleTypes.Scientist: return CustomRoles.Scientist;
                case RoleTypes.GuardianAngel: return CustomRoles.GuardianAngel;
                case RoleTypes.Impostor: return CustomRoles.Impostor;
                case RoleTypes.Shapeshifter: return CustomRoles.Shapeshifter;
                default: return CustomRoles.Crewmate;
            }
        }

        public static CustomRoles getCustomSubRole(this PlayerControl player)
        {
            if (player == null)
            {
                Logger.warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
                return CustomRoles.NoSubRoleAssigned;
            }
            var cRoleFound = main.AllPlayerCustomSubRoles.TryGetValue(player.PlayerId, out var cRole);
            if (cRoleFound) return cRole;
            else return CustomRoles.NoSubRoleAssigned;
        }
        public static void RpcSetNameEx(this PlayerControl player, string name)
        {
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            }
            HudManagerPatch.LastSetNameDesyncCount++;

            player.RpcSetName(name);
        }

        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;
            if (!force && main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
            {
                //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
                return;
            }
            main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            HudManagerPatch.LastSetNameDesyncCount++;
            Logger.info($"Set:{player?.Data?.PlayerName}:{name} for {seer.getNameWithRole()}", "RpcSetNamePrivate");

            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.Reliable, clientId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, PlayerControl seer = null)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー

            if (player == null) return;
            if (seer == null) seer = player;
            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
            writer.Write((ushort)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (target == null) target = killer;
            main.SelfGuard[target.PlayerId] = true;
            killer.RpcProtectPlayer(target, colorId);
            new LateTask(() =>
            {
                if (target == null) return;
                main.SelfGuard[target.PlayerId] = false;
                if (!target.Data.IsDead && target.protectedByGuardian)
                {
                    killer?.RpcMurderPlayer(target);
                }
                else
                {
                    main.BlockKilling[killer.PlayerId] = false;
                }
            }, 0.5f, "GuardAndKill");
        }
        public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.getClientId());
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (AmongUsClient.Instance.AmClient)
            {
                killer.ProtectPlayer(target, colorId);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.getClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(colorId);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static byte GetRoleCount(this Dictionary<CustomRoles, byte> dic, CustomRoles role)
        {
            if (!dic.ContainsKey(role))
            {
                dic[role] = 0;
            }

            return dic[role];
        }

        public static bool canBeKilledBySheriff(this PlayerControl player)
        {
            var cRole = player.getCustomRole();
            switch (cRole)
            {
                case CustomRoles.Jester:
                    return Options.SheriffCanKillJester.GetBool();
                case CustomRoles.Terrorist:
                    return Options.SheriffCanKillTerrorist.GetBool();
                case CustomRoles.Executioner:
                    return Options.SheriffCanKillExecutioner.GetBool();
                case CustomRoles.Opportunist:
                    return Options.SheriffCanKillOpportunist.GetBool();
                case CustomRoles.Arsonist:
                    return Options.SheriffCanKillArsonist.GetBool();
                case CustomRoles.Egoist:
                    return Options.SheriffCanKillEgoist.GetBool();
                case CustomRoles.EgoSchrodingerCat:
                    return Options.SheriffCanKillEgoShrodingerCat.GetBool();
                case CustomRoles.SchrodingerCat:
                    return true;
            }
            CustomRoles role = player.getCustomRole();
            RoleType roleType = role.getRoleType();
            switch (roleType)
            {
                case RoleType.Impostor:
                    return true;
                case RoleType.Madmate:
                    return Options.SheriffCanKillMadmate.GetBool();
            }
            return false;
        }

        public static void SendDM(this PlayerControl target, string text)
        {
            Utils.SendMessage(text, target.PlayerId);
        }

        /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
            if(!AmongUsClient.Instance.AmHost) return;
            byte KilledById;
            if(KilledBy == null)
                KilledById = byte.MaxValue;
            else
                KilledById = KilledBy.PlayerId;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(KilledById);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            RPC.BeKilled(player.PlayerId, KilledById);
        }*/
        public static void CustomSyncSettings(this PlayerControl player)
        {
            if (player == null || !AmongUsClient.Instance.AmHost) return;
            if (main.RealOptionsData == null)
            {
                main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            }

            var clientId = player.getClientId();
            var opt = main.RealOptionsData.DeepCopy();

            switch (player.getCustomRole())
            {
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                case CustomRoles.ShapeMaster:
                    opt.RoleOptions.ShapeshifterCooldown = 0.1f;
                    opt.RoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                    opt.RoleOptions.ShapeshifterLeaveSkin = false;
                    break;
                case CustomRoles.Warlock:
                    if (!main.AirshipMeetingCheck)
                    {
                        if (!main.isCursed) opt.RoleOptions.ShapeshifterCooldown = Options.BHDefaultKillCooldown.GetFloat();
                        if (main.isCursed) opt.RoleOptions.ShapeshifterCooldown = 1f;
                    }
                    else
                        opt.RoleOptions.ShapeshifterCooldown = Options.BHDefaultKillCooldown.GetFloat() - 10f;
                    break;
                case CustomRoles.SerialKiller:
                    if (!main.AirshipMeetingCheck)
                        opt.RoleOptions.ShapeshifterCooldown = Options.SerialKillerLimit.GetFloat();
                    else
                        opt.RoleOptions.ShapeshifterCooldown = Options.SerialKillerLimit.GetFloat() - 10f;
                    break;
                case CustomRoles.BountyHunter:
                    if (!main.AirshipMeetingCheck)
                        opt.RoleOptions.ShapeshifterCooldown = Options.BountyTargetChangeTime.GetFloat() + Options.BountyFailureKillCooldown.GetFloat();
                    else
                        opt.RoleOptions.ShapeshifterCooldown = Options.BountyTargetChangeTime.GetFloat() + Options.BountyFailureKillCooldown.GetFloat() - 10f;
                    break;
                case CustomRoles.Shapeshifter:
                case CustomRoles.Mafia:
                    opt.RoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoles.EvilWatcher:
                case CustomRoles.NiceWatcher:
                    if (opt.AnonymousVotes)
                        opt.AnonymousVotes = false;
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    opt.SetVision(player, false);
                    break;
                case CustomRoles.Lighter:
                    if (player.getPlayerTaskState().isTaskFinished)
                        opt.SetVision(player, true);
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    opt.SetVision(player, true);
                    break;
                case CustomRoles.Doctor:
                    opt.RoleOptions.ScientistCooldown = 0f;
                    opt.RoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                    break;
                case CustomRoles.SpeedBooster:
                    if (!player.Data.IsDead)
                    {
                        if (player.getPlayerTaskState().isTaskFinished)
                        {
                            if (!main.SpeedBoostTarget.ContainsKey(player.PlayerId))
                            {
                                var rand = new System.Random();
                                List<PlayerControl> targetplayers = new List<PlayerControl>();
                                //切断者と死亡者を除外
                                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                                {
                                    if (!p.Data.Disconnected && !p.Data.IsDead && !main.SpeedBoostTarget.ContainsValue(p.PlayerId)) targetplayers.Add(p);
                                }
                                //ターゲットが0ならアップ先をプレイヤーをnullに
                                if (targetplayers.Count >= 1)
                                {
                                    PlayerControl target = targetplayers[rand.Next(0, targetplayers.Count)];
                                    //Logger.SendInGame("スピードブースターの相手:"+target.nameText.text);
                                    main.SpeedBoostTarget.Add(player.PlayerId, target.PlayerId);
                                }
                                else
                                {
                                    main.SpeedBoostTarget.Add(player.PlayerId, 255);
                                }
                            }
                            if (main.SpeedBoostTarget.ContainsValue(player.PlayerId))
                                main.AllPlayerSpeed[player.PlayerId] = Options.SpeedBoosterUpSpeed.GetFloat();
                        }
                    }
                    break;
                case CustomRoles.Mayor:
                    opt.RoleOptions.EngineerCooldown = opt.EmergencyCooldown;
                    opt.RoleOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Mare:
                    main.AllPlayerSpeed[player.PlayerId] = main.RealOptionsData.PlayerSpeedMod;
                    if (Utils.isActive(SystemTypes.Electrical))//もし停電発生した場合
                    {
                        main.AllPlayerSpeed[player.PlayerId] = Options.BlackOutMareSpeed.GetFloat();//Mareの速度を設定した値にする
                        main.AllPlayerKillCooldown[player.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() / 2;//Mareのキルクールを÷2する
                    }
                    break;


                InfinityVent:
                    opt.RoleOptions.EngineerCooldown = 0;
                    opt.RoleOptions.EngineerInVentMaxTime = 0;
                    break;
            }
            CustomRoles role = player.getCustomRole();
            RoleType roleType = role.getRoleType();
            switch (roleType)
            {
                case RoleType.Madmate:
                    opt.RoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    opt.RoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(player, true);
                    break;
            }
            if (main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
            {
                foreach (var kc in main.AllPlayerKillCooldown)
                {
                    if (kc.Key == player.PlayerId)
                    {
                        if (kc.Value > 0)
                            opt.KillCooldown = kc.Value;
                        else
                            opt.KillCooldown = 0.01f;
                    }
                }
            }
            if (main.AllPlayerSpeed.ContainsKey(player.PlayerId))
            {
                foreach (var speed in main.AllPlayerSpeed)
                {
                    if (speed.Key == player.PlayerId)
                    {
                        if (speed.Value > 0)
                            opt.PlayerSpeedMod = speed.Value;
                        else
                            opt.PlayerSpeedMod = 0.0001f;
                    }
                }
            }
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead && opt.AnonymousVotes)
                opt.AnonymousVotes = false;
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetSelection() <= Options.UsedButtonCount)
                opt.EmergencyCooldown = 3600;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.ImpostorLightMod = 0f;
            }
            opt.DiscussionTime = main.DiscussionTime;
            opt.VotingTime = main.VotingTime;

            if (player.AmOwner) PlayerControl.GameOptions = opt;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SyncSettings, SendOption.Reliable, clientId);
            writer.WriteBytesAndSize(opt.ToBytes(5));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static TaskState getPlayerTaskState(this PlayerControl player)
        {
            return PlayerState.taskState[player.PlayerId];
        }

        public static GameOptionsData DeepCopy(this GameOptionsData opt)
        {
            var optByte = opt.ToBytes(5);
            return GameOptionsData.FromBytes(optByte);
        }

        public static string getRoleName(this PlayerControl player)
        {
            return $"{Utils.getRoleName(player.getCustomRole())}" /*({getString("Last")})"*/;
        }
        public static string getSubRoleName(this PlayerControl player)
        {
            return $"{Utils.getRoleName(player.getCustomSubRole())}";
        }
        public static string getAllRoleName(this PlayerControl player)
        {
            if (!player) return null;
            var text = player.getRoleName();
            text += player.getCustomSubRole() != CustomRoles.NoSubRoleAssigned ? $" + {player.getSubRoleName()}" : "";
            return text;
        }
        public static string getNameWithRole(this PlayerControl player)
        {
            return $"{player?.Data?.PlayerName}({player?.getAllRoleName()})";
        }
        public static string getRoleColorCode(this PlayerControl player)
        {
            return Utils.getRoleColorCode(player.getCustomRole());
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;
            int clientId = pc.getClientId();

            byte reactorId = 3;
            if (PlayerControl.GameOptions.MapId == 2) reactorId = 21;

            new LateTask(() =>
            {
                MessageWriter SabotageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                SabotageWriter.Write(reactorId);
                MessageExtensions.WriteNetObject(SabotageWriter, pc);
                SabotageWriter.Write((byte)128);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageWriter);
            }, 0f + delay, "Reactor Desync");

            new LateTask(() =>
            {
                MessageWriter MurderWriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, clientId);
                MessageExtensions.WriteNetObject(MurderWriter, pc);
                AmongUsClient.Instance.FinishRpcImmediately(MurderWriter);
            }, 0.2f + delay, "Murder To Reset Cam");

            new LateTask(() =>
            {
                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                SabotageFixWriter.Write(reactorId);
                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                SabotageFixWriter.Write((byte)16);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
            }, 0.4f + delay, "Fix Desync Reactor");

            if (PlayerControl.GameOptions.MapId == 4) //Airship用
                new LateTask(() =>
                {
                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                    SabotageFixWriter.Write(reactorId);
                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                    SabotageFixWriter.Write((byte)17);
                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                }, 0.4f + delay, "Fix Desync Reactor 2");
        }

        public static string getRealName(this PlayerControl player, bool isMeeting = false)
        {
            return isMeeting ? player?.Data?.PlayerName : player?.name;
        }

        public static PlayerControl getBountyTarget(this PlayerControl player)
        {
            if (player == null) return null;
            if (main.BountyTargets == null) main.BountyTargets = new Dictionary<byte, PlayerControl>();

            if (!main.BountyTargets.TryGetValue(player.PlayerId, out var target))
            {
                target = player.ResetBountyTarget();
            }
            return target;
        }
        public static PlayerControl ResetBountyTarget(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost/* && AmongUsClient.Instance.GameMode != GameModes.FreePlay*/) return null;
            List<PlayerControl> cTargets = new List<PlayerControl>();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                // 死者/切断者/インポスターを除外
                if (!pc.Data.IsDead &&
                    !pc.Data.Disconnected &&
                    !pc.getCustomRole().isImpostor()
                )
                {
                    cTargets.Add(pc);
                }
            }

            var rand = new System.Random();
            if (cTargets.Count <= 0)
            {
                Logger.error("ターゲットの指定に失敗しました:ターゲット候補が存在しません", "BountyHunter");
                return null;
            }
            var target = cTargets[rand.Next(0, cTargets.Count - 1)];
            main.BountyTargets[player.PlayerId] = target;
            Logger.info($"プレイヤー{player.getNameWithRole()}のターゲットを{target.getNameWithRole()}に変更", "BountyHunter");

            //RPCによる同期
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBountyTarget, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(target.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return target;
        }
        public static bool GetKillOrSpell(this PlayerControl player)
        {
            if (!main.KillOrSpell.TryGetValue(player.PlayerId, out var KillOrSpell))
            {
                main.KillOrSpell[player.PlayerId] = false;
                KillOrSpell = false;
            }
            return KillOrSpell;
        }
        public static void SyncKillOrSpell(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(player.GetKillOrSpell());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetSheriffShotLimit(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(main.SheriffShotLimit[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetTimeThiefKillCount(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTimeThiefKillCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(main.TimeThiefKillCount[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static bool CanUseKillButton(this PlayerControl pc)
        {
            bool canUse =
                pc.getCustomRole().isImpostor() ||
                pc.Is(CustomRoles.Sheriff) ||
                pc.Is(CustomRoles.Arsonist);

            if (pc.Is(CustomRoles.Mafia))
            {
                if (main.AliveImpostorCount > 1) canUse = false;
            }
            else if (pc.Is(CustomRoles.Mare))
                return Utils.isActive(SystemTypes.Electrical);
            if (pc.Is(CustomRoles.FireWorks)) return FireWorks.CanUseKillButton(pc);
            if (pc.Is(CustomRoles.Sniper)) return Sniper.CanUseKillButton(pc);
            return canUse;
        }
        public static bool isLastImpostor(this PlayerControl pc)
        { //キルクールを変更するインポスター役職は省く
            if (pc.getCustomRole().isImpostor() &&
                !pc.Data.IsDead &&
                Options.EnableLastImpostor.GetBool() &&
                !pc.Is(CustomRoles.Vampire) &&
                !pc.Is(CustomRoles.BountyHunter) &&
                !pc.Is(CustomRoles.SerialKiller) &&
                main.AliveImpostorCount == 1)
                return true;
            return false;
        }
        public static bool isDousedPlayer(this PlayerControl arsonist, PlayerControl target)
        {
            if (arsonist == null) return false;
            if (target == null) return false;
            if (main.isDoused == null) return false;
            main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
            return isDoused;
        }
        public static void RpcSendDousedPlayerCount(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendDousedPlayerCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(main.DousedPlayerCount[player.PlayerId].Item1);
            writer.Write(main.DousedPlayerCount[player.PlayerId].Item2);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ExiledSchrodingerCatTeamChange(this PlayerControl player)
        {
            var rand = new System.Random();
            System.Collections.Generic.List<CustomRoles> RandSchrodinger = new System.Collections.Generic.List<CustomRoles>();
            RandSchrodinger.Add(CustomRoles.CSchrodingerCat);
            RandSchrodinger.Add(CustomRoles.MSchrodingerCat);
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (CustomRoles.Egoist.isEnable() && (pc.Is(CustomRoles.Egoist) && !pc.Data.IsDead))
                {
                    RandSchrodinger.Add(CustomRoles.EgoSchrodingerCat);
                }
            var SchrodingerTeam = RandSchrodinger[rand.Next(RandSchrodinger.Count)];
            player.RpcSetCustomRole(SchrodingerTeam);
        }
        public static void ResetKillCooldown(this PlayerControl player)
        {
            main.AllPlayerKillCooldown[player.PlayerId] = Options.BHDefaultKillCooldown.GetFloat(); //キルクールをデフォルトキルクールに変更
            switch (player.getCustomRole())
            {
                case CustomRoles.SerialKiller:
                    main.AllPlayerKillCooldown[player.PlayerId] = Options.SerialKillerCooldown.GetFloat(); //シリアルキラーはシリアルキラーのキルクールに。
                    break;
                case CustomRoles.Arsonist:
                    main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                    break;
                case CustomRoles.Sheriff:
                    main.AllPlayerKillCooldown[player.PlayerId] = Options.SheriffKillCooldown.GetFloat(); //シェリフはシェリフのキルクールに。
                    break;
            }
            if (player.isLastImpostor())
                main.AllPlayerKillCooldown[player.PlayerId] = Options.LastImpostorKillCooldown.GetFloat();
        }
        public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
        {
            Logger.info($"{target?.Data?.PlayerName}はTrapperだった", "Trapper");
            main.AllPlayerSpeed[killer.PlayerId] = 0.00001f;
            killer.CustomSyncSettings();
            new LateTask(() =>
            {
                main.AllPlayerSpeed[killer.PlayerId] = main.RealOptionsData.PlayerSpeedMod;
                killer.CustomSyncSettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        public static void CanUseImpostorVent(this PlayerControl player)
        {
            switch (player.getCustomRole())
            {
                case CustomRoles.Sheriff:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(false);
                    player.Data.Role.CanVent = false;
                    return;
                case CustomRoles.Arsonist:
                    bool CanUse = player.isDouseDone();
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(CanUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = CanUse;
                    return;
            }
        }
        public static bool isDouseDone(this PlayerControl player)
        {
            if (!main.DousedPlayerCount.ContainsKey(player.PlayerId)) return false;
            if (main.DousedPlayerCount.TryGetValue(player.PlayerId, out (int, int) count) && count.Item1 == count.Item2)
                return true;

            return false;
        }
        public static void ResetThiefVotingTime(this PlayerControl thief)
        {
            for (var i = 0; i < main.TimeThiefKillCount[thief.PlayerId]; i++)
                main.VotingTime += Options.TimeThiefDecreaseVotingTime.GetInt();
            main.TimeThiefKillCount[thief.PlayerId] = 0; //初期化
        }
        public static void RemoveDousePlayer(this PlayerControl target) //死亡時、切断時に呼ばれる
        {
            foreach (var arsonist in PlayerControl.AllPlayerControls)
            {
                if (target == arsonist || !main.DousedPlayerCount.ContainsKey(arsonist.PlayerId) || arsonist.Data.IsDead) continue;
                if (arsonist.Is(CustomRoles.Arsonist))
                {
                    bool isDoused = false;
                    main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out isDoused); //targetを塗っているかどうかを判定
                    if (main.DousedPlayerCount.TryGetValue(arsonist.PlayerId, out (int, int) count) && count.Item1 < count.Item2) //塗った人数より塗るべき人数のほうが多いとき
                    {
                        main.isDeadDoused[target.PlayerId] = true;
                        var ArsonistDic = main.DousedPlayerCount[arsonist.PlayerId];
                        var LeftPlayer = ArsonistDic.Item1; //塗った人数
                        var RequireDouse = ArsonistDic.Item2; //塗るべき人数
                        if (isDoused)
                            LeftPlayer--;
                        RequireDouse--;
                        main.DousedPlayerCount[arsonist.PlayerId] = (LeftPlayer, RequireDouse);
                        Logger.info($"{arsonist.getRealName()} : {ArsonistDic}", "Arsonist");
                        arsonist.RpcSendDousedPlayerCount(); //RPCで他クライアントと塗り状況を同期
                    }
                }
            }
        }
        public static bool isModClient(this PlayerControl player) => main.playerVersion.ContainsKey(player.PlayerId);

        //汎用
        public static bool Is(this PlayerControl target, CustomRoles role)
        {
            if (role > CustomRoles.NoSubRoleAssigned)
            {
                return target.getCustomSubRole() == role;
            }
            return target.getCustomRole() == role;
        }
        public static bool Is(this PlayerControl target, RoleType type) { return target.getCustomRole().getRoleType() == type; }

    }
}
