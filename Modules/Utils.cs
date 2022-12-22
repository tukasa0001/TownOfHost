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
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost.Modules;
using static TownOfHost.Translator;

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
        public static void SetVision(this IGameOptions opt, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
                }
                return;
            }
            else
            {
                opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod) / 5);
                }
                return;
            }
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
            if (seer.Is(CustomRoles.GM)) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;
            return seer.GetCustomRole() switch
            {
                CustomRoles.EvilTracker => EvilTracker.KillFlashCheck(killer, target),
                CustomRoles.Seer => true,
                _ => seer.Is(RoleType.Madmate) && Options.MadmateCanSeeKillFlash.GetBool(),
            };
        }
        public static void KillFlash(this PlayerControl player)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = false; //リアクターフラッシュの確認
            if (Main.NormalOptions.MapId == 2) ReactorCheck = IsActive(SystemTypes.Laboratory);
            else ReactorCheck = IsActive(SystemTypes.Reactor);

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            Main.PlayerStates[player.PlayerId].IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            ExtendedPlayerControl.MarkDirtySettings(player);
            new LateTask(() =>
            {
                Main.PlayerStates[player.PlayerId].IsBlackOut = false; //ブラックアウト解除
                ExtendedPlayerControl.MarkDirtySettings(player);
            }, Options.KillFlashDuration.GetFloat(), "RemoveKillFlash");
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
            var TextData = GetRoleText(playerId);
            return ColorString(TextData.Item2, TextData.Item1);
        }
        public static string GetRoleName(CustomRoles role)
        {
            return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        }
        public static string GetDeathReason(PlayerState.DeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason), status));
        }
        public static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            return hexColor;
        }
        public static (string, Color) GetRoleText(byte playerId)
        {
            string RoleText = "Invalid Role";
            Color RoleColor = Color.red;

            var mainRole = Main.PlayerStates[playerId].MainRole;
            var SubRoles = Main.PlayerStates[playerId].SubRoles;
            RoleText = GetRoleName(mainRole);
            RoleColor = GetRoleColor(mainRole);
            foreach (var subRole in Main.PlayerStates[playerId].SubRoles)
            {
                switch (subRole)
                {
                    case CustomRoles.LastImpostor:
                        RoleText = GetRoleString("Last-") + RoleText;
                        break;
                }
            }
            return (RoleText, RoleColor);
        }

        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = Main.PlayerStates[playerId];
            string deathReason = state.IsDead ? GetString("DeathReason." + state.deathReason) : GetString("Alive");
            if (RealKillerColor)
            {
                var KillerId = state.GetRealKiller();
                Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(CustomRoles.Doctor);
                deathReason = ColorString(color, deathReason);
            }
            return deathReason;
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
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
                    switch (hRole)
                    {
                        case CustomRoles.Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.HASFox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.HASTroll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }

        public static bool HasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            if (GameStates.IsLobby) return false;
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;

            var hasTasks = true;
            var States = Main.PlayerStates[p.PlayerId];
            if (p.Disconnected) hasTasks = false;
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
            }
            else
            {
                if (p.IsDead && Options.GhostIgnoreTasks.GetBool()) hasTasks = false;
                var role = States.MainRole;
                switch (role)
                {
                    case CustomRoles.GM:
                    case CustomRoles.Madmate:
                    case CustomRoles.SKMadmate:
                    case CustomRoles.Sheriff:
                    case CustomRoles.Arsonist:
                    case CustomRoles.Egoist:
                    case CustomRoles.Jackal:
                    case CustomRoles.Jester:
                    case CustomRoles.Opportunist:
                        hasTasks = false;
                        break;
                    case CustomRoles.MadGuardian:
                    case CustomRoles.MadSnitch:
                    case CustomRoles.Terrorist:
                        if (ForRecompute)
                            hasTasks = false;
                        break;
                    case CustomRoles.Executioner:
                        if (Executioner.ChangeRolesAfterTargetKilled.GetValue() == 0)
                            hasTasks = !ForRecompute;
                        else hasTasks = false;
                        break;
                    default:
                        if (role.IsImpostor() || role.IsKilledSchrodingerCat()) hasTasks = false;
                        break;
                }

                foreach (var subRole in States.SubRoles)
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            //ラバーズがクルー陣営の場合タスクを付与しない
                            if (role.IsCrewmate())
                                hasTasks = false;
                            break;
                    }
            }
            return hasTasks;
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
                case CustomRoles.Arsonist:
                    var doused = GetDousedPlayerCount(playerId);
                    ProgressText = ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})");
                    break;
                case CustomRoles.Sheriff:
                    ProgressText += Sheriff.GetShotLimit(playerId);
                    break;
                case CustomRoles.Sniper:
                    ProgressText += Sniper.GetBulletCount(playerId);
                    break;
                case CustomRoles.EvilTracker:
                    ProgressText += EvilTracker.GetMarker(playerId);
                    break;
                default:
                    //タスクテキスト
                    var taskState = Main.PlayerStates?[playerId].GetTaskState();
                    if (taskState.hasTasks)
                    {
                        Color TextColor = Color.yellow;
                        var info = GetPlayerInfoById(playerId);
                        var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(role).ShadeColor(0.5f); //タスク完了後の色
                        var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色
                        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
                        TextColor = comms ? Color.gray : NormalColor;
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
                    }
                    break;
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText += ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $" [{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]");

            return ProgressText;
        }
        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong"), PlayerId); }
                if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong"), PlayerId); }
            }
            else
            {
                if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo"), PlayerId); }
                if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = Main.NormalOptions.MapId;
            if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = "";
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                text = GetString("Roles") + ":";
                if (CustomRoles.HASFox.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += GetString("HideAndSeek");
            }
            else
            {
                text = GetString("Settings") + ":";
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    text += $"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n";
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref text);
                    text = text.RemoveHtmlTags();
                }
                foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
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
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            text += $"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━";
            foreach (var role in Options.CustomRoleCounts)
            {
                if (!role.Key.IsEnable()) continue;
                text += $"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n";
                ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref text);
                text = text.RemoveHtmlTags();
            }
            text += $"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━";
            foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
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
            if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = GetString("Roles") + ":";
            text += string.Format("\n{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.IsEnable()) text += string.Format("\n{0}:{1}x{2}", GetRoleName(role), $"{role.GetChance() * 100}%", role.GetCount());
            }
            SendMessage(text, PlayerId);
        }
        public static void ShowChildrenSettings(OptionItem option, ref string text, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) continue;
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
            var SubRoles = Main.PlayerStates[id].SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role is CustomRoles.NotAssigned or
                            CustomRoles.LastImpostor) continue;

                var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
                sb.Append($"</color> + {RoleText}");
            }

            return sb.ToString();
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
            if (taskState.IsTaskFinished && (!Main.PlayerStates[Terrorist.PlayerId].IsSuicide() || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                        {
                            //追放された場合は生存扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            //生存扱いのためSetDeadは必要なし
                        }
                        else
                        {
                            //キルされた場合は自爆扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //生存者は爆死
                        pc.SetRealKiller(Terrorist.Object);
                        pc.RpcMurderPlayer(pc);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
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
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId);
            }
            else
            {
                if (AmongUsClient.Instance.IsGamePublic)
                    name = $"<color={Main.ModColor}>TownOfHost v{Main.PluginVersion}</color>\r\n" + name;
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += $"\r\n<color={Main.ModColor}>TOH v{Main.PluginVersion}</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color>";
                        break;
                    case SuffixModes.Recording:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color>";
                        break;
                    case SuffixModes.RoomHost:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color>";
                        break;
                    case SuffixModes.OriginalName:
                        name += $"\r\n<color={Main.ModColor}>{DataManager.player.Customization.Name}</color>";
                        break;
                }
            }
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();
        public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
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
            if (CustomRoles.Snitch.IsEnable())
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
                var canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //LocalPlayerがインポスター
                    (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト

                if (canFindSnitchRole && ShowSnitchWarning && !isMeeting)
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
                    SelfMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>★{arrows}</color>";
                }

                //ハートマークを付ける(自分に)
                if (seer.Is(CustomRoles.Lovers)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";

                //呪われている場合
                if (Witch.IsSpelled(seer.PlayerId) && isMeeting)
                    SelfMark += "<color=#ff0000>†</color>";

                if (Sniper.IsEnable())
                {
                    //銃声が聞こえるかチェック
                    SelfMark += Sniper.GetShotNotify(seer.PlayerId);
                }
                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) != null)
                {
                    string BountyTargetName = BountyHunter.GetTarget(seer).GetRealName(isMeeting);
                    SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                }
                if (seer.Is(CustomRoles.FireWorks))
                {
                    string stateText = FireWorks.GetStateText(seer);
                    SelfSuffix = $"{stateText}";
                }
                if (seer.Is(CustomRoles.Witch))
                {
                    SelfSuffix = Witch.GetSpellModeText(seer, false);
                }

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

                if (seer.Is(CustomRoles.EvilTracker)) SelfSuffix += EvilTracker.UtilsGetTargetArrow(isMeeting, seer);
                if (seer.Is(CustomRoles.BountyHunter)) SelfSuffix += BountyHunter.UtilsGetTargetArrow(isMeeting, seer);

                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.GetRealName(isMeeting);

                if (!isMeeting && MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool())
                    SeerRealName = seer.GetRoleInfo();

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = $"<size={fontSize}>{seer.GetDisplayRoleName()}{SelfTaskText}</size>";
                string SelfDeathReason = seer.KnowDeathReason(seer) ? $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId))})" : "";
                string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{SelfMark}";
                if (seer.Is(CustomRoles.Arsonist) && seer.IsDouseDone())
                    SelfName = $"</size>\r\n{ColorString(seer.GetRoleColor(), GetString("EnterVentToWin"))}";
                SelfName = SelfRoleName + "\r\n" + SelfName;
                SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                if (!isMeeting) SelfName += "\r\n";

                //適用
                seer.RpcSetNamePrivate(SelfName, true, force: NoCache);

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || SeerKnowsImpostors //seerがインポスターを知っている状態
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || seer.Is(CustomRoles.EgoSchrodingerCat) //seerがエゴイストのシュレディンガーの猫
                    || seer.Is(CustomRoles.JSchrodingerCat) //seerがJackal陣営のシュレディンガーの猫
                    || seer.Is(CustomRoles.MSchrodingerCat) //seerがインポスター陣営のシュレディンガーの猫
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Lovers)
                    || Witch.HaveSpelledPlayer()
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
                        string TargetTaskText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"{GetProgressText(target)}" : "";

                        //名前の後ろに付けるマーカー
                        string TargetMark = "";

                        //呪われている人
                        TargetMark += Witch.GetSpelledMark(target.PlayerId, isMeeting);

                        //タスク完了直前のSnitchにマークを表示
                        canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //Seerがインポスター
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト

                        if (target.Is(CustomRoles.Snitch) && canFindSnitchRole)
                        {
                            var taskState = target.GetPlayerTaskState();
                            if (taskState.DoExpose)
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>★</color>";
                        }

                        //ハートマークを付ける(相手に)
                        if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                        }
                        //霊界からラバーズ視認
                        else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                        }

                        if (seer.Is(CustomRoles.Arsonist))//seerがアーソニストの時
                        {
                            if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                            }
                            if (
                                Main.ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && //seerがオイルを塗っている途中(現在進行)
                                ar_kvp.Item1 == target //オイルを塗っている対象がtarget
                            )
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>△</color>";
                            }
                        }
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>";
                        if (seer.Is(CustomRoles.EvilTracker))
                            TargetMark += EvilTracker.GetTargetMark(seer, target);

                        //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}>{target.GetDisplayRoleName()}{TargetTaskText}</size>\r\n" : "";

                        if (target.Is(CustomRoles.GM))
                            TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.GetRealName(isMeeting);

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors) //Seerがインポスターが誰かわかる状態
                        {
                            //スニッチはオプション有効なら第三陣営のキル可能役職も見れる
                            var snitchOption = seer.Is(CustomRoles.Snitch) && Options.SnitchCanFindNeutralKiller.GetBool();
                            var foundCheck = target.GetCustomRole().IsImpostor() || (snitchOption && target.IsNeutralKiller());
                            if (foundCheck)
                                TargetPlayerName = ColorString(target.GetRoleColor(), TargetPlayerName);
                        }
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = ColorString(GetRoleColor(CustomRoles.Egoist), TargetPlayerName);
                        else if ((seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist)) || //エゴ猫 --> エゴイスト
                                 (seer.Is(CustomRoles.JSchrodingerCat) && target.Is(CustomRoles.Jackal)) || // J猫 --> ジャッカル
                                 (seer.Is(CustomRoles.MSchrodingerCat) && target.Is(RoleType.Impostor))) // M猫 --> インポスター
                            TargetPlayerName = ColorString(target.GetRoleColor(), TargetPlayerName);
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting)
                            TargetPlayerName = ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName); //targetの赤色で表示
                        else
                        {
                            //NameColorManager準拠の処理
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }
                        if (seer.Is(RoleType.Impostor) && target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool())
                            TargetMark += ColorString(GetRoleColor(CustomRoles.MadSnitch), "★");
                        TargetMark += Executioner.TargetMark(seer, target);

                        string TargetDeathReason = "";
                        if (seer.KnowDeathReason(target))
                            TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})";

                        if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isMeeting)
                            TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);

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
        public static void AfterMeetingTasks()
        {
            BountyHunter.AfterMeetingTasks();
            SerialKiller.AfterMeetingTasks();
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
                CustomRoles pc_role = pc.GetCustomRole();
                if (pc_role.IsImpostor() && !Main.PlayerStates[pc.PlayerId].IsDead) AliveImpostorCount++;
            }
            if (Main.AliveImpostorCount == AliveImpostorCount) return;
            TownOfHost.Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
            LastImpostor.SetSubRole();
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
            string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{Main.PluginVersion}-{t}.log";
            FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(@filename);
            System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }
        public static (int, int) GetDousedPlayerCount(byte playerId)
        {
            int doused = 0, all = 0; //学校で習った書き方
                                     //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Data.Disconnected ||
                    pc.PlayerId == playerId
                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                all++;
                if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                    //塗れている場合
                    doused++;
            }

            return (doused, all);
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
                var role = pc.GetCustomRole();
                if (!pc.Data.IsDead && role != CustomRoles.Mafia && role.IsImpostor()) LivingImpostorsNum++;
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