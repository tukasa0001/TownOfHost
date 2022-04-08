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
            main.AllPlayerCustomRoles[player.PlayerId] = role;
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.Write((byte)role);
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
                Logger.warn("CustomRoleを取得しようとしましたが、対象がnullでした。");
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
                Logger.warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。");
                return CustomRoles.NoSubRoleAssigned;
            }
            var cRoleFound = main.AllPlayerCustomSubRoles.TryGetValue(player.PlayerId, out var cRole);
            if (cRoleFound) return cRole;
            else return CustomRoles.NoSubRoleAssigned;
        }

        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;
            //Logger.info($"{player.name}:{name} => {seer.name}");
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

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null)
        {
            if (target == null) target = killer;
            killer.RpcProtectPlayer(target, 0);
            new LateTask(() =>
            {
                if (target.protectedByGuardian)
                {
                    killer.RpcMurderPlayer(target);
                }
            }, 0.5f, "GuardAndKill");
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
                case CustomRoles.Madmate:
                    goto InfinityVent;
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                case CustomRoles.ShapeMaster:
                    opt.RoleOptions.ShapeshifterCooldown = 0.1f;
                    opt.RoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                    opt.RoleOptions.ShapeshifterLeaveSkin = false;
                    goto DefaultKillcooldown;
                case CustomRoles.Vampire:
                    if (main.BountyMeetingCheck) opt.KillCooldown = Options.BHDefaultKillCooldown.GetFloat();
                    if (!main.BountyMeetingCheck) opt.KillCooldown = Options.BHDefaultKillCooldown.GetFloat() * 2;
                    break;
                case CustomRoles.Warlock:
                    if (!main.isCursed) opt.RoleOptions.ShapeshifterCooldown = Options.BHDefaultKillCooldown.GetFloat();
                    if (main.isCursed) opt.RoleOptions.ShapeshifterCooldown = 1f;
                    opt.KillCooldown = Options.BHDefaultKillCooldown.GetFloat() * 2;
                    break;
                case CustomRoles.SerialKiller:
                    opt.RoleOptions.ShapeshifterCooldown = Options.SerialKillerLimit.GetFloat();
                    opt.KillCooldown = Options.SerialKillerCooldown.GetFloat() * 2;
                    break;
                case CustomRoles.BountyHunter:
                    opt.RoleOptions.ShapeshifterCooldown = Options.BountyTargetChangeTime.GetFloat();
                    if (main.BountyMeetingCheck)
                    {//会議後のキルクール
                        opt.KillCooldown = Options.BHDefaultKillCooldown.GetFloat() * 2;
                    }
                    else
                    {
                        if (!main.isBountyKillSuccess)
                        {//ターゲット以外をキルした時の処理
                            opt.KillCooldown = Options.BountyFailureKillCooldown.GetFloat();
                            Logger.info("ターゲット以外をキル");
                        }
                        if (!main.BountyTimerCheck)
                        {//ゼロって書いてあるけど実際はキルクールはそのまま維持されるので大丈夫
                            opt.KillCooldown = 10;
                            Logger.info("ターゲットリセット");
                        }
                        if (main.isBountyKillSuccess)
                        {//ターゲットをキルした時の処理
                            opt.KillCooldown = Options.BountySuccessKillCooldown.GetFloat() * 2;
                            Logger.info("ターゲットをキル");
                        }
                    }
                    break;
                case CustomRoles.Shapeshifter:
                case CustomRoles.Mafia:
                    opt.RoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    goto DefaultKillcooldown;
                case CustomRoles.Impostor:
                case CustomRoles.Witch:
                    goto DefaultKillcooldown;
                case CustomRoles.EvilWatcher:
                case CustomRoles.NiceWatcher:
                    if (opt.AnonymousVotes)
                        opt.AnonymousVotes = false;
                    break;
                case CustomRoles.Sheriff:
                    opt.KillCooldown = Options.SheriffKillCooldown.GetFloat();
                    opt.ImpostorLightMod = opt.CrewLightMod;
                    var switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (switchSystem != null && switchSystem.IsActive)
                    {
                        opt.ImpostorLightMod /= 5;
                    }
                    break;
                case CustomRoles.Arsonist:
                    opt.ImpostorLightMod = opt.CrewLightMod;
                    var switchSystema = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (switchSystema != null && switchSystema.IsActive)
                    {
                        opt.ImpostorLightMod /= 5;
                    }
                    if (!main.ArsonistKillCooldownCheck) opt.KillCooldown = Options.ArsonistCooldown.GetFloat() * 2;
                    if (main.ArsonistKillCooldownCheck) opt.KillCooldown = 10f;
                    break;
                case CustomRoles.Lighter:
                    if (player.getPlayerTaskState().isTaskFinished)
                    {
                        opt.CrewLightMod = opt.ImpostorLightMod;
                        var li = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        if (li != null && li.IsActive)
                        {
                            opt.CrewLightMod *= 5;
                        }
                    }
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
                        }
                    }
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    opt.CrewLightMod = opt.ImpostorLightMod;
                    switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (switchSystem != null && switchSystem.IsActive)
                    {
                        opt.CrewLightMod *= 5;
                    }
                    break;


                InfinityVent:
                    opt.RoleOptions.EngineerCooldown = 0;
                    opt.RoleOptions.EngineerInVentMaxTime = 0;
                    break;
                DefaultKillcooldown:
                    opt.KillCooldown = Options.BHDefaultKillCooldown.GetFloat();
                    break;
            }
            CustomRoles role = player.getCustomRole();
            RoleType roleType = role.getRoleType();
            switch (roleType)
            {
                case RoleType.Impostor:
                    if (player.isLastImpostor())
                    {
                        if (Options.LastImpostorKillCooldown.GetFloat() > 0)
                        {
                            opt.KillCooldown = Options.LastImpostorKillCooldown.GetFloat();
                        }
                        else
                            opt.KillCooldown = 0.01f;
                    }
                    break;
                case RoleType.Madmate:
                    if (Options.MadmateHasImpostorVision.GetBool())
                    {
                        opt.CrewLightMod = opt.ImpostorLightMod;
                        var switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        if (switchSystem != null && switchSystem.IsActive)
                        {
                            opt.CrewLightMod *= 5;
                        }
                    }
                    break;
            }
            if (main.SpeedBoostTarget.ContainsValue(player.PlayerId))
            {
                opt.PlayerSpeedMod = Options.SpeedBoosterUpSpeed.GetFloat();
            }
            if (player.Data.IsDead && opt.AnonymousVotes)
                opt.AnonymousVotes = false;
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetSelection() <= Options.UsedButtonCount)
                opt.EmergencyCooldown = 3600;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.ImpostorLightMod = 0f;
            }

            if (player.AmOwner) PlayerControl.GameOptions = opt;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SyncSettings, SendOption.Reliable, clientId);
            writer.WriteBytesAndSize(opt.ToBytes(5));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static TaskState getPlayerTaskState(this PlayerControl player)
        {
            if (player == null || player.Data == null || player.Data.Tasks == null) return new TaskState();
            if (!Utils.hasTasks(player.Data, false)) return new TaskState();
            int AllTasksCount = 0;
            int CompletedTaskCount = 0;
            foreach (var task in player.Data.Tasks)
            {
                AllTasksCount++;
                if (task.Complete) CompletedTaskCount++;
            }
            //役職ごとにタスク量の調整を行う
            var adjustedTasksCount = AllTasksCount;
            switch (player.getCustomRole())
            {
                case CustomRoles.MadSnitch:
                    adjustedTasksCount = Options.MadSnitchTasks.GetSelection();
                    break;
                default:
                    break;
            }
            //タスク数が通常タスクより多い場合は再設定が必要
            AllTasksCount = Math.Min(adjustedTasksCount, AllTasksCount);
            //調整後のタスク量までしか表示しない
            CompletedTaskCount = Math.Min(AllTasksCount, CompletedTaskCount);
            Logger.info($"{player.name}: {CompletedTaskCount}/{AllTasksCount}", "TaskCounts");
            return new TaskState(AllTasksCount, CompletedTaskCount);
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
            if (player.CurrentOutfitType == PlayerOutfitType.Shapeshifted && isMeeting == false)
            {
                return player.Data.Outfits[PlayerOutfitType.Shapeshifted].PlayerName;
            }

            if (!main.RealNames.TryGetValue(player.PlayerId, out var RealName))
            {
                RealName = player.name;
                if (RealName == "Player(Clone)") return RealName;
                main.RealNames[player.PlayerId] = RealName;
                TownOfHost.Logger.warn("プレイヤー" + player.PlayerId + "のRealNameが見つからなかったため、" + RealName + "を代入しました");
            }
            return RealName;
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
                Logger.error("バウンティ―ハンターのターゲットの指定に失敗しました:ターゲット候補が存在しません");
                return null;
            }
            var target = cTargets[rand.Next(0, cTargets.Count - 1)];
            main.BountyTargets[player.PlayerId] = target;
            Logger.info($"プレイヤー{player.name}のターゲットを{target.name}に変更");

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
        public static bool CanUseKillButton(this PlayerControl pc)
        {
            bool canUse =
                pc.getCustomRole().isImpostor() ||
                pc.isSheriff() ||
                pc.isArsonist();

            if (pc.isMafia())
            {
                if (main.AliveImpostorCount > 1) canUse = false;
            }
            return canUse;
        }
        public static bool isLastImpostor(this PlayerControl pc)
        { //キルクールを変更するインポスター役職は省く
            if (pc.getCustomRole().isImpostor() &&
                !pc.Data.IsDead &&
                Options.EnableLastImpostor.GetBool() &&
                !pc.isVampire() &&
                !pc.isBountyHunter() &&
                !pc.isSerialKiller() &&
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
        public static void ExiledSchrodingerCatTeamChange(this PlayerControl player)
        {
            var rand = new System.Random();
            System.Collections.Generic.List<CustomRoles> RandSchrodinger = new System.Collections.Generic.List<CustomRoles>();
            RandSchrodinger.Add(CustomRoles.CSchrodingerCat);
            RandSchrodinger.Add(CustomRoles.MSchrodingerCat);
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (CustomRoles.Egoist.isEnable() && (pc.isEgoist() && !pc.Data.IsDead))
                {
                    RandSchrodinger.Add(CustomRoles.EgoSchrodingerCat);
                }
            var SchrodingerTeam = RandSchrodinger[rand.Next(RandSchrodinger.Count)];
            player.RpcSetCustomRole(SchrodingerTeam);
        }
        public static bool isCrewmate(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Crewmate; }
        public static bool isEngineer(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Engineer; }
        public static bool isScientist(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Scientist; }
        public static bool isGurdianAngel(this PlayerControl target) { return target.getCustomRole() == CustomRoles.GuardianAngel; }
        public static bool isImpostor(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Impostor; }
        public static bool isShapeshifter(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Shapeshifter; }
        public static bool isWatcher(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Watcher; }
        public static bool isEvilWatcher(this PlayerControl target) { return target.getCustomRole() == CustomRoles.EvilWatcher; }
        public static bool isNiceWatcher(this PlayerControl target) { return target.getCustomRole() == CustomRoles.NiceWatcher; }
        public static bool isJester(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Jester; }
        public static bool isMadmate(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Madmate; }
        public static bool isSKMadmate(this PlayerControl target) { return target.getCustomRole() == CustomRoles.SKMadmate; }
        public static bool isBait(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Bait; }
        public static bool isTerrorist(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Terrorist; }
        public static bool isMafia(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Mafia; }
        public static bool isVampire(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Vampire; }
        public static bool isSabotageMaster(this PlayerControl target) { return target.getCustomRole() == CustomRoles.SabotageMaster; }
        public static bool isMadGuardian(this PlayerControl target) { return target.getCustomRole() == CustomRoles.MadGuardian; }
        public static bool isMadSnitch(this PlayerControl target) { return target.getCustomRole() == CustomRoles.MadSnitch; }
        public static bool isMayor(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Mayor; }
        public static bool isOpportunist(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Opportunist; }
        public static bool isSnitch(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Snitch; }
        public static bool isSheriff(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Sheriff; }
        public static bool isBountyHunter(this PlayerControl target) { return target.getCustomRole() == CustomRoles.BountyHunter; }
        public static bool isWitch(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Witch; }
        public static bool isShapeMaster(this PlayerControl target) { return target.getCustomRole() == CustomRoles.ShapeMaster; }
        public static bool isWarlock(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Warlock; }
        public static bool isSerialKiller(this PlayerControl target) { return target.getCustomRole() == CustomRoles.SerialKiller; }
        public static bool isArsonist(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Arsonist; }
        public static bool isSpeedBooster(this PlayerControl target) { return target.getCustomRole() == CustomRoles.SpeedBooster; }
        public static bool isSchrodingerCat(this PlayerControl target) { return target.getCustomRole() == CustomRoles.SchrodingerCat; }
        public static bool isCSchrodingerCat(this PlayerControl target) { return target.getCustomRole() == CustomRoles.CSchrodingerCat; }
        public static bool isMSchrodingerCat(this PlayerControl target) { return target.getCustomRole() == CustomRoles.MSchrodingerCat; }
        public static bool isEgoSchrodingerCat(this PlayerControl target) { return target.getCustomRole() == CustomRoles.EgoSchrodingerCat; }
        public static bool isEgoist(this PlayerControl target) { return target.getCustomRole() == CustomRoles.Egoist; }
    }
}
