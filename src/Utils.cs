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
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            //Logger.Info($"SystemTypes:{type}", "IsActive");
            int mapId = TOHPlugin.NormalOptions.MapId;
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
            if (TOHPlugin.NormalOptions.MapId == 2) ReactorCheck = IsActive(SystemTypes.Laboratory);
            else ReactorCheck = IsActive(SystemTypes.Reactor);

            var Duration = StaticOptions.KillFlashDuration;
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            TOHPlugin.PlayerStates[player.PlayerId].IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                /*if (Constants.ShouldPlaySfx()) OldRPC.PlaySound(player.PlayerId, Sounds.KillSound);*/
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ

            PlayerControlExtensions.MarkDirtySettings(player);
            new DTask(() =>
            {
                TOHPlugin.PlayerStates[player.PlayerId].IsBlackOut = false; //ブラックアウト解除
                PlayerControlExtensions.MarkDirtySettings(player);
            }, StaticOptions.KillFlashDuration, "RemoveKillFlash");
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

        public static Color ConvertHexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        public static string GetRoleColorCode(CustomRole role)
        {
            Color c = role?.RoleColor ?? Color.white;
            return "#" + (c.r * 255).ToString("X2") + (c.g * 255).ToString("X2") + (c.b * 255).ToString("X2") +
                   (c.a * 255).ToString("X2");
        }

        public static (string, Color) GetRoleText(PlayerControl player)
        {
            CustomRole role = player.GetCustomRole();
            return (role.RoleName, role.RoleColor);
        }

        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = TOHPlugin.PlayerStates[playerId];
            string deathReason = state.IsDead ? GetString("DeathReason." + state.deathReason) : GetString("Alive");
            if (RealKillerColor)
            {
                var KillerId = state.GetRealKiller();
                Color color = KillerId != byte.MaxValue
                    ? TOHPlugin.PlayerColors[KillerId]
                    : GetRoleColor(Doctor.Ref<Doctor>());
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

            return CustomRoleManager.PlayersCustomRolesRedux.TryGetValue(p.PlayerId, out CustomRole? role) &&
                   role.HasTasks();
        }

        public static string GetProgressText(PlayerControl pc)
        {
            if (!TOHPlugin.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
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
            if (!TOHPlugin.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            string ProgressText = "";
            var role = TOHPlugin.PlayerStates[playerId].MainRole;
            switch (role)
            {
                default:
                    //タスクテキスト
                    var taskState = TOHPlugin.PlayerStates?[playerId].GetTaskState();
                    if (taskState.hasTasks)
                    {
                        Color TextColor = Color.yellow;
                        var info = GetPlayerInfoById(playerId);
                        var TaskCompleteColor =
                            HasTasks(info) ? Color.green : role.GetReduxRole().RoleColor.ShadeColor(0.5f); //タスク完了後の色
                        var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色
                        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
                        TextColor = comms ? Color.gray : NormalColor;
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
                    }

                    break;
            }

            if (GetPlayerById(playerId).CanMakeMadmate())
                ProgressText += ColorString(Palette.ImpostorRed.ShadeColor(0.5f),
                    $" [{StaticOptions.CanMakeMadmateCount - TOHPlugin.SKMadmateNowCount}]");

            return ProgressText;
        }

        // GM.Ref<GM>()
        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (TOHPlugin.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.GetReduxRole().IsEnable())
                {
                    SendMessage(GetRoleName(Fox.Ref<Fox>()) + GetString("HASFoxIhmmnfoLong"), PlayerId);
                }

                if (CustomRoles.HASTroll.GetReduxRole().IsEnable())
                {
                    SendMessage(GetRoleName(Troll.Ref<Troll>()) + GetString("HASTrollInfoLong"), PlayerId);
                }
            }
            else
            {
                if (StaticOptions.DisableDevices)
                {
                    SendMessage(GetString("DisableDevicesInfo"), PlayerId);
                }

                if (StaticOptions.SyncButtonMode)
                {
                    SendMessage(GetString("SyncButtonModeInfo"), PlayerId);
                }

                if (StaticOptions.SabotageTimeControl)
                {
                    SendMessage(GetString("SabotageTimeControlInfo"), PlayerId);
                }

                if (StaticOptions.RandomMapsMode)
                {
                    SendMessage(GetString("RandomMapsModeInfo"), PlayerId);
                }

                if (OldOptions.IsStandardHAS)
                {
                    SendMessage(GetString("StandardHASInfo"), PlayerId);
                }

                if (StaticOptions.EnableGM)
                {
                    SendMessage(GetRoleName(GM.Ref<GM>()) + GetString("GMInfoLong"), PlayerId);
                }

                foreach (var role in CustomRoleManager.AllRoles)
                {
                    if (role is Fox or Troll) continue;
                    if (role.IsEnable() && !role.IsVanilla())
                        SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"),
                            PlayerId);
                }
            }

            if (TOHPlugin.NoGameEnd)
            {
                SendMessage(GetString("NoGameEndInfo"), PlayerId);
            }
        }

        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = TOHPlugin.NormalOptions.MapId;
            var text = "";
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                text = GetString("Roles") + ":";
                if (Fox.Ref<Fox>().IsEnable())
                    text += String.Format("\n{0}:{1}", GetRoleName(Fox.Ref<Fox>()), Fox.Ref<Fox>().Count);
                if (Troll.Ref<Troll>().IsEnable())
                    text += String.Format("\n{0}:{1}", GetRoleName(Troll.Ref<Troll>()), Troll.Ref<Troll>().Count);
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

                foreach (var opt in OptionItem.AllOptions.Where(x =>
                             x.GetBool() && x.Parent == null && x.Id >= 80000 &&
                             !x.IsHiddenOn(OldOptions.CurrentGameMode)))
                {
                    if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                        text += $"\n【{opt.GetName(true)}: {{opt.GetString()}}】\n";
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
            text += $"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━";
            foreach (var role in OldOptions.CustomRoleCounts)
            {
                if (!role.Key.GetReduxRole().IsEnable()) continue;
                text += $"\n【{GetRoleName(role.Key.GetReduxRole())}×{role.Key.GetReduxRole().Count}】\n";
                ShowChildrenSettings(OldOptions.CustomRoleSpawnChances[role.Key], ref text);
                text = text.RemoveHtmlTags();
            }

            text += $"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━";
            foreach (var opt in OptionItem.AllOptions.Where(x =>
                         x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(OldOptions.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    text += $"\n【{opt.GetName(true)}: {{opt.GetString()}}】\n";
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
            var text = GetString("Roles") + ":";
            text += string.Format("\n{0}:{1}", GetRoleName(GM.Ref<GM>()),
                StaticOptions.EnableGM.ToString().RemoveHtmlTags());
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.GetReduxRole().IsEnable())
                    text += string.Format("\n{0}:{1}x{2}", GetRoleName(role.GetReduxRole()),
                        $"{role.GetReduxRole().Chance * 100}%", role.GetReduxRole().Count);
            }

            SendMessage(text, PlayerId);
        }

        public static void Teleport(CustomNetworkTransform nt, Vector2 location)
        {
            if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
            MessageWriter writer =
                AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
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

                text += $"{opt.Value.GetName(true)}: {{opt.Value.GetString()}}\n";
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
            List<byte> cloneRoles = new(TOHPlugin.PlayerStates.Keys);
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            /*foreach (var id in TOHPlugin.winnerList)
            {
                text += $"\n★ " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
                cloneRoles.Remove(id);
            }*/

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
            if (taskState.IsTaskFinished && (!TOHPlugin.PlayerStates[Terrorist.PlayerId].IsSuicide() ||
                                             StaticOptions.CanTerroristSuicideWin)) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (TOHPlugin.PlayerStates[pc.PlayerId].deathReason == PlayerStateOLD.DeathReason.Vote)
                        {
                            //追放された場合は生存扱い
                            TOHPlugin.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.etc;
                            //生存扱いのためSetDeadは必要なし
                        }
                        else
                        {
                            //キルされた場合は自爆扱い
                            TOHPlugin.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.Suicide;
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        pc.RpcMurderPlayer(pc);
                        TOHPlugin.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.Bombed;
                        TOHPlugin.PlayerStates[pc.PlayerId].SetDead();
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
            TOHPlugin.MessagesToSend.Add((text.RemoveHtmlTags(), sendTo, title));
        }

        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return Game.GetAllPlayers().FirstOrDefault(pc => pc.PlayerId == PlayerId);
        }

        public static PlayerControl GetPlayerByClientId(int clientId)
        {
            return Game.GetAllPlayers().FirstOrDefault(p => p.GetClientId() == clientId) ??
                   throw new NullReferenceException(
                       $"No player found for {clientId}.. Players: {Game.GetAllPlayers().Select(pc => pc.GetClientId()).PrettyString()}");
        }

        public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);


        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
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
            string filename =
                $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{TOHPlugin.PluginVersion}{(TOHPlugin.DevVersion ? TOHPlugin.DevVersionStr : "")}-{t}.log";
            FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(@filename);
            System.Diagnostics.Process.Start(
                @$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer,
                    "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }

        public static string SummaryTexts(byte id, bool disableColor = true)
        {
            var RolePos = TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English ? 47 : 37;
            string summary =
                $"{ColorString(TOHPlugin.PlayerColors[id], TOHPlugin.AllPlayerNames[id])}<pos=22%> {GetProgressText(id)}</pos><pos=29%> {GetVitalText(id)}</pos><pos={RolePos}%> {GetDisplayRoleName(id)}{GetSubRolesText(id)}</pos>";
            return disableColor ? summary.RemoveHtmlTags() : summary;
        }

        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");

        public static bool CanMafiaKill()
        {
            if (TOHPlugin.PlayerStates == null) return false;
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
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b,
                    Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static Sprite LoadSprite(string path, float pixelsPerUnit = 100f)
        {
            Sprite sprite = null;
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(path);
                Assembly.GetExecutingAssembly().GetName().DebugLog("Exe Assembly Name: ");
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray());
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f),
                    pixelsPerUnit);
            }
            catch (Exception e)
            {
                Logger.Error($"Error Loading Asset: \"{path}\"", "LoadImage");
                Logger.Exception(e, "LoadImage");
            }

            return sprite;
        }

        public static AudioClip LoadAudioClip(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
        {
            // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity� to export)
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(path);
                var byteAudio = new byte[stream.Length];
                _ = stream.Read(byteAudio, 0, (int)stream.Length);
                float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
                int offset;
                for (int i = 0; i < samples.Length; i++)
                {
                    offset = i * 4;
                    samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
                }

                int channels = 2;
                int sampleRate = 48000;
                AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }
            catch
            {
                System.Console.WriteLine("Error loading AudioClip from resources: " + path);
            }

            return null;

            /* Usage example:
            AudioClip exampleClip = Helpers.loadAudioClipFromResources("TownOfHost.assets.exampleClip.raw");
            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
            */
        }

        public static string ColorString(Color32 color, string str) =>
            $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";

        public static string GetOnOff(bool value) => value ? "ON" : "OFF";

        public static string GetOnOffColored(bool value) =>
            value ? Color.cyan.Colorize("ON") : Color.red.Colorize("OFF");

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

        public static List<T> SingletonList<T>(T t)
        {
            return new List<T> { t };
        }
    }
}