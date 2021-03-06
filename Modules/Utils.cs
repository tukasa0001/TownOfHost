using System.Text.RegularExpressions;
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
            //Tasks???null????????????????????????????????????????????????????????????
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;

            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if (p.Role.IsImpostor)
                hasTasks = false; //????????????CustomRole?????????????????????
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
                        //??????????????????????????????????????????????????????????????????
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
            if (!Main.playerVersion.ContainsKey(0)) return ""; //????????????MOD?????????????????????????????????????????????
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
            if (!Main.playerVersion.ContainsKey(0)) return ""; //????????????MOD?????????????????????????????????????????????
            if (!Main.AllPlayerCustomRoles.TryGetValue(playerId, out var role)) return Helpers.ColorString(Color.yellow, "Invalid");
            string ProgressText = "";
            switch (role)
            {
                case CustomRoles.Arsonist:
                    var doused = getDousedPlayerCount(playerId);
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Arsonist), $"({doused.Item1}/{doused.Item2})");
                    break;
                case CustomRoles.Sheriff:
                    ProgressText += Helpers.ColorString(Color.yellow, Main.SheriffShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
                    break;
                case CustomRoles.Sniper:
                    ProgressText += $" {Sniper.GetBulletCount(playerId)}";
                    break;
                default:
                    //?????????????????????
                    var taskState = PlayerState.taskState?[playerId];
                    if (taskState.hasTasks)
                    {
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = Helpers.ColorString(Color.yellow, $"({Completed}/{taskState.AllTasksCount})");
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
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo")); }
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

                        //????????????????????????????????????
                        if (c.Name == "doOverride" && c.GetBool() == true)
                        {
                            foreach (var d in c.Children)
                            {
                                text += $"\n{d.GetName(disableColor: true)}:{d.GetString()}";
                            }
                        }
                        //?????????????????????????????????????????????????????????
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
                if (Options.VoteMode.GetBool())
                {
                    if (Options.GetWhenSkipVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenSkipVote"), Options.WhenSkipVote.GetString());
                    if (Options.GetWhenNonVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenNonVote"), Options.WhenNonVote.GetString());
                    if ((Options.GetWhenNonVote() == VoteMode.Suicide || Options.GetWhenSkipVote() == VoteMode.Suicide) && CustomRoles.Terrorist.IsEnable()) text += String.Format("\n{0}:{1}", GetString("CanTerroristSuicideWin"), Options.CanTerroristSuicideWin.GetBool());
                }
            }
            if (Options.LadderDeath.GetBool())
            {
                text += String.Format("\n{0}:{1}", GetString("LadderDeath"), GetOnOff(Options.LadderDeath.GetBool()));
                text += String.Format("\n{0}:{1}", GetString("LadderDeathChance"), Options.LadderDeathChance.GetString());
            }
            if (Options.IsStandardHAS) text += String.Format("\n{0}:{1}", GetString("StandardHAS"), GetOnOff(Options.StandardHAS.GetBool()));
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
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            foreach (var id in Main.winnerList)
            {
                text += $"\n??? " + SummaryTexts(id);
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                text += $"\n??? " + SummaryTexts(id);
            }
            SendMessage(text, PlayerId);
        }


        public static string GetShowLastSubRolesText(byte id, bool disableColor = false)
        {
            var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(id, out var cSubRole);
            if (!cSubRoleFound || cSubRole == CustomRoles.NoSubRoleAssigned) return "";
            return disableColor ? " + " + GetRoleName(cSubRole) : Helpers.ColorString(Color.white, "+ ") + Helpers.ColorString(GetRoleColor(cSubRole), GetRoleName(cSubRole));
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
            if (taskState.IsTaskFinished && (!PlayerState.IsSuicide(Terrorist.PlayerId) || Options.CanTerroristSuicideWin.GetBool())) //?????????????????????????????????????????? OR ??????????????????????????????????????????
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (PlayerState.GetDeathReason(pc.PlayerId) != PlayerState.DeathReason.Vote)
                        {
                            //????????????????????????????????????
                            PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //??????????????????
                        pc.RpcMurderPlayerV2(pc);
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Bombed);
                        PlayerState.SetDead(pc.PlayerId);
                    }
                }
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                writer.Write((byte)CustomWinner.Terrorist);
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
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId);
            }
            else
            {
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + Main.modColor + ">TOH v" + Main.PluginVersion + "</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += $"\r\n{GetString("SuffixMode.Streaming")}";
                        break;
                    case SuffixModes.Recording:
                        name += $"\r\n{GetString("SuffixMode.Recording")}";
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
            TownOfHost.Logger.Info("NotifyRoles???" + callerClassName + "." + callerMethodName + "??????????????????????????????", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            //Snitch???????????????ON/OFF
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
            //seer:?????????????????????????????????????????????????????????????????????
            //target:seer??????????????????????????????????????????????????????????????????
            foreach (var seer in seerList)
            {
                if (seer.IsModClient()) continue;
                string fontSize = "1.5";
                if (isMeeting && (seer.GetClient().PlatformData.Platform.ToString() == "Playstation" || seer.GetClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
                TownOfHost.Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START", "NotifyRoles");
                //Loop1-bottle???START-END??????KeyNotFoundException
                //seer??????????????????????????????????????????
                if (seer.Data.Disconnected) continue;

                //????????????????????????????????????????????????
                string SelfTaskText = GetProgressText(seer);

                //???????????????????????????????????????
                string SelfMark = "";

                //??????????????????/???????????????????????????????????????Snitch??????
                var canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //LocalPlayer?????????????????????
                    (Options.SnitchCanFindNeutralKiller.GetBool() && seer.Is(CustomRoles.Egoist));//or ???????????????

                if (canFindSnitchRole && ShowSnitchWarning && !isMeeting)
                {
                    var arrows = "";
                    foreach (var arrow in Main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                        {
                            //????????????????????????????????????????????????
                            arrows += arrow.Value;
                        }
                    }
                    SelfMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>???{arrows}</color>";
                }

                //??????????????????????????????(?????????)
                if (seer.Is(CustomRoles.Lovers)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>???</color>";

                //????????????????????????
                if (Main.SpelledPlayer.Find(x => x.PlayerId == seer.PlayerId) != null && isMeeting)
                    SelfMark += "<color=#ff0000>???</color>";

                if (Sniper.IsEnable())
                {
                    //????????????????????????????????????
                    SelfMark += Sniper.GetShotNotify(seer.PlayerId);
                }
                //Mark??????????????????????????????????????????????????????
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
                    SelfSuffix = seer.IsSpellMode() ? "Mode:" + GetString("WitchModeSpell") : "Mode:" + GetString("WitchModeKill");
                }

                //????????????????????????
                bool SeerKnowsImpostors = false; //true?????????????????????????????????????????????????????????

                //?????????????????????Snitch?????????????????????/??????????????????????????????????????????????????????
                if (seer.Is(CustomRoles.Snitch))
                {
                    var TaskState = seer.GetPlayerTaskState();
                    if (TaskState.IsTaskFinished)
                    {
                        SeerKnowsImpostors = true;
                        //??????????????????????????????????????????
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                //????????????????????????????????????????????????
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

                //RealName????????? ??????????????????????????????RealNames???????????????
                string SeerRealName = seer.GetRealName(isMeeting);

                //seer???????????????SelfTaskText???seer????????????????????????SelfMark?????????
                string SelfRoleName = $"<size={fontSize}>{Helpers.ColorString(seer.GetRoleColor(), seer.GetRoleName())}{SelfTaskText}</size>";
                string SelfName = $"{Helpers.ColorString(seer.GetRoleColor(), SeerRealName)}{SelfMark}";
                if (seer.Is(CustomRoles.Arsonist) && seer.IsDouseDone())
                    SelfName = $"</size>\r\n{Helpers.ColorString(seer.GetRoleColor(), GetString("EnterVentToWin"))}";
                SelfName = SelfRoleName + "\r\n" + SelfName;
                SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                if (!isMeeting) SelfName += "\r\n";

                //??????
                seer.RpcSetNamePrivate(SelfName, true, force: NoCache);

                //seer????????????????????????????????????????????????????????????????????????????????????
                if (seer.Data.IsDead //seer??????????????????
                    || SeerKnowsImpostors //seer?????????????????????????????????????????????
                    || seer.GetCustomRole().IsImpostor() //seer?????????????????????
                    || seer.Is(CustomRoles.EgoSchrodingerCat) //seer???????????????????????????????????????????????????
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer???????????????????????????????????????????????????
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Lovers)
                    || Main.SpelledPlayer.Count > 0
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seer???????????????
                    || seer.Is(CustomRoles.Puppeteer)
                    || IsActive(SystemTypes.Electrical)
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //target???seer?????????????????????????????????
                        if (target == seer || target.Data.Disconnected) continue;
                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START", "NotifyRoles");

                        //?????????????????????target???????????????????????????????????????seer????????????????????????????????????????????????????????????????????????????????????????????????
                        string TargetTaskText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"{GetProgressText(target)}" : "";

                        //???????????????????????????????????????
                        string TargetMark = "";
                        //?????????????????????
                        if (Main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null && isMeeting)
                            TargetMark += "<color=#ff0000>???</color>";
                        //????????????????????????Snitch?????????????????????
                        canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //Seer?????????????????????
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.Is(CustomRoles.Egoist));//or ???????????????

                        if (target.Is(CustomRoles.Snitch) && canFindSnitchRole)
                        {
                            var taskState = target.GetPlayerTaskState();
                            if (taskState.DoExpose)
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>???</color>";
                        }

                        //??????????????????????????????(?????????)
                        if (seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>???</color>";
                        }
                        //??????????????????????????????
                        else if (seer.Data.IsDead && !seer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Lovers)}>???</color>";
                        }

                        if (seer.Is(CustomRoles.Arsonist))//seer???????????????????????????
                        {
                            if (seer.IsDousedPlayer(target)) //seer???target????????????????????????????????????(??????)
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>???</color>";
                            }
                            if (
                                Main.ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && //seer????????????????????????????????????(????????????)
                                ar_kvp.Item1 == target //????????????????????????????????????target
                            )
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>???</color>";
                            }
                        }
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>???</color>";

                        //???????????????????????????????????????????????????????????????????????????????????????????????????seer????????????????????????????????????????????????????????????????????????????????????????????????
                        string TargetRoleText = seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool() ? $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}{TargetTaskText}</size>\r\n" : "";

                        //RealName????????? ??????????????????????????????RealNames???????????????
                        string TargetPlayerName = target.GetRealName(isMeeting);

                        //??????????????????????????????????????????????????????????????????
                        if (SeerKnowsImpostors) //Seer?????????????????????????????????????????????
                        {
                            //???????????????????????????????????????????????????????????????????????????????????????
                            var snitchOption = seer.Is(CustomRoles.Snitch) && Options.SnitchCanFindNeutralKiller.GetBool();
                            var foundCheck = target.GetCustomRole().IsImpostor() || (snitchOption && target.Is(CustomRoles.Egoist));
                            if (foundCheck)
                                TargetPlayerName = Helpers.ColorString(target.GetRoleColor(), TargetPlayerName);
                        }
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Egoist), TargetPlayerName);
                        else if (seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist))
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Egoist), TargetPlayerName);
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting)
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName); //target??????????????????
                        else
                        {
                            //NameColorManager???????????????
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }
                        foreach (var ExecutionerTarget in Main.ExecutionerTarget)
                        {
                            if ((seer.PlayerId == ExecutionerTarget.Key || seer.Data.IsDead) && //seer???Key or Dead
                            target.PlayerId == ExecutionerTarget.Value) //target???Value
                                TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Executioner)}>???</color>";
                        }

                        string TargetDeathReason = "";
                        if (seer.Is(CustomRoles.Doctor) && //seer???Doctor
                        target.Data.IsDead //?????????????????????
                        )
                            TargetDeathReason = $"({Helpers.ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})";

                        //??????????????????????????????????????????
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                        //??????
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
            TownOfHost.Logger.Info("????????????????????????????????????:" + AliveImpostorCount + "???", "CountAliveImpostors");
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
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????");
        }
        public static (int, int) getDousedPlayerCount(byte playerId)
        {
            int doused = 0, all = 0; //???????????????????????????
            //??????????????????Main.isDoused???foreach????????????????????????????????????????????????????????????????????????
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Data.Disconnected ||
                    pc.PlayerId == playerId
                ) continue; //???????????????????????? (??????????????????????????????????????? ?????????????????????????????????)

                all++;
                if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                    //?????????????????????
                    doused++;
            }

            return (doused, all);
        }
        public static string SummaryTexts(byte id, bool disableColor = true)
        {
            string summary = $"{Helpers.ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id])}<pos=25%> {Helpers.ColorString(GetRoleColor(Main.AllPlayerCustomRoles[id]), GetRoleName(Main.AllPlayerCustomRoles[id]))}{GetShowLastSubRolesText(id)}</pos><pos=44%> {GetProgressText(id)}</pos><pos=51%> {GetVitalText(id)}</pos>";
            return disableColor ? summary.RemoveHtmlTags() : Regex.Replace(summary, " ", "");
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
    }
}