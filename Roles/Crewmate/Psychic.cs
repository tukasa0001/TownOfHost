using System.Collections.Generic;
using System.Linq;

using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Psychic
{
    private static readonly int Id = 8020450;
    private static List<byte> playerIdList = new();

    private static OptionItem CanSeeNum;
    private static OptionItem Fresh;
    private static OptionItem CkshowEvil;
    private static OptionItem NBshowEvil;
    private static OptionItem NEshowEvil;

    private static List<byte> RedPlayer;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Psychic);
        CanSeeNum = IntegerOptionItem.Create(Id + 2, "PsychicCanSeeNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic])
            .SetValueFormat(OptionFormat.Pieces);
        Fresh = BooleanOptionItem.Create(Id + 6, "PsychicFresh", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        CkshowEvil = BooleanOptionItem.Create(Id + 3, "CrewKillingRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NBshowEvil = BooleanOptionItem.Create(Id + 4, "NBareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NEshowEvil = BooleanOptionItem.Create(Id + 5, "NEareRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
    }
    public static void Init()
    {
        playerIdList = new();
        RedPlayer = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsRedForPsy(this byte id) => RedPlayer != null && RedPlayer.Count != 0 && RedPlayer.Contains(id);
    public static bool IsRedForPsy(this PlayerControl pc) => RedPlayer != null && RedPlayer.Count != 0 && RedPlayer.Contains(pc.PlayerId);
    public static void OnMeetingStart()
    {
        if (Fresh.GetBool() || RedPlayer == null || RedPlayer.Count == 0)
            GetRedName();
    }
    public static void GetRedName()
    {
        if (!IsEnable) return;
        if (!PlayerControl.LocalPlayer.Is(CustomRoles.Psychic) && !AmongUsClient.Instance.AmHost) return;

        List<PlayerControl> BadListPc = Main.AllAlivePlayerControls.Where(x =>
        x.GetCustomRole().IsImpostor() ||
        (x.GetCustomRole().IsCK() && CkshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNeutralKilling() && NEshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNeutral() && !x.GetCustomRole().IsNeutralKilling() && NBshowEvil.GetBool())
        ).ToList();

        List<byte> BadList = new();
        foreach (var pc in BadListPc) BadList.Add(pc.PlayerId);
        List<byte> AllList = new();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => !BadList.Contains(x.PlayerId) && x.PlayerId != PlayerControl.LocalPlayer.PlayerId))
            AllList.Add(pc.PlayerId);

        int ENum = 1;
        for (int i = 1; i < CanSeeNum.GetInt(); i++)
            if (IRandom.Instance.Next(0, 100) < 20) ENum++;
        int BNum = CanSeeNum.GetInt() - ENum;

        RedPlayer = new();
        for (int i = 0; i < ENum; i++)
        {
            if (BadList.Count < 1) break;
            RedPlayer.Add(BadList[IRandom.Instance.Next(0, BadList.Count)]);
        }
        for (int i = 0; i < BNum; i++)
        {
            var list = AllList.Where(x => !RedPlayer.Contains(x)).ToList();
            if (list.Count < 1) break;
            RedPlayer.Add(list[IRandom.Instance.Next(0, AllList.Count)]);
        }
        Logger.Info($"需要{CanSeeNum.GetInt()}个红名，其中{ENum}个邪恶，计算后显示红名{RedPlayer.Count}个", "Psychic");
    }
}