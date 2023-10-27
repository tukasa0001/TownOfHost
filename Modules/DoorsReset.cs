using TownOfHost.Attributes;

namespace TownOfHost.Modules;

public static class DoorsReset
{
    private static bool isEnabled = false;
    private static ResetMode mode;
    private static DoorsSystemType DoorsSystem => ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Doors, out var system) ? system.TryCast<DoorsSystemType>() : null;
    private static readonly LogHandler logger = Logger.Handler(nameof(DoorsReset));

    [GameModuleInitializer]
    public static void Initialize()
    {
        // AirshipとPolusとFungle以外は非対応
        if ((MapNames)Main.NormalOptions.MapId is not (MapNames.Airship or MapNames.Polus or MapNames.Fungle))
        {
            isEnabled = false;
            return;
        }
        isEnabled = Options.ResetDoorsEveryTurns.GetBool();
        mode = (ResetMode)Options.DoorsResetMode.GetValue();
        logger.Info($"初期化: [ {isEnabled}, {mode} ]");
    }

    /// <summary>設定に応じてドア状況をリセット</summary>
    public static void ResetDoors()
    {
        if (!isEnabled || DoorsSystem == null)
        {
            return;
        }
        logger.Info("リセット");

        switch (mode)
        {
            case ResetMode.AllOpen: OpenAllDoors(); break;
            case ResetMode.AllClosed: CloseAllDoors(); break;
            case ResetMode.RandomByDoor: OpenOrCloseAllDoorsRandomly(); break;
            default: logger.Warn($"無効なモード: {mode}"); break;
        }
    }
    /// <summary>マップ上の全ドアを開放</summary>
    private static void OpenAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, true);
        }
        DoorsSystem.IsDirty = true;
    }
    /// <summary>マップ上の全ドアを閉鎖</summary>
    private static void CloseAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, false);
        }
        DoorsSystem.IsDirty = true;
    }
    /// <summary>マップ上の全ドアをランダムに開閉</summary>
    private static void OpenOrCloseAllDoorsRandomly()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            var isOpen = IRandom.Instance.Next(2) > 0;
            SetDoorOpenState(door, isOpen);
        }
        DoorsSystem.IsDirty = true;
    }

    /// <summary>ドアの開閉状況を設定する．サボタージュで閉められないドアに対しては何もしない</summary>
    /// <param name="door">対象のドア</param>
    /// <param name="isOpen">開けるならtrue，閉めるならfalse</param>
    private static void SetDoorOpenState(OpenableDoor door, bool isOpen)
    {
        if (IsValidDoor(door))
        {
            door.SetDoorway(isOpen);
        }
    }
    /// <summary>リセット対象のドアかどうか判定する</summary>
    /// <returns>リセット対象ならtrue</returns>
    private static bool IsValidDoor(OpenableDoor door)
    {
        // エアシラウンジトイレとPolus除染室のドアは対象外
        if (door.Room is SystemTypes.Lounge or SystemTypes.Decontamination)
        {
            return false;
        }
        return true;
    }

    public enum ResetMode { AllOpen, AllClosed, RandomByDoor, }
}
