using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class CandleLighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Lighter),
            player => new CandleLighter(player),
            CustomRoles.CandleLighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            41000,
            SetupOptionItem,
            "キャンドルライター",
            "#ff7f50"
        );
    public CandleLighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        StartVision = OptionTaskStartVision.GetFloat();
        EndVision = OptionTaskEndVision.GetFloat();
        CountStartTime = OptionCountStartTime.GetInt();
        EndVisionTime = OptionTaskEndVisionTime.GetInt();
        TimeMoveMeeting = OptionTaskTimeMoveMeeting.GetBool();
    }

    private static OptionItem OptionTaskStartVision;
    private static OptionItem OptionCountStartTime;
    private static OptionItem OptionTaskEndVisionTime;
    private static OptionItem OptionTaskEndVision;
    private static OptionItem OptionTaskTimeMoveMeeting;
    enum OptionName
    {
        CandleLighterStartVision,
        CandleLighterCountStartTime,
        CandleLighterEndVisionTime,
        CandleLighterEndVision,
        CandleLighterTimeMoveMeeting,
    }

    private static float StartVision;
    private static float EndVision;
    private static int EndVisionTime;
    private static int CountStartTime;
    private static bool TimeMoveMeeting;

    private static float UpdateTime;
    float ElapsedTime;

    private static void SetupOptionItem()
    {
        OptionTaskStartVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.CandleLighterStartVision, new(0.5f, 5f, 0.1f), 2.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCountStartTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CandleLighterCountStartTime, new(0, 300, 10), 0, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskEndVisionTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CandleLighterEndVisionTime, new(60, 1200, 60), 480, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskEndVision = FloatOptionItem.Create(RoleInfo, 13, OptionName.CandleLighterEndVision, new(0f, 0.5f, 0.05f), 0.1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskTimeMoveMeeting = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CandleLighterTimeMoveMeeting, false, false);
    }
    public override void Add()
    {
        UpdateTime = 1.0f;
        ElapsedTime = EndVisionTime + CountStartTime;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        float Vision;
        //初めの変化待機時間中は最大視界
        if (ElapsedTime > EndVisionTime) Vision = StartVision;
        else Vision = StartVision * (ElapsedTime / EndVisionTime);

        //視界が設定最小視界より小さくなった時は強制
        if (Vision <= EndVision) Vision = EndVision;

        opt.SetFloat(FloatOptionNames.CrewLightMod, Vision);
        // 停電は無効
        if (Utils.IsActive(SystemTypes.Electrical))
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision * 5);
    }

    public override bool OnCompleteTask()
    {
        if (Player.IsAlive() && IsTaskFinished)
        {
            // タスク完了で視界を一番広く(更新時間をリセット)する
            ElapsedTime = EndVisionTime;
        }
        return true;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        // 会議時間中に変化しない設定の場合はタスクターン以外返す
        if (!GameStates.IsInTask && !TimeMoveMeeting) return;

        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1.0f; // 負荷軽減の為1秒ごとの更新

        if (ElapsedTime > 0f)
        {
            ElapsedTime -= Time.fixedDeltaTime; //時間をカウント

            if (UpdateTime == 1.0f) player.SyncSettings();
        }
    }
}