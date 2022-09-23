using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        private static List<byte> OldDesyncCommsPlayers = new();
        private static int Count = 0;
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
            var Map = (MapNames)PlayerControl.GameOptions.MapId;
            return Map switch
            {
                MapNames.Skeld => 1.5f,
                MapNames.Mira => 2.2f,
                MapNames.Polus => 1.5f,
                //MapNames.Dleks => 1.5f,
                MapNames.Airship => 1.5f,
                _ => 0.0f
            };
        }
        public static void FixedUpdate()
        {
            Count--;
            if (Count > 0) return;
            Count = 3;
            var DisableDevices =
                Options.DisableDevices.GetBool() ||
                Options.IsStandardHAS; //他に無効化するデバイスを設定する場合はここへ追加

            if (DisableDevices)
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    try
                    {
                        if (!pc.IsModClient())
                        {
                            var clientId = pc.GetClientId();
                            bool IsGuard = false;
                            Vector2 PlayerPos = pc.GetTruePosition();

                            if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                            {
                                switch (PlayerControl.GameOptions.MapId)
                                {
                                    case 0:
                                        if (Options.DisableSkeldAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                        if (Options.DisableSkeldCamera.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                        break;
                                    case 1:
                                        if (Options.DisableMiraHQAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                        if (Options.DisableMiraHQDoorLog.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                        break;
                                    case 2:
                                        if (Options.DisablePolusAdmin.GetBool())
                                        {
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                                        }
                                        if (Options.DisablePolusCamera.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                        if (Options.DisablePolusVital.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                        break;
                                    case 4:
                                        if (Options.DisableAirshipCockpitAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                        if (Options.DisableAirshipRecordsAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                        if (Options.DisableAirshipCamera.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                        if (Options.DisableAirshipVital.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                        break;
                                }
                            }
                            if (IsGuard && !pc.inVent)
                            {
                                if (!OldDesyncCommsPlayers.Contains(pc.PlayerId))
                                    OldDesyncCommsPlayers.Add(pc.PlayerId);

                                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                                SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                SabotageFixWriter.Write((byte)128);
                                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                            }
                            else if (!Utils.IsActive(SystemTypes.Comms) && OldDesyncCommsPlayers.Contains(pc.PlayerId))
                            {
                                OldDesyncCommsPlayers.Remove(pc.PlayerId);

                                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                                SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                SabotageFixWriter.Write((byte)16);
                                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

                                if (PlayerControl.GameOptions.MapId == 1)
                                {
                                    SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                                    SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                    SabotageFixWriter.Write((byte)17);
                                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString(), "DeviceBlock");
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (!Options.DisableDevices.GetBool()) return;
            var admins = GameObject.FindObjectsOfType<MapConsole>();
            var consoles = GameObject.FindObjectsOfType<SystemConsole>();
            if (admins == null || consoles == null) return;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                    if (Options.DisableSkeldAdmin.GetBool())
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false;
                    if (Options.DisableSkeldCamera.GetBool())
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = false);
                    break;
                case 1:
                    if (Options.DisableMiraHQAdmin.GetBool())
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = false;
                    if (Options.DisableMiraHQDoorLog.GetBool())
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    break;
                case 2:
                    if (Options.DisablePolusAdmin.GetBool())
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    if (Options.DisablePolusCamera.GetBool())
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    if (Options.DisablePolusVital.GetBool())
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    break;
                case 4:
                    admins.Do(x =>
                    {
                        if ((Options.DisableAirshipCockpitAdmin.GetBool() && x.name == "panel_cockpit_map") ||
                            (Options.DisableAirshipRecordsAdmin.GetBool() && x.name == "records_admin_map"))
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = false;
                    });
                    if (Options.DisableAirshipCamera.GetBool())
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    if (Options.DisableAirshipVital.GetBool())
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = false);
                    break;
            }
        }
    }
}