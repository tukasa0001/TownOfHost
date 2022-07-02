using System;
using System.Collections.Generic;
using Hazel;
using InnerNet;
using UnityEngine;
using System.Linq;

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
                //MapNames.Mira => 2.2f,
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
                Options.StandardHAS.GetBool(); //他に無効化するデバイスを設定する場合はここへ追加

            if (DisableDevices)
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    try
                    {
                        if (pc.IsAlive() && !pc.IsModClient())
                        {
                            var clientId = pc.GetClientId();
                            bool IsGuard = false;
                            Vector2 PlayerPos = pc.GetTruePosition();
                            //アドミンチェック
                            if (AdminPatch.DisableAdmin)
                            {
                                if (AdminPatch.DisableAllAdmins || Options.StandardHAS.GetBool())
                                {
                                    var AdminDistance = Vector2.Distance(PlayerPos, GetAdminTransform());
                                    IsGuard = AdminDistance <= UsableDistance();

                                    if (!IsGuard && PlayerControl.GameOptions.MapId == 2) //Polus用のアドミンチェック。Polusはアドミンが2つあるから
                                    {
                                        var SecondaryPolusAdminDistance = Vector2.Distance(PlayerPos, AdminPatch.SecondaryPolusAdminPos);
                                        IsGuard = SecondaryPolusAdminDistance <= UsableDistance();
                                    }
                                }
                                if (!IsGuard && AdminPatch.DisableAllAdmins || AdminPatch.DisableArchiveAdmin || Options.StandardHAS.GetBool()) //憎きアーカイブのアドミンチェック
                                {
                                    var ArchiveAdminDistance = Vector2.Distance(PlayerPos, AdminPatch.ArchiveAdminPos);
                                    IsGuard = ArchiveAdminDistance <= UsableDistance();
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
                            else
                            {
                                if (!RepairSystemPatch.IsComms && OldDesyncCommsPlayers.Contains(pc.PlayerId))
                                {
                                    OldDesyncCommsPlayers.Remove(pc.PlayerId);

                                    /*var sender = CustomRpcSender.Create("DisableDevice", SendOption.Reliable);

                                    sender.AutoStartRpc(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, clientId)
                                            .Write((byte)SystemTypes.Comms)
                                            .WriteNetObject(pc)
                                            .Write((byte)16)
                                            .EndRpc();
                                    if (PlayerControl.GameOptions.MapId == 2)
                                        sender.AutoStartRpc(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, clientId)
                                                .Write((byte)SystemTypes.Comms)
                                                .WriteNetObject(pc)
                                                .Write((byte)17)
                                                .EndRpc();

                                    sender.SendMessage();*/

                                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, clientId);
                                    SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                    SabotageFixWriter.Write((byte)16);
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
        public static Vector2 GetAdminTransform()
        {
            var MapName = (MapNames)PlayerControl.GameOptions.MapId;
            return MapName switch
            {
                MapNames.Skeld => new Vector2(3.48f, -8.624401f),
                //MapNames.Mira => new Vector2(20.524f, 20.595f),
                MapNames.Polus => new Vector2(22.13707f, -21.523f),
                //MapNames.Dleks => new Vector2(-3.48f, -8.624401f),
                MapNames.Airship => new Vector2(-22.323f, 0.9099998f),
                _ => new Vector2(1000, 1000)
            };
        }
    }
}