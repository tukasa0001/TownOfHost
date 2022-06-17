using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
            Logger.Info($"SystemTypes:{type}", "SwitchSystem");
            return SwitchSystem != null && SwitchSystem.IsActive;
        }
        public static void SetVision(this GameOptionsData opt, PlayerControl player, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.CrewLightMod = opt.ImpostorLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.CrewLightMod *= 5;
                return;
            }
            else
            {
                opt.ImpostorLightMod = opt.CrewLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.ImpostorLightMod /= 5;
                return;
            }
        }
        public static string GetOnOff(bool value) => value ? "ON" : "OFF";
        public static int SetRoleCountToggle(int currentCount) => currentCount > 0 ? 0 : 1;
        public static void SetRoleCountToggle(CustomRoles role)
        {
            int count = Options.GetRoleCount(role);
            count = SetRoleCountToggle(count);
            Options.SetRoleCount(role, count);
        }
        public static string GetRoleName(CustomRoles role)
        {
            var lang = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || Main.ForceJapanese.Value) &&
                Main.JapaneseRoleName.Value == true ? SupportedLangs.Japanese : SupportedLangs.English;

            return GetRoleName(role, lang);
        }
        public static string GetRoleName(CustomRoles role, SupportedLangs lang)
        {
            return GetString(Enum.GetName(typeof(CustomRoles), role), lang);
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
        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.GetCustomRole();
            /*if (player.isLastImpostor())
            {
                RoleText = $"{getRoleName(cRole)} ({getString("Last")})";
            }
            else*/
            RoleText = GetRoleName(cRole);

            return (RoleText, GetRoleColor(cRole));
        }

        public static string GetVitalText(byte player) =>
            PlayerState.isDead[player] ? GetString("DeathReason." + PlayerState.GetDeathReason(player)) : GetString("Alive");
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
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;

            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                var hasRole = Main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
                }
            }
            else
            {
                var cRoleFound = Main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
                if (cRoleFound)
                {
                    if (cRole == CustomRoles.Jester) hasTasks = false;
                    if (cRole == CustomRoles.MadGuardian && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.MadSnitch && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Sheriff) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.SKMadmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Executioner) hasTasks = false;
                    if (cRole == CustomRoles.Impostor) hasTasks = false;
                    if (cRole == CustomRoles.Shapeshifter) hasTasks = false;
                    if (cRole == CustomRoles.Arsonist) hasTasks = false;
                    if (cRole == CustomRoles.SchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.CSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.MSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.EgoSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.Egoist) hasTasks = false;

                    //foreach (var pc in PlayerControl.AllPlayerControls)
                    //{
                    //if (cRole == CustomRoles.Sheriff && main.SheriffShotLimit[pc.PlayerId] == 0) hasTasks = true;
                    //}
                }
                var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(p.PlayerId, out var cSubRole);
                if (cSubRoleFound)
                {
                    if (cSubRole == CustomRoles.Lovers)
                    {
                        //ラバーズがクルー陣営の場合タスクを付与しない
                        if (cRole.GetRoleType() == RoleType.Crewmate)
                        {
                            hasTasks = false;
                        }
                    }
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
            string colorCode = "<color=#ffff00>";
            string closeCode = "</color>";
            if (!Main.AllPlayerCustomRoles.TryGetValue(playerId, out var role)) return $" {colorCode}Invalid{closeCode}";
            string ProgressText = "";
            switch (role)
            {
                case CustomRoles.Arsonist:
                    var doused = Utils.getDousedPlayerCount(playerId);
                    ProgressText = $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>({doused.Item1}/{doused.Item2}){closeCode}";
                    break;
                case CustomRoles.Sheriff:
                    ProgressText += colorCode + (Main.SheriffShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid") + closeCode;
                    break;
                case CustomRoles.Sniper:
                    ProgressText += $" {Sniper.GetBulletCount(playerId)}";
                    break;
                default:
                    //タスクテキスト
                    var taskState = PlayerState.taskState?[playerId];
                    if (taskState.hasTasks)
                    {
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = $"{colorCode}({Completed}/{taskState.AllTasksCount}){closeCode}";
                    }
                    break;
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText += $" [{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]";

            return ProgressText;
        }
        public static void ShowActiveSettingsHelp()
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":");
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"));
                if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong")); }
                if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong")); }
            }
            else
            {
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo")); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo")); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo")); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"));
                }
                if (Options.EnableLastImpostor.GetBool()) { SendMessage(GetString("LastImpostor") + GetString("LastImpostorInfo")); }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo")); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
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
                ShowActiveRoles(PlayerId);
                text = GetString("Attributes") + ":";
                if (Options.EnableLastImpostor.GetBool())
                {
                    text += String.Format("\n{0}:{1}", GetString("LastImpostor"), Options.EnableLastImpostor.GetString());
                }
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    bool isFirst = true;
                    foreach (var c in Options.CustomRoleSpawnChances[role.Key].Children)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        text += $"\n{c.GetName(disableColor: true)}:{c.GetString()}";

                        //タスク上書き設定用の処理
                        if (c.Name == "doOverride" && c.GetBool() == true)
                        {
                            foreach (var d in c.Children)
                            {
                                text += $"\n{d.GetName(disableColor: true)}:{d.GetString()}";
                            }
                        }
                        //メイヤーのポータブルボタン使用可能回数
                        if (c.Name == "MayorHasPortableButton" && c.GetBool() == true)
                        {
                            foreach (var d in c.Children)
                            {
                                text += $"\n{d.GetName(disableColor: true)}:{d.GetString()}";
                            }
                        }
                    }
                }
                if (Options.EnableLastImpostor.GetBool()) text += String.Format("\n{0}:{1}", GetString("LastImpostorKillCooldown"), Options.LastImpostorKillCooldown.GetString());
                if (Options.SyncButtonMode.GetBool()) text += String.Format("\n{0}:{1}", GetString("SyncedButtonCount"), Options.SyncedButtonCount.GetInt());
                if (Options.SabotageTimeControl.GetBool())
                {
                    if (PlayerControl.GameOptions.MapId == 2) text += String.Format("\n{0}:{1}", GetString("PolusReactorTimeLimit"), Options.PolusReactorTimeLimit.GetString());
                    if (PlayerControl.GameOptions.MapId == 4) text += String.Format("\n{0}:{1}", GetString("AirshipReactorTimeLimit"), Options.AirshipReactorTimeLimit.GetString());
                }
                if (Options.GetWhenSkipVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenSkipVote"), Options.WhenSkipVote.GetString());
                if (Options.GetWhenNonVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenNonVote"), Options.WhenNonVote.GetString());
                if ((Options.GetWhenNonVote() == VoteMode.Suicide || Options.GetWhenSkipVote() == VoteMode.Suicide) && CustomRoles.Terrorist.IsEnable()) text += String.Format("\n{0}:{1}", GetString("CanTerroristSuicideWin"), Options.CanTerroristSuicideWin.GetBool());
            }
            if (Options.StandardHAS.GetBool()) text += String.Format("\n{0}:{1}", GetString("StandardHAS"), GetOnOff(Options.StandardHAS.GetBool()));
            if (Options.NoGameEnd.GetBool()) text += String.Format("\n{0}:{1}", GetString("NoGameEnd"), GetOnOff(Options.NoGameEnd.GetBool()));
            SendMessage(text, PlayerId);
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            var text = GetString("Roles") + ":";
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.IsEnable()) text += string.Format("\n{0}:{1}x{2}", GetRoleName(role), $"{role.GetChance() * 100}%", role.GetCount());
            }
            SendMessage(text, PlayerId);
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastroles"), PlayerId);
                return;
            }
            var text = GetString("LastResult") + ":";
            Dictionary<byte, CustomRoles> cloneRoles = new(Main.AllPlayerCustomRoles);
            foreach (var id in Main.winnerList)
            {
                text += $"\n★ {Main.AllPlayerNames[id]}:{GetRoleName(Main.AllPlayerCustomRoles[id])}{GetShowLastSubRolesText(id)}";
                text += $" {GetVitalText(id)}";
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                text += $"\n　 {Main.AllPlayerNames[id]}:{GetRoleName(Main.AllPlayerCustomRoles[id])}{GetShowLastSubRolesText(id)}";
                text += $" {GetVitalText(id)}";
            }
            SendMessage(text, PlayerId);
        }

        public static string GetShowLastSubRolesText(byte id)
        {
            var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(id, out var cSubRole);
            return !cSubRoleFound || cSubRole == CustomRoles.NoSubRoleAssigned ? "" : " + " + GetRoleName(cSubRole);
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
                + $"\n/h attributes {GetString("Command.h_attributes")}"
                + $"\n/h modes {GetString("Command.h_modes")}"
                + $"\n/dump - {GetString("Command.dump")}"
                );

        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taskState = GetPlayerById(Terrorist.PlayerId).GetPlayerTaskState();
            if (taskState.IsTaskFinished && (!PlayerState.IsSuicide(Terrorist.PlayerId) || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (PlayerState.GetDeathReason(pc.PlayerId) != PlayerState.DeathReason.Vote)
                        {
                            //キルされた場合は自爆扱い
                            PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //生存者は爆死
                        pc.RpcMurderPlayer(pc);
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Bombed);
                        PlayerState.SetDead(pc.PlayerId);
                    }
                }
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TerroristWin, Hazel.SendOption.Reliable, -1);
                writer.Write(Terrorist.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPC.TerroristWin(Terrorist.PlayerId);
            }
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Main.MessagesToSend.Add((text, sendTo));
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
            if (Main.nickName != "") name = Main.nickName;
            if (!AmongUsClient.Instance.IsGameStarted)
            {
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + Main.modColor + ">TOH v" + Main.PluginVersion + "</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += "\r\n配信中";
                        break;
                    case SuffixModes.Recording:
                        name += "\r\n録画中";
                        break;
                }
            }
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
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
                    (Options.SnitchCanFindNeutralKiller.GetBool() && seer.Is(CustomRoles.Egoist));//or エゴイスト

                if (canFindSnitchRole && ShowSnitchWarning && !isMeeting)
                {
                    var arrows = "";
                    foreach (var arrow in Main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
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
                if (Main.SpelledPlayer.Find(x => x.PlayerId == seer.PlayerId) != null && isMeeting)
                    SelfMark += "<color=#ff0000>†</color>";

                if (Sniper.IsEnable())
                {
                    //銃声が聞こえるかチェック
                    SelfMark += Sniper.GetShotNotify(seer.PlayerId);
                }
                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if (seer.Is(CustomRoles.BountyHunter) && seer.GetBountyTarget() != null)
                {
                    string BountyTargetName = seer.GetBountyTarget().GetRealName(isMeeting);
                    SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                }
                if (seer.Is(CustomRoles.FireWorks))
                {
                    string stateText = FireWorks.GetStateText(seer);
                    SelfSuffix = $"{stateText}";
                }
                if (seer.Is(CustomRoles.Witch))
                {
                    if (seer.GetKillOrSpell() == false) SelfSuffix = "Mode:" + GetString("WitchModeKill");
                    if (seer.GetKillOrSpell() == true) SelfSuffix = "Mode:" + GetString("WitchModeSpell");
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
                                if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
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

                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.GetRealName(isMeeting);

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = $"<size={fontSize}><color={seer.GetRoleColorCode()}>{seer.GetRoleName()}</color>{SelfTaskText}</size>";
                string SelfName = $"<color={seer.GetRoleColorCode()}>{SeerRealName}</color>{SelfMark}";
                if (seer.Is(CustomRoles.Arsonist) && seer.IsDouseDone())
                    SelfName = $"</size>\r\n<color={seer.GetRoleColorCode()}>{GetString("EnterVentToWin")}</color>";
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
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Lovers)
                    || Main.SpelledPlayer.Count > 0
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.Is(CustomRoles.Puppeteer)
                    || IsActive(SystemTypes.Electrical)
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START", "NotifyRoles");

                        //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetTaskText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"{GetProgressText(target)}" : "";

                        //名前の後ろに付けるマーカー
                        string TargetMark = "";
                        //呪われている人
                        if (Main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null && isMeeting)
                            TargetMark += "<color=#ff0000>†</color>";
                        //タスク完了直前のSnitchにマークを表示
                        canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //Seerがインポスター
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.Is(CustomRoles.Egoist));//or エゴイスト

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

                        //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}><color={target.GetRoleColorCode()}>{target.GetRoleName()}</color>{TargetTaskText}</size>" : "";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.GetRealName(isMeeting);

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors) //Seerがインポスターが誰かわかる状態
                        {
                            //スニッチはオプション有効なら第三陣営のキル可能役職も見れる
                            var snitchOption = seer.Is(CustomRoles.Snitch) && Options.SnitchCanFindNeutralKiller.GetBool();
                            var foundCheck = target.GetCustomRole().IsImpostor() || (snitchOption && target.Is(CustomRoles.Egoist));
                            if (foundCheck)
                                TargetPlayerName = $"<color={target.GetRoleColorCode()}>{TargetPlayerName}</color>";
                        }
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = $"<color={GetRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else if (seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = $"<color={GetRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting)
                            TargetPlayerName = $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>{TargetPlayerName}</color>"; //targetの赤色で表示
                        else
                        {
                            //NameColorManager準拠の処理
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }
                        foreach (var ExecutionerTarget in Main.ExecutionerTarget)
                        {
                            if ((seer.PlayerId == ExecutionerTarget.Key || seer.Data.IsDead) && //seerがKey or Dead
                            target.PlayerId == ExecutionerTarget.Value) //targetがValue
                                TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Executioner)}>♦</color>";
                        }

                        string TargetDeathReason = "";
                        if (seer.Is(CustomRoles.Doctor) && //seerがDoctor
                        target.Data.IsDead //変更対象が死人
                        )
                            TargetDeathReason = $"(<color={GetRoleColorCode(CustomRoles.Doctor)}>{GetVitalText(target.PlayerId)}</color>)";

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}\r\n{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);

                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END", "NotifyRoles");
                    }
                }
                TownOfHost.Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END", "NotifyRoles");
            }
            Main.witchMeeting = false;
        }
        public static void CustomSyncAllSettings()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.CustomSyncSettings();
            }
        }
        public static void AfterMeetingTasks()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.SerialKiller))
                {
                    pc.RpcResetAbilityCooldown();
                    Main.SerialKillerTimer.TryAdd(pc.PlayerId, 0f);
                }
                if (pc.Is(CustomRoles.BountyHunter))
                {
                    pc.RpcResetAbilityCooldown();
                    Main.BountyTimer.TryAdd(pc.PlayerId, 0f);
                }
            }
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
                if (pc_role.IsImpostor() && !pc.Data.IsDead) AliveImpostorCount++;
            }
            TownOfHost.Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
        }
        public static string GetAllRoleName(byte playerId)
        {
            return GetPlayerById(playerId)?.GetAllRoleName() ?? "";
        }
        public static string GetNameWithRole(byte playerId)
        {
            return GetPlayerById(playerId)?.GetNameWithRole() ?? "";
        }
        public static string GetNameWithRole(this GameData.PlayerInfo player)
        {
            return GetPlayerById(player.PlayerId)?.GetNameWithRole() ?? "";
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
            return t?.PadRight(num - (bc - t.Length));
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
        public static (int, int) getDousedPlayerCount(byte playerId)
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
    }
}