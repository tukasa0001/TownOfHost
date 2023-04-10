using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Judge
{
    private static readonly int Id = 8035678;
    private static List<byte> playerIdList = new();
    private static OptionItem TrialLimitPerMeeting;
    private static OptionItem TryHideMsg;
    private static OptionItem CanTrialMadmate;
    private static OptionItem CanTrialCrewKilling;
    private static OptionItem CanTrialNeutralB;
    private static OptionItem CanTrialNeutralK;
    private static Dictionary<byte, int> TrialLimit;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Judge);
        TrialLimitPerMeeting = IntegerOptionItem.Create(Id + 10, "TrialLimitPerMeeting", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetValueFormat(OptionFormat.Times);
        CanTrialMadmate = BooleanOptionItem.Create(Id + 12, "JudgeCanTrialMadmate", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialCrewKilling = BooleanOptionItem.Create(Id + 13, "JudgeCanTrialnCrewKilling", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralB = BooleanOptionItem.Create(Id + 14, "JudgeCanTrialNeutralB", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        CanTrialNeutralK = BooleanOptionItem.Create(Id + 15, "JudgeCanTrialNeutralK", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge]);
        TryHideMsg = BooleanOptionItem.Create(Id + 11, "JudgeTryHideMsg", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Judge])
            .SetColor(Color.green);
    }
    public static void Init()
    {
        playerIdList = new();
        TrialLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void OnMeetingStart()
    {
        TrialLimit.Clear();
        foreach (var pc in playerIdList) TrialLimit.Add(pc, TrialLimitPerMeeting.GetInt());
    }
    public static bool TrialMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Judge)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("JudgeDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GuessManager.GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {

            if (TryHideMsg.GetBool()) GuessManager.TryHideMsg();
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool judgeSuicide = true;
                if (TrialLimit[pc.PlayerId] < 1)
                {
                    Utils.SendMessage(GetString("JudgeTrialMax"), pc.PlayerId);
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("LaughToWhoTrialSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromDevTitle")));
                    judgeSuicide = true;
                }
                else if (pc.Is(CustomRoles.Madmate)) judgeSuicide = false;
                else if (target.Is(CustomRoles.Madmate) && CanTrialMadmate.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsCK() && CanTrialCrewKilling.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNK() && CanTrialNeutralK.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsNNK() && CanTrialNeutralB.GetBool()) judgeSuicide = false;
                else if (target.GetCustomRole().IsImpostor()) judgeSuicide = false;
                else judgeSuicide = true;

                var dp = judgeSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                TrialLimit[pc.PlayerId]--;

                new LateTask(() =>
                {
                    Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Trialed;
                    dp.SetRealKiller(pc);
                    GuessManager.RpcGuesserMurderPlayer(dp);

                    //死者检查
                    FixedUpdatePatch.LoversSuicide(target.PlayerId);
                    if (target.Is(CustomRoles.Terrorist))
                    {
                        Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                        Utils.CheckTerroristWin(target.Data);
                    }
                    if (Executioner.Target.ContainsValue(target.PlayerId))
                        Executioner.ChangeRoleByTarget(target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    new LateTask(() => { Utils.SendMessage(string.Format(GetString("TrialKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("TrialKillTitle"))); }, 0.6f, "Guess Msg");

                }, 0.2f, "Trial Kill");
            }
        }
        return true;
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("TrialHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("TrialNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}