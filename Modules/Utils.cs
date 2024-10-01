using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Crewmate;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            // ないものはfalse
            if (!ShipStatus.Instance.Systems.ContainsKey(type))
            {
                return false;
            }
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
                        if (mapId is 1 or 5)
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
                case SystemTypes.HeliSabotage:
                    {
                        var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                        return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                    }
                case SystemTypes.MushroomMixupSabotage:
                    {
                        var mushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                        return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                    }
                default:
                    return false;
            }
        }
        public static SystemTypes GetCriticalSabotageSystemType() => (MapNames)Main.NormalOptions.MapId switch
        {
            MapNames.Polus => SystemTypes.Laboratory,
            MapNames.Airship => SystemTypes.HeliSabotage,
            _ => SystemTypes.Reactor,
        };
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
        public static void TargetDies(MurderInfo info)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (!target.Data.IsDead || GameStates.IsMeeting) return;
            foreach (var seer in Main.AllPlayerControls)
            {
                if (KillFlashCheck(info, seer))
                {
                    seer.KillFlash();
                }
            }
        }
        public static bool KillFlashCheck(MurderInfo info, PlayerControl seer)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (seer.Is(CustomRoles.GM)) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;

            if (seer.GetRoleClass() is IKillFlashSeeable killFlashSeeable)
            {
                return killFlashSeeable.CheckKillFlash(info);
            }

            return seer.GetCustomRole() switch
            {
                // IKillFlashSeeable未適用役職はここに書く
                _ => seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeKillFlash.GetBool(),
            };
        }
        public static void KillFlash(this PlayerControl player)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = IsActive(GetCriticalSabotageSystemType());

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            state.IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                state.IsBlackOut = false; //ブラックアウト解除
                player.MarkDirtySettings();
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
        /// <summary>
        /// seerが自分であるときのseenのRoleName + ProgressText
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>RoleName + ProgressTextを表示するか、構築する色とテキスト(bool, Color, string)</returns>
        public static (bool enabled, string text) GetRoleNameAndProgressTextData(PlayerControl seer, PlayerControl seen = null)
        {
            var roleName = GetDisplayRoleName(seer, seen);
            var progressText = GetProgressText(seer, seen);
            var text = roleName + (roleName != "" ? " " : "") + progressText;
            return (text != "", text);
        }
        /// <summary>
        /// GetDisplayRoleNameDataからRoleNameを構築
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>構築されたRoleName</returns>
        public static string GetDisplayRoleName(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            //デフォルト値
            bool enabled = seer == seen
                        || seen.Is(CustomRoles.GM)
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool());
            var (roleColor, roleText) = GetTrueRoleNameData(seen.PlayerId);

            //seen側による変更
            seen.GetRoleClass()?.OverrideDisplayRoleNameAsSeen(seer, ref enabled, ref roleColor, ref roleText);

            //seer側による変更
            seer.GetRoleClass()?.OverrideDisplayRoleNameAsSeer(seen, ref enabled, ref roleColor, ref roleText);

            return enabled ? ColorString(roleColor, roleText) : "";
        }
        /// <summary>
        /// 引数の指定通りのRoleNameを表示
        /// </summary>
        /// <param name="mainRole">表示する役職</param>
        /// <param name="subRolesList">表示する属性のList</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        public static (Color color, string text) GetRoleNameData(CustomRoles mainRole, List<CustomRoles> subRolesList, bool showSubRoleMarks = true)
        {
            string roleText = "";
            Color roleColor = Color.white;

            if (mainRole < CustomRoles.NotAssigned)
            {
                roleText = GetRoleName(mainRole);
                roleColor = GetRoleColor(mainRole);
            }

            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList)
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.LastImpostor:
                            roleText = GetRoleString("Last-") + roleText;
                            break;
                    }
                }
            }

            string subRoleMarks = showSubRoleMarks ? GetSubRoleMarks(subRolesList) : "";
            if (roleText != "" && subRoleMarks != "")
                subRoleMarks = " " + subRoleMarks; //空じゃなければ空白を追加

            return (roleColor, roleText + subRoleMarks);
        }
        public static string GetSubRoleMarks(List<CustomRoles> subRolesList)
        {
            var sb = new StringBuilder(100);
            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList)
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.Watcher:
                            sb.Append(Watcher.SubRoleMark);
                            break;
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        private static (Color color, string text) GetTrueRoleNameData(byte playerId, bool showSubRoleMarks = true)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            var (color, text) = GetRoleNameData(state.MainRole, state.SubRoles, showSubRoleMarks);
            CustomRoleManager.GetByPlayerId(playerId)?.OverrideTrueRoleName(ref color, ref text);
            return (color, text);
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>構築したRoleName</returns>
        public static string GetTrueRoleName(byte playerId, bool showSubRoleMarks = true)
        {
            var (color, text) = GetTrueRoleNameData(playerId, showSubRoleMarks);
            return ColorString(color, text);
        }
        public static string GetRoleName(CustomRoles role)
        {
            return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        }
        public static string GetDeathReason(CustomDeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(CustomDeathReason), status));
        }
        public static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode;
            _ = ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode;
            return hexColor;
        }

        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            string deathReason = state.IsDead ? GetString("DeathReason." + state.DeathReason) : GetString("Alive");
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

        public static bool HasTasks(NetworkedPlayerInfo p, bool ForRecompute = true)
        {
            if (GameStates.IsLobby) return false;
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;
            if (p.Disconnected) return false;

            var hasTasks = true;
            var States = PlayerState.GetByPlayerId(p.PlayerId);
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
            }
            else
            {
                // 死んでいて，死人のタスク免除が有効なら確定でfalse
                if (p.IsDead && Options.GhostIgnoreTasks.GetBool())
                {
                    return false;
                }
                var role = States.MainRole;
                var roleClass = CustomRoleManager.GetByPlayerId(p.PlayerId);
                if (roleClass != null)
                {
                    switch (roleClass.HasTasks)
                    {
                        case HasTask.True:
                            hasTasks = true;
                            break;
                        case HasTask.False:
                            hasTasks = false;
                            break;
                        case HasTask.ForRecompute:
                            hasTasks = !ForRecompute;
                            break;
                    }
                }
                switch (role)
                {
                    case CustomRoles.GM:
                    case CustomRoles.SKMadmate:
                        hasTasks = false;
                        break;
                    default:
                        if (role.IsImpostor()) hasTasks = false;
                        break;
                }

                foreach (var subRole in States.SubRoles)
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            //ラバーズはタスクを勝利用にカウントしない
                            hasTasks &= !ForRecompute;
                            break;
                    }
            }
            return hasTasks;
        }
        private static string GetProgressText(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            var comms = IsActive(SystemTypes.Comms);
            bool enabled = seer == seen
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherTasks.GetBool());
            string text = GetProgressText(seen.PlayerId, comms);

            //seer側による変更
            seer.GetRoleClass()?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);

            return enabled ? text : "";
        }
        private static string GetProgressText(byte playerId, bool comms = false)
        {
            var ProgressText = new StringBuilder();
            var State = PlayerState.GetByPlayerId(playerId);
            var role = State.MainRole;
            var roleClass = CustomRoleManager.GetByPlayerId(playerId);
            ProgressText.Append(GetTaskProgressText(playerId, comms));
            if (roleClass != null)
            {
                ProgressText.Append(roleClass.GetProgressText(comms));
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText.Append(ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"[{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]"));

            return ProgressText.ToString();
        }
        public static string GetTaskProgressText(byte playerId, bool comms = false)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            if (state == null || state.taskState == null || !state.taskState.hasTasks)
            {
                return "";
            }

            Color TextColor = Color.yellow;
            var info = GetPlayerInfoById(playerId);
            var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(state.MainRole).ShadeColor(0.5f); //タスク完了後の色
            var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色

            if (Workhorse.IsThisRole(playerId))
                NonCompleteColor = Workhorse.RoleColor;

            var NormalColor = state.taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

            TextColor = comms ? Color.gray : NormalColor;
            string Completed = comms ? "?" : $"{state.taskState.CompletedTasksCount}";
            return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})");

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
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsEnable())
                    {
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            SendMessage(description.FullFormatHelp, PlayerId, removeTags: false);
                        }
                        // RoleInfoがない役職は従来処理
                        else
                        {
                            SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                        }
                    }
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
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                sb.Append(GetString("Roles")).Append(':');
                if (CustomRoles.HASFox.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(sb.ToString(), PlayerId);
                sb.Clear().Append(GetString("Settings")).Append(':');
                sb.Append(GetString("HideAndSeek"));
            }
            else
            {
                sb.AppendFormat("<size={0}>", ActiveSettingsSize);
                sb.Append("<size=100%>").Append(GetString("Settings")).Append('\n').Append("</size>");
                sb.AppendFormat("\n【{0}: {1}】\n", RoleAssignManager.OptionAssignMode.GetName(true), RoleAssignManager.OptionAssignMode.GetString());
                if (RoleAssignManager.OptionAssignMode.GetBool())
                {
                    ShowChildrenSettings(RoleAssignManager.OptionAssignMode, ref sb);
                    CheckPageChange(PlayerId, sb);
                }
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    if (role.Key is CustomRoles.HASFox or CustomRoles.HASTroll) continue;

                    sb.Append($"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n");
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
                    CheckPageChange(PlayerId, sb);
                }
                foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
                {
                    if (opt.Name is "RandomSpawn")
                    {
                        foreach (var randomOpt in opt.Children)
                        {
                            if ((randomOpt.Id / 100) % 10 != mapId) continue;
                            //現在のマップのみ表示する
                            if (randomOpt.GetBool())
                            {
                                //Onの時は頭に改ページを入れる
                                CheckPageChange(PlayerId, sb, true);
                                sb.Append($"\n【{opt.GetName(true)}】");
                                sb.Append($"\n {randomOpt.GetName(true)}: {randomOpt.GetString()}\n");

                                ShowChildrenSettings(randomOpt, ref sb, 1);
                            }
                            else
                            {
                                //オフならそのままで大丈夫
                                sb.Append($"\n【{opt.GetName(true)}】");
                                sb.Append($"\n {randomOpt.GetName(true)}: {randomOpt.GetString()}\n");
                            }
                        }
                        CheckPageChange(PlayerId, sb);
                    }
                    else
                    {
                        if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                            sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
                        else
                            sb.Append($"\n【{opt.GetName(true)}】\n");
                        ShowChildrenSettings(opt, ref sb);
                        CheckPageChange(PlayerId, sb);
                    }
                }
            }
            SendMessage(sb.ToString(), PlayerId, removeTags: false);
        }

        private static void CheckPageChange(byte PlayerId, StringBuilder sb, bool force = false)
        {
            //2Byte文字想定で1000byt越えるならページを変える
            if (force || sb.Length > 500)
            {
                SendMessage(sb.ToString(), PlayerId, removeTags: false);
                sb.Clear();
                sb.AppendFormat("<size={0}>", ActiveSettingsSize);
            }
        }

        public static void CopyCurrentSettings()
        {
            var sb = new StringBuilder();
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━");
            foreach (var role in Options.CustomRoleCounts)
            {
                if (!role.Key.IsEnable()) continue;
                sb.Append($"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n");
                ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━");
            foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
                else
                    sb.Append($"\n【{opt.GetName(true)}】\n");
                ShowChildrenSettings(opt, ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            ClipboardHelper.PutClipboardString(sb.ToString());
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            sb.AppendFormat("<size={0}>", ActiveSettingsSize);
            sb.Append("<size=100%>").Append(GetString("Roles")).Append('\n').Append("</size>");
            sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString());
            foreach (CustomRoles role in CustomRolesHelper.AllStandardRoles)
            {
                if (role.IsEnable()) sb.AppendFormat("\n{0}:{1}x{2}", GetRoleName(role), $"{role.GetChance()}%", role.GetCount());
            }
            SendMessage(sb.ToString(), PlayerId, removeTags: false);
        }
        public static void ShowChildrenSettings(OptionItem option, ref StringBuilder sb, int deep = 0)
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
                    sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                    sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
                }
                sb.Append($"{opt.Value.GetName(true).RemoveHtmlTags()}: {opt.Value.GetString()}\n");
                if (opt.Value.GetBool()) ShowChildrenSettings(opt.Value, ref sb, deep + 1);
            }
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastresult"), PlayerId);
                return;
            }
            var sb = new StringBuilder();
            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? Palette.DisabledGrey;

            sb.Append("""<align="center">""");
            sb.Append("<size=150%>").Append(GetString("LastResult")).Append("</size>");
            sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText.Mark(winnerColor, false));
            sb.Append("</align>");

            sb.Append("<size=70%>\n");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n★ ".Color(winnerColor)).Append(SummaryTexts(id, true));
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(SummaryTexts(id, true));
            }
            SendMessage(sb.ToString(), PlayerId, removeTags: false);
        }
        public static void ShowKillLog(byte PlayerId = byte.MaxValue)
        {
            if (GameStates.IsInGame)
            {
                SendMessage(GetString("CantUse.killlog"), PlayerId);
                return;
            }
            SendMessage(EndGamePatch.KillLog, PlayerId, removeTags: false);
        }
        public static string GetSubRolesText(byte id, bool disableColor = false)
        {
            var SubRoles = PlayerState.GetByPlayerId(id).SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role is CustomRoles.NotAssigned or
                            CustomRoles.LastImpostor) continue;

                var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
                sb.Append($"{ColorString(Color.white, " + ")}{RoleText}");
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
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool removeTags = true)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            Main.MessagesToSend.Add((removeTags ? text.RemoveHtmlTags() : text, sendTo, title));
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(Camouflage.PlayerSkins[PlayerControl.LocalPlayer.PlayerId].ColorId);
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
        private static Dictionary<byte, PlayerControl> cachedPlayers = new(15);
        public static PlayerControl GetPlayerById(int playerId) => GetPlayerById((byte)playerId);
        public static PlayerControl GetPlayerById(byte playerId)
        {
            if (cachedPlayers.TryGetValue(playerId, out var cachedPlayer) && cachedPlayer != null)
            {
                return cachedPlayer;
            }
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == playerId).FirstOrDefault();
            cachedPlayers[playerId] = player;
            return player;
        }
        public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();
        private static StringBuilder SelfMark = new(20);
        private static StringBuilder SelfSuffix = new(20);
        private static StringBuilder TargetMark = new(20);
        private static StringBuilder TargetSuffix = new(20);
        public static void NotifyRoles(bool isForMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Main.AllPlayerControls == null) return;

            //ミーティング中の呼び出しは不正
            if (GameStates.IsMeeting) return;

            var caller = new StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            var logger = Logger.Handler("NotifyRoles");
            logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            var isMushroomMixupActive = IsActive(SystemTypes.MushroomMixupSabotage);
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                var seerRole = seer.GetRoleClass();
                string fontSize = isForMeeting ? "1.5" : Main.RoleTextSize.ToString();
                if (isForMeeting && (seer.GetClient().PlatformData.Platform is Platforms.Playstation or Platforms.Switch)) fontSize = "70%";
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START");

                // 会議じゃなくて，キノコカオス中で，seerが生きていてdesyncインポスターの場合に自身の名前を消す
                if (!isForMeeting && isMushroomMixupActive && seer.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                {
                    seer.RpcSetNamePrivate("<size=0>", true, force: NoCache);
                }
                else
                {
                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seer役職が対象のMark
                    SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: isForMeeting));

                    //ハートマークを付ける(自分に)
                    if (seer.Is(CustomRoles.Lovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Lovers), "♡"));

                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: isForMeeting));

                    //seer役職が対象のSuffix
                    SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: isForMeeting));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = seer.GetRealName(isForMeeting);

                    if (!isForMeeting && MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool())
                        SeerRealName = seer.GetRoleInfo();

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = seer.KnowDeathReason(seer) ? $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId))})" : "";
                    string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{SelfMark}";
                    SelfName = SelfRoleName + "\r\n" + SelfName;
                    SelfName += SelfSuffix.ToString() == "" ? "" : "\r\n " + SelfSuffix.ToString();
                    if (!isForMeeting) SelfName += "\r\n";

                    //適用
                    seer.RpcSetNamePrivate(SelfName, true, force: NoCache);
                }

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Lovers)
                    || Witch.IsSpelled()
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.Is(CustomRoles.Puppeteer)
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in Main.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START");

                        // 会議じゃなくて，キノコカオス中で，targetが生きていてseerがdesyncインポスターの場合にtargetの名前を消す
                        if (!isForMeeting && isMushroomMixupActive && target.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                        {
                            target.RpcSetNamePrivate("<size=0>", true, seer, force: NoCache);
                        }
                        else
                        {
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            //seer役職が対象のMark
                            TargetMark.Append(seerRole?.GetMark(seer, target, isForMeeting));
                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                            //ハートマークを付ける(相手に)
                            if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                            }
                            //霊界からラバーズ視認
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : "";

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: isForMeeting));

                            //seer役職が対象のSuffix
                            TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: isForMeeting));
                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                            {
                                TargetSuffix.Insert(0, "\r\n");
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = target.GetRealName(isForMeeting);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isForMeeting);

                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})";

                            if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isForMeeting)
                                TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                            //全てのテキストを合成します。
                            string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}{TargetSuffix}";

                            //適用
                            target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);
                        }

                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END");
                    }
                }
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END");
            }
        }
        public static void MarkEveryoneDirtySettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
        }
        public static void SyncAllSettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
            GameOptionsSender.SendAllGameOptions();
        }
        public static void AfterMeetingTasks()
        {
            foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                roleClass.AfterMeetingTasks();
            if (Options.AirShipVariableElectrical.GetBool())
                AirShipElectricalDoors.Initialize();
            DoorsReset.ResetDoors();
            // 空デデンバグ対応 会議後にベントを空にする
            var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.TryCast<VentilationSystem>() : null;
            if (ventilationSystem != null)
            {
                ventilationSystem.PlayersInsideVents.Clear();
                ventilationSystem.IsDirty = true;
            }
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static void CountAlivePlayers(bool sendLog = false)
        {
            int AliveImpostorCount = Main.AllAlivePlayerControls.Count(pc => pc.Is(CustomRoleTypes.Impostor));
            if (Main.AliveImpostorCount != AliveImpostorCount)
            {
                Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
                Main.AliveImpostorCount = AliveImpostorCount;
                LastImpostor.SetSubRole();
            }

            if (sendLog)
            {
                var sb = new StringBuilder(100);
                foreach (var countTypes in EnumHelper.GetAllValues<CountTypes>())
                {
                    var playersCount = PlayersCount(countTypes);
                    if (playersCount == 0) continue;
                    sb.Append($"{countTypes}:{AlivePlayersCount(countTypes)}/{playersCount}, ");
                }
                sb.Append($"All:{AllAlivePlayersCount}/{AllPlayersCount}");
                Logger.Info(sb.ToString(), "CountAlivePlayers");
            }
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        public static DirectoryInfo GetLogFolder(bool auto = false)
        {
            var folder = Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/TownOfHost/Logs");
            if (auto)
            {
                folder = Directory.CreateDirectory($"{folder.FullName}/AutoLogs");
            }
            return folder;
        }
        public static void DumpLog()
        {
            var logs = GetLogFolder();
            var filename = CopyLog(logs.FullName);
            OpenDirectory(filename);
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "ログフォルダにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }
        public static void SaveNowLog()
        {
            var logs = GetLogFolder(true);
            // 7日以上前のログを削除
            logs.EnumerateFiles().Where(f => f.CreationTime < DateTime.Now.AddDays(-7)).ToList().ForEach(f => f.Delete());
            CopyLog(logs.FullName);
        }
        public static string CopyLog(string path)
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string fileName = $"{path}/TownOfHost-v{Main.PluginVersion}-{t}.log";
            FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            var logFile = file.CopyTo(fileName);
            return logFile.FullName;
        }
        public static void OpenLogFolder()
        {
            var logs = GetLogFolder(true);
            OpenDirectory(logs.FullName);
        }
        public static void OpenDirectory(string path)
        {
            Process.Start("Explorer.exe", $"/select,{path}");
        }
        public static string SummaryTexts(byte id, bool isForChat)
        {

            var builder = new StringBuilder();
            // チャットならposタグを使わない(文字数削減)
            if (isForChat)
            {
                builder.Append(Main.AllPlayerNames[id]);
                builder.Append(": ").Append(GetProgressText(id).RemoveColorTags());
                builder.Append(' ').Append(GetVitalText(id));
                builder.Append(' ').Append(GetTrueRoleName(id, false).RemoveColorTags());
                builder.Append(' ').Append(GetSubRolesText(id).RemoveColorTags());
            }
            else
            {
                // 全プレイヤー中最長の名前の長さからプレイヤー名の後の水平位置を計算する
                // 1em ≒ 半角2文字
                // 空白は0.5emとする
                // SJISではアルファベットは1バイト，日本語は基本的に2バイト
                var longestNameByteCount = Main.AllPlayerNames.Values.Select(name => name.GetByteCount()).OrderByDescending(byteCount => byteCount).FirstOrDefault();
                //最大11.5emとする(★+日本語10文字分+半角空白)
                var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f /* ★+末尾の半角空白 */ , 11.5f);
                builder.Append(ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id]));
                builder.AppendFormat("<pos={0}em>", pos).Append(GetProgressText(id)).Append("</pos>");
                // "(00/00) " = 4em
                pos += 4f;
                builder.AppendFormat("<pos={0}em>", pos).Append(GetVitalText(id)).Append("</pos>");
                // "Lover's Suicide " = 8em
                // "回線切断 " = 4.5em
                pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8f : 4.5f;
                builder.AppendFormat("<pos={0}em>", pos);
                builder.Append(GetTrueRoleName(id, false));
                builder.Append(GetSubRolesText(id));
                builder.Append("</pos>");
            }
            return builder.ToString();
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static string RemoveColorTags(this string str) => Regex.Replace(str, "</?color(=#[0-9a-fA-F]*)?>", "");
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
        public static int AllPlayersCount => PlayerState.AllPlayerStates.Values.Count(state => state.CountType != CountTypes.OutOfGame);
        public static int AllAlivePlayersCount => Main.AllAlivePlayerControls.Count(pc => !pc.Is(CountTypes.OutOfGame));
        public static bool IsAllAlive => PlayerState.AllPlayerStates.Values.All(state => state.CountType == CountTypes.OutOfGame || !state.IsDead);
        public static int PlayersCount(CountTypes countTypes) => PlayerState.AllPlayerStates.Values.Count(state => state.CountType == countTypes);
        public static int AlivePlayersCount(CountTypes countTypes) => Main.AllAlivePlayerControls.Count(pc => pc.Is(countTypes));

        private const string ActiveSettingsSize = "70%";
        private const string ActiveSettingsLineHeight = "55%";
    }
}