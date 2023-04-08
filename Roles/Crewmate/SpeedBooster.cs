using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class SpeedBooster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(SpeedBooster),
            player => new SpeedBooster(player),
            CustomRoles.SpeedBooster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20600,
            SetupOptionItem,
            "#00ffff"
        );
    public SpeedBooster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        UpSpeed = OptionUpSpeed.GetFloat();
        TaskTrigger = OptionTaskTrigger.GetInt();

        BoostTarget = byte.MaxValue;
    }

    private static OptionItem OptionUpSpeed; //加速値
    private static OptionItem OptionTaskTrigger; //効果を発動するタスク完了数
    enum OptionName
    {
        SpeedBoosterUpSpeed,
        SpeedBoosterTaskTrigger
    }
    private static float UpSpeed;
    private static int TaskTrigger;

    public byte BoostTarget;

    private static void SetupOptionItem()
    {
        OptionUpSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.SpeedBoosterUpSpeed, new(1.1f, 1.5f, 1.1f), 1.3f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SpeedBoosterTaskTrigger, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override bool OnCompleteTask()
    {
        var playerId = Player.PlayerId;
        var taskState = Player.GetPlayerTaskState();
        if (Player.IsAlive()
            && BoostTarget == byte.MaxValue
            && (taskState.IsTaskFinished || taskState.CompletedTasksCount >= TaskTrigger))
        {   //ｽﾋﾟﾌﾞが生きていて、SpeedBoostTargetに登録済みでなく、全タスク完了orトリガー数までタスクを完了している場合
            var rand = IRandom.Instance;
            List<PlayerControl> targetPlayers = new();
            foreach (var p in Main.AllAlivePlayerControls.ToArray())
            {
                if (BoostTarget != p.PlayerId)
                    targetPlayers.Add(p);
            }
            if (targetPlayers.Count >= 1)
            {
                var target = targetPlayers[rand.Next(0, targetPlayers.Count)];
                Logger.Info("スピードブースト先:" + target.GetNameWithRole(), "SpeedBooster");
                BoostTarget = target.PlayerId;
                Main.AllPlayerSpeed[BoostTarget] += 1 - UpSpeed;
                target.MarkDirtySettings();
            }
            else //ターゲットが0ならアップ先をプレイヤーをnullに
            {
                BoostTarget = byte.MaxValue;
                Logger.SendInGame("Error.SpeedBoosterNullException");
                Logger.Warn("スピードブースト先がnullです。", "SpeedBooster");
            }
        }

        return true;
    }
}