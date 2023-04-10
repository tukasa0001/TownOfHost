using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Mediumshiper
{
    private static readonly int Id = 8021812;
    public static List<byte> playerIdList = new();
    public static Dictionary<byte, byte> ContactPlayer = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mediumshiper);
    }
    public static void Init()
    {
        playerIdList = new();
        ContactPlayer = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void OnReportOnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        if (!pc.Is(CustomRoles.Mediumshiper) || target == null) return;
        ContactPlayer.TryAdd(target.PlayerId, pc.PlayerId);
        Logger.Info($"通灵师{pc.GetNameWithRole()}报告了{target.PlayerName}的尸体，已建立联系", "Mediumshiper");
    }
    public static bool MsMsg(PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!ContactPlayer.ContainsKey(pc.PlayerId)) return false;
        if (pc.IsAlive()) return false;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (!CheckCommond(ref msg, "通灵|ms", false)) return false;

        bool ans;
        if (msg.Contains("n") || msg.Contains(GetString("No")) || msg.Contains("错") || msg.Contains("不是")) ans = false;
        else if (msg.Contains("y") || msg.Contains(GetString("Yes")) || msg.Contains("对")) ans = true;
        else
        {
            Utils.SendMessage(GetString("MediumshipHelp"), pc.PlayerId);
            return true;
        }

        Utils.SendMessage(GetString("Mediumship" + (ans ? "Yes" : "No")), ContactPlayer[pc.PlayerId], Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
        Utils.SendMessage(GetString("MediumshipDone"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));

        ContactPlayer.Remove(pc.PlayerId);

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