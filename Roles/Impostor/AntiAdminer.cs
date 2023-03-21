using System;
using System.Collections.Generic;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE.Roles.Impostor;

// 参考 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
// 贡献：https://github.com/Yumenopai/TownOfHost_Y/tree/AntiAdminer
internal class AntiAdminer
{
    private static readonly int Id = 3100;
    private static List<byte> playerIdList = new();

    private static OptionItem CanCheckCamera;
    public static bool IsAdminWatch;
    public static bool IsVitalWatch;
    public static bool IsDoorLogWatch;
    public static bool IsCameraWatch;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.AntiAdminer);
        CanCheckCamera = BooleanOptionItem.Create(Id + 10, "CanCheckCamera", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.AntiAdminer]);
    }
    public static void Init()
    {
        playerIdList = new();
        IsAdminWatch = false;
        IsVitalWatch = false;
        IsDoorLogWatch = false;
        IsCameraWatch = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable() => playerIdList.Count > 0;

    private static int Count = 0;
    public static void FixedUpdate()
    {
        if (!IsEnable()) return;

        Count--; if (Count > 0) return; Count = 5;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (Pelican.IsEaten(pc.PlayerId) || pc.inVent || pc.GetCustomRole().IsImpostor()) continue;
            try
            {
                Vector2 PlayerPos = pc.GetTruePosition();

                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance();
                        break;
                    case 1:
                        if (!Options.DisableMiraHQAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableMiraHQDoorLog.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance();
                        break;
                    case 2:
                        if (!Options.DisablePolusAdmin.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance();
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance();
                        }
                        if (!Options.DisablePolusCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisablePolusVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance();
                        break;
                    case 4:
                        if (!Options.DisableAirshipCockpitAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipRecordsAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "AntiAdmin");
            }
        }

        var isChange = false;

        isChange |= IsAdminWatch != Admin;
        IsAdminWatch = Admin;
        isChange |= IsVitalWatch != Vital;
        IsVitalWatch = Vital;
        isChange |= IsDoorLogWatch != DoorLog;
        IsDoorLogWatch = DoorLog;
        if (CanCheckCamera.GetBool())
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            Utils.NotifyRoles();
            foreach (PlayerControl pc in Main.AllPlayerControls)
                FixedUpdatePatch.Postfix(pc);
        }
    }
}