using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using InnerNet;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    static class ExtendedPlayerControl
    {
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
        {
            if (role < CustomRoles.NoSubRoleAssigned)
            {
                Main.AllPlayerCustomRoles[player.PlayerId] = role;
            }
            else if (role >= CustomRoles.NoSubRoleAssigned)   //500:NoSubRole 501~:SubRole
            {
                Main.AllPlayerCustomSubRoles[player.PlayerId] = role;
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
            Main.AllPlayerCustomRoles[player.PlayerId] = role;
        }

        public static void RpcExile(this PlayerControl player)
        {
            RPC.ExileAsync(player);
        }
        public static InnerNet.ClientData GetClient(this PlayerControl player)
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        public static int GetClientId(this PlayerControl player)
        {
            var client = player.GetClient();
            return client == null ? -1 : client.Id;
        }
        public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
        {
            return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
        }

        public static CustomRoles GetCustomRole(this PlayerControl player)
        {
            var cRole = CustomRoles.Crewmate;
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
                return cRole;
            }
            var cRoleFound = Main.AllPlayerCustomRoles.TryGetValue(player.PlayerId, out cRole);
            return cRoleFound || player.Data.Role == null
                ? cRole
                : player.Data.Role.Role switch
                {
                    RoleTypes.Crewmate => CustomRoles.Crewmate,
                    RoleTypes.Engineer => CustomRoles.Engineer,
                    RoleTypes.Scientist => CustomRoles.Scientist,
                    RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                    RoleTypes.Impostor => CustomRoles.Impostor,
                    RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                    _ => CustomRoles.Crewmate,
                };
        }

        public static CustomRoles GetCustomSubRole(this PlayerControl player)
        {
            if (player == null)
            {
                Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
                return CustomRoles.NoSubRoleAssigned;
            }
            var cRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(player.PlayerId, out var cRole);
            return cRoleFound ? cRole : CustomRoles.NoSubRoleAssigned;
        }
        public static void RpcSetNameEx(this PlayerControl player, string name)
        {
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            }
            HudManagerPatch.LastSetNameDesyncCount++;

            Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
            player.RpcSetName(name);
        }

        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;
            if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
            {
                //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
                return;
            }
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            HudManagerPatch.LastSetNameDesyncCount++;
            Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");

            var clientId = seer.GetClientId();
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
            var clientId = seer.GetClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
            writer.Write((ushort)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (target == null) target = killer;
            // Host
            killer.ProtectPlayer(target, colorId);
            killer.MurderPlayer(target);
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var sender = CustomRpcSender.Create("GuardAndKill Sender", SendOption.Reliable);
                sender.StartMessage(killer.GetClientId());
                sender.StartRpc(killer.NetId, (byte)RpcCalls.ProtectPlayer)
                    .WriteNetObject((InnerNetObject)target)
                    .Write(colorId)
                    .EndRpc();
                sender.StartRpc(killer.NetId, (byte)RpcCalls.MurderPlayer)
                    .WriteNetObject((InnerNetObject)target)
                    .EndRpc();
                sender.EndMessage();
                sender.SendMessage();
            }
            Main.BlockKilling[killer.PlayerId] = false;
        }
        public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (AmongUsClient.Instance.AmClient)
            {
                killer.ProtectPlayer(target, colorId);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(colorId);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcResetAbilityCooldown(this PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
            Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
            if (PlayerControl.LocalPlayer == target)
            {
                //targetがホストだった場合
                PlayerControl.LocalPlayer.Data.Role.SetCooldown();
            }
            else
            {
                //targetがホスト以外だった場合
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                writer.Write(0); //writer.WriteNetObject(null); と同じ
                writer.Write(0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            /*  nullにバリアを張ろうとすると、アビリティーのクールダウンがリセットされてからnull参照で中断されます。
                ホストに対しての場合、RPCを介さず直接クールダウンを書き換えています。
                万が一他クライアントへの影響があった場合を考慮して、Desyncを使っています。*/
        }
        public static byte GetRoleCount(this Dictionary<CustomRoles, byte> dic, CustomRoles role)
        {
            if (!dic.ContainsKey(role))
            {
                dic[role] = 0;
            }

            return dic[role];
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
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            }

            var clientId = player.GetClientId();
            var opt = Main.RealOptionsData.DeepCopy();

            CustomRoles role = player.GetCustomRole();
            RoleType roleType = role.GetRoleType();
            switch (roleType)
            {
                case RoleType.Impostor:
                    opt.RoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case RoleType.Madmate:
                    opt.RoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    opt.RoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(player, true);
                    break;
            }

            switch (player.GetCustomRole())
            {
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                // case CustomRoles.ShapeMaster:
                //     opt.RoleOptions.ShapeshifterCooldown = 0.1f;
                //     opt.RoleOptions.ShapeshifterLeaveSkin = false;
                //     opt.RoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                //     break;
                case CustomRoles.Warlock:
                    opt.RoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyGameOptions(opt);
                    break;
                case CustomRoles.BountyHunter:
                    opt.RoleOptions.ShapeshifterCooldown = Options.BountyTargetChangeTime.GetFloat();
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
                    if (player.GetPlayerTaskState().IsTaskFinished)
                    {
                        opt.CrewLightMod = Options.LighterTaskCompletedVision.GetFloat();
                        if (Utils.IsActive(SystemTypes.Electrical) && Options.LighterTaskCompletedDisableLightOut.GetBool())
                            opt.CrewLightMod *= 5;
                    }
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
                        if (player.GetPlayerTaskState().IsTaskFinished)
                        {
                            if (!Main.SpeedBoostTarget.ContainsKey(player.PlayerId))
                            {
                                var rand = new System.Random();
                                List<PlayerControl> targetplayers = new();
                                //切断者と死亡者を除外
                                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                                {
                                    if (!p.Data.Disconnected && !p.Data.IsDead && !Main.SpeedBoostTarget.ContainsValue(p.PlayerId)) targetplayers.Add(p);
                                }
                                //ターゲットが0ならアップ先をプレイヤーをnullに
                                if (targetplayers.Count >= 1)
                                {
                                    PlayerControl target = targetplayers[rand.Next(0, targetplayers.Count)];
                                    Logger.Info("スピードブースト先:" + target.cosmetics.nameText.text, "SpeedBooster");
                                    Main.SpeedBoostTarget.Add(player.PlayerId, target.PlayerId);
                                }
                                else
                                {
                                    Main.SpeedBoostTarget.Add(player.PlayerId, 255);
                                    Logger.SendInGame(GetString("Error.SpeedBoosterNullException"));
                                    Logger.Warn("スピードブースト先がnullです。", "SpeedBooster");
                                }
                            }
                            if (Main.SpeedBoostTarget.ContainsValue(player.PlayerId))
                                Main.AllPlayerSpeed[player.PlayerId] = Options.SpeedBoosterUpSpeed.GetFloat();
                        }
                    }
                    break;
                case CustomRoles.Mayor:
                    opt.RoleOptions.EngineerCooldown =
                        Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt()
                        ? opt.EmergencyCooldown
                        : 300f;
                    opt.RoleOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(opt, player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.JSchrodingerCat:
                    opt.SetVision(player, Options.JackalHasImpostorVision.GetBool());
                    break;


                InfinityVent:
                    opt.RoleOptions.EngineerCooldown = 0;
                    opt.RoleOptions.EngineerInVentMaxTime = 0;
                    break;
            }
            if (Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
            {
                foreach (var kc in Main.AllPlayerKillCooldown)
                {
                    if (kc.Key == player.PlayerId)
                        opt.KillCooldown = kc.Value > 0 ? kc.Value : 0.01f;
                }
            }
            if (Main.AllPlayerSpeed.ContainsKey(player.PlayerId))
            {
                foreach (var speed in Main.AllPlayerSpeed)
                {
                    if (speed.Key == player.PlayerId)
                        opt.PlayerSpeedMod = Mathf.Clamp(speed.Value, 0.0001f, 3f);
                }
            }
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead && opt.AnonymousVotes)
                opt.AnonymousVotes = false;
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetSelection() <= Options.UsedButtonCount)
                opt.EmergencyCooldown = 3600;
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.ImpostorLightMod = 0f;
                if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Egoist)) opt.PlayerSpeedMod = 0.0001f;
            }
            opt.DiscussionTime = Mathf.Clamp(Main.DiscussionTime, 0, 300);
            opt.VotingTime = Mathf.Clamp(Main.VotingTime, TimeThief.LowerLimitVotingTime.GetInt(), 300);

            opt.RoleOptions.ShapeshifterCooldown = Mathf.Max(1f, opt.RoleOptions.ShapeshifterCooldown);

            if (player.AmOwner) PlayerControl.GameOptions = opt;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SyncSettings, SendOption.Reliable, clientId);
            writer.WriteBytesAndSize(opt.ToBytes(5));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static TaskState GetPlayerTaskState(this PlayerControl player)
        {
            return PlayerState.taskState[player.PlayerId];
        }

        public static GameOptionsData DeepCopy(this GameOptionsData opt)
        {
            var optByte = opt.ToBytes(5);
            return GameOptionsData.FromBytes(optByte);
        }

        public static string GetRoleName(this PlayerControl player)
        {
            return $"{Utils.GetRoleName(player.GetCustomRole())}" /*({getString("Last")})"*/;
        }
        public static string GetSubRoleName(this PlayerControl player)
        {
            return $"{Utils.GetRoleName(player.GetCustomSubRole())}";
        }
        public static string GetAllRoleName(this PlayerControl player)
        {
            if (!player) return null;
            var text = player.GetRoleName();
            text += player.GetCustomSubRole() != CustomRoles.NoSubRoleAssigned ? $" + {player.GetSubRoleName()}" : "";
            return text;
        }
        public static string GetNameWithRole(this PlayerControl player)
        {
            return $"{player?.Data?.PlayerName}({player?.GetAllRoleName()})";
        }
        public static string GetRoleColorCode(this PlayerControl player)
        {
            return Utils.GetRoleColorCode(player.GetCustomRole());
        }
        public static Color GetRoleColor(this PlayerControl player)
        {
            return Utils.GetRoleColor(player.GetCustomRole());
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;
            int clientId = pc.GetClientId();

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

        public static string GetRealName(this PlayerControl player, bool isMeeting = false)
        {
            return isMeeting ? player?.Data?.PlayerName : player?.name;
        }

        public static PlayerControl GetBountyTarget(this PlayerControl player)
        {
            if (player == null) return null;
            if (Main.BountyTargets == null) Main.BountyTargets = new Dictionary<byte, PlayerControl>();

            if (!Main.BountyTargets.TryGetValue(player.PlayerId, out var target))
            {
                target = player.ResetBountyTarget();
            }
            return target;
        }
        public static PlayerControl ResetBountyTarget(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost/* && AmongUsClient.Instance.GameMode != GameModes.FreePlay*/) return null;
            List<PlayerControl> cTargets = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                // 死者/切断者/インポスターを除外
                if (!pc.Data.IsDead &&
                    !pc.Data.Disconnected &&
                    !pc.GetCustomRole().IsImpostor()
                )
                {
                    cTargets.Add(pc);
                }
            }
            if (cTargets.Count >= 2 && Main.BountyTargets.TryGetValue(player.PlayerId, out var p)) cTargets.RemoveAll(x => x.PlayerId == p.PlayerId);

            var rand = new System.Random();
            if (cTargets.Count <= 0)
            {
                Logger.Error("ターゲットの指定に失敗しました:ターゲット候補が存在しません", "BountyHunter");
                return null;
            }
            var target = cTargets[rand.Next(0, cTargets.Count)];
            Main.BountyTargets[player.PlayerId] = target;
            Logger.Info($"プレイヤー{player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に変更", "BountyHunter");

            //RPCによる同期
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBountyTarget, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(target.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return target;
        }
        public static bool IsSpellMode(this PlayerControl player)
        {
            if (!Main.KillOrSpell.TryGetValue(player.PlayerId, out var KillOrSpell))
            {
                Main.KillOrSpell[player.PlayerId] = false;
                KillOrSpell = false;
            }
            return KillOrSpell;
        }
        public static void SyncKillOrSpell(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(player.IsSpellMode());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static bool CanUseKillButton(this PlayerControl pc)
        {
            bool canUse =
                pc.GetCustomRole().IsImpostor() ||
                pc.Is(CustomRoles.Arsonist);

            return pc.GetCustomRole() switch
            {
                CustomRoles.Mafia => Utils.CanMafiaKill() && canUse,
                CustomRoles.Mare => Utils.IsActive(SystemTypes.Electrical),
                CustomRoles.FireWorks => FireWorks.CanUseKillButton(pc),
                CustomRoles.Sniper => Sniper.CanUseKillButton(pc),
                CustomRoles.Sheriff => Sheriff.CanUseKillButton(pc),
                _ => canUse,
            };
        }
        public static bool IsLastImpostor(this PlayerControl pc)
        { //キルクールを変更するインポスター役職は省く
            return pc.GetCustomRole().IsImpostor() &&
                !pc.Data.IsDead &&
                Options.CurrentGameMode != CustomGameMode.HideAndSeek &&
                Options.EnableLastImpostor.GetBool() &&
                !pc.Is(CustomRoles.Vampire) &&
                !pc.Is(CustomRoles.BountyHunter) &&
                !pc.Is(CustomRoles.SerialKiller) &&
                Main.AliveImpostorCount == 1;
        }
        public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
        {
            if (arsonist == null) return false;
            if (target == null) return false;
            if (Main.isDoused == null) return false;
            Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
            return isDoused;
        }
        public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
            writer.Write(player.PlayerId);
            writer.Write(target.PlayerId);
            writer.Write(isDoused);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ExiledSchrodingerCatTeamChange(this PlayerControl player)
        {
            var rand = new System.Random();
            List<CustomRoles> RandSchrodinger = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.MSchrodingerCat
            };
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (CustomRoles.Egoist.IsEnable() && pc.Is(CustomRoles.Egoist) && !pc.Data.IsDead)
                    RandSchrodinger.Add(CustomRoles.EgoSchrodingerCat);

                if (CustomRoles.Jackal.IsEnable() && pc.Is(CustomRoles.Jackal) && !pc.Data.IsDead)
                    RandSchrodinger.Add(CustomRoles.JSchrodingerCat);
            }
            var SchrodingerTeam = RandSchrodinger[rand.Next(RandSchrodinger.Count)];
            player.RpcSetCustomRole(SchrodingerTeam);
        }
        public static void ResetKillCooldown(this PlayerControl player)
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
            switch (player.GetCustomRole())
            {
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                    break;
                case CustomRoles.Arsonist:
                    Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.SetKillCooldown(player.PlayerId); //シェリフはシェリフのキルクールに。
                    break;
                case CustomRoles.TimeThief:
                    TimeThief.SetKillCooldown(player.PlayerId); //タイムシーフはタイムシーフのキルクールに。
                    break;
                case CustomRoles.Mare:
                    Mare.SetKillCooldown(player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                    Main.AllPlayerKillCooldown[player.PlayerId] = Options.JackalKillCooldown.GetFloat();
                    break;
            }
            if (player.IsLastImpostor())
                Main.AllPlayerKillCooldown[player.PlayerId] = Options.LastImpostorKillCooldown.GetFloat();
        }
        public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
        {
            Logger.Info($"{target?.Data?.PlayerName}はTrapperだった", "Trapper");
            Main.AllPlayerSpeed[killer.PlayerId] = 0.00001f;
            killer.CustomSyncSettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.RealOptionsData.PlayerSpeedMod;
                killer.CustomSyncSettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        public static void CanUseImpostorVent(this PlayerControl player)
        {
            switch (player.GetCustomRole())
            {
                case CustomRoles.Sheriff:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(false);
                    player.Data.Role.CanVent = false;
                    return;
                case CustomRoles.Arsonist:
                    bool CanUse = player.IsDouseDone();
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(CanUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = CanUse;
                    return;
                case CustomRoles.Jackal:
                    bool jackal_canUse = Options.JackalCanVent.GetBool();
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(jackal_canUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = jackal_canUse;
                    return;
            }
        }
        public static bool IsDouseDone(this PlayerControl player)
        {
            if (!player.Is(CustomRoles.Arsonist)) return false;
            var count = Utils.GetDousedPlayerCount(player.PlayerId);
            return count.Item1 == count.Item2;
        }
        public static bool CanMakeMadmate(this PlayerControl player)
        {
            return Options.CanMakeMadmateCount.GetInt() > Main.SKMadmateNowCount
                    && player != null
                    && player.Data.Role.Role == RoleTypes.Shapeshifter
                    && !player.Is(CustomRoles.Warlock) && !player.Is(CustomRoles.FireWorks) && !player.Is(CustomRoles.Sniper) && !player.Is(CustomRoles.BountyHunter);
        }
        public static void RpcExileV2(this PlayerControl player)
        {
            player.Exiled();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            Utils.NotifyRoles();
            Main.BlockKilling[killer.PlayerId] = false;
        }
        public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target)
        { /*サボタージュ中でも関係なしに会議を起こせるメソッド
            targetがnullの場合はボタンとなる*/
            MeetingRoomManager.Instance.AssignSelf(reporter, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
            reporter.RpcStartMeeting(target);
        }
        public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
        ///</summary>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
        ///</summary>
        ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
        {
            var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
            List<PlayerControl> rangePlayers = new();
            player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
            foreach (var pc in rangePlayersIL)
            {
                if (predicate(pc)) rangePlayers.Add(pc);
            }
            return rangePlayers;
        }
        public static bool IsNeutralKiller(this PlayerControl player)
        {
            return
                player.GetCustomRole() is
                CustomRoles.Egoist or
                CustomRoles.Jackal;
        }

        //汎用
        public static bool Is(this PlayerControl target, CustomRoles role) =>
            role > CustomRoles.NoSubRoleAssigned ? target.GetCustomSubRole() == role : target.GetCustomRole() == role;
        public static bool Is(this PlayerControl target, RoleType type) { return target.GetCustomRole().GetRoleType() == type; }
        public static bool IsAlive(this PlayerControl target) { return target != null && !PlayerState.isDead[target.PlayerId]; }

    }
}