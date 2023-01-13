using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using UnityEngine;
using VentLib.Logging;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        public static bool DoDisable => OldOptions.DisableDevices.GetBool() || OldOptions.IsStandardHAS;
        private static List<byte> DesyncComms = new();
        private static int frame = 0;
        public static readonly Dictionary<string, Vector2> DevicePos = new()
        {
            ["SkeldAdmin"] = new(3.48f, -8.62f),
            ["SkeldCamera"] = new(-13.06f, -2.45f),
            ["MiraHQAdmin"] = new(21.02f, 19.09f),
            ["MiraHQDoorLog"] = new(16.22f, 5.82f),
            ["PolusLeftAdmin"] = new(22.80f, -21.52f),
            ["PolusRightAdmin"] = new(24.66f, -21.52f),
            ["PolusCamera"] = new(2.96f, -12.74f),
            ["PolusVital"] = new(26.70f, -15.94f),
            ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
            ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
            ["AirshipCamera"] = new(8.10f, -9.63f),
            ["AirshipVital"] = new(25.24f, -7.94f)
        };
        public static float UsableDistance()
        {
            var Map = (MapNames)TOHPlugin.NormalOptions.MapId;
            return Map switch
            {
                MapNames.Skeld => 1.8f,
                MapNames.Mira => 2.4f,
                MapNames.Polus => 1.8f,
                //MapNames.Dleks => 1.5f,
                MapNames.Airship => 1.8f,
                _ => 0.0f
            };
        }
        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            if (frame != 0) return;

            if (!DoDisable) return;
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                try
                {
                    if (pc.IsModClient()) continue;

                    bool doComms = false;
                    Vector2 PlayerPos = pc.GetTruePosition();
                    bool ignore = (OldOptions.DisableDevicesIgnoreImpostors.GetBool() && pc.Is(RoleType.Impostor)) ||
                            (OldOptions.DisableDevicesIgnoreMadmates.GetBool() && pc.Is(RoleType.Madmate)) ||
                            (OldOptions.DisableDevicesIgnoreNeutrals.GetBool() && pc.Is(RoleType.Neutral)) ||
                            (OldOptions.DisableDevicesIgnoreCrewmates.GetBool() && pc.Is(RoleType.Crewmate)) ||
                            (OldOptions.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);

                    if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                    {
                        switch (TOHPlugin.NormalOptions.MapId)
                        {
                            case 0:
                                if (OldOptions.DisableSkeldAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                if (OldOptions.DisableSkeldCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                break;
                            case 1:
                                if (OldOptions.DisableMiraHQAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                if (OldOptions.DisableMiraHQDoorLog.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                break;
                            case 2:
                                if (OldOptions.DisablePolusAdmin.GetBool())
                                {
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                                }
                                if (OldOptions.DisablePolusCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                if (OldOptions.DisablePolusVital.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                break;
                            case 4:
                                if (OldOptions.DisableAirshipCockpitAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                if (OldOptions.DisableAirshipRecordsAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                if (OldOptions.DisableAirshipCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                if (OldOptions.DisableAirshipVital.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                break;
                        }
                    }
                    doComms &= !ignore;
                    if (doComms && !pc.inVent)
                    {
                        if (!DesyncComms.Contains(pc.PlayerId))
                            DesyncComms.Add(pc.PlayerId);

                        pc.RpcDesyncRepairSystem(SystemTypes.Comms, 128);
                    }
                    else if (!Utils.IsActive(SystemTypes.Comms) && DesyncComms.Contains(pc.PlayerId))
                    {
                        DesyncComms.Remove(pc.PlayerId);
                        pc.RpcDesyncRepairSystem(SystemTypes.Comms, 16);

                        if (TOHPlugin.NormalOptions.MapId == 1)
                            pc.RpcDesyncRepairSystem(SystemTypes.Comms, 17);
                    }
                }
                catch (Exception ex)
                {
                    VentLogger.Error(ex.ToString(), "DisableDevice");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (!OldOptions.DisableDevices.GetBool()) return;
            UpdateDisableDevices();
        }

        public static void UpdateDisableDevices()
        {
            var player = PlayerControl.LocalPlayer;
            bool ignore = player.Is(CustomRoles.GM) ||
                player.Data.IsDead ||
                (OldOptions.DisableDevicesIgnoreImpostors.GetBool() && player.Is(RoleType.Impostor)) ||
                (OldOptions.DisableDevicesIgnoreMadmates.GetBool() && player.Is(RoleType.Madmate)) ||
                (OldOptions.DisableDevicesIgnoreNeutrals.GetBool() && player.Is(RoleType.Neutral)) ||
                (OldOptions.DisableDevicesIgnoreCrewmates.GetBool() && player.Is(RoleType.Crewmate)) ||
                (OldOptions.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);
            var admins = GameObject.FindObjectsOfType<MapConsole>(true);
            var consoles = GameObject.FindObjectsOfType<SystemConsole>(true);
            if (admins == null || consoles == null) return;
            switch (TOHPlugin.NormalOptions.MapId)
            {
                case 0:
                    if (OldOptions.DisableSkeldAdmin.GetBool())
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore;
                    if (OldOptions.DisableSkeldCamera.GetBool())
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = false || ignore);
                    break;
                case 1:
                    if (OldOptions.DisableMiraHQAdmin.GetBool())
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore;
                    if (OldOptions.DisableMiraHQDoorLog.GetBool())
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                    break;
                case 2:
                    if (OldOptions.DisablePolusAdmin.GetBool())
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                    if (OldOptions.DisablePolusCamera.GetBool())
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                    if (OldOptions.DisablePolusVital.GetBool())
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                    break;
                case 4:
                    admins.Do(x =>
                    {
                        if ((OldOptions.DisableAirshipCockpitAdmin.GetBool() && x.name == "panel_cockpit_map") ||
                            (OldOptions.DisableAirshipRecordsAdmin.GetBool() && x.name == "records_admin_map"))
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore;
                    });
                    if (OldOptions.DisableAirshipCamera.GetBool())
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false || ignore);
                    if (OldOptions.DisableAirshipVital.GetBool())
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = false || ignore);
                    break;
            }
        }
    }
}