using System;
using System.Linq;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Core;

public abstract class RoleBase : IDisposable
{
    public PlayerControl Player { get; private set; }
    /// <summary>
    /// プレイヤーの状態
    /// </summary>
    public readonly PlayerState MyState;
    /// <summary>
    /// プレイヤーのタスク状態
    /// </summary>
    public readonly TaskState MyTaskState;
    /// <summary>
    /// タスクを持っているか。
    /// 初期値はクルー役職のみ持つ
    /// </summary>
    protected Func<HasTask> hasTasks;
    /// <summary>
    /// タスクを持っているか
    /// </summary>
    public HasTask HasTasks => hasTasks.Invoke();
    /// <summary>
    /// タスクが完了しているか
    /// </summary>
    public bool IsTaskFinished => MyTaskState.IsTaskFinished;
    /// <summary>
    /// アビリティボタンで発動する能力を持っているか
    /// </summary>
    public bool HasAbility { get; private set; }
    public RoleBase(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null
    )
    {
        Player = player;
        this.hasTasks = hasTasks ?? (roleInfo.CustomRoleType == CustomRoleTypes.Crewmate ? () => HasTask.True : () => HasTask.False);
        HasAbility = hasAbility ?? roleInfo.BaseRoleType.Invoke() is
            RoleTypes.Shapeshifter or
            RoleTypes.Engineer or
            RoleTypes.Scientist or
            RoleTypes.GuardianAngel or
            RoleTypes.CrewmateGhost or
            RoleTypes.ImpostorGhost;

        MyState = PlayerState.GetByPlayerId(player.PlayerId);
        MyTaskState = MyState.GetTaskState();

        CustomRoleManager.AllActiveRoles.Add(Player.PlayerId, this);
    }
#pragma warning disable CA1816
    public void Dispose()
    {
        OnDestroy();
        CustomRoleManager.AllActiveRoles.Remove(Player.PlayerId);
        Player = null;
    }
#pragma warning restore CA1816
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
    /// RoleBase専用のRPC送信クラス
    /// 自身のPlayerIdを自動的に送信する
    /// </summary>
    protected class RoleRPCSender : IDisposable
    {
        public MessageWriter Writer;
        public RoleRPCSender(RoleBase role, CustomRPC rpcType)
        {
            Writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)rpcType, SendOption.Reliable, -1);
            Writer.Write(role.Player.PlayerId);
        }
        public void Dispose()
        {
            AmongUsClient.Instance.FinishRpcImmediately(Writer);
        }
    }
    /// <summary>
    /// RPC送信クラスの作成
    /// PlayerIdは自動的に追記されるので意識しなくてもよい。
    /// </summary>
    /// <param name="rpcType">送信するCustomRPC</param>
    /// <returns>送信に使用するRoleRPCSender</returns>
    protected RoleRPCSender CreateSender(CustomRPC rpcType)
    {
        return new RoleRPCSender(this, rpcType);
    }
    /// <summary>
    /// RPCを受け取った時に呼ばれる関数
    /// RoleRPCSenderで送信されたPlayerIdは削除されて渡されるため意識しなくてもよい。
    /// </summary>
    /// <param name="reader">届いたRPCの情報</param>
    /// <param name="rpcType">届いたCustomRPC</param>
    public virtual void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    { }
    /// <summary>
    /// 能力ボタンを使えるかどうか
    /// </summary>
    /// <returns>trueを返した場合、能力ボタンを使える</returns>
    public virtual bool CanUseAbilityButton() => true;
    /// <summary>
    /// BuildGameOptionsで呼ばれる関数
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt)
    { }

    /// <summary>
    /// ターゲットとしてのCheckMurder処理
    /// キラーより先に判定
    /// キル出来ない状態(無敵など)はinfo.CanKill=falseとしてtrueを返す
    /// キル行為自体をなかったことにする場合はfalseを返す。
    /// </summary>
    /// <param name="info">キル関係者情報</param>
    /// <returns>false:キル行為を起こさせない</returns>
    public virtual bool OnCheckMurderAsTarget(MurderInfo info) => true;

    /// <summary>
    /// ターゲットとしてのMurderPlayer処理
    /// </summary>
    /// <param name="info">キル関係者情報</param>
    public virtual void OnMurderPlayerAsTarget(MurderInfo info)
    { }

    /// <summary>
    /// シェイプシフト時に呼ばれる関数
    /// 自分自身について呼ばれるため本人確認不要
    /// Host以外も呼ばれるので注意
    /// </summary>
    /// <param name="target">変身先</param>
    public virtual void OnShapeshift(PlayerControl target)
    { }

    /// <summary>
    /// タスクターンに常時呼ばれる関数
    /// 自分自身について呼ばれるため本人確認不要
    /// Host以外も呼ばれるので注意
    /// playerが自分以外であるときに処理したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.OnFixedUpdateOthersに登録する
    /// </summary>
    /// <param name="player">対象プレイヤー</param>
    public virtual void OnFixedUpdate(PlayerControl player)
    { }

    /// <summary>
    /// 通報時，会議が呼ばれることが確定してから呼ばれる関数<br/>
    /// 通報に関係ないプレイヤーも呼ばれる
    /// </summary>
    /// <param name="reporter">通報したプレイヤー</param>
    /// <param name="target">通報されたプレイヤー</param>
    public virtual void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    { }

    /// <summary>
    /// <para>ベントに入ったときに呼ばれる関数</para>
    /// <para>キャンセル可</para>
    /// </summary>
    /// <param name="physics"></param>
    /// <param name="id"></param>
    /// <returns>falseを返すとベントから追い出され、他人からアニメーションも見られません</returns>
    public virtual bool OnEnterVent(PlayerPhysics physics, int ventId) => true;

    /// <summary>
    /// ミーティングが始まった時に呼ばれる関数
    /// </summary>
    public virtual void OnStartMeeting()
    { }

    /// <summary>
    /// 誰かが投票したときに発火する
    /// </summary>
    /// <param name="voterId">投票した人のID</param>
    /// <param name="sourceVotedForId">投票された人のID</param>
    /// <returns>(変更後の投票先(変更しないならnull), 変更後の票数(変更しないならnull), 投票をカウントするか)</returns>
    public virtual (byte? votedForId, int? numVotes, bool doVote) OnVote(byte voterId, byte sourceVotedForId) => (null, null, true);

    /// <summary>
    /// 追放後に行われる処理
    /// </summary>
    /// <param name="exiled">追放されるプレイヤー</param>
    /// <param name="DecidedWinner">勝者を確定させるか</param>
    public virtual void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    { }

    /// <summary>
    /// タスクターンが始まる直前に毎回呼ばれる関数
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }

    /// <summary>
    /// タスクが一個完了するごとに呼ばれる関数
    /// </summary>
    /// <returns>falseを返すとバニラ処理をキャンセルする</returns>
    public virtual bool OnCompleteTask() => true;

    // == Sabotage関連処理 ==
    /// <summary>
    /// 自身がサボタージュを発生させたときに発火する
    /// ドア閉めには関与できない
    /// </summary>
    /// <param name="systemType">サボタージュの種類</param>
    /// <returns>falseでサボタージュをキャンセル</returns>
    public virtual bool OnInvokeSabotage(SystemTypes systemType) => true;

    /// <summary>
    /// 誰かがサボタージュが発生させたときに呼ばれる
    /// amount&0x80!=0がサボタージュ開始タイミング
    /// その他の値は修理情報など
    /// </summary>
    /// <param name="player">アクションを起こしたプレイヤー</param>
    /// <param name="systemType">サボタージュの種類</param>
    /// <param name="amount">現在の状態など</param>
    /// <returns>falseで修理活動等のキャンセル</returns>
    public virtual bool OnSabotage(PlayerControl player, SystemTypes systemType, byte amount) => true;

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
    /// seenによるRoleNameの書き換え
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="enabled">RoleNameを表示するかどうか</param>
    /// <param name="roleColor">RoleNameの色</param>
    /// <param name="roleText">RoleNameのテキスト</param>
    public virtual void OverrideRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// seerによるRoleNameの書き換え
    /// </summary>
    /// <param name="seen">見られる側</param>
    /// <param name="enabled">RoleNameを表示するかどうか</param>
    /// <param name="roleColor">RoleNameの色</param>
    /// <param name="roleText">RoleNameのテキスト</param>
    public virtual void OverrideRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// seerによるProgressTextの書き換え
    /// </summary>
    /// <param name="seen">見られる側</param>
    /// <param name="enabled">ProgressTextを表示するかどうか</param>
    /// <param name="text">ProgressTextのテキスト</param>
    public virtual void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    { }
    /// <summary>
    /// 役職名の横に出るテキスト
    /// </summary>
    /// <param name="comms">コミュサボ中扱いするかどうか</param>
    public virtual string GetProgressText(bool comms = false) => "";
    /// <summary>
    /// seerが自分であるときのMark
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.MarkOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";
    /// <summary>
    /// seerが自分であるときのLowerTex
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
    /// seer自分であるときのSuffix
    /// seer,seenともに自分以外であるときに表示したい場合は同じ引数でstaticとして実装し
    /// CustomRoleManager.SuffixOthersに登録する
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>構築したMark</returns>
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";

    /// <summary>
    /// シェイプシフトボタンのテキストを変更します
    /// </summary>
    public virtual string GetAbilityButtonText()
    {
        StringNames? str = Player.Data.Role.Role switch
        {
            RoleTypes.Engineer => StringNames.VentAbility,
            RoleTypes.Scientist => StringNames.VitalsAbility,
            RoleTypes.Shapeshifter => StringNames.ShapeshiftAbility,
            RoleTypes.GuardianAngel => StringNames.ProtectAbility,
            RoleTypes.ImpostorGhost or RoleTypes.CrewmateGhost => StringNames.HauntAbilityName,
            _ => null
        };
        return str.HasValue ? GetString(str.Value) : "Invalid";
    }

    protected static AudioClip GetIntroSound(RoleTypes roleType) =>
        RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;

    protected enum GeneralOption
    {
        Cooldown,
        KillCooldown,
        CanVent,
        ImpostorVision,
        CanUseSabotage,
        CanCreateMadmate,
    }
}
