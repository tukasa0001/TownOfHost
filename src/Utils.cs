using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using TownOfHost.Extensions;
using TownOfHost.Modules;
using TownOfHost.Patches;
using static TownOfHost.Translator;
using TownOfHost.Roles;

using HarmonyLib;
using Hazel;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            //Logger.Info($"SystemTypes:{type}", "IsActive");
            int mapId = Main.NormalOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else if (mapId == 4)
                        {
                            var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                            return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                        }
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId == 1)
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }
                    }
                default:
                    return false;
            }
        }
        public static string GetNameWithRole(this GameData.PlayerInfo player)
        {
            return GetPlayerById(player.PlayerId)?.GetNameWithRole() ?? "";
        }

        //誰かが死亡したときのメソッド
        public static void TargetDies(PlayerControl killer, PlayerControl target)
        {
            if (!target.Data.IsDead || GameStates.IsMeeting) return;
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                if (!KillFlashCheck(killer, target, seer)) continue;
                seer.KillFlash();
            }
        }
        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
        {
            //if (seer.Is(GameMaster)) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;
            return seer.GetCustomRole() switch
            {
                //CustomRoles.EvilTracker => EvilTracker.KillFlashCheck(killer, target),
                Mystic => true,
                _ => seer.Is(Roles.RoleType.Madmate) && StaticOptions.MadmateCanSeeKillFlash,
            };
        }
        public static void KillFlash(this PlayerControl player)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = false; //リアクターフラッシュの確認
            if (Main.NormalOptions.MapId == 2) ReactorCheck = IsActive(SystemTypes.Laboratory);
            else ReactorCheck = IsActive(SystemTypes.Reactor);

            var Duration = StaticOptions.KillFlashDuration;
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            Main.PlayerStates[player.PlayerId].IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx()) OldRPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            PlayerControlExtensions.MarkDirtySettings(player);
            new DTask(() =>
            {
                Main.PlayerStates[player.PlayerId].IsBlackOut = false; //ブラックアウト解除
                PlayerControlExtensions.MarkDirtySettings(player);
            }, StaticOptions.KillFlashDuration, "RemoveKillFlash");
        }
        public static void BlackOut(this IGameOptions opt, bool IsBlackOut)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            if (IsBlackOut)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
            }
            return;
        }
        public static string GetDisplayRoleName(byte playerId)
        {
            var TextData = GetRoleText(Utils.GetPlayerById(playerId));
            return ColorString(TextData.Item2, TextData.Item1);
        }
        public static string GetRoleName(CustomRole role)
        {
            // return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
            return role.RoleName;
        }
        public static string GetDeathReason(PlayerStateOLD.DeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(PlayerStateOLD.DeathReason), status));
        }
        public static Color GetRoleColor(CustomRole role)
        {
            // if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            // ColorUtility.TryParseHtmlString(hexColor, out Color c);
            // return c;
            return role.RoleColor;
        }
        public static string GetRoleColorCode(CustomRole role)
        {
            Color c = role?.RoleColor ?? Color.white;
            return "#" + (c.r * 255).ToString("X2") + (c.g * 255).ToString("X2") + (c.b * 255).ToString("X2") + (c.a * 255).ToString("X2");
        }
        public static (string, Color) GetRoleText(PlayerControl player)
        {
            CustomRole role = player.GetCustomRole();
            return (role.RoleName, role.RoleColor);
        }

        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = Main.PlayerStates[playerId];
            string deathReason = state.IsDead ? GetString("DeathReason." + state.deathReason) : GetString("Alive");
            if (RealKillerColor)
            {
                var KillerId = state.GetRealKiller();
                Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(Doctor.Ref<Doctor>());
                deathReason = ColorString(color, deathReason);
            }
            return deathReason;
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRole hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    // TODO: RECREATE HNS ROLES
                    switch (hRole)
                    {
                        case Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                            //     case HASFox:
                            //       text = "Fox";
                            //        color = Color.magenta;
                            //        break;
                            //    case HASTroll:
                            //        text = "Troll";
                            //         color = Color.green;
                            //       break;
                    }
                    break;
            }
            return (text, color);
        }

        public static bool HasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null || p.Role == null || p.Disconnected) return false;

            return CustomRoleManager.PlayersCustomRolesRedux.TryGetValue(p.PlayerId, out CustomRole? role) && role.HasTasks();
        }
        public static string GetProgressText(PlayerControl pc)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            var taskState = pc.GetPlayerTaskState();
            var Comms = false;
            if (taskState.hasTasks)
            {
                foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                    if (task.TaskType == TaskTypes.FixComms)
                    {
                        Comms = true;
                        break;
                    }
            }
            return GetProgressText(pc.PlayerId, Comms);
        }
        public static string GetProgressText(byte playerId, bool comms = false)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            string ProgressText = "";
            var role = Main.PlayerStates[playerId].MainRole;
            switch (role)
            {
                default:
                    //タスクテキスト
                    var taskState = Main.PlayerStates?[playerId].GetTaskState();
                    if (taskState.hasTasks)
                    {
                        Color TextColor = Color.yellow;
                        var info = GetPlayerInfoById(playerId);
                        var TaskCompleteColor = HasTasks(info) ? Color.green : role.GetReduxRole().RoleColor.ShadeColor(0.5f); //タスク完了後の色
                        var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色
                        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
                        TextColor = comms ? Color.gray : NormalColor;
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
                    }
                    break;
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText += ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $" [{StaticOptions.CanMakeMadmateCount - Main.SKMadmateNowCount}]");

            return ProgressText;
        }
        // GM.Ref<GM>()
        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (Main.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.GetReduxRole().IsEnable()) { SendMessage(GetRoleName(Fox.Ref<Fox>()) + GetString("HASFoxIhmmnfoLong"), PlayerId); }
                if (CustomRoles.HASTroll.GetReduxRole().IsEnable()) { SendMessage(GetRoleName(Troll.Ref<Troll>()) + GetString("HASTrollInfoLong"), PlayerId); }
            }
            else
            {
                if (StaticOptions.DisableDevices) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
                if (StaticOptions.SyncButtonMode) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
                if (StaticOptions.SabotageTimeControl) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
                if (StaticOptions.RandomMapsMode) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
                if (OldOptions.IsStandardHAS) { SendMessage(GetString("StandardHASInfo"), PlayerId); }
                if (StaticOptions.EnableGM == "") { SendMessage(GetRoleName(GM.Ref<GM>()) + GetString("GMInfoLong"), PlayerId); }
                foreach (var role in CustomRoleManager.Roles)
                {
                    if (role is Fox or Troll) continue;
                    if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                }
            }
            if (Main.NoGameEnd) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = Main.NormalOptions.MapId;
            if (OldOptions.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = "";
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                text = GetString("Roles") + ":";
                if (Fox.Ref<Fox>().IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(Fox.Ref<Fox>()), Fox.Ref<Fox>().Count);
                if (Troll.Ref<Troll>().IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(Troll.Ref<Troll>()), Troll.Ref<Troll>().Count);
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += GetString("HideAndSeek");
            }
            else
            {
                text = GetString("Settings") + ":";
                foreach (var role in OldOptions.CustomRoleCounts)
                {
                    if (!role.Key.GetReduxRole().IsEnable()) continue;
                    text += $"\n【{GetRoleName(role.Key.GetReduxRole())}×{role.Key.GetReduxRole().Count}】\n";
                    ShowChildrenSettings(OldOptions.CustomRoleSpawnChances[role.Key], ref text);
                    text = text.RemoveHtmlTags();
                }
                foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(OldOptions.CurrentGameMode)))
                {
                    if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                        text += $"\n【{opt.GetName(true)}: {opt.GetString()}】\n";
                    else
                        text += $"\n【{opt.GetName(true)}】\n";
                    ShowChildrenSettings(opt, ref text);
                    text = text.RemoveHtmlTags();
                }
            }
            SendMessage(text, PlayerId);
        }
        public static void CopyCurrentSettings()
        {
            var text = "";
            if (OldOptions.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            text += $"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━";
            foreach (var role in OldOptions.CustomRoleCounts)
            {
                if (!role.Key.GetReduxRole().IsEnable()) continue;
                text += $"\n【{GetRoleName(role.Key.GetReduxRole())}×{role.Key.GetReduxRole().Count}】\n";
                ShowChildrenSettings(OldOptions.CustomRoleSpawnChances[role.Key], ref text);
                text = text.RemoveHtmlTags();
            }
            text += $"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━";
            foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(OldOptions.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    text += $"\n【{opt.GetName(true)}: {opt.GetString()}】\n";
                else
                    text += $"\n【{opt.GetName(true)}】\n";
                ShowChildrenSettings(opt, ref text);
                text = text.RemoveHtmlTags();
            }
            text += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            ClipboardHelper.PutClipboardString(text);
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            if (OldOptions.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = GetString("Roles") + ":";
            text += string.Format("\n{0}:{1}", GetRoleName(GM.Ref<GM>()), StaticOptions.EnableGM.RemoveHtmlTags());
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.GetReduxRole().IsEnable()) text += string.Format("\n{0}:{1}x{2}", GetRoleName(role.GetReduxRole()), $"{role.GetReduxRole().Chance * 100}%", role.GetReduxRole().Count);
            }
            SendMessage(text, PlayerId);
        }
        public static void Teleport(CustomNetworkTransform nt, Vector2 location)
        {
            if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            NetHelpers.WriteVector2(location, writer);
            writer.Write(nt.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ShowChildrenSettings(OptionItem option, ref string text, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "DisableSkeldDevices" && !OldOptions.IsActiveSkeld) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !OldOptions.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !OldOptions.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !OldOptions.IsActiveAirship) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !OldOptions.IsActivePolus) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !OldOptions.IsActiveAirship) continue;
                if (deep > 0)
                {
                    text += string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0)));
                    text += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                }
                text += $"{opt.Value.GetName(true)}: {opt.Value.GetString()}\n";
                if (opt.Value.GetBool()) ShowChildrenSettings(opt.Value, ref text, deep + 1);
            }
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastroles"), PlayerId);
                return;
            }
            var text = GetString("LastResult") + ":";
            List<byte> cloneRoles = new(Main.PlayerStates.Keys);
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            foreach (var id in Main.winnerList)
            {
                text += $"\n★ " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                text += $"\n　 " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
            }
            SendMessage(text, PlayerId);
            SendMessage(EndGamePatch.KillLog, PlayerId);
        }


        public static string GetSubRolesText(byte id, bool disableColor = false)
        {
            return GetPlayerById(id).GetDynamicName().GetComponentValue(UI.Subrole);
        }

        public static void ShowHelp()
        {
            SendMessage(
                GetString("CommandList")
                + $"\n/winner - {GetString("Command.winner")}"
                + $"\n/lastresult - {GetString("Command.lastresult")}"
                + $"\n/rename - {GetString("Command.rename")}"
                + $"\n/now - {GetString("Command.now")}"
                + $"\n/h now - {GetString("Command.h_now")}"
                + $"\n/h roles {GetString("Command.h_roles")}"
                + $"\n/h addons {GetString("Command.h_addons")}"
                + $"\n/h modes {GetString("Command.h_modes")}"
                + $"\n/dump - {GetString("Command.dump")}"
                );

        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taskState = GetPlayerById(Terrorist.PlayerId).GetPlayerTaskState();
            if (taskState.IsTaskFinished && (!Main.PlayerStates[Terrorist.PlayerId].IsSuicide() || StaticOptions.CanTerroristSuicideWin)) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (Main.PlayerStates[pc.PlayerId].deathReason == PlayerStateOLD.DeathReason.Vote)
                        {
                            //追放された場合は生存扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.etc;
                            //生存扱いのためSetDeadは必要なし
                        }
                        else
                        {
                            //キルされた場合は自爆扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.Suicide;
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        pc.RpcMurderPlayer(pc);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.Bombed;
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Terrorist);
                CustomWinnerHolder.WinnerIds.Add(Terrorist.PlayerId);
            }
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            Main.MessagesToSend.Add((text.RemoveHtmlTags(), sendTo, title));
        }
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return Game.GetAllPlayers().FirstOrDefault(pc => pc.PlayerId == PlayerId);
        }

        public static PlayerControl GetPlayerByClientId(int clientId)
        {
            return Game.GetAllPlayers().FirstOrDefault(p => p.GetClientId() == clientId) ?? throw new NullReferenceException($"No player found for {clientId}.. Players: {Game.GetAllPlayers().Select(pc => pc.GetClientId()).PrettyString()}");
        }

        public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);

        public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
            return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.AllPlayerControls == null) return;

            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            TownOfHost.Logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            //Snitch警告表示のON/OFF
            bool ShowSnitchWarning = false;
            if (CustomRoleManager.Static.Snitch.IsEnable())
            {
                foreach (var snitch in PlayerControl.AllPlayerControls)
                {
                    if (snitch.Is(CustomRoles.Snitch) && !snitch.Data.IsDead && !snitch.Data.Disconnected)
                    {
                        var taskState = snitch.GetPlayerTaskState();
                        if (taskState.DoExpose)
                        {
                            ShowSnitchWarning = true;
                            break;
                        }
                    }
                }
            }

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                if (seer.IsModClient()) continue;
                string fontSize = "1.5";
                if (isMeeting && (seer.GetClient().PlatformData.Platform.ToString() == "Playstation" || seer.GetClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
                TownOfHost.Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START", "NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない
                if (seer.Data.Disconnected) continue;

                //タスクなど進行状況を含むテキスト
                string SelfTaskText = GetProgressText(seer);

                //名前の後ろに付けるマーカー
                string SelfMark = "";

                //インポスター/キル可能な第三陣営に対するSnitch警告
                /*var canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //LocalPlayerがインポスター
                    (StaticOptions.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト*/

                if (/*canFindSnitchRole && */ShowSnitchWarning && !isMeeting)
                {
                    var arrows = "";
                    foreach (var arrow in Main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !Main.PlayerStates[arrow.Key.Item2].IsDead && GetPlayerById(arrow.Key.Item2).Is(CustomRoles.Snitch))
                        {
                            //自分用の矢印で対象が死んでない時
                            arrows += arrow.Value;
                        }
                    }
                    SelfMark += $"<color={GetRoleColorCode(Snitch.Ref<Snitch>())}>★{arrows}</color>";
                }

                //ハートマークを付ける(自分に)
                if (seer.Is(CustomRoles.Lovers)) SelfMark += $"<color={GetRoleColorCode(Lovers.Ref<Lovers>())}>♡</color>";

                //呪われている場合
                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";
                //他人用の変数定義
                bool SeerKnowsImpostors = false; //trueの時、インポスターの名前が赤色に見える

                //タスクを終えたSnitchがインポスター/キル可能な第三陣営の方角を確認できる
                if (seer.Is(CustomRoles.Snitch))
                {
                    var TaskState = seer.GetPlayerTaskState();
                    if (TaskState.IsTaskFinished)
                    {
                        SeerKnowsImpostors = true;
                        //ミーティング以外では矢印表示
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                //自分用の矢印で対象が死んでない時
                                if (arrow.Key.Item1 == seer.PlayerId && !Main.PlayerStates[arrow.Key.Item2].IsDead)
                                    SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.Is(CustomRoles.MadSnitch))
                {
                    var TaskState = seer.GetPlayerTaskState();
                    if (TaskState.IsTaskFinished)
                        SeerKnowsImpostors = true;
                }

                /*if (seer.Is(CustomRoles.EvilTracker)) SelfSuffix += EvilTrackerOLD.UtilsGetTargetArrow(isMeeting, seer);*/

                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.GetRealName(isMeeting);

                if (!isMeeting && MeetingStates.FirstMeeting && StaticOptions.ChangeNameToRoleInfo)
                    SeerRealName = seer.GetRoleInfo();

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = $"<size={fontSize}>{seer.GetDisplayRoleName()}{SelfTaskText}</size>";
                string SelfDeathReason = seer.KnowDeathReason(seer) ? $"({ColorString(GetRoleColor(Doctor.Ref<Doctor>()), GetVitalText(seer.PlayerId))})" : "";
                string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{SelfMark}";
                SelfName = SelfRoleName + "\r\n" + SelfName;
                SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                if (!isMeeting) SelfName += "\r\n";

                //適用

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || SeerKnowsImpostors //seerがインポスターを知っている状態
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || seer.Is(CustomRoles.EgoSchrodingerCat) //seerがエゴイストのシュレディンガーの猫
                    || seer.Is(CustomRoles.JSchrodingerCat) //seerがJackal陣営のシュレディンガーの猫
                    || seer.Is(CustomRoles.MSchrodingerCat) //seerがインポスター陣営のシュレディンガーの猫
                    || seer.Is(CustomRoles.Lovers)
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.Is(CustomRoles.Puppeteer)
                    || seer.IsNeutralKiller() //seerがキル出来る第三陣営
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer || target.Data.Disconnected) continue;
                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START", "NotifyRoles");

                        //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetTaskText = seer.Data.IsDead && StaticOptions.GhostsCanSeeOtherRoles ? $"{GetProgressText(target)}" : "";

                        //名前の後ろに付けるマーカー
                        string TargetMark = "";

                        //呪われている人

                        //タスク完了直前のSnitchにマークを表示
                        /*canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //Seerがインポスター
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト*/

                        if (target.Is(CustomRoles.Snitch)/* && canFindSnitchRole*/)
                        {
                            var taskState = target.GetPlayerTaskState();
                            if (taskState.DoExpose)
                                TargetMark += $"<color={GetRoleColorCode(Snitch.Ref<Snitch>())}>★</color>";
                        }

                        //ハートマークを付ける(相手に)
                        if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(Lovers.Ref<Lovers>())}>♡</color>";
                        }
                        //霊界からラバーズ視認
                        else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(Lovers.Ref<Lovers>())}>♡</color>";
                        }

                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(Impostor.Ref<Impostor>())}>◆</color>";
                        /*if (seer.Is(CustomRoles.EvilTracker))
                            TargetMark += EvilTrackerOLD.GetTargetMark(seer, target);*/

                        //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetRoleText = seer.Data.IsDead && StaticOptions.GhostsCanSeeOtherRoles ? $"<size={fontSize}>{target.GetDisplayRoleName()}{TargetTaskText}</size>\r\n" : "";

                        if (target.Is(CustomRoles.GM))
                            TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.GetRealName(isMeeting);

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors) //Seerがインポスターが誰かわかる状態
                        {
                            //スニッチはオプション有効なら第三陣営のキル可能役職も見れる
                            var snitchOption = seer.Is(CustomRoles.Snitch)/* && Options.SnitchCanFindNeutralKiller.GetBool()*/;
                            var foundCheck = target.GetCustomRole().IsImpostor() || (snitchOption && target.IsNeutralKiller());
                            if (foundCheck)
                                TargetPlayerName = ColorString(target.GetRoleColor(), TargetPlayerName);
                        }
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = ColorString(GetRoleColor(Egoist.Ref<Egoist>()), TargetPlayerName);
                        else if ((seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist)) || //エゴ猫 --> エゴイスト
                                 (seer.Is(CustomRoles.JSchrodingerCat) && target.Is(CustomRoles.Jackal)) || // J猫 --> ジャッカル
                                 (seer.Is(CustomRoles.MSchrodingerCat) && target.Is(Roles.RoleType.Impostor))) // M猫 --> インポスター
                            TargetPlayerName = ColorString(target.GetRoleColor(), TargetPlayerName);
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting)
                            TargetPlayerName = ColorString(GetRoleColor(Impostor.Ref<Impostor>()), TargetPlayerName); //targetの赤色で表示

                        if (seer.Is(Roles.RoleType.Impostor) && target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && StaticOptions.MadSnitchCanAlsoBeExposedToImpostor)
                            TargetMark += ColorString(GetRoleColor(MadSnitch.Ref<MadSnitch>()), "★");

                        string TargetDeathReason = "";
                        if (seer.KnowDeathReason(target))
                            TargetDeathReason = $"({ColorString(GetRoleColor(Doctor.Ref<Doctor>()), GetVitalText(target.PlayerId))})";

                        /*if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isMeeting)
                            TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";*/

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                        //適用

                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END", "NotifyRoles");
                    }
                }
                TownOfHost.Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END", "NotifyRoles");
            }
        }
        public static void MarkEveryoneDirtySettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static void CountAliveImpostors()
        {
            int AliveImpostorCount = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                CustomRole pc_role = pc.GetCustomRole();
                if (pc_role.IsImpostor() && !Main.PlayerStates[pc.PlayerId].IsDead) AliveImpostorCount++;
            }
            if (Main.AliveImpostorCount == AliveImpostorCount) return;
            TownOfHost.Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
            /*LastImpostor.SetSubRole();*/
        }
        public static string GetVoteName(byte num)
        {
            string name = "invalid";
            var player = GetPlayerById(num);
            if (num < 15 && player != null) name = player?.GetNameWithRole();
            if (num == 253) name = "Skip";
            if (num == 254) name = "None";
            if (num == 255) name = "Dead";
            return name;
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        public static void DumpLog()
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{Main.PluginVersion}{(Main.DevVersion ? Main.DevVersionStr : "")}-{t}.log";
            FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(@filename);
            System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }

        public static string SummaryTexts(byte id, bool disableColor = true)
        {
            var RolePos = TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English ? 47 : 37;
            string summary = $"{ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id])}<pos=22%> {GetProgressText(id)}</pos><pos=29%> {GetVitalText(id)}</pos><pos={RolePos}%> {GetDisplayRoleName(id)}{GetSubRolesText(id)}</pos>";
            return disableColor ? summary.RemoveHtmlTags() : summary;
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static bool CanMafiaKill()
        {
            if (Main.PlayerStates == null) return false;
            //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
            int LivingImpostorsNum = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                CustomRole role = pc.GetCustomRole();
                if (!pc.Data.IsDead && role is not Mafia && role.IsImpostor()) LivingImpostorsNum++;
            }
            return LivingImpostorsNum <= 0;
        }
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
        {
            Sprite sprite = null;
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray());
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            }
            catch
            {
                Logger.Error($"\"{path}\"の読み込みに失敗しました。", "LoadImage");
            }
            return sprite;
        }
        public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        /// <summary>
        /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
        /// </summary>
        public static Color ShadeColor(this Color color, float Darkness = 0)
        {
            bool IsDarker = Darkness >= 0; //黒と混ぜる
            if (!IsDarker) Darkness = -Darkness;
            float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
            float R = (color.r + Weight) / (Darkness + 1);
            float G = (color.g + Weight) / (Darkness + 1);
            float B = (color.b + Weight) / (Darkness + 1);
            return new Color(R, G, B, color.a);
        }

        /// <summary>
        /// 乱数の簡易的なヒストグラムを取得する関数
        /// <params name="nums">生成した乱数を格納したint配列</params>
        /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
        /// </summary>
        public static string WriteRandomHistgram(int[] nums, float scale = 1.0f)
        {
            int[] countData = new int[nums.Max() + 1];
            foreach (var num in nums)
            {
                if (0 <= num) countData[num]++;
            }
            StringBuilder sb = new();
            for (int i = 0; i < countData.Length; i++)
            {
                // 倍率適用
                countData[i] = (int)(countData[i] * scale);

                // 行タイトル
                sb.AppendFormat("{0:D2}", i).Append(" : ");

                // ヒストグラム部分
                for (int j = 0; j < countData[i]; j++)
                    sb.Append('|');

                // 改行
                sb.Append('\n');
            }

            // その他の情報
            sb.Append("最大数 - 最小数: ").Append(countData.Max() - countData.Min());

            return sb.ToString();
        }

        public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
        {
            casted = obj.TryCast<T>();
            return casted != null;
        }
    }
}