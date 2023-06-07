using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class SabotageMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SabotageMaster),
            player => new SabotageMaster(player),
            CustomRoles.SabotageMaster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20300,
            SetupOptionItem,
            "sa",
            "#0000ff",
            introSound: () => ShipStatus.Instance.SabotageSound
        );
    public SabotageMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SkillLimit = OptionSkillLimit.GetInt();
        FixesDoors = OptionFixesDoors.GetBool();
        FixesReactors = OptionFixesReactors.GetBool();
        FixesOxygens = OptionFixesOxygens.GetBool();
        FixesComms = OptionFixesComms.GetBool();
        FixesElectrical = OptionFixesElectrical.GetBool();

        UsedSkillCount = 0;
    }

    public static OptionItem OptionSkillLimit;
    public static OptionItem OptionFixesDoors;
    public static OptionItem OptionFixesReactors;
    public static OptionItem OptionFixesOxygens;
    public static OptionItem OptionFixesComms;
    public static OptionItem OptionFixesElectrical;
    enum OptionName
    {
        SabotageMasterSkillLimit,
        SabotageMasterFixesDoors,
        SabotageMasterFixesReactors,
        SabotageMasterFixesOxygens,
        SabotageMasterFixesCommunications,
        SabotageMasterFixesElectrical,
    }
    private int SkillLimit;
    private bool FixesDoors;
    private bool FixesReactors;
    private bool FixesOxygens;
    private bool FixesComms;
    private bool FixesElectrical;
    public int UsedSkillCount;

    private bool DoorsProgressing = false;
    private bool fixedComms = false;

    public static void SetupOptionItem()
    {
        OptionSkillLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SabotageMasterSkillLimit, new(0, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionFixesDoors = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SabotageMasterFixesDoors, false, false);
        OptionFixesReactors = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SabotageMasterFixesReactors, false, false);
        OptionFixesOxygens = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SabotageMasterFixesOxygens, false, false);
        OptionFixesComms = BooleanOptionItem.Create(RoleInfo, 14, OptionName.SabotageMasterFixesCommunications, false, false);
        OptionFixesElectrical = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SabotageMasterFixesElectrical, false, false);
    }
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType, byte amount)
    {
        if (!Is(player)) return true;
        var shipStatus = ShipStatus.Instance;
        if (SkillLimit > 0 && UsedSkillCount >= SkillLimit) return true;
        switch (systemType)
        {
            case SystemTypes.Reactor:
                if (!FixesReactors) break;
                if (amount.HasAnyBit(64))
                {
                    //片方の入力が正解したタイミング

                    //Skeld、Miraは16だけでOK。Airshipは16,17とも必要
                    shipStatus.RepairSystem(SystemTypes.Reactor, Player, 16);
                    shipStatus.RepairSystem(SystemTypes.Reactor, Player, 17);
                    UsedSkillCount++;
                }
                break;
            case SystemTypes.Laboratory:
                if (!FixesReactors) break;
                if (amount.HasAnyBit(64))
                {
                    //片方の入力がされたタイミング

                    //Polusラボは16だけで完了
                    shipStatus.RepairSystem(SystemTypes.Laboratory, Player, 16);
                    UsedSkillCount++;
                }
                break;
            case SystemTypes.LifeSupp:
                if (!FixesOxygens) break;
                if (amount.HasAnyBit(64))
                {
                    //片方の入力が正解したタイミング

                    //Skeld,MiraのO2は16だけで完了
                    shipStatus.RepairSystem(SystemTypes.LifeSupp, Player, 16);
                    UsedSkillCount++;
                }
                break;
            case SystemTypes.Comms:
                if (!FixesComms) break;
                if (amount.HasAnyBit(64))
                {
                    //パネル開いたタイミング
                    fixedComms = false;
                }
                if (!fixedComms && amount.HasAnyBit(16))
                {
                    //片方の入力が正解したタイミング

                    fixedComms = true;
                    //MiraHQのコミュは16,17がそろったとき完了。
                    //もう一方のパネルの完了報告
                    shipStatus.RepairSystem(SystemTypes.Comms, Player, (byte)(16 | (~amount & 1)));
                    UsedSkillCount++;
                }
                break;
            case SystemTypes.Electrical:
                if (!FixesElectrical) break;
                if (!amount.HasAnyBit(128))
                {
                    //いずれかのスイッチが変更されたタイミング

                    var sw = shipStatus.Systems[SystemTypes.Electrical].TryCast<SwitchSystem>();
                    if (sw != null)
                    {
                        //現在のスイッチ状態を今から動かすスイッチ以外を正解にする
                        var fixbit = 1 << amount;
                        sw.ActualSwitches = (byte)(sw.ExpectedSwitches ^ fixbit);
                        UsedSkillCount++;
                    }
                }
                break;
            case SystemTypes.Doors:
                if (!FixesDoors) break;
                if (DoorsProgressing) break;

                int mapId = Main.NormalOptions.MapId;
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                DoorsProgressing = true;
                if (mapId == 2)
                {
                    //Polus
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 71, 72);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 67, 68);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 64, 66);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 73, 74);
                }
                else if (mapId == 4)
                {
                    //Airship
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 64, 67);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 71, 73);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 74, 75);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 76, 78);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 68, 70);
                    RepairSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 83, 84);
                }
                DoorsProgressing = false;
                break;
        }
        return true;
    }
}