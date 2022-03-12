using System.Collections.Generic;
using Hazel;
using System;
using System.Linq;
using InnerNet;

namespace TownOfHost {
    static class ExtendedPlayerControl {
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role) {
            main.AllPlayerCustomRoles[player.PlayerId] = role;
            if(AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role) {
            if(AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void SetCustomRole(this PlayerControl player, CustomRoles role) {
            main.AllPlayerCustomRoles[player.PlayerId] = role;
        }

        public static void RpcExile(this PlayerControl player) {
            main.ExileAsync(player);
        }
        public static InnerNet.ClientData getClient(this PlayerControl player) {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        public static int getClientId(this PlayerControl player) {
            var client = player.getClient();
            if(client == null) return -1;
            return client.Id;
        }
        public static CustomRoles getCustomRole(this GameData.PlayerInfo player)
        {
            if(player == null || player.Object == null) return CustomRoles.Crewmate;
            return player.Object.getCustomRole();
        }

        public static CustomRoles getCustomRole(this PlayerControl player) {
            if(player == null) {
                Logger.warn("CustomRoleを取得しようとしましたが、対象がnullでした。");
                return CustomRoles.Crewmate;
            }
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(player.PlayerId, out var cRole);
            if(!cRoleFound)
            {
                switch(player.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        cRole = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Engineer:
                        cRole = CustomRoles.Engineer;
                        break;
                    case RoleTypes.Scientist:
                        cRole = CustomRoles.Scientist;
                        break;
                    case RoleTypes.GuardianAngel:
                        cRole = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Impostor:
                        cRole = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Shapeshifter:
                        cRole = CustomRoles.Shapeshifter;
                        break;
                    default:
                        cRole = CustomRoles.Crewmate;
                        break;
                }
            }
            return cRole;
        }


        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null) {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if(player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if(seer == null) seer = player;
            //Logger.info($"{player.name}:{name} => {seer.name}");
            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.Reliable, clientId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, PlayerControl seer = null) {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー

            if(player == null) return;
            if(seer == null) seer = player;
            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
            writer.Write((ushort) role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null) {
            if(target == null) target = killer;
            killer.RpcProtectPlayer(target, 0);
            new LateTask(() => {
                if(target.protectedByGuardian)
                    killer.RpcMurderPlayer(target);
            }, 0.5f, "GuardAndKill");
        }

        public static byte GetRoleCount(this Dictionary<CustomRoles, byte> dic, CustomRoles role) {
            if(!dic.ContainsKey(role))
                dic[role] = 0;
            return dic[role];
        }

        public static bool canBeKilledBySheriff(this PlayerControl player) {
            var cRole = player.getCustomRole();
            switch(cRole) {
                case CustomRoles.Jester:
                    return main.SheriffCanKillJester;
                case CustomRoles.Terrorist:
                    return main.SheriffCanKillTerrorist;
                case CustomRoles.Opportunist:
                    return main.SheriffCanKillOpportunist;
            }
            CustomRoles role = player.getCustomRole();
            IntroTypes introType = role.GetIntroType();
            switch(introType) {
                case IntroTypes.Impostor:
                    return true;
                case IntroTypes.Madmate:
                    return main.SheriffCanKillMadmate;
            }
            return false;
        }

        public static void SendDM(this PlayerControl target, string text) {
            main.SendMessage(text, target.PlayerId);
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

            RPCProcedure.BeKilled(player.PlayerId, KilledById);
        }*/
        public static void CustomSyncSettings(this PlayerControl player) {
            if(player == null || !AmongUsClient.Instance.AmHost) return;
            if(main.RealOptionsData == null)
                main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            var clientId = player.getClientId();
            var opt = main.RealOptionsData.DeepCopy();

            switch(player.getCustomRole()) {
                case CustomRoles.Madmate:
                    if(main.MadmateVisionAsImpostor){
                        opt.CrewLightMod = opt.ImpostorLightMod;
                        var mm = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        if(mm != null && mm.IsActive) {
                            opt.CrewLightMod *= 5;
                        }
                    }
                    goto InfinityVent;
                case CustomRoles.MadGuardian:
                case CustomRoles.SKMadmate:
                case CustomRoles.MadSnitch:
                    goto MadmateVision;
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                case CustomRoles.ShapeMaster:
                    opt.RoleOptions.ShapeshifterCooldown = 0.1f;
                    opt.RoleOptions.ShapeshifterDuration = main.ShapeMasterShapeshiftDuration;
                    opt.RoleOptions.ShapeshifterLeaveSkin = false;
                    goto DefaultKillcooldown;
                case CustomRoles.Vampire:
                    if(main.BountyHunterCount > 0){
                        if(main.BountyMeetingCheck)opt.KillCooldown = main.BHDefaultKillCooldown;
                        if(!main.BountyMeetingCheck)opt.KillCooldown = main.BHDefaultKillCooldown*2;
                    }
                    if(main.RefixCooldownDelay <= 0){
                        opt.KillCooldown *= 2;
                    }
                    break;
                case CustomRoles.Warlock:
                    if(main.BountyHunterCount == 0){
                        opt.RoleOptions.ShapeshifterCooldown = opt.KillCooldown;
                        opt.KillCooldown *= 2;
                    }
                    if(main.BountyHunterCount > 0){
                        opt.RoleOptions.ShapeshifterCooldown = main.BHDefaultKillCooldown;
                        opt.KillCooldown = main.BHDefaultKillCooldown*2;
                    }
                    break;
                case CustomRoles.SerialKiller:
                    opt.RoleOptions.ShapeshifterCooldown = main.SerialKillerLimit;
                    opt.KillCooldown = main.SerialKillerCooldown*2;
                    if(main.BountyHunterCount > 0)opt.KillCooldown = opt.KillCooldown = main.SerialKillerCooldown*2;
                    break;
                case CustomRoles.BountyHunter:
                    opt.RoleOptions.ShapeshifterCooldown = main.BountyTargetChangeTime;
                    if(main.BountyMeetingCheck){//会議後のキルクール
                        opt.KillCooldown = main.BHDefaultKillCooldown*2;
                    }
                    else{
                        if(!main.isBountyKillSuccess){//ターゲット以外をキルした時の処理
                            opt.KillCooldown = main.BountyFailureKillCooldown;
                            Logger.info("ターゲット以外をキル");
                        }
                        if(!main.BountyTimerCheck){//ゼロって書いてあるけど実際はキルクールはそのまま維持されるので大丈夫
                            opt.KillCooldown = 0;
                            Logger.info("ターゲットリセット");
                        }
                        if(main.isBountyKillSuccess){//ターゲットをキルした時の処理
                            opt.KillCooldown = main.BountySuccessKillCooldown*2;
                            Logger.info("ターゲットをキル");
                        }
                    }
                    break;
                case CustomRoles.Impostor:
                case CustomRoles.Shapeshifter:
                case CustomRoles.Mafia:
                case CustomRoles.Witch:
                    goto DefaultKillcooldown;
                case CustomRoles.Sheriff:
                    opt.KillCooldown = main.SheriffKillCooldown;
                    opt.ImpostorLightMod = opt.CrewLightMod;
                    var switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if(switchSystem != null && switchSystem.IsActive) {
                        opt.ImpostorLightMod /= 5;
                    }
                    goto DefaultKillcooldown;


                InfinityVent:
                    opt.RoleOptions.EngineerCooldown = 0;
                    opt.RoleOptions.EngineerInVentMaxTime = 0;
                    break;
                DefaultKillcooldown:
                    if(main.BountyHunterCount > 0){
                        opt.KillCooldown = main.BHDefaultKillCooldown;
                    }
                    break;
                MadmateVision://マッドメイトの視野をインポスターと同じにする処理
                    if(main.MadmateVisionAsImpostor){
                        opt.CrewLightMod = opt.ImpostorLightMod;
                        var sm = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                        if(sm != null && sm.IsActive) {
                            opt.CrewLightMod *= 5;
                        }
                    }
                    break;
            }
            CustomRoles role = player.getCustomRole();
            IntroTypes introType = role.GetIntroType();
            switch(introType) {
                case IntroTypes.Madmate:
                    opt.CrewLightMod = opt.ImpostorLightMod;
                    var switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if(switchSystem != null && switchSystem.IsActive) {
                        opt.CrewLightMod *= 5;
                    }
                    break;
            }
            if(player.Data.IsDead && opt.AnonymousVotes)
                opt.AnonymousVotes = false;
            if(main.SyncButtonMode && main.SyncedButtonCount <= main.UsedButtonCount)
                opt.EmergencyCooldown = 3600;
            if(main.IsHideAndSeek && main.HideAndSeekKillDelayTimer > 0) {
                opt.ImpostorLightMod = 0f;
            }

            if(player.AmOwner) PlayerControl.GameOptions = opt;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SyncSettings, SendOption.Reliable, clientId);
            writer.WriteBytesAndSize(opt.ToBytes(5));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static TaskState getPlayerTaskState(this PlayerControl player) {
            if(player == null || player.Data == null || player.Data.Tasks == null) return new TaskState();
            if(!main.hasTasks(player.Data, false)) return new TaskState();
            int AllTasksCount = 0;
            int CompletedTaskCount = 0;
            foreach(var task in player.Data.Tasks) {
                AllTasksCount++;
                if(task.Complete) CompletedTaskCount++;
            }
            //役職ごとにタスク量の調整を行う
            var adjustedTasksCount = AllTasksCount;
            switch (player.getCustomRole())
            {
                case CustomRoles.MadSnitch:
                    adjustedTasksCount = main.MadSnitchTasks;
                    break;
                default:
                    break;
            }
            //タスク数が通常タスクより多い場合は再設定が必要
            AllTasksCount = Math.Min(adjustedTasksCount, AllTasksCount);
            //調整後のタスク量までしか表示しない
            CompletedTaskCount = Math.Min(AllTasksCount, CompletedTaskCount);
            Logger.info(player.name + ": " + AllTasksCount + ", " + CompletedTaskCount);
            return new TaskState(AllTasksCount, CompletedTaskCount);
        }

        public static GameOptionsData DeepCopy(this GameOptionsData opt) {
            var optByte = opt.ToBytes(5);
            return GameOptionsData.FromBytes(optByte);
        }

        public static string getRoleName(this PlayerControl player) {
            return main.getRoleName(player.getCustomRole());
        }
        public static string getRoleColorCode(this PlayerControl player) {
            return main.getRoleColorCode(player.getCustomRole());
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f) {
            if(pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;
            int clientId = pc.getClientId();

            byte reactorId = 3;
            if(PlayerControl.GameOptions.MapId == 2) reactorId = 21;

            new LateTask(() => {
                MessageWriter SabotageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                SabotageWriter.Write(reactorId);
                MessageExtensions.WriteNetObject(SabotageWriter, pc);
                SabotageWriter.Write((byte)128);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageWriter);
            }, 0f + delay, "Reactor Desync");

            new LateTask(() => {
                MessageWriter MurderWriter = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, clientId);
                MessageExtensions.WriteNetObject(MurderWriter, pc);
                AmongUsClient.Instance.FinishRpcImmediately(MurderWriter);
            }, 0.2f + delay, "Murder To Reset Cam");

            new LateTask(() => {
                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                SabotageFixWriter.Write(reactorId);
                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                SabotageFixWriter.Write((byte)16);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
            }, 0.4f + delay, "Fix Desync Reactor");

            if(PlayerControl.GameOptions.MapId == 4) //Airship用
            new LateTask(() => {
                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                SabotageFixWriter.Write(reactorId);
                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                SabotageFixWriter.Write((byte)17);
                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
            }, 0.4f + delay, "Fix Desync Reactor 2");
        }

        public static string getRealName(this PlayerControl player, bool isMeeting = false) {

            string RealName;
            if(player.CurrentOutfitType == PlayerOutfitType.Shapeshifted && isMeeting == false) {
                return player.Data.Outfits[PlayerOutfitType.Shapeshifted].PlayerName;
            }

            if(!main.RealNames.TryGetValue(player.PlayerId, out RealName)) {
                RealName = player.name;
                if(RealName == "Player(Clone)") return RealName;
                main.RealNames[player.PlayerId] = RealName;
                TownOfHost.Logger.warn("プレイヤー" + player.PlayerId + "のRealNameが見つからなかったため、" + RealName + "を代入しました");
            }
            return RealName;
        }

        public static PlayerControl getBountyTarget(this PlayerControl player) {
            if(player == null) return null;
            if(main.BountyTargets == null) main.BountyTargets = new Dictionary<byte, PlayerControl>();
            PlayerControl target;
            if(!main.BountyTargets.TryGetValue(player.PlayerId, out target)) {
                target = player.ResetBountyTarget();
            }
            return target;
        }
        public static PlayerControl ResetBountyTarget(this PlayerControl player) {
            if(!AmongUsClient.Instance.AmHost/* && AmongUsClient.Instance.GameMode != GameModes.FreePlay*/) return null;
            List<PlayerControl> cTargets = new List<PlayerControl>();
            foreach(var pc in PlayerControl.AllPlayerControls)
                if(!pc.Data.IsDead && //死者を除外
                !pc.Data.Disconnected && //切断者を除外
                !pc.getCustomRole().isImpostor() //インポスターを除外
                ) cTargets.Add(pc);

            var rand = new System.Random();
            if(cTargets.Count <= 0) {
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
        public static bool GetKillOrSpell(this PlayerControl player) {
            bool KillOrSpell;
            if(!main.KillOrSpell.TryGetValue(player.PlayerId, out KillOrSpell)) {
                main.KillOrSpell[player.PlayerId] = false;
                KillOrSpell = false;
            }
            return KillOrSpell;
        }
        public static void SyncKillOrSpell(this PlayerControl player) {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(player.GetKillOrSpell());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static bool isCrewmate(this PlayerControl target){return target.getCustomRole() == CustomRoles.Crewmate;}
        public static bool isEngineer(this PlayerControl target){return target.getCustomRole() == CustomRoles.Engineer;}
        public static bool isScientist(this PlayerControl target){return target.getCustomRole() == CustomRoles.Scientist;}
        public static bool isGurdianAngel(this PlayerControl target){return target.getCustomRole() == CustomRoles.GuardianAngel;}
        public static bool isImpostor(this PlayerControl target){return target.getCustomRole() == CustomRoles.Impostor;}
        public static bool isShapeshifter(this PlayerControl target){return target.getCustomRole() == CustomRoles.Shapeshifter;}
        public static bool isJester(this PlayerControl target){return target.getCustomRole() == CustomRoles.Jester;}
        public static bool isMadmate(this PlayerControl target){return target.getCustomRole() == CustomRoles.Madmate;}
        public static bool isSKMadmate(this PlayerControl target){return target.getCustomRole() == CustomRoles.SKMadmate;}
        public static bool isBait(this PlayerControl target){return target.getCustomRole() == CustomRoles.Bait;}
        public static bool isTerrorist(this PlayerControl target){return target.getCustomRole() == CustomRoles.Terrorist;}
        public static bool isMafia(this PlayerControl target){return target.getCustomRole() == CustomRoles.Mafia;}
        public static bool isVampire(this PlayerControl target){return target.getCustomRole() == CustomRoles.Vampire;}
        public static bool isSabotageMaster(this PlayerControl target){return target.getCustomRole() == CustomRoles.SabotageMaster;}
        public static bool isMadGuardian(this PlayerControl target){return target.getCustomRole() == CustomRoles.MadGuardian;}
        public static bool isMadSnitch(this PlayerControl target){return target.getCustomRole() == CustomRoles.MadSnitch;}
        public static bool isMayor(this PlayerControl target){return target.getCustomRole() == CustomRoles.Mayor;}
        public static bool isOpportunist(this PlayerControl target){return target.getCustomRole() == CustomRoles.Opportunist;}
        public static bool isSnitch(this PlayerControl target){return target.getCustomRole() == CustomRoles.Snitch;}
        public static bool isSheriff(this PlayerControl target){return target.getCustomRole() == CustomRoles.Sheriff;}
        public static bool isBountyHunter(this PlayerControl target){return target.getCustomRole() == CustomRoles.BountyHunter;}
        public static bool isWitch(this PlayerControl target){return target.getCustomRole() == CustomRoles.Witch;}
        public static bool isShapeMaster(this PlayerControl target){return target.getCustomRole() == CustomRoles.ShapeMaster;}
        public static bool isWarlock(this PlayerControl target){return target.getCustomRole() == CustomRoles.Warlock;}
        public static bool isSerialKiller(this PlayerControl target){return target.getCustomRole() == CustomRoles.SerialKiller;}
    }
}
