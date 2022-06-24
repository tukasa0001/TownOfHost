using System;
using System.Collections.Generic;
using Hazel;
using InnerNet;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        private static List<byte> OldDesyncCommsPlayers = new();
        public static float UsableDistance(int CurrentMapId)
        {
            //読み替え引数(多い...)
            var Skeld = CurrentMapId == 0;
            var Mira = CurrentMapId == 1;
            var Polus = CurrentMapId == 2;
            //var dlekS = CurrentMapId == 3;
            var Airship = CurrentMapId == 4;
            if (Skeld)
                return 1.5f;
            else if (Mira)
                return 1.5f;
            else if (Polus)
                return 1.5f;
            //else if (dlekS)
            //    return 1.5f;
            else if (Airship)
                return 1.5f;


            return 0f;
        }
        public static void FixedUpdate()
        {
            var DisableDevices =
                Options.DisableAdmin.GetBool();
            var DisableArchiveAdmin = Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[1]);
            var DisableAllAdmins = Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[0]) ||
                (PlayerControl.GameOptions.MapId == 4 && DisableArchiveAdmin); //エアシップ以外でアーカイブのみ制限になっていたら制限しない

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
                                if (DisableAllAdmins && AdminDistance <= UsableDistance(PlayerControl.GameOptions.MapId))
                                {
                                    IsGuard = true;
                                }
                                //Polus用のアドミンチェック。Polusはアドミンが2つあるから
                                if (!IsGuard)
                                {
                                    if (PlayerControl.GameOptions.MapId == 2)
                                    {
                                        var SecondaryPolusAdminDistance = Vector2.Distance(playerposition, new Vector2(24.66107f, -21.523f));
                                        if (SecondaryPolusAdminDistance <= UsableDistance(PlayerControl.GameOptions.MapId))
                                            IsGuard = true;
                                    }
                                    else if ((DisableAllAdmins || DisableArchiveAdmin) && PlayerControl.GameOptions.MapId == 4) //憎きアーカイブのアドミンチェック
                                    {
                                        var ArchiveAdminDistance = Vector2.Distance(playerposition, new Vector2(20.0f, 12.3f));
                                        if (ArchiveAdminDistance <= UsableDistance(PlayerControl.GameOptions.MapId))
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
                                    OldDesyncCommsPlayers.Remove(pc.PlayerId);
                                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.None, clientId);
                                    SabotageFixWriter.Write((byte)SystemTypes.Comms);
                                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                                    SabotageFixWriter.Write((byte)16);
                                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

                                    if (PlayerControl.GameOptions.MapId == 4)
                                    {
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
                return new Vector2(25.13707f, -21.523f);
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