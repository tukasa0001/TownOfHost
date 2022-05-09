using System;
using System.Linq;
using UnityEngine;
using Hazel;
using System.Collections.Generic;
using static TownOfHost.Translator;
namespace TownOfHost
{
    public static class Utils
    {
        public static bool isActive(SystemTypes type)
        {
            var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
            Logger.info($"SystemTypes:{type}", "SwitchSystem");

            if (SwitchSystem != null && SwitchSystem.IsActive)
                return true;

            return false;
        }
        public static void SetVision(this GameOptionsData opt, PlayerControl player, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.CrewLightMod = opt.ImpostorLightMod;
                if (isActive(SystemTypes.Electrical))
                    opt.CrewLightMod *= 5;
                return;
            }
            else
            {
                opt.ImpostorLightMod = opt.CrewLightMod;
                if (isActive(SystemTypes.Electrical))
                    opt.ImpostorLightMod /= 5;
                return;
            }
        }
        public static string getOnOff(bool value) => value ? "ON" : "OFF";
        public static int SetRoleCountToggle(int currentCount)
        {
            if (currentCount > 0) return 0;
            else return 1;
        }
        public static void SetRoleCountToggle(CustomRoles role)
        {
            int count = Options.getRoleCount(role);
            count = SetRoleCountToggle(count);
            Options.setRoleCount(role, count);
        }
        public static string getRoleName(CustomRoles role)
        {
            var lang = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || main.ForceJapanese.Value) &&
                main.JapaneseRoleName.Value == true ? SupportedLangs.Japanese : SupportedLangs.English;

            return getRoleName(role, lang);
        }
        public static string getRoleName(CustomRoles role, SupportedLangs lang)
        {
            return getString(Enum.GetName(typeof(CustomRoles), role), lang);
        }
        public static string getDeathReason(PlayerState.DeathReason status)
        {
            return getString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason), status));
        }
        public static Color getRoleColor(CustomRoles role)
        {
            if (!main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string getRoleColorCode(CustomRoles role)
        {
            if (!main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            return hexColor;
        }
        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.getCustomRole();
            /*if (player.isLastImpostor())
            {
                RoleText = $"{getRoleName(cRole)} ({getString("Last")})";
            }
            else*/
            RoleText = getRoleName(cRole);
            if (player.isSheriff())
                RoleText += $" ({main.SheriffShotLimit[player.PlayerId]})";

            return (RoleText, getRoleColor(cRole));
        }

        public static string getVitalText(byte player)
        {
            string text = null;
            if (PlayerState.isDead[player])
            {
                text = getString("DeathReason." + PlayerState.getDeathReason(player));
            }
            else
            {
                text = getString("Alive");
            }
            return text;
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
        public static bool hasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
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
                var hasRole = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role == CustomRoles.HASFox || role == CustomRoles.HASTroll) hasTasks = false;
                }
            }
            else
            {
                var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
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
            }
            return hasTasks;
        }
        public static string getTaskText(PlayerControl pc)
        {
            var taskState = pc.getPlayerTaskState();
            if (!taskState.hasTasks) return "null";
            var Comms = false;
            foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                if (task.TaskType == TaskTypes.FixComms)
                {
                    Comms = true;
                    break;
                }
            string Completed = Comms ? "?" : $"{taskState.CompletedTasksCount}";
            return $"<color=#ffff00>({Completed}/{taskState.AllTasksCount})</color>";
        }
        public static string getTaskText(byte playerId)
        {
            var taskState = PlayerState.taskState[playerId];
            if (!taskState.hasTasks) return "";
            return $"<color=#ffff00>({taskState.CompletedTasksCount}/{taskState.AllTasksCount})</color>";
        }
        public static void ShowActiveRoles()
        {
            SendMessage(getString("CurrentActiveSettingsHelp") + ":");
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(getString("HideAndSeekInfo"));
                if (CustomRoles.HASFox.isEnable()) { SendMessage(getRoleName(CustomRoles.HASFox) + getString("HASFoxInfoLong")); }
                if (CustomRoles.HASTroll.isEnable()) { SendMessage(getRoleName(CustomRoles.HASTroll) + getString("HASTrollInfoLong")); }
            }
            else
            {
                if (Options.SyncButtonMode.GetBool()) { SendMessage(getString("SyncButtonModeInfo")); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(getString("SabotageTimeControlInfo")); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(getString("RandomMapsModeInfo")); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role == CustomRoles.HASFox || role == CustomRoles.HASTroll) continue;
                    if (role.isEnable()) SendMessage(getRoleName(role) + getString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"));
                }
                if (Options.EnableLastImpostor.GetBool()) { SendMessage(getString("LastImpostor") + getString("LastImpostorInfo")); }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(getString("NoGameEndInfo")); }
        }
        public static void ShowActiveSettings()
        {
            var text = getString("Roles") + ":";
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (CustomRoles.HASFox.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(CustomRoles.HASFox), CustomRoles.HASFox.getCount());
                if (CustomRoles.HASTroll.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.getCount());
                SendMessage(text);
                text = getString("Settings") + ":";
                text += getString("HideAndSeek");
            }
            else
            {
                foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
                {
                    if (role == CustomRoles.HASFox || role == CustomRoles.HASTroll) continue;
                    if (role.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(role), role.getCount());
                }
                SendMessage(text);
                text = getString("Attributes") + ":";
                if (Options.EnableLastImpostor.GetBool())
                {
                    text += String.Format("\n{0}:{1}", getString("LastImpostor"), Options.EnableLastImpostor.GetString());
                }
                SendMessage(text);
                text = getString("Settings") + ":";
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.isEnable()) continue;
                    bool isFirst = true;
                    foreach (var c in Options.CustomRoleSpawnChances[role.Key].Children)
                    {
                        if (isFirst) { isFirst = false; continue; }
                        text += $"\n{getString(c.Name)}:{c.GetString()}";
                    }
                }
                if (Options.EnableLastImpostor.GetBool()) text += String.Format("\n{0}:{1}", getString("LastImpostorKillCooldown"), Options.LastImpostorKillCooldown.GetString());
                if (Options.SyncButtonMode.GetBool()) text += String.Format("\n{0}:{1}", getString("SyncedButtonCount"), Options.SyncedButtonCount);
                if (Options.SabotageTimeControl.GetBool())
                {
                    if (PlayerControl.GameOptions.MapId == 2) text += String.Format("\n{0}:{1}", getString("PolusReactorTimeLimit"), Options.PolusReactorTimeLimit.GetString());
                    if (PlayerControl.GameOptions.MapId == 4) text += String.Format("\n{0}:{1}", getString("AirshipReactorTimeLimit"), Options.AirshipReactorTimeLimit.GetString());
                }
                if (Options.GetWhenSkipVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", getString("WhenSkipVote"), Options.WhenSkipVote.GetString());
                if (Options.GetWhenNonVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", getString("WhenNonVote"), Options.WhenNonVote.GetString());
                if ((Options.GetWhenNonVote() == VoteMode.Suicide || Options.GetWhenSkipVote() == VoteMode.Suicide) && CustomRoles.Terrorist.isEnable()) text += String.Format("\n{0}:{1}", getString("CanTerroristSuicideWin"), Options.CanTerroristSuicideWin.GetBool());
            }
            if (Options.StandardHAS.GetBool()) text += String.Format("\n{0}:{1}", getString("StandardHAS"), getOnOff(Options.StandardHAS.GetBool()));
            if (Options.NoGameEnd.GetBool()) text += String.Format("\n{0}:{1}", getString("NoGameEnd"), getOnOff(Options.NoGameEnd.GetBool()));
            SendMessage(text);
        }
        public static void ShowLastRoles()
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(getString("CantUse/lastroles"));
                return;
            }
            var text = getString("LastResult") + ":";
            Dictionary<byte, CustomRoles> cloneRoles = new(main.AllPlayerCustomRoles);
            foreach (var id in main.winnerList)
            {
                text += $"\n★ {main.AllPlayerNames[id]}:{getRoleName(main.AllPlayerCustomRoles[id])}";
                text += $" {getVitalText(id)}";
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                text += $"\n　 {main.AllPlayerNames[id]} : {getRoleName(main.AllPlayerCustomRoles[id])}";
                text += $" {getVitalText(id)}";
            }
            SendMessage(text);
        }

        public static void ShowHelp()
        {
            SendMessage(
                getString("CommandList")
                + $"\n/winner - {getString("Command.winner")}"
                + $"\n/lastroles - {getString("Command.lastroles")}"
                + $"\n/rename - {getString("Command.rename")}"
                + $"\n/now - {getString("Command.now")}"
                + $"\n/h now - {getString("Command.h_now")}"
                + $"\n/h roles {getString("Command.h_roles")}"
                + $"\n/h attributes {getString("Command.h_attributes")}"
                + $"\n/h modes {getString("Command.h_modes")}"
                + $"\n/dump - {getString("Command.dump")}"
                );

        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taskState = getPlayerById(Terrorist.PlayerId).getPlayerTaskState();
            if (taskState.isTaskFinished && (!PlayerState.isSuicide(Terrorist.PlayerId) || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.isTerrorist())
                    {
                        if (PlayerState.getDeathReason(pc.PlayerId) != PlayerState.DeathReason.Vote)
                        {
                            //キルされた場合は自爆扱い
                            PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //生存者は爆死
                        pc.MurderPlayer(pc);
                        PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Bombed);
                        PlayerState.setDead(pc.PlayerId);
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
            var tmp_text = text.Replace("#", "＃").Replace("<", "＜").Replace(">", "＞");
            string[] textList = tmp_text.Split('\n');
            string tmp = "";
            var l = 0;
            foreach (string t in textList)
            {
                if (tmp.Length + t.Length < 120 && l < 4)
                {
                    tmp += t + "\n";
                    l++;
                }
                else
                {
                    main.MessagesToSend.Add((tmp, sendTo));
                    tmp = t + "\n";
                    l = 1;
                }
            }
            if (tmp.Length != 0) main.MessagesToSend.Add((tmp, sendTo));
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
            if (main.nickName != "") name = main.nickName;
            if (!AmongUsClient.Instance.IsGameStarted)
            {
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + main.modColor + ">TOH v" + main.PluginVersion + "</color>";
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
        public static PlayerControl getPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.AllPlayerControls == null) return;

            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            TownOfHost.Logger.info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            //Snitch警告表示のON/OFF
            bool ShowSnitchWarning = false;
            if (CustomRoles.Snitch.isEnable())
            {
                foreach (var snitch in PlayerControl.AllPlayerControls)
                {
                    if (snitch.isSnitch() && !snitch.Data.IsDead && !snitch.Data.Disconnected)
                    {
                        var taskState = snitch.getPlayerTaskState();
                        if (taskState.doExpose)
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
                string fontSize = "1.5";
                if (isMeeting && (seer.getClient().PlatformData.Platform.ToString() == "Playstation" || seer.getClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
                TownOfHost.Logger.info("NotifyRoles-Loop1-" + seer.name + ":START", "NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない
                if (seer.Data.Disconnected) continue;

                //seerがタスクを持っている：タスク残量の色コードなどを含むテキスト
                //seerがタスクを持っていない：空
                string SelfTaskText = hasTasks(seer.Data, false) ? $"{getTaskText(seer)}" : "";
                //Loversのハートマークなどを入れてください。
                string SelfMark = "";

                //インポスター/キル可能な第三陣営に対するSnitch警告
                var canFindSnitchRole = seer.getCustomRole().isImpostor() || //LocalPlayerがインポスター
                    (Options.SnitchCanFindNeutralKiller.GetBool() && seer.isEgoist());//or エゴイスト

                if (canFindSnitchRole && ShowSnitchWarning && !isMeeting)
                {
                    var arrows = "";
                    foreach (var arrow in main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                        {
                            //自分用の矢印で対象が死んでない時
                            arrows += arrow.Value;
                        }
                    }
                    SelfMark += $"<color={getRoleColorCode(CustomRoles.Snitch)}>★{arrows}</color>";
                }

                //呪われている場合
                if (main.SpelledPlayer.Find(x => x.PlayerId == seer.PlayerId) != null && isMeeting)
                    SelfMark += "<color=#ff0000>†</color>";
                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if (seer.isBountyHunter() && seer.getBountyTarget() != null)
                {
                    string BountyTargetName = seer.getBountyTarget().getRealName(isMeeting);
                    SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                }
                if (seer.isWitch())
                {
                    if (seer.GetKillOrSpell() == false) SelfSuffix = "Mode:" + getString("WitchModeKill");
                    if (seer.GetKillOrSpell() == true) SelfSuffix = "Mode:" + getString("WitchModeSpell");
                }

                //他人用の変数定義
                bool SeerKnowsImpostors = false; //trueの時、インポスターの名前が赤色に見える

                //タスクを終えたSnitchがインポスター/キル可能な第三陣営の方角を確認できる
                if (seer.isSnitch())
                {
                    var TaskState = seer.getPlayerTaskState();
                    if (TaskState.isTaskFinished)
                    {
                        SeerKnowsImpostors = true;
                        //ミーティング以外では矢印表示
                        if (!isMeeting)
                        {
                            foreach (var arrow in main.targetArrows)
                            {
                                //自分用の矢印で対象が死んでない時
                                if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                                    SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.isMadSnitch())
                {
                    var TaskState = seer.getPlayerTaskState();
                    if (TaskState.isTaskFinished)
                        SeerKnowsImpostors = true;
                }

                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.getRealName(isMeeting);

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = "";
                if (seer.isSheriff())
                    SelfRoleName = $"<size={fontSize}><color={seer.getRoleColorCode()}>{seer.getRoleName()} ({main.SheriffShotLimit[seer.PlayerId]})</color>";
                else
                    SelfRoleName = $"<size={fontSize}><color={seer.getRoleColorCode()}>{seer.getRoleName()}</color>";
                string SelfName = $"{SelfTaskText}</size>\r\n<color={seer.getRoleColorCode()}>{SeerRealName}</color>{SelfMark}";
                if (seer.isArsonist() && seer.isDouseDone())
                    SelfName = $"</size>\r\n<color={seer.getRoleColorCode()}>{getString("EnterVentToWin")}</color>";
                SelfName = SelfRoleName + SelfName;
                SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                if (!isMeeting) SelfName += "\r\n";

                //適用
                seer.RpcSetNamePrivate(SelfName, true, force: isMeeting);

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || SeerKnowsImpostors //seerがインポスターを知っている状態
                    || seer.getCustomRole().isImpostor() //seerがインポスター
                    || seer.isEgoSchrodingerCat() //seerがエゴイストのシュレディンガーの猫
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.isArsonist()
                    || main.SpelledPlayer.Count > 0
                    || seer.isExecutioner()
                    || seer.isDoctor() //seerがドクター
                    || seer.isPuppeteer()
                    || isActive(SystemTypes.Electrical)
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        TownOfHost.Logger.info("NotifyRoles-Loop2-" + target.name + ":START", "NotifyRoles");

                        //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetTaskText = hasTasks(target.Data, false) && seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"{getTaskText(target)}" : "";

                        //Loversのハートマークなどを入れてください。
                        string TargetMark = "";
                        //呪われている人
                        if (main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null && isMeeting)
                            TargetMark += "<color=#ff0000>†</color>";
                        //タスク完了直前のSnitchにマークを表示
                        canFindSnitchRole = seer.getCustomRole().isImpostor() || //Seerがインポスター
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.isEgoist());//or エゴイスト

                        if (target.isSnitch() && canFindSnitchRole)
                        {
                            var taskState = target.getPlayerTaskState();
                            if (taskState.doExpose)
                                TargetMark += $"<color={getRoleColorCode(CustomRoles.Snitch)}>★</color>";
                        }
                        if (seer.isArsonist() && seer.isDousedPlayer(target))
                        {
                            TargetMark += $"<color={getRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                        }
                        if (seer.isPuppeteer() &&
                        main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        main.PuppeteerList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>◆</color>";

                        //他人の役職とタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetRoleText = "";
                        if (target.isSheriff())
                            TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}><color={target.getRoleColorCode()}>{target.getRoleName()} ({main.SheriffShotLimit[target.PlayerId]})</color>{TargetTaskText}</size>\r\n" : "";
                        else
                            TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}><color={target.getRoleColorCode()}>{target.getRoleName()}</color>{TargetTaskText}</size>\r\n" : "";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.getRealName(isMeeting);

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors) //Seerがインポスターが誰かわかる状態
                        {
                            //スニッチはオプション有効なら第三陣営のキル可能役職も見れる
                            var snitchOption = seer.isSnitch() && Options.SnitchCanFindNeutralKiller.GetBool();
                            var foundCheck = target.getCustomRole().isImpostor() || (snitchOption && target.isEgoist());
                            if (foundCheck)
                                TargetPlayerName = $"<color={target.getRoleColorCode()}>{TargetPlayerName}</color>";
                        }
                        else if (seer.getCustomRole().isImpostor() && target.isEgoist())
                            TargetPlayerName = $"<color={getRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else if (seer.isEgoSchrodingerCat() && target.isEgoist())
                            TargetPlayerName = $"<color={getRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else if (Utils.isActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting)
                            TargetPlayerName = $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>{TargetPlayerName}</color>"; //targetの赤色で表示
                        else
                        {
                            //NameColorManager準拠の処理
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }
                        if (seer.isExecutioner()) //seerがエクスキューショナー
                            foreach (var ExecutionerTarget in main.ExecutionerTarget)
                            {
                                if (seer.PlayerId == ExecutionerTarget.Key && //seerがKey
                                target.PlayerId == ExecutionerTarget.Value) //targetがValue
                                    TargetMark += $"<color={Utils.getRoleColorCode(CustomRoles.Executioner)}>♦</color>";
                            }

                        string TargetDeathReason = "";
                        if (seer.isDoctor() && //seerがDoctor
                        target.Data.IsDead //変更対象が死人
                        )
                            TargetDeathReason = $"(<color={getRoleColorCode(CustomRoles.Doctor)}>{getVitalText(target.PlayerId)}</color>)";

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";
                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer, force: isMeeting);

                        TownOfHost.Logger.info("NotifyRoles-Loop2-" + target.name + ":END", "NotifyRoles");
                    }
                }
                TownOfHost.Logger.info("NotifyRoles-Loop1-" + seer.name + ":END", "NotifyRoles");
            }
            main.witchMeeting = false;
        }
        public static void CustomSyncAllSettings()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.CustomSyncSettings();
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
                CustomRoles pc_role = pc.getCustomRole();
                if (pc_role.isImpostor() && !pc.Data.IsDead) AliveImpostorCount++;
            }
            TownOfHost.Logger.info("生存しているインポスター:" + AliveImpostorCount + "人");
            main.AliveImpostorCount = AliveImpostorCount;
        }
    }
}