using System;
using System.Collections.Generic;
using System.Text;
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
        private static float UsableDistance = 1.5f;
        public static void FixedUpdate()
        {
            var DisableDevices =
                Options.DisableAdmin.GetBool();

            if (DisableDevices || Options.StandardHAS.GetBool())
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    try
                    {
                        if (pc.IsAlive() && !pc.IsModClient())
                        {
                            var clientId = pc.GetClientId();
                            bool IsGuard = false;
                            Vector2 playerposition = pc.GetTruePosition();
                            //アドミンチェック
                            if (Options.DisableAdmin.GetBool() || Options.StandardHAS.GetBool())
                            {
                                var AdminDistance = Vector2.Distance(playerposition, GetAdminTransform());
                                if (AdminDistance <= UsableDistance)
                                {
                                    IsGuard = true;
                                }
                                //Polus用のアドミンチェック。Polusはアドミンが2つあるから
                                if (!IsGuard)
                                {
                                    if (PlayerControl.GameOptions.MapId == 2)
                                    {
                                        var SecondaryPolusAdminDistance = Vector2.Distance(playerposition, new Vector2(24.66107f, -21.523f));
                                        if (SecondaryPolusAdminDistance <= UsableDistance)
                                            IsGuard = true;
                                    }
                                    else if (PlayerControl.GameOptions.MapId == 4) //憎きアーカイブのアドミンチェック
                                    {
                                        var SecondaryPolusAdminDistance = Vector2.Distance(playerposition, new Vector2(20.0f, 12.3f));
                                        if (SecondaryPolusAdminDistance <= UsableDistance)
                                            IsGuard = true;
                                    }
                                }
                            }
                            if (IsGuard && !pc.inVent)
                            {
                                if (!OldDesyncCommsPlayers.Contains(pc.PlayerId))
                                    OldDesyncCommsPlayers.Add(pc.PlayerId);

                                MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.None, clientId);
                                SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                SabotageFixWriter.Write((byte)128);
                                AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                            }
                            else
                            {
                                if (!RepairSystemPatch.IsComms && OldDesyncCommsPlayers.Contains(pc.PlayerId))
                                {
                                    //Logger.Warn("Runned", "DisableAdmin");
                                    OldDesyncCommsPlayers.Remove(pc.PlayerId);
                                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.None, clientId);
                                    SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                    SabotageFixWriter.Write((byte)16);
                                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

                                    if (PlayerControl.GameOptions.MapId == 4)
                                    {
                                        //Logger.Warn("Runned", "DisableAdmin");
                                        SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.None, clientId);
                                        SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                        MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                        SabotageFixWriter.Write((byte)17);
                                        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                                    }
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
            if (PlayerControl.GameOptions.MapId == 0)
            {
                return new Vector2(3.48f, -8.624401f);
            }
            else if (PlayerControl.GameOptions.MapId == 1)
            {
                return new Vector2(21.024f, 19.095f);
            }
            else if (PlayerControl.GameOptions.MapId == 2)
            {
                return new Vector2(23.13707f, -21.523f);
            }
            else if (PlayerControl.GameOptions.MapId == 3)
            {
                return new Vector2(-3.48f, -8.624401f);
            }
            else if (PlayerControl.GameOptions.MapId == 4)
            {
                return new Vector2(-22.323f, 0.9099998f);
            }
            return new Vector2(1000, 1000);
        }
    }
}