using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor;
public sealed class AntiAdminer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(AntiAdminer),
            player => new AntiAdminer(player),
            CustomRoles.AntiAdminer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3100,
            SetupOptionItem,
            "aa"
        );
    public AntiAdminer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanCheckCamera = OptionCanCheckCamera.GetBool();
    }

    private static OptionItem OptionCanCheckCamera;
    enum OptionName
    {
        AntiAdminerCanCheckCamera
    }
    private static bool CanCheckCamera;

    public static bool IsAdminWatch;
    public static bool IsVitalWatch;
    public static bool IsDoorLogWatch;
    public static bool IsCameraWatch;
    int Count = 0;

    private static void SetupOptionItem()
    {
        OptionCanCheckCamera = BooleanOptionItem.Create(RoleInfo, 10, OptionName.AntiAdminerCanCheckCamera, false, false);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive()) return;

        Count--;
        if (Count > 0) return;
        Count = 3;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (pc.inVent) continue;
            try
            {
                Vector2 PlayerPos = pc.GetTruePosition();
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        if (!DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance();
                        break;
                    case 1:
                        if (!DisableMiraHQAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableMiraHQDoorLog.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance();
                        break;
                    case 2:
                        if (!DisablePolusAdmin.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance();
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance();
                        }
                        if (!DisablePolusCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance();
                        if (!DisablePolusVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance();
                        break;
                    case 4:
                        if (!DisableAirshipCockpitAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableAirshipRecordsAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance();
                        if (!DisableAirshipCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance();
                        if (!DisableAirshipVital.GetBool())
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
        if (CanCheckCamera)
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            Utils.NotifyRoles();
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        if (isForMeeting) return "";

        StringBuilder sb = new();
        if (IsAdminWatch) sb.Append("★").Append(GetString("AntiAdminerAD"));
        if (IsVitalWatch) sb.Append("★").Append(GetString("AntiAdminerVI"));
        if (IsDoorLogWatch) sb.Append("★").Append(GetString("AntiAdminerDL"));
        if (IsCameraWatch) sb.Append("★").Append(GetString("AntiAdminerCA"));

        return sb.ToString();
    }
}