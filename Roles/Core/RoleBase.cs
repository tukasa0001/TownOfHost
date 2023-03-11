using System;
using System.Collections.Generic;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.AddOns.Crewmate;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Core;

public abstract class RoleBase : IDisposable
{
    public PlayerControl Player;
    public bool HasTasks;
    public bool CanKill;
    public RoleBase(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        bool? hasTasks = null,
        bool? canKill = null
    )
    {
        Player = player;
        HasTasks = hasTasks ?? roleInfo.CustomRoleType == CustomRoleTypes.Crewmate;
        CanKill = canKill ?? roleInfo.BaseRoleType is RoleTypes.Impostor or RoleTypes.Shapeshifter;

        CustomRoleManager.AllActiveRoles.Add(this);
    }
    public void Dispose()
    {
        Player = null;
        OnDestroy();
        CustomRoleManager.AllActiveRoles.Remove(this);
    }
    public bool Is(PlayerControl player)
    {
        return player.PlayerId == Player.PlayerId;
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
    public virtual bool CanUseKillButton() => CanKill;
    /// <summary>
    /// キルクールダウンを設定する関数
    /// </summary>
    public virtual float SetKillCooldown() => 30f;
    /// <summary>
    /// BuildGameOptionsで呼ばれる関数
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt)
    { }
    // == CheckMurder関連処理 ==
    public virtual IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target, CustomRoleManager.CheckMurderInfo info) => null;
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
    /// ミーティングが始まった時に呼ばれる関数
    /// </summary>
    public virtual void OnStartMeeting()
    { }
    /// <summary>
    /// タスクターンが始まる直前に毎回呼ばれる関数
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }
    /// <summary>
    /// タスクが一個完了するごとに呼ばれる関数
    /// </summary>
    public virtual void OnCompleteTask()
    { }

    // NameSystem
    // 名前は下記の構成で表示される
    // [Role][Progress]
    // [Name][Mark]
    // [Lower][suffix]
    // Progress:タスク進捗/残弾等の状態表示
    // Mark:役職能力によるターゲットマークなど
    // Lower:役職用追加文字情報。Modの場合画面下に表示される。
    // Suffix:ターゲット矢印などの追加情報。

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
    /// <summary>
    /// seerもしくはseenが自分であるときのMark
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.MarkOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";
    /// <summary>
    /// seerもしくはseenが自分であるときのLowerTex
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.LowerOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <param name="isForHud">ModでHudとして表示する場合</param>
    /// <returns>構築したLowerText</returns>
    public virtual string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => "";
    /// <summary>
    /// seerもしくはseenが自分であるときのSuffix
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.SuffixOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";

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