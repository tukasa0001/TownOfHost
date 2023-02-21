using System;
using System.Collections.Generic;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using static TownOfHost.Translator;

namespace TownOfHost.Roles;

public abstract class RoleBase : IDisposable
{
    public PlayerControl Player;
    public bool HasTasks = false;
    public RoleBase(
        PlayerControl player,
        bool hasTasks
    )
    {
        Player = player;
        HasTasks = hasTasks;

        CustomRoleManager.AllActiveRoles.Add(this);
    }
    public virtual void Add()
    { }
    public void Dispose()
    {
        Player = null;
        OnDestroy();
        CustomRoleManager.AllActiveRoles.Remove(this);
    }
    public virtual void OnDestroy()
    { }
    public virtual void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    { }
    public virtual bool CanUseKillButton() => false;
    public virtual void SetKillCooldown()
    { }
    public virtual void ApplyGameOptions(IGameOptions opt)
    { }
    public virtual string GetProgressText(bool comms = false)
    {
        var playerId = Player.PlayerId;
        //タスクテキスト
        var taskState = Main.PlayerStates?[playerId].GetTaskState();
        if (!taskState.hasTasks) return "";

        Color TextColor = Color.yellow;
        var info = Utils.GetPlayerInfoById(playerId);
        var TaskCompleteColor = Utils.HasTasks(info) ? Color.green : Utils.GetRoleColor(info.GetCustomRole()).ShadeColor(0.5f); //タスク完了後の色
        var NonCompleteColor = Utils.HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色

        if (Workhorse.IsThisRole(playerId))
            NonCompleteColor = Workhorse.RoleColor;

        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

        TextColor = comms ? Color.gray : NormalColor;
        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
        return Utils.ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
    }
    // == CheckMurder関連処理 ==
    public virtual IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target) => null;
    // ==/CheckMurder関連処理 ==
    public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    { }
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target)
    { }
    public virtual void OnFixedUpdate()
    { }
    public virtual bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target) => true;
    public virtual void OnStartMeeting()
    { }
    public virtual void AfterMeetingTasks()
    { }
    public virtual string GetTargetArrow() => "";
    public virtual string GetKillButtonText() => GetString(StringNames.KillLabel);
    public virtual string GetAbilityButtonText()
    {
        StringNames str = Player.Data.Role.Role switch
        {
            RoleTypes.Engineer => StringNames.VentAbility,
            RoleTypes.Scientist => StringNames.VitalsAbility,
            RoleTypes.Shapeshifter => StringNames.ShapeshiftAbility,
            RoleTypes.GuardianAngel => StringNames.ProtectAbility,
            RoleTypes.ImpostorGhost or RoleTypes.CrewmateGhost => StringNames.HauntAbilityName,
            _ => StringNames.ErrorInvalidName
        };
        return GetString(str);
    }
}