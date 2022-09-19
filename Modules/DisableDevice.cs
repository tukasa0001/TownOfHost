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
            ["PolusLeftAdmin"] = new(23.14f, -21.52f),
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
                            //アドミンチェック
                            if (pc.IsAlive())
                            {
                                switch (PlayerControl.GameOptions.MapId)
                                {
                                    case 0:
                                        if (Options.DisableSkeldAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                        break;
                                    case 1:
                                        if (Options.DisableMiraHQAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                        break;
                                    case 2:
                                        if (Options.DisablePolusAdmin.GetBool())
                                        {
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance();
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance();
                                        }
                                        break;
                                    case 4:
                                        if (Options.DisableAirshipCockpitAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                        if (Options.DisableAirshipRecordsAdmin.GetBool())
                                            IsGuard |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
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
            var map = GameObject.FindObjectsOfType<MapConsole>();
            if (map == null) return;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                case 1:
                    map[0].gameObject.GetComponent<CircleCollider2D>().enabled = false;
                    break;
                case 2:
                    map.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    break;
                case 4:
                    map.Do(x =>
                    {
                        if (Options.DisableAirshipCockpitAdmin.GetBool() && x.name == "panel_cockpit_map" ||
                            Options.DisableAirshipRecordsAdmin.GetBool() && x.name == "records_admin_map")
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = false;
                    });
                    break;
            }
        }
    }
}