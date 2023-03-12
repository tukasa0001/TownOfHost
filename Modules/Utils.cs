using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnhollowerBaseLib;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class Utils
{
    private static readonly DateTime timeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    /// <param name="dateTime">现在的时间</param>
    /// <returns></returns>
    public static long GetTimeStamp(DateTime dateTime) => (long)(dateTime.ToUniversalTime() - timeStampStartTime).TotalSeconds;
    public static void ErrorEnd(string text)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            Logger.Fatal($"{text} 错误，触发防黑屏措施", "Anti-black");
            ChatUpdatePatch.DoBlockChat = true;
            Main.OverrideWelcomeMsg = "由于未知错误发生，已终止游戏以防止黑屏。很抱歉，所有的H系列模组都存在这个问题，自动结束游戏是必要的保护措施，否则游戏将无法运行。";
            new LateTask(() =>
            {
                Logger.SendInGame("由于未知错误发生，将终止游戏以防止黑屏", true);
            }, 3f, "Anti-Black Msg SendInGame");
            new LateTask(() =>
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                RPC.ForceEndGame(CustomWinner.Error);
            }, 5.5f, "Anti-Black End Game");
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AntiBlack, SendOption.Reliable);
            writer.Write(text);
            writer.EndMessage();
            if (Options.EndWhenPlayerBug.GetBool())
            {
                new LateTask(() =>
                {
                    Logger.SendInGame("您触发了黑屏Bug，正在请求房主终止游戏...", true);
                }, 3f, "Anti-Black Msg SendInGame");
            }
            else
            {
                new LateTask(() =>
                {
                    Logger.SendInGame("您触发了黑屏Bug，房主拒绝终止游戏，稍后将为您断开连接", true);
                }, 3f, "Anti-Black Msg SendInGame");
                new LateTask(() =>
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.Custom);
                    Logger.Fatal($"{text} 错误，已断开游戏", "Anti-black");
                }, 8f, "Anti-Black Exit Game");
            }
        }
    }
    public static void TPAll(Vector2 location)
    {
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
            TP(pc.NetTransform, location);
    }

    public static void TP(CustomNetworkTransform nt, Vector2 location)
    {
        location += new Vector2(0, 0.3636f);
        if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
        //nt.WriteVector2(location, writer);
        NetHelpers.WriteVector2(location, writer);
        writer.Write(nt.lastSequenceId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
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
        foreach (var seer in Main.AllPlayerControls)
        {
            if (KillFlashCheck(killer, target, seer))
            {
                seer.KillFlash();
                continue;
            }
            else if (target.Is(CustomRoles.CyberStar))
            {
                if (!Options.ImpKnowCyberStarDead.GetBool() && seer.GetCustomRole().IsImpostor()) continue;
                if (!Options.NeutralKnowCyberStarDead.GetBool() && seer.GetCustomRole().IsNeutral()) continue;
                seer.KillFlash();
            }
        }
        if (target.Is(CustomRoles.CyberStar) && !Main.CyberStarDead.Contains(target.PlayerId)) Main.CyberStarDead.Add(target.PlayerId);
    }
    public static bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
    {
        return seer.Is(CustomRoles.GM)
|| seer.Is(CustomRoles.Seer)
|| !seer.Data.IsDead && killer != seer && target != seer
&& seer.GetCustomRole() switch
{
    CustomRoles.EvilTracker => EvilTracker.KillFlashCheck(killer, target),
    CustomRoles.Seer => true,
    _ => false
};
    }
    public static void KillFlash(this PlayerControl player)
    {
        //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
        bool ReactorCheck = Main.NormalOptions.MapId == 2 ? IsActive(SystemTypes.Laboratory) : IsActive(SystemTypes.Reactor); //リアクターフラッシュの確認

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
        RoleColor = GetPlayerById(playerId).Is(CustomRoles.Madmate) ? Color.red : GetRoleColor(mainRole);
        foreach (var subRole in Main.PlayerStates[playerId].SubRoles)
        {
            switch (subRole)
            {
                case CustomRoles.LastImpostor:
                    RoleText = GetRoleString("Last-") + RoleText;
                    break;
                case CustomRoles.Madmate:
                    RoleText = GetRoleString("Mad-") + RoleText;
                    break;
            }
        }
        return (RoleText, RoleColor);
    }
    public static string GetKillerText(byte playerId)
    {
        var state = Main.PlayerStates[playerId];

        var KillerId = state.GetRealKiller();
        Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(CustomRoles.Doctor);
        string killer = state.IsDead ? (GetString("KilledBy") + Main.AllPlayerNames[KillerId].RemoveHtmlTags().Replace("\r\n", string.Empty)) : "";
        killer = ColorString(color, killer);

        return killer;
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

        if (p.IsDead && Options.GhostIgnoreTasks.GetBool()) hasTasks = false;
        var role = States.MainRole;
        switch (role)
        {
            case CustomRoles.GM:
            case CustomRoles.Sheriff:
            case CustomRoles.Arsonist:
            case CustomRoles.Jackal:
            case CustomRoles.Jester:
            case CustomRoles.Opportunist:
            case CustomRoles.Mario:
            case CustomRoles.God:
            case CustomRoles.SwordsMan:
            case CustomRoles.Innocent:
            case CustomRoles.Pelican:
            case CustomRoles.Revolutionist:
            case CustomRoles.FFF:
            case CustomRoles.Gamer:
            case CustomRoles.DarkHide:
                hasTasks = false;
                break;
            case CustomRoles.Terrorist:
                if (ForRecompute)
                    hasTasks = false;
                break;
            case CustomRoles.Executioner:
                hasTasks = Executioner.ChangeRolesAfterTargetKilled.GetValue() == 0 && !ForRecompute;
                break;
            case CustomRoles.Workaholic:
                hasTasks = !ForRecompute && (!Options.WorkaholicCannotWinAtDeath.GetBool() || !p.IsDead);
                break;
            default:
                if (role.IsImpostor()) hasTasks = false;
                break;
        }

        foreach (var subRole in States.SubRoles)
            switch (subRole)
            {
                case CustomRoles.Lovers:
                    //ラバーズがクルー陣営の場合タスクを付与しない
                    if (role.IsCrewmate() && Options.LoverHasNoTask.GetBool())
                        hasTasks = false;
                    break;
                case CustomRoles.Madmate:
                    if (role is not CustomRoles.SpeedBooster or CustomRoles.Snitch or CustomRoles.Transporter or CustomRoles.TimeManager)
                        hasTasks = false;
                    else
                    {
                        if (ForRecompute)
                            hasTasks = false;
                    }
                    break;
            }

        return hasTasks;
    }
    public static bool CanBeMadmate(this PlayerControl pc)
    {
        return pc != null && pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Madmate)
&& !(
            (pc.Is(CustomRoles.Sheriff) && !Options.SheriffCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Mayor) && !Options.MayorCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.NiceGuesser) && !Options.NGuesserCanBeMadmate.GetBool()) ||
            pc.Is(CustomRoles.Needy) ||
            pc.Is(CustomRoles.Snitch) ||
            pc.Is(CustomRoles.CyberStar) ||
            pc.Is(CustomRoles.Egoist)
            );
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
        var ProgressText = new StringBuilder();
        var role = Main.PlayerStates[playerId].MainRole;
        switch (role)
        {
            case CustomRoles.Mario:
                ProgressText.Append(ColorString(GetRoleColor(CustomRoles.Mario).ShadeColor(0.25f), $"({(Main.MarioVentCount.TryGetValue(playerId, out var count) ? count : 0)}/{Options.MarioVentNumWin.GetInt()})"));
                break;
            case CustomRoles.Arsonist:
                var doused = GetDousedPlayerCount(playerId);
                ProgressText.Append(ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})"));
                break;
            case CustomRoles.Sheriff:
                ProgressText.Append(Sheriff.GetShotLimit(playerId));
                break;
            case CustomRoles.Sniper:
                ProgressText.Append(Sniper.GetBulletCount(playerId));
                break;
            case CustomRoles.EvilTracker:
                ProgressText.Append(EvilTracker.GetMarker(playerId));
                break;
            case CustomRoles.SwordsMan:
                ProgressText.Append(SwordsMan.GetKillLimit(playerId));
                break;
            case CustomRoles.TimeThief:
                ProgressText.Append(TimeThief.GetProgressText(playerId));
                break;
            case CustomRoles.Pelican:
                ProgressText.Append(Pelican.GetProgressText(playerId));
                break;
            case CustomRoles.Counterfeiter:
                ProgressText.Append(Counterfeiter.GetSeelLimit(playerId));
                break;
            case CustomRoles.Revolutionist:
                var draw = GetDrawPlayerCount(playerId, out var _);
                ProgressText.Append(ColorString(GetRoleColor(CustomRoles.Revolutionist).ShadeColor(0.25f), $"({draw.Item1}/{draw.Item2})"));
                break;
            case CustomRoles.Gangster:
                ProgressText.Append(Gangster.GetRecruitLimit(playerId));
                break;
            case CustomRoles.Medicaler:
                ProgressText.Append(Medicaler.GetSkillLimit(playerId));
                break;
            case CustomRoles.CursedWolf:
                int SpellCount = Main.CursedWolfSpellCount[playerId];
                ProgressText.Append(ColorString(GetRoleColor(CustomRoles.CursedWolf), $"({SpellCount})"));
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

                    if (Workhorse.IsThisRole(playerId))
                        NonCompleteColor = Workhorse.RoleColor;

                    var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

                    TextColor = comms ? Color.gray : NormalColor;
                    string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                    ProgressText.Append(ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})"));
                }
                break;
        }
        if (ProgressText.Length != 0)
            ProgressText.Insert(0, " "); //空じゃなければ空白を追加

        return ProgressText.ToString();
    }
    public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
    {
        SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);

        if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
        if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
        if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
        if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
        if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
        foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
        {
            if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
        }

        if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
    }
    public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
    {
        if (Options.DIYGameSettings.GetBool())
        {
            SendMessage(GetString("Message.NowOverrideText"), PlayerId);
            return;
        }
        if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
        {
            SendMessage(GetString("Message.HideGameSettings"), PlayerId);
            return;
        }
        var mapId = Main.NormalOptions.MapId;
        var sb = new StringBuilder();
        sb.Append(GetString("Settings")).Append(":");
        foreach (var role in Options.CustomRoleCounts)
        {
            if (!role.Key.IsEnable()) continue;
            string mode = role.Key.GetMode() == 1 ? "启用" : "优先";
            sb.Append($"\n【{GetRoleName(role.Key)}:{mode}×{role.Key.GetCount()}】\n");
            ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
        {
            if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
            else
                sb.Append($"\n【{opt.GetName(true)}】\n");
            ShowChildrenSettings(opt, ref sb);
            var text = sb.ToString();
            sb.Clear().Append(text.RemoveHtmlTags());
        }
        SendMessage(sb.ToString(), PlayerId);
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
            string mode = role.Key.GetMode() == 1 ? "启用" : "优先";
            sb.Append($"\n【{GetRoleName(role.Key)}:{mode}×{role.Key.GetCount()}】\n");
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
        var sb = new StringBuilder(GetString("Roles")).Append(":");
        sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
        int headCount = -1;
        foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
        {
            headCount++;
            if (role.IsImpostor() && headCount == 0) sb.Append("\n\n● " + GetString("TabGroup.ImpostorRoles"));
            else if (role.IsCrewmate() && headCount == 1) sb.Append("\n\n● " + GetString("TabGroup.CrewmateRoles"));
            else if (role.IsNeutral() && headCount == 2) sb.Append("\n\n● " + GetString("TabGroup.NeutralRoles"));
            else if (role.IsAdditionRole() && headCount == 3) sb.Append("\n\n● " + GetString("TabGroup.Addons"));
            else headCount--;

            string mode = role.GetMode() == 1 ? "启用" : "优先";
            if (role.IsEnable()) sb.AppendFormat("\n{0}:{1}x{2}", GetRoleName(role), $"{mode}", role.GetCount());
        }
        SendMessage(sb.ToString(), PlayerId);
    }
    public static void ShowChildrenSettings(OptionItem option, ref StringBuilder sb, int deep = 0, bool command = false)
    {
        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
        {
            if (command)
            {
                sb.Append("\n\n");
                command = false;
            }
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
            sb.Append($"{opt.Value.GetName(true)}: {opt.Value.GetString()}\n");
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
        var text = $"{GetString("PlayerInfo")}:";
        List<byte> cloneRoles = new(Main.PlayerStates.Keys);
        foreach (var id in Main.winnerList)
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            text += $"\n★ " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
            cloneRoles.Remove(id);
        }
        foreach (var id in cloneRoles)
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            text += $"\n　 " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
        }
        if (text == $"{GetString("PlayerInfo")}:") text = "";

        string sumText = "";
        if (SetEverythingUpPatch.LastWinsText != "") sumText += GetString("LastResult") + ": " + $"{SetEverythingUpPatch.LastWinsText}";
        if (SetEverythingUpPatch.LastWinsReason != "") sumText += "\n" + GetString("LastEndReason") + ": " + $"{SetEverythingUpPatch.LastWinsReason}";

        if (text != "") SendMessage(text, PlayerId);
        if (EndGamePatch.KillLog != "") SendMessage(EndGamePatch.KillLog, PlayerId);
        if (sumText != "") SendMessage(sumText, PlayerId);

        if (text == "" && EndGamePatch.KillLog == "" && sumText == "") SendMessage(GetString("NoInfoExists"), PlayerId);

        if (IsUP(PlayerControl.LocalPlayer) && Options.EnableUpMode.GetBool()) SendMessage($"提示：该房间启用了【创作者素材保护计划】，房主可以指定自己的职业。\n该功能仅允许创作者用于获取视频素材，如遇滥用情况，请退出游戏或举报。\n当前创作者认证：{GetUpName(PlayerControl.LocalPlayer)}", PlayerId);

    }


    public static string GetSubRolesText(byte id, bool disableColor = false, bool intro = false, bool summary = false)
    {
        var SubRoles = Main.PlayerStates[id].SubRoles;
        if (SubRoles.Count == 0 && intro == false) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role is CustomRoles.NotAssigned or
                        CustomRoles.LastImpostor) continue;
            if (summary && role is CustomRoles.Madmate) continue;

            var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
            sb.Append($"{ColorString(Color.white, " + ")}{RoleText}");
        }

        if (intro && !SubRoles.Contains(CustomRoles.Lovers) && !SubRoles.Contains(CustomRoles.Ntr) && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
        {
            var RoleText = disableColor ? GetRoleName(CustomRoles.Lovers) : ColorString(GetRoleColor(CustomRoles.Lovers), GetRoleName(CustomRoles.Lovers));
            sb.Append($"{ColorString(Color.white, " + ")}{RoleText}");
        }

        return sb.ToString();
    }

    public static byte MsgToColor(string text, bool isHost = false)
    {
        text = text.ToLowerInvariant();
        text = text.Replace("色", string.Empty);
        int color = -1;
        try { color = int.Parse(text); } catch { color = -1; }
        switch (text)
        {
            case "0": case "红": case "紅": case "red": color = 0; break;
            case "1": case "蓝": case "藍": case "深蓝": case "blue": color = 1; break;
            case "2": case "绿": case "綠": case "深绿": case "green": color = 2; break;
            case "3": case "粉红": case "pink": color = 3; break;
            case "4": case "橘": case "orange": color = 4; break;
            case "5": case "黄": case "黃": case "yellow": color = 5; break;
            case "6": case "黑": case "black": color = 6; break;
            case "7": case "白": case "white": color = 7; break;
            case "8": case "紫": case "purple": color = 8; break;
            case "9": case "棕": case "brown": color = 9; break;
            case "10": case "青": case "cyan": color = 10; break;
            case "11": case "黄绿": case "黃綠": case "浅绿": case "lime": color = 11; break;
            case "12": case "红褐": case "紅褐": case "深红": case "maroon": color = 12; break;
            case "13": case "玫红": case "玫紅": case "浅粉": case "rose": color = 13; break;
            case "14": case "焦黄": case "焦黃": case "淡黄": case "banana": color = 14; break;
            case "15": case "灰": case "gray": color = 15; break;
            case "16": case "茶": case "tan": color = 16; break;
            case "17": case "珊瑚": case "coral": color = 17; break;
            case "18": case "隐藏": case "?": color = 18; break;
        }
        return !isHost && color == 18 ? byte.MaxValue : color is < 0 or > 18 ? byte.MaxValue : Convert.ToByte(color);
    }

    public static void ShowHelpToClient(byte ID)
    {
        SendMessage(
            GetString("CommandList")
            + $"\n  ○ /n {GetString("Command.now")}"
            + $"\n  ○ /r {GetString("Command.roles")}"
            + $"\n  ○ /m {GetString("Command.myrole")}"
            + $"\n  ○ /xf {GetString("Command.solvecover")}"
            + $"\n  ○ /l {GetString("Command.lastresult")}"
            + $"\n  ○ /win {GetString("Command.winner")}"
            + "\n\n" + GetString("CommandOtherList")
            + $"\n  ○ /color {GetString("Command.color")}"
            + $"\n  ○ /qt {GetString("Command.quit")}"
            , ID);
    }

    public static void ShowHelp(byte ID)
    {
        SendMessage(
            GetString("CommandList")
            + $"\n  ○ /n {GetString("Command.now")}"
            + $"\n  ○ /r {GetString("Command.roles")}"
            + $"\n  ○ /m {GetString("Command.myrole")}"
            + $"\n  ○ /l {GetString("Command.lastresult")}"
            + $"\n  ○ /win {GetString("Command.winner")}"
            + "\n\n" + GetString("CommandOtherList")
            + $"\n  ○ /color {GetString("Command.color")}"
            + $"\n  ○ /rn {GetString("Command.rename")}"
            + $"\n  ○ /qt {GetString("Command.quit")}"
            + "\n\n" + GetString("CommandHostList")
            + $"\n  ○ /s {GetString("Command.say")}"
            + $"\n  ○ /rn {GetString("Command.rename")}"
            + $"\n  ○ /xf {GetString("Command.solvecover")}"
            + $"\n  ○ /mw {GetString("Command.mw")}"
            + $"\n  ○ /kill {GetString("Command.kill")}"
            + $"\n  ○ /exe {GetString("Command.exe")}"
            + $"\n  ○ /level {GetString("Command.level")}"
            + $"\n  ○ /id {GetString("Command.idlist")}"
            + $"\n  ○ /qq {GetString("Command.qq")}"
            + $"\n  ○ /dump {GetString("Command.dump")}"
            , ID);
    }

    public static void GetPsychicStuff(PlayerControl seer)
    {
        if (Options.PsychicFresh.GetBool() || !Main.PsychicTarget.ContainsKey(seer.PlayerId))
        {
            var rd = IRandom.Instance;
            int numOfPsychicBad = 0;
            for (int i = 0; i < Options.PsychicCanSeeNum.GetInt(); i++)
            {
                if (rd.Next(1, 100) < 18) numOfPsychicBad++;
            }
            if (numOfPsychicBad > Options.PsychicCanSeeNum.GetInt() || numOfPsychicBad < 1) numOfPsychicBad = 1;

            List<byte> goodids = new();
            List<byte> badids = new();
            Dictionary<byte, bool> isGood = new();
            if (!seer.Data.IsDead)
            {
                List<PlayerControl> badPlayers = new();
                List<PlayerControl> goodPlayers = new();
                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != seer.PlayerId))
                {
                    isGood.Add(pc.PlayerId, true);
                    var role = pc.GetCustomRole();
                    switch (role.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Crewmate:
                            if (Options.CkshowEvil.GetBool())
                                if (!role.IsCK())
                                {
                                    badPlayers.Add(pc);
                                    isGood[pc.PlayerId] = false;
                                }
                            break;
                        case CustomRoleTypes.Impostor:
                            badPlayers.Add(pc); isGood[pc.PlayerId] = false;
                            break;
                        case CustomRoleTypes.Neutral:
                            if (Options.NBshowEvil.GetBool())
                                if (!role.IsNeutralKilling())
                                {
                                    badPlayers.Add(pc);
                                    isGood[pc.PlayerId] = false;
                                }
                            if (Options.NEshowEvil.GetBool())
                            {
                                if (role.IsNeutralKilling())
                                {
                                    badPlayers.Add(pc);
                                    isGood[pc.PlayerId] = false;
                                }
                            }
                            break;
                    }
                    if (isGood[pc.PlayerId]) goodPlayers.Add(pc);
                }
                List<byte> badpcids = new();
                foreach (var p in badPlayers)
                {
                    badpcids.Add(p.PlayerId);
                }
                if (numOfPsychicBad > Options.PsychicCanSeeNum.GetInt() || numOfPsychicBad < 1) numOfPsychicBad = 1;
                int goodPeople = Options.PsychicCanSeeNum.GetInt() - numOfPsychicBad;

                if (numOfPsychicBad != 0)
                    for (var i = 0; i < numOfPsychicBad; i++)
                    {
                        if (badPlayers.Count <= 0) break;
                        var rando = new System.Random();
                        var player = badPlayers[rando.Next(0, badPlayers.Count)];
                        badPlayers.Remove(player);
                        badids.Add(player.PlayerId);
                    }
                if (goodPeople != 0)
                    for (var i = 0; i < goodPeople; i++)
                    {
                        if (goodPlayers.Count <= 0) break;
                        var rando = new System.Random();
                        var player = goodPlayers[rando.Next(0, goodPlayers.Count)];
                        goodPlayers.Remove(player);
                        goodids.Add(player.PlayerId);
                    }
                if (!Main.PsychicTarget.ContainsKey(seer.PlayerId)) Main.PsychicTarget.Add(seer.PlayerId, new List<byte>());
                Main.PsychicTarget[seer.PlayerId] = new();
                Main.PsychicTarget[seer.PlayerId].AddRange(goodids);
                Main.PsychicTarget[seer.PlayerId].AddRange(badids);
            }
        }
    }
    public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var taskState = GetPlayerById(Terrorist.PlayerId).GetPlayerTaskState();
        if (taskState.IsTaskFinished && (!Main.PlayerStates[Terrorist.PlayerId].IsSuicide() || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
        {
            foreach (var pc in Main.AllPlayerControls)
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
        if (IsDev(PlayerControl.LocalPlayer)) return;
        string name = DataManager.player.Customization.Name;
        if (Main.nickName != "") name = Main.nickName;
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId);
        }
        else
        {
            if (!GameStates.IsLobby) return;
            if (AmongUsClient.Instance.IsGamePublic)
                name = $"<color=#ffd6ec>TOHE</color><color=#baf7ca>★</color>" + name;
            switch (Options.GetSuffixMode())
            {
                case SuffixModes.None:
                    break;
                case SuffixModes.TOHE:
                    name += $"\r\n<color={Main.ModColor}>TOHE v{Main.PluginVersion}</color>";
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
                case SuffixModes.DoNotKillMe:
                    name += $"\r\n<color={Main.ModColor}>{GetString("SuffixModeText.DoNotKillMe")}</color>";
                    break;
                case SuffixModes.NoAndroidPlz:
                    name += $"\r\n<color={Main.ModColor}>{GetString("SuffixModeText.NoAndroidPlz")}</color>";
                    break;
            }
        }
        if (name.CompareTo(PlayerControl.LocalPlayer.name) == 0) return;
        if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
    }
    public static PlayerControl GetPlayerById(int PlayerId)
    {
        return Main.AllPlayerControls.Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
    }
    public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
        GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();
    private static readonly StringBuilder SelfSuffix = new();
    private static readonly StringBuilder SelfMark = new(20);
    private static readonly StringBuilder TargetMark = new(20);
    public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Main.AllPlayerControls == null) return;

        var caller = new System.Diagnostics.StackFrame(1, false);
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
        //seer:ここで行われた変更を見ることができるプレイヤー
        //target:seerが見ることができる変更の対象となるプレイヤー
        foreach (var seer in seerList)
        {
            //seerが落ちているときに何もしない
            if (seer == null || seer.Data.Disconnected) continue;

            if (seer.IsModClient()) continue;
            string fontSize = "1.5";
            if (isMeeting && (seer.GetClient().PlatformData.Platform.ToString() == "Playstation" || seer.GetClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
            logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START");

            //タスクなど進行状況を含むテキスト
            string SelfTaskText = GetProgressText(seer);

            //名前の後ろに付けるマーカー
            SelfMark.Clear();

            //インポスター/キル可能なニュートラルに対するSnitch警告
            SelfMark.Append(Snitch.GetWarningArrow(seer));

            //愚者初始化红名玩家
            if (seer.Is(CustomRoles.Psychic)) GetPsychicStuff(seer);

            //ハートマークを付ける(自分に)
            if (seer.Is(CustomRoles.Lovers) || CustomRolesHelper.RoleExist(CustomRoles.Ntr)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Lovers), "♡"));

            //呪われている場合
            SelfMark.Append(Witch.GetSpelledMark(seer.PlayerId, isMeeting));

            //如果是大明星
            if (seer.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                SelfMark.Append(ColorString(GetRoleColor(CustomRoles.SuperStar), "★"));

            //球状闪电提示
            if (BallLightning.IsGhost(seer))
                SelfMark.Append(ColorString(GetRoleColor(CustomRoles.BallLightning), "■"));

            //法医护盾提示
            SelfMark.Append(Medicaler.GetSheildMark(seer));

            //玩家自身血量提示
            SelfMark.Append(Gamer.TargetMark(seer, seer));

            //銃声が聞こえるかチェック
            SelfMark.Append(Sniper.GetShotNotify(seer.PlayerId));
            //Markとは違い、改行してから追記されます。
            SelfSuffix.Clear();

            if (seer.Is(CustomRoles.BountyHunter))
            {
                SelfSuffix.Append(BountyHunter.GetTargetText(seer, false));
                SelfSuffix.Append(BountyHunter.GetTargetArrow(seer));
            }
            if (seer.Is(CustomRoles.FireWorks))
            {
                string stateText = FireWorks.GetStateText(seer);
                SelfSuffix.Append(stateText);
            }
            if (seer.Is(CustomRoles.Witch))
            {
                SelfSuffix.Append(Witch.GetSpellModeText(seer, false, isMeeting));
            }
            if (seer.Is(CustomRoles.AntiAdminer))
            {
                if (AntiAdminer.IsAdminWatch) SelfSuffix.Append("★").Append(GetString("AntiAdminerAD"));
                if (AntiAdminer.IsVitalWatch) SelfSuffix.Append("★").Append(GetString("AntiAdminerVI"));
                if (AntiAdminer.IsDoorLogWatch) SelfSuffix.Append("★").Append(GetString("AntiAdminerDL"));
                if (AntiAdminer.IsCameraWatch) SelfSuffix.Append("★").Append(GetString("AntiAdminerCA"));
            }

            //タスクを終えたSnitchがインポスター/キル可能なニュートラルの方角を確認できる
            SelfSuffix.Append(Snitch.GetSnitchArrow(seer));

            SelfSuffix.Append(EvilTracker.GetTargetArrow(seer, seer));

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
            if (seer.Is(CustomRoles.Revolutionist) && seer.IsDrawDone())
                SelfName = $"</size>\r\n{ColorString(seer.GetRoleColor(), string.Format(GetString("EnterVentWinCountDown"), Main.RevolutionistCountdown.TryGetValue(seer.PlayerId, out var x) ? x : 10))}";
            if (Pelican.IsEaten(seer.PlayerId))
                SelfName = $"</size>\r\n{ColorString(GetRoleColor(CustomRoles.Pelican), GetString("EatenByPelican"))}";
            SelfName = SelfRoleName + "\r\n" + SelfName;
            SelfName += SelfSuffix.ToString() == "" ? "" : "\r\n " + SelfSuffix.ToString();
            if (!isMeeting) SelfName += "\r\n";

            //適用
            seer.RpcSetNamePrivate(SelfName, true, force: NoCache);

            //seerが死んでいる場合など、必要なときのみ第二ループを実行する
            if (true)
            {
                foreach (var target in Main.AllPlayerControls)
                {
                    //targetがseer自身の場合は何もしない
                    if (target == seer) continue;
                    logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START");

                    //名前の後ろに付けるマーカー
                    TargetMark.Clear();

                    //呪われている人
                    TargetMark.Append(Witch.GetSpelledMark(target.PlayerId, isMeeting));

                    //如果是大明星
                    if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                        TargetMark.Append(ColorString(GetRoleColor(CustomRoles.SuperStar), "★"));

                    //球状闪电提示
                    if (BallLightning.IsGhost(target))
                        TargetMark.Append(ColorString(GetRoleColor(CustomRoles.BallLightning), "■"));

                    //タスク完了直前のSnitchにマークを表示
                    TargetMark.Append(Snitch.GetWarningMark(seer, target));

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
                    else if (target.Is(CustomRoles.Ntr) || seer.Is(CustomRoles.Ntr))
                    {
                        TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                    }

                    if (seer.Is(CustomRoles.Arsonist))//seerがアーソニストの時
                    {
                        if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>");
                        }
                        if (
                            Main.ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && //seerがオイルを塗っている途中(現在進行)
                            ar_kvp.Item1 == target //オイルを塗っている対象がtarget
                        )
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Arsonist)}>△</color>");
                        }
                    }
                    if (seer.Is(CustomRoles.Revolutionist))//seer是革命家时
                    {
                        if (seer.IsDrawPlayer(target)) //seer已完成拉拢船员
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>●</color>");
                        }
                        if (Main.RevolutionistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && ar_kvp.Item1 == target)//seer正在拉拢船员
                        {
                            TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Revolutionist)}>○</color>");
                        }
                    }
                    if (seer.Is(CustomRoles.Puppeteer) &&
                    Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                    Main.PuppeteerList.ContainsKey(target.PlayerId))
                        TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Impostor)}>◆</color>");

                    //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                    string TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}>{target.GetDisplayRoleName()}{GetProgressText(target)}</size>\r\n" : "";

                    if (TargetRoleText == "" && !seer.Data.IsDead && seer.GetCustomRole().IsImpostor() && target.GetCustomRole().IsImpostor() && Options.ImpKnowAlliesRole.GetBool())
                        TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                    if (TargetRoleText == "" && !seer.Data.IsDead && seer.Is(CustomRoles.Madmate) && target.GetCustomRole().IsImpostor())
                        TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                    if (seer.Is(CustomRoles.God) && !seer.Data.IsDead)
                        TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                    if (target.Is(CustomRoles.GM))
                        TargetRoleText = $"<size={fontSize}>{target.GetDisplayRoleName()}</size>\r\n";

                    if (seer.Is(CustomRoles.EvilTracker))
                    {
                        TargetMark.Append(EvilTracker.GetTargetMark(seer, target));
                        if (isMeeting && EvilTracker.IsTrackTarget(seer, target) && EvilTracker.CanSeeLastRoomInMeeting)
                            TargetRoleText = $"<size={fontSize}>{EvilTracker.GetArrowAndLastRoom(seer, target)}</size>\r\n";
                    }

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string TargetPlayerName = target.GetRealName(isMeeting);

                    if (seer.Is(CustomRoles.Psychic))
                    {
                        foreach (var id in Main.PsychicTarget[seer.PlayerId])
                        {
                            if (target.PlayerId == id)
                                TargetPlayerName = ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                        }
                    }
                    if (seer.Is(CustomRoles.Mafia) && !seer.IsAlive() && target.IsAlive())
                    {
                        TargetPlayerName = ColorString(GetRoleColor(CustomRoles.Mafia), target.PlayerId.ToString()) + " " + TargetPlayerName;
                    }
                    if (seer.Is(CustomRoles.NiceGuesser) || seer.Is(CustomRoles.EvilGuesser))
                    {
                        if (seer.IsAlive() && target.IsAlive() && isMeeting)
                        {
                            TargetPlayerName = ColorString(GetRoleColor(seer.Is(CustomRoles.NiceGuesser) ? CustomRoles.NiceGuesser : CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + TargetPlayerName;
                        }
                    }

                    //ターゲットのプレイヤー名の色を書き換えます。
                    TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isMeeting);

                    TargetMark.Append(Executioner.TargetMark(seer, target));

                    TargetMark.Append(Gamer.TargetMark(seer, target));

                    TargetMark.Append(Medicaler.TargetMark(seer, target));

                    string TargetDeathReason = "";
                    if (seer.KnowDeathReason(target))
                        TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})";

                    if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isMeeting)
                        TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                    //全てのテキストを合成します。
                    string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                    //適用
                    target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);

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
        BountyHunter.AfterMeetingTasks();
        EvilTracker.AfterMeetingTasks();
        SerialKiller.AfterMeetingTasks();
        if (Options.AirShipVariableElectrical.GetBool())
            AirShipElectricalDoors.Initialize();
    }
    public static void DevNameCheck(ClientData client)
    {
        new LateTask(() =>
        {
            if (client.Character == null) return;
            Dictionary<string, string> DevColor = new()
            {
                { "actorour#0029", Main.ModColor },
            };
            foreach (var dc in DevColor)
            {
                if (client.FriendCode.Equals(dc.Key))
                {
                    string t1 = $"<color={dc.Value}>";
                    string t2 = client.PlayerName;
                    string t3 = "</color>";
                    string name = t1 + t2 + t3;
                    client.Character.RpcSetName(name);
                }
            }
        }, 3f, "Dev Name Check");
    }
    public static void ApplyDevSuffix(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!IsDev(player)) return;
        string name = DataManager.player.Customization.Name;
        if (player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
        {
            if (!player.IsModClient()) return;
            if (Main.OriginalName.ContainsKey(player.GetClientId()))
                name = Main.OriginalName[player.GetClientId()];
            else return;
        }
        if (Main.nickName != "") name = Main.nickName;
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(player.Data.DefaultOutfit.ColorId);
        }
        else
        {
            if (!GameStates.IsLobby) return;
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && AmongUsClient.Instance.IsGamePublic)
                name = $"<color=#ffd6ec>TOHE</color><color=#baf7ca>★</color>" + name;
            switch (player.FriendCode)
            {
                case "actorour#0029":
                    name = $"<color={Main.ModColor}><size=1.7>开发者</size></color>\r\n" + name;
                    break;
                case "pinklaze#1776":
                    name = $"<color=#30548e><size=1.7>开发者</size></color>\r\n" + name;
                    break;
                case "bannerfond#3960":
                    name = $"<color={Main.ModColor}><size=1.7>贡献者</size></color>\r\n" + name;
                    break;
                case "recentduct#6068":
                    name = $"<color=#be4d16><size=1.7>新世纪法师</size></color>\r\n" + name;
                    break;
                case "heavyclod#2286":
                    name = $"<color=#FFFF00><size=1.7>小叨.exe已停止运行</size></color>\r\n" + name;
                    break;
                case "canneddrum#2370":
                    name = $"<color=#fffcbe><size=1.7>我是喜唉awa</size></color>\r\n" + name;
                    break;
                case "dovefitted#5329":
                    name = $"<color=#1379bf><size=1.7>不要首刀我</size></color>\r\n" + name;
                    break;
                case "teamelder#5856":
                    name = $"<color=#1379bf><size=1.7>正义之师（无信誉）</size></color>\r\n" + name;
                    break;
                case "luckylogo#7352":
                    name = $"<color=#f30000><size=1.7>林@林</size></color>\r\n" + name;
                    break;
                case "axefitful#8788":
                    name = $"<color=#8e8171><size=1.7>寄才是真理</size></color>\r\n" + name;
                    break;
                case "storeroan#0331":
                    name = $"<color=#FF0066><size=1.7>Night_瓜</size></color>\r\n" + name;
                    break;
                case "twainrobin#8089":
                    name = $"<color=#0000FF><size=1.7>啊哈修maker</size></color>\r\n" + name;
                    break;
            }
        }
        if (name.CompareTo(player.name) == 0) return;
        if (name != player.name && player.CurrentOutfitType == PlayerOutfitType.Default)
        {
            Logger.Info("设置玩家名字后缀", "ApplyDevSuffix");
            player.RpcSetName(name);
        }
    }
    public static bool IsDev(PlayerControl pc)
    {
        return pc.FriendCode is
            "actorour#0029" or
            "pinklaze#1776" or //NCM
            "bannerfond#3960" or
            "recentduct#6068" or
            "heavyclod#2286" or //小叨院长
            "canneddrum#2370" or //屑人
            "dovefitted#5329" or //ltemten
            "teamelder#5856" or //Slok
            "luckylogo#7352" or //林林林
            "axefitful#8788" or //罗寄
            "storeroan#0331" or //西瓜
            "twainrobin#8089"; //辣鸡
    }
    public static bool CanUseDevCommand(PlayerControl pc)
    {
        return pc.FriendCode is
            "actorour#0029" or
            "pinklaze#1776" or
            "bannerfond#3960" or
            "recentduct#6068";
    }
    public static bool CanUseDevCommand(int pcId)
    {
        var pc = GetPlayerById(pcId);
        return pc != null && IsDev(pc);
    }
    public static bool IsUP(PlayerControl pc)
    {
        return pc.FriendCode is
            "actorour#0029" or
            "truantwarm＃9165" or //萧暮
            "drilldinky#1386" or //河豚
            "heavyclod#2286" or //小叨院长
            "storeroan#0331" or //西瓜
            "farardour#6818"; //提米
    }
    public static string GetUpName(PlayerControl pc)
    {
        return pc.FriendCode switch
        {
            "actorour#0029" => "KARPED1EM",
            "truantwarm＃9165" => "萧暮不姓萧",
            "drilldinky#1386" => "爱玩AU的河豚",
            "heavyclod#2286" => "小叨院长",
            "storeroan#0331" => "Night_瓜",
            "farardour#6818" => "-提米SaMa-",
            _ => "未认证用户",
        };
    }
    public static bool IsUP(int pcId)
    {
        var pc = GetPlayerById(pcId);
        return pc != null && IsUP(pc);
    }
    public static bool IsDev(int pcId)
    {
        var pc = GetPlayerById(pcId);
        return pc != null && IsDev(pc);
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
            foreach (var countTypes in Enum.GetValues(typeof(CountTypes)).Cast<CountTypes>())
            {
                var playersCount = PlayersCount(countTypes);
                if (playersCount == 0) continue;
                sb.Append($"{countTypes}:{AlivePlayersCount(countTypes)}/{playersCount}, ");
            }
            sb.Append($"All:{AllAlivePlayersCount}/{AllPlayersCount}");
            Logger.Info(sb.ToString(), "CountAlivePlayers");
        }
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
        string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TOHE-v{Main.PluginVersion}-{t}.log";
        FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
        file.CopyTo(@filename);
        System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
        if (PlayerControl.LocalPlayer != null)
            HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.DumpfileSaved"), $"TOHE - v{Main.PluginVersion}-{t}.log"));
    }
    public static (int, int) GetDousedPlayerCount(byte playerId)
    {
        int doused = 0, all = 0; //学校で習った書き方
                                 //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

            all++;
            if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                //塗れている場合
                doused++;
        }

        return (doused, all);
    }

    public static (int, int) GetDrawPlayerCount(byte playerId, out List<PlayerControl> winnerList)
    {
        int draw = 0;
        int all = Options.RevolutionistDrawCount.GetInt();
        int max = Main.AllAlivePlayerControls.Count() - 1;
        winnerList = new();
        if (all > max) all = max;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (Main.isDraw.TryGetValue((playerId, pc.PlayerId), out var isDraw) && isDraw)
            {
                winnerList.Add(pc);
                draw++;
            }
        }
        return (draw, all);
    }

    public static string SummaryTexts(byte id, bool disableColor = true, bool check = false)
    {
        var RolePos = TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English ? 47 : 37;
        var name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
        if (id == PlayerControl.LocalPlayer.PlayerId) name = DataManager.player.Customization.Name;
        string summary = $"{ColorString(Main.PlayerColors[id], name)}<pos=22%>{GetProgressText(id)}</pos><pos=30%>{GetVitalText(id, true)}</pos><pos={RolePos}%> {GetDisplayRoleName(id)}{GetSubRolesText(id, summary: true)}</pos>";
        return check && GetDisplayRoleName(id).RemoveHtmlTags().Contains("INVALID:NotAssigned")
            ? "INVALID"
            : disableColor ? summary.RemoveHtmlTags() : summary;
    }
    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
    public static bool CanMafiaKill()
    {
        if (Main.PlayerStates == null) return false;
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int LivingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) LivingImpostorsNum++;
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
    public static int AllPlayersCount => Main.PlayerStates.Values.Count(state => state.countTypes != CountTypes.OutOfGame);
    public static int AllAlivePlayersCount => Main.AllAlivePlayerControls.Count(pc => !pc.Is(CountTypes.OutOfGame));
    public static bool IsAllAlive => Main.PlayerStates.Values.All(state => state.countTypes == CountTypes.OutOfGame || !state.IsDead);
    public static int PlayersCount(CountTypes countTypes) => Main.PlayerStates.Values.Count(state => state.countTypes == countTypes);
    public static int AlivePlayersCount(CountTypes countTypes) => Main.AllAlivePlayerControls.Count(pc => pc.Is(countTypes));
}