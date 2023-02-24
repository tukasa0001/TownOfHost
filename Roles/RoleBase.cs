using System;
using System.Collections.Generic;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.AddOns.Crewmate;
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
    public void Dispose()
    {
        Player = null;
        OnDestroy();
        CustomRoleManager.AllActiveRoles.Remove(this);
    }
    /// <summary>
    /// インスタンス作成後すぐに呼ばれる関数
    /// </summary>
    public virtual void Add()
    { }
    /// <summary>
    /// ロールベースが破棄されるときに呼ばれる関数
    /// </summary>
    public virtual void OnDestroy()
    { }
    /// <summary>
    /// RPCを受け取った時に呼ばれる関数
    /// </summary>
    /// <param name="reader">届いたRPCの情報</param>
    /// <param name="rpcType">届いたCustomRPC</param>
    public virtual void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    { }
    /// <summary>
    /// キルボタンを使えるかどうか
    /// </summary>
    /// <returns>trueを返した場合、キルボタンを使える</returns>
    public virtual bool CanUseKillButton() => false;
    /// <summary>
    /// キルクールダウンを設定する関数
    /// </summary>
    public virtual float SetKillCooldown() => 30f;
    /// <summary>
    /// BuildGameOptionsで呼ばれる関数
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt)
    { }
    /// <summary>
    /// 役職名の横に出るテキスト
    /// </summary>
    /// <param name="comms">コミュサボ中扱いするかどうか</param>
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
    /// <summary>
    /// キルが実行された直後に呼ばれる関数
    /// </summary>
    /// <param name="killer">キルしたプレイヤー</param>
    /// <param name="target">キルされたプレイヤー</param>
    public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    { }
    /// <summary>
    /// シェイプシフト時に呼ばれる関数
    /// </summary>
    /// <param name="shapeshifter">シェイプシフター</param>
    /// <param name="target">変身先</param>
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target)
    { }
    /// <summary>
    /// タスクターンに常時呼ばれる関数
    /// </summary>
    public virtual void OnFixedUpdate()
    { }
    /// <summary>
    /// 通報時に呼ばれる関数
    /// </summary>
    /// <param name="reporter">通報したプレイヤー</param>
    /// <param name="target">通報されたプレイヤー</param>
    /// <returns>falseを返すと通報がキャンセルされます</returns>
    public virtual bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target) => true;
    /// <summary>
    /// ミーティングが始まった時に読まれる関数
    /// </summary>
    public virtual void OnStartMeeting()
    { }
    /// <summary>
    /// タスクターンが始まる直前に毎回呼ばれる関数
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }
    public virtual string GetTargetArrow() => "";
    /// <summary>
    /// シェイプシフトボタンを変更します
    /// </summary>
    public virtual string GetKillButtonText() => GetString(StringNames.KillLabel);
    /// <summary>
    /// シェイプシフトボタンのテキストを変更します
    /// </summary>
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