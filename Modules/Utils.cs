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
                        case CustomRoles.Fox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.Troll:
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
                    if (role == CustomRoles.Fox || role == CustomRoles.Troll) hasTasks = false;
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
            return $"{taskState.CompletedTasksCount}/{taskState.AllTasksCount}";
        }
        public static void ShowActiveRoles()
        {
            SendMessage(getString("CurrentActiveSettingsHelp") + ":");
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(getString("HideAndSeekInfo"));
                if (CustomRoles.Fox.isEnable()) { SendMessage(getRoleName(CustomRoles.Fox) + getString("FoxInfoLong")); }
                if (CustomRoles.Troll.isEnable()) { SendMessage(getRoleName(CustomRoles.Troll) + getString("TrollInfoLong")); }
            }
            else
            {
                if (Options.SyncButtonMode.GetBool()) { SendMessage(getString("SyncButtonModeInfo")); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(getString("RandomMapsModeInfo")); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role == CustomRoles.Fox || role == CustomRoles.Troll) continue;
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
                if (CustomRoles.Fox.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(CustomRoles.Fox), CustomRoles.Fox.getCount());
                if (CustomRoles.Troll.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(CustomRoles.Troll), CustomRoles.Troll.getCount());
                SendMessage(text);
                text = getString("Settings") + ":";
                text += getString("HideAndSeek");
            }
            else
            {
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (role.Key == CustomRoles.Fox || role.Key == CustomRoles.Troll) continue;
                    if (role.Key.isEnable()) text += String.Format("\n{0}:{1}", getRoleName(role.Key), role.Key.getCount());
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
                if (Options.GetWhenSkipVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", getString("WhenSkipVote"), Options.WhenSkipVote.GetString());
                if (Options.GetWhenNonVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", getString("WhenNonVote"), Options.WhenNonVote.GetString());
                if ((Options.GetWhenNonVote() == VoteMode.Suicide || Options.GetWhenSkipVote() == VoteMode.Suicide) && CustomRoles.Terrorist.isEnable()) text += String.Format("\n{0}:{1}", getString("CanTerroristSuicideWin"), Options.CanTerroristSuicideWin.GetBool());
            }
            if (Options.NoGameEnd.GetBool()) text += String.Format("\n{0}:{1}", getString("NoGameEnd"), getOnOff(Options.NoGameEnd.GetBool()));
            SendMessage(text);
        }
        public static void ShowLastRoles()
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage("試合中に/lastrolesを使用することはできません。");
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
                "コマンド一覧:"
                + "\n/winner - 勝者を表示"
                + "\n/lastroles - 最後の役職割り当てを表示"
                + "\n/rename - ホストの名前を変更"
                + "\n/now - 現在有効な設定を表示"
                + "\n/h now - 現在有効な設定の説明を表示"
                + "\n/h roles <役職名> - 役職の説明を表示"
                + "\n/h attributes <属性名> - 属性の説明を表示"
                + "\n/h modes <モード名> - モードの説明を表示"
                + "\n/dump - デスクトップにログを出力"
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
                        PlayerState.isDead[pc.PlayerId] = true;
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
                        name += "\r\n<color=" + main.modColor + ">TOH v" + main.PluginVersion + main.VersionSuffix + "</color>";
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
        public static void NotifyRoles(bool isMeeting = false)
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
                        }
                    }
                }
            }

            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                TownOfHost.Logger.info("NotifyRoles-Loop1-" + seer.name + ":START", "NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない
                if (seer.Data.Disconnected) continue;

                //seerがタスクを持っている：タスク残量の色コードなどを含むテキスト
                //seerがタスクを持っていない：空
                string SelfTaskText = hasTasks(seer.Data, false) ? $"<color=#ffff00>({getTaskText(seer)})</color>" : "";
                //Loversのハートマークなどを入れてください。
                string SelfMark = "";
                //インポスターに対するSnitch警告
                if (ShowSnitchWarning && seer.getCustomRole().isImpostor())
                    SelfMark += $"<color={getRoleColorCode(CustomRoles.Snitch)}>★</color>";

                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if (seer.isBountyHunter() && seer.getBountyTarget() != null)
                {
                    string BountyTargetName = seer.getBountyTarget().getRealName(isMeeting);
                    SelfSuffix = $"<size=1.5>Target:{BountyTargetName}</size>";
                }
                if (seer.isWitch())
                {
                    if (seer.GetKillOrSpell() == false) SelfSuffix = "Mode:" + getString("WitchModeKill");
                    if (seer.GetKillOrSpell() == true) SelfSuffix = "Mode:" + getString("WitchModeSpell");
                }


                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.getRealName(isMeeting);

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = "";
                if (seer.isSheriff())
                    SelfRoleName = $"<size=1.5><color={seer.getRoleColorCode()}>{seer.getRoleName()} ({main.SheriffShotLimit[seer.PlayerId]})</color>";
                else
                    SelfRoleName = $"<size=1.5><color={seer.getRoleColorCode()}>{seer.getRoleName()}</color>";
                string SelfName = $"{SelfTaskText}</size>\r\n<color={seer.getRoleColorCode()}>{SeerRealName}</color>{SelfMark}";
                SelfName = SelfRoleName += SelfName;
                SelfRoleName += SelfName += SelfSuffix == "" ? "" : "\r\n" + SelfSuffix;

                //適用
                seer.RpcSetNamePrivate(SelfName, true);
                HudManagerPatch.LastSetNameDesyncCount++;

                //他人用の変数定義
                bool SeerKnowsImpostors = false; //trueの時、インポスターの名前が赤色に見える
                //タスクを終えたSnitchがインポスターを確認できる
                if (seer.isSnitch())
                {
                    var TaskState = seer.getPlayerTaskState();
                    if (TaskState.isTaskFinished)
                        SeerKnowsImpostors = true;
                }
                if (seer.isMadSnitch())
                {
                    var TaskState = seer.getPlayerTaskState();
                    if (TaskState.isTaskFinished)
                        SeerKnowsImpostors = true;
                }

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || SeerKnowsImpostors //seerがインポスターを知っている状態
                    || seer.getCustomRole().isImpostor() //seerがインポスター
                    || seer.isEgoSchrodingerCat() //seerがエゴイストのシュレディンガーの猫
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.isArsonist()
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        TownOfHost.Logger.info("NotifyRoles-Loop2-" + target.name + ":START", "NotifyRoles");

                        //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetTaskText = hasTasks(target.Data, false) && seer.Data.IsDead ? $"<color=#ffff00>({getTaskText(target)})</color>" : "";

                        //Loversのハートマークなどを入れてください。
                        string TargetMark = "";
                        //タスク完了直前のSnitchにマークを表示
                        if (target.isSnitch() && seer.getCustomRole().isImpostor())
                        {
                            var taskState = target.getPlayerTaskState();
                            if (taskState.doExpose)
                                TargetMark += $"<color={getRoleColorCode(CustomRoles.Snitch)}>★</color>";
                        }
                        if (seer.isArsonist() && seer.isDousedPlayer(target))
                        {
                            TargetMark += $"<color={getRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                        }

                        //他人の役職とタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetRoleText = "";
                        if (target.isSheriff())
                            TargetRoleText = seer.Data.IsDead ? $"<size=1.5><color={target.getRoleColorCode()}>{target.getRoleName()} ({main.SheriffShotLimit[target.PlayerId]})</color>{TargetTaskText}</size>\r\n" : "";
                        else
                            TargetRoleText = seer.Data.IsDead ? $"<size=1.5><color={target.getRoleColorCode()}>{target.getRoleName()}</color>{TargetTaskText}</size>\r\n" : "";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.getRealName(isMeeting);

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors && target.getCustomRole().isImpostor()) //Seerがインポスターが誰かわかる状態
                        {
                            TargetPlayerName = "<color=#ff0000>" + TargetPlayerName + "</color>";
                        }
                        else if (seer.getCustomRole().isImpostor() && target.isEgoist())
                            TargetPlayerName = $"<color={getRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else if (seer.isEgoSchrodingerCat() && target.isEgoist())
                            TargetPlayerName = $"<color={getRoleColorCode(CustomRoles.Egoist)}>{TargetPlayerName}</color>";
                        else
                        {
                            //NameColorManager準拠の処理
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetMark}";
                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer);
                        HudManagerPatch.LastSetNameDesyncCount++;

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