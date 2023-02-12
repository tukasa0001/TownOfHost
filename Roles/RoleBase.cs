using System.Collections.Generic;
using UnityEngine;
using Hazel;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public abstract class RoleBase
{
    public static RoleBase Instance;
    public List<byte> PlayerIdList;
    public CustomRoles RoleName;
    public RoleType CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;


    public RoleBase(
        CustomRoles roleName,
        RoleType type,
        int configId,
        string colorCode = "",
        TabGroup tab = TabGroup.MainSettings
    )
    {
        RoleName = roleName;
        CustomRoleType = type;
        ConfigId = configId;

        if (colorCode == "")
            colorCode = type switch
            {
                RoleType.Impostor or RoleType.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        if (tab == TabGroup.MainSettings)
            tab = type switch
            {
                RoleType.Impostor => TabGroup.ImpostorRoles,
                RoleType.Madmate => TabGroup.ImpostorRoles,
                RoleType.Crewmate => TabGroup.CrewmateRoles,
                RoleType.Neutral => TabGroup.NeutralRoles,
                _ => tab
            };
        Tab = tab;

        CustomRoleManager.AllRoles.Add(this);

        RoleColor = Utils.GetRoleColor(roleName);
        Instance = this;
    }
    public virtual void SetupCustomOption() => SetupRoleOptions(ConfigId, Tab, RoleName);
    public virtual void Init()
    {
        PlayerIdList = new(GameData.Instance.PlayerCount);
    }
    public virtual void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);
    }
    public virtual void ReceiveRPC(MessageReader reader)
    { }
    public virtual bool CanUseKillButton() => false;
    public virtual void SetKillCooldown(byte playerId)
    { }
    public virtual void ApplyGameOptions(byte playerId)
    { }
    public virtual string GetProgressText(byte playerId, bool comms = false)
    {
        //タスクテキスト
        var taskState = Main.PlayerStates?[playerId].GetTaskState();
        if (!taskState.hasTasks) return "";

        Color TextColor = Color.yellow;
        var info = Utils.GetPlayerInfoById(playerId);
        var TaskCompleteColor = Utils.HasTasks(info) ? Color.green : Utils.GetRoleColor(RoleName).ShadeColor(0.5f); //タスク完了後の色
        var NonCompleteColor = Utils.HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色
        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
        int numCompleted = taskState.CompletedTasksCount;
        int numAllTasks = taskState.AllTasksCount;

        if (Workhorse.IsThisRole(playerId))
            (NormalColor, numCompleted, numAllTasks) = Workhorse.GetTaskTextData(taskState);

        TextColor = comms ? Color.gray : NormalColor;
        string Completed = comms ? "?" : $"{numCompleted}";
        return Utils.ColorString(TextColor, $"({Completed}/{numAllTasks})");
    }
    public virtual bool OnCheckMurder(PlayerControl killer, PlayerControl target) => true;
    public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    { }
    public virtual void OnFixedUpdate(PlayerControl player)
    { }
    public virtual bool OnReportDeadBody() => true;
    public virtual void OnStartMeeting()
    { }
}