using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class SabotageMaster : RoleBase, ISystemTypeUpdateHook
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
    private bool fixedSabotage;

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
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesReactors) return true;
        if (amount.HasAnyBit(ReactorSystemType.AddUserOp))
        {
            //片方を直したタイミング
            ShipStatus.Instance.UpdateSystem((MapNames)Main.NormalOptions.MapId == MapNames.Polus ? SystemTypes.Laboratory : SystemTypes.Reactor, Player, ReactorSystemType.ClearCountdown);
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesReactors) return true;
        var tags = (HeliSabotageSystem.Tags)(amount & HeliSabotageSystem.TagMask);
        if (tags == HeliSabotageSystem.Tags.ActiveBit)
        {
            //パネル開いたタイミング
            fixedSabotage = false;
        }
        if (!fixedSabotage && tags == HeliSabotageSystem.Tags.FixBit)
        {
            //片方の入力が正解したタイミング
            fixedSabotage = true;
            //ヘリサボは16,17がそろったとき完了。
            var consoleId = amount & HeliSabotageSystem.IdMask;
            var otherConsoleId = (consoleId + 1) % 2;
            //もう一方のパネルの完了報告
            ShipStatus.Instance.UpdateSystem(SystemTypes.HeliSabotage, Player, (byte)(otherConsoleId | (int)HeliSabotageSystem.Tags.FixBit));
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesOxygens) return true;
        if (amount.HasAnyBit(LifeSuppSystemType.AddUserOp))
        {
            //片方の入力が正解したタイミング
            ShipStatus.Instance.UpdateSystem(SystemTypes.LifeSupp, Player, LifeSuppSystemType.ClearCountdown);
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesComms) return true;
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        if (tags == HqHudSystemType.Tags.ActiveBit)
        {
            //パネル開いたタイミング
            fixedSabotage = false;
        }
        if (!fixedSabotage && tags == HqHudSystemType.Tags.FixBit)
        {
            //片方の入力が正解したタイミング
            fixedSabotage = true;
            //MiraHQのコミュは16,17がそろったとき完了。
            var consoleId = amount & HqHudSystemType.IdMask;
            var otherConsoleId = (consoleId + 1) % 2;
            //もう一方のパネルの完了報告
            ShipStatus.Instance.UpdateSystem(SystemTypes.Comms, Player, (byte)(otherConsoleId | (int)HqHudSystemType.Tags.FixBit));
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesElectrical) return true;
        if (amount.HasBit(SwitchSystem.DamageSystem)) return true;
        //いずれかのスイッチが変更されたタイミング
        //現在のスイッチ状態を今から動かすスイッチ以外を正解にする

        var fixbit = 1 << amount;
        switchSystem.ActualSwitches = (byte)(switchSystem.ExpectedSwitches ^ fixbit);
        UsedSkillCount++;
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateDoorsSystem(DoorsSystemType doorsSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (!FixesDoors) return true;
        if (DoorsProgressing) return true;

        int mapId = Main.NormalOptions.MapId;
        if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;
        var shipStatus = ShipStatus.Instance;

        DoorsProgressing = true;
        if (mapId == 2)
        {
            //Polus
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 71, 72);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 67, 68);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 64, 66);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 73, 74);
        }
        else if (mapId == 4)
        {
            //Airship
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 64, 67);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 71, 73);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 74, 75);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 76, 78);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 68, 70);
            ShipStatusUpdateSystemPatch.CheckAndOpenDoorsRange(shipStatus, amount, 83, 84);
        }
        else if (mapId == 5)
        {
            // Fungle
            var openedDoorId = amount & DoorsSystemType.IdMask;
            var openedDoor = shipStatus.AllDoors.FirstOrDefault(door => door.Id == openedDoorId);
            if (openedDoor == null)
            {
                Logger.Warn($"不明なドアが開けられました: {openedDoorId}", nameof(SabotageMaster));
            }
            else
            {
                // 同じ部屋のドアで，今から開けるドアではないものを全部開ける
                var room = openedDoor.Room;
                foreach (var door in shipStatus.AllDoors)
                {
                    if (door.Id != openedDoorId && door.Room == room)
                    {
                        door.SetDoorway(true);
                    }
                }
            }
        }
        DoorsProgressing = false;
        return true;
    }
    private bool IsSkillAvailable() => SkillLimit <= 0 || UsedSkillCount < SkillLimit;
}