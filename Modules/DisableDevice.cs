using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Crewmate;

namespace TownOfHostForE
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        public static bool DoDisable => Options.DisableDevices.GetBool() || Options.IsStandardHAS || IsPoorEnable();
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
            ["AirshipVital"] = new(25.24f, -7.94f),
            ["FungleVital"] = new(-2.765f, -9.819f)
        };
        public static float UsableDistance()
        {
            var Map = (MapNames)Main.NormalOptions.MapId;
            return Map switch
            {
                MapNames.Skeld => 1.8f,
                MapNames.Mira => 2.4f,
                MapNames.Polus => 1.8f,
                //MapNames.Dleks => 1.5f,
                MapNames.Airship => 1.8f,
                MapNames.Fungle => 1.8f,
                _ => 0.0f
            };
        }
        public static bool IsInfoPoor(PlayerControl pc)
        {
            return ((pc.Is(CustomRoles.Sheriff) && Sheriff.IsInfoPoor.GetBool())
                    || (pc.Is(CustomRoles.SillySheriff) && SillySheriff.IsInfoPoor.GetBool())
                    || pc.Is(CustomRoles.InfoPoor));
        }
        public static bool IsPoorEnable()
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (IsInfoPoor(pc))
                    return true;
            }
            return false;
        }
        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            if (frame != 0) return;

            if (!DoDisable) return;
            foreach (var pc in Main.AllPlayerControls)
            {
                bool PcIsPoor = IsInfoPoor(pc);

                try
                {
                    if (pc.IsModClient()) continue;

                    bool doComms = false;
                    Vector2 PlayerPos = pc.GetTruePosition();
                    bool ignore = (Options.DisableDevicesIgnoreImpostors.GetBool() && pc.Is(CustomRoleTypes.Impostor)) ||
                            (Options.DisableDevicesIgnoreMadmates.GetBool() && pc.Is(CustomRoleTypes.Madmate)) ||
                            (Options.DisableDevicesIgnoreNeutrals.GetBool() && pc.Is(CustomRoleTypes.Neutral)) ||
                            (Options.DisableDevicesIgnoreAnimals.GetBool() && pc.Is(CustomRoleTypes.Animals)) ||
                            (Options.DisableDevicesIgnoreCrewmates.GetBool() && pc.Is(CustomRoleTypes.Crewmate)) ||
                            (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);

                    if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                    {
                        switch (Main.NormalOptions.MapId)
                        {
                            case 0:
                                if (Options.DisableSkeldAdmin.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                if (Options.DisableSkeldCamera.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                break;
                            case 1:
                                if (Options.DisableMiraHQAdmin.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                if (Options.DisableMiraHQDoorLog.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                break;
                            case 2:
                                if (Options.DisablePolusAdmin.GetBool() || PcIsPoor)
                                {
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                                }
                                if (Options.DisablePolusCamera.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                if (Options.DisablePolusVital.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                break;
                            case 4:
                                if (Options.DisableAirshipCockpitAdmin.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                if (Options.DisableAirshipRecordsAdmin.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                if (Options.DisableAirshipCamera.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                if (Options.DisableAirshipVital.GetBool() || PcIsPoor)
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                break;
                            case 5:
                                if (Options.DisableFungleVital.GetBool())
                                {
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= UsableDistance();
                                }
                                break;
                        }
                    }
                    doComms &= !ignore;
                    if (doComms && !pc.inVent && GameStates.IsInTask)
                    {
                        if (!DesyncComms.Contains(pc.PlayerId))
                            DesyncComms.Add(pc.PlayerId);

                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 128);
                    }
                    else if (!Utils.IsActive(SystemTypes.Comms) && DesyncComms.Contains(pc.PlayerId))
                    {
                        DesyncComms.Remove(pc.PlayerId);
                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 16);

                        if (Main.NormalOptions.MapId is 1 or 5)
                            pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 17);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "DisableDevice");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (!(Options.DisableDevices.GetBool() || DisableDevice.IsPoorEnable())) return;
            UpdateDisableDevices();
        }

        public static void UpdateDisableDevices()
        {
            var player = PlayerControl.LocalPlayer;
            bool PcIsPoor = DisableDevice.IsInfoPoor(player);

            bool ignore = player.Is(CustomRoles.GM) ||
                !player.IsAlive() ||
                (Options.DisableDevicesIgnoreImpostors.GetBool() && player.Is(CustomRoleTypes.Impostor)) ||
                (Options.DisableDevicesIgnoreMadmates.GetBool() && player.Is(CustomRoleTypes.Madmate)) ||
                (Options.DisableDevicesIgnoreNeutrals.GetBool() && player.Is(CustomRoleTypes.Neutral)) ||
                (Options.DisableDevicesIgnoreAnimals.GetBool() && player.Is(CustomRoleTypes.Animals)) ||
                (Options.DisableDevicesIgnoreCrewmates.GetBool() && player.Is(CustomRoleTypes.Crewmate)) ||
                (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);
            var admins = GameObject.FindObjectsOfType<MapConsole>(true);
            var consoles = GameObject.FindObjectsOfType<SystemConsole>(true);
            if (admins == null || consoles == null) return;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    if (Options.DisableSkeldAdmin.GetBool() || PcIsPoor)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = ignore;
                    if (Options.DisableSkeldCamera.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = ignore);
                    break;
                case 1:
                    if (Options.DisableMiraHQAdmin.GetBool() || PcIsPoor)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = ignore;
                    if (Options.DisableMiraHQDoorLog.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    break;
                case 2:
                    if (Options.DisablePolusAdmin.GetBool() || PcIsPoor)
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if (Options.DisablePolusCamera.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if (Options.DisablePolusVital.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    break;
                case 4:
                    admins.Do(x =>
                    {
                        if (((Options.DisableAirshipCockpitAdmin.GetBool() || PcIsPoor) && x.name == "panel_cockpit_map") ||
                            ((Options.DisableAirshipRecordsAdmin.GetBool() || PcIsPoor) && x.name == "records_admin_map"))
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore;
                    });
                    if (Options.DisableAirshipCamera.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = ignore);
                    if (Options.DisableAirshipVital.GetBool() || PcIsPoor)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = ignore);
                    break;
                case 5:
                    if (Options.DisableFungleVital.GetBool())
                    {
                        consoles.DoIf(x => x.name == "VitalsConsole", x => x.GetComponent<Collider2D>().enabled = ignore);
                    }
                    break;
            }
        }
    }
}