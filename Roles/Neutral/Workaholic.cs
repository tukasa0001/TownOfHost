using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;
namespace TownOfHostForE.Roles.Neutral;

public sealed class Workaholic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Workaholic),
            player => new Workaholic(player),
            CustomRoles.Workaholic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            60100,
            SetupOptionItem,
            "ワーカホリック",
            "#008b8b",
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Workaholic(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => (CannotWinAtDeath && player.Data.IsDead) ? HasTask.False : HasTask.ForRecompute
    )
    {
        Seen = OptionSeen.GetBool();
        TaskSeen = OptionTaskSeen.GetBool();
        CannotWinAtDeath = OptionCannotWinAtDeath.GetBool();
        ventCooldown = OptionVentCooldown.GetFloat();
    }

    private static OptionItem OptionSeen;
    private static OptionItem OptionTaskSeen;
    private static OptionItem OptionCannotWinAtDeath;
    private static OptionItem OptionVentCooldown;
    private static Options.OverrideTasksData Tasks;
    private enum OptionName
    {
        WorkaholicSeen,
        WorkaholicTaskSeen,
        WorkaholicCannotWinAtDeath,
    }
    public static bool Seen;
    public static bool TaskSeen;
    private static bool CannotWinAtDeath;
    private static float ventCooldown;

    private static void SetupOptionItem()
    {
        OptionSeen = BooleanOptionItem.Create(RoleInfo, 10, OptionName.WorkaholicSeen, true, false);
        OptionTaskSeen = BooleanOptionItem.Create(RoleInfo, 11, OptionName.WorkaholicTaskSeen, true, false, OptionSeen);
        OptionCannotWinAtDeath = BooleanOptionItem.Create(RoleInfo, 13, OptionName.WorkaholicCannotWinAtDeath, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.VentCooldown, new(0f, 180f, 2.5f), 0f, false)
                .SetValueFormat(OptionFormat.Seconds);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = ventCooldown;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }

    public override bool OnCompleteTask()
    {
        if (IsTaskFinished && !(CannotWinAtDeath && !Player.IsAlive()))
        {   //全タスク完了で、死亡後勝利無効でない場合
            //生存者は全員爆破
            //foreach (var otherPlayer in Main.AllAlivePlayerControls)
            //{
            //    if (otherPlayer == Player) continue;

            //    otherPlayer.SetRealKiller(Player);
            //    otherPlayer.RpcMurderPlayer(otherPlayer);
            //    var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
            //    playerState.DeathReason = CustomDeathReason.Bombed;
            //    playerState.SetDead();
            //}
            GameManager.Instance.enabled = false;

            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Workaholic);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            if (!AmongUsClient.Instance.AmHost) return true;
            GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
        }
        return true;
    }

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (Seen) enabled = true;
    }

}
