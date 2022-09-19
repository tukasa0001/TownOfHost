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
                AdminPatch.DisableAdmin ||
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
                            if ((AdminPatch.DisableAdmin || Options.IsStandardHAS) && pc.IsAlive())
                            {
                                float distance;
                                switch (PlayerControl.GameOptions.MapId)
                                {
                                    case 0:
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["SkeldAdmin"]);
                                        IsGuard = distance <= UsableDistance();
                                        break;
                                    case 1:
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["MiraHQAdmin"]);
                                        IsGuard = distance <= UsableDistance();
                                        break;
                                    case 2:
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["PolusLeftAdmin"]);
                                        IsGuard = distance <= UsableDistance();
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["PolusRightAdmin"]);
                                        IsGuard |= distance <= UsableDistance();
                                        break;
                                    case 4:
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["AirshipCockpitAdmin"]);
                                        IsGuard = distance <= UsableDistance();
                                        distance = Vector2.Distance(PlayerPos, AdminPatch.AdminPos["AirshipRecordsAdmin"]);
                                        IsGuard |= distance <= UsableDistance();
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
    public class RemoveAdminPatch
    {
        public static void Postfix()
        {
            if (!AdminPatch.DisableAdmin) return;
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