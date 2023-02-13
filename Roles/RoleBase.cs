using System.Collections.Generic;
using UnityEngine;
using Hazel;

namespace TownOfHost.Roles;

public abstract class RoleBase : RoleInfoBase
{
    public bool HasTasks = false;
    public RoleBase(
        bool hasTasks
    )
    {
        HasTasks = hasTasks;
    }
    public virtual void Init()
    {
        PlayerIdList = new(GameData.Instance.PlayerCount);
    }
    public virtual void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);
    }
    public virtual void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    { }
    public virtual bool CanUseKillButton(byte playerId) => false;
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
    // == CheckMurder関連処理 ==
    public virtual IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target) => null;
    // ==/CheckMurder関連処理 ==
    public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    { }
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target)
    { }
    public virtual void OnFixedUpdate(PlayerControl player)
    { }
    public virtual bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target) => true;
    public virtual void OnStartMeeting()
    { }
}