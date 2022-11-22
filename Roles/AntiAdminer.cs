using System;
using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class AntiAdminer
    {
        static readonly int Id = 3100;
        static List<byte> playerIdList = new();

        private static CustomOption CanCheckCamera;
        public static bool IsAdminWatch;
        public static bool IsVitalWatch;
        public static bool IsDoorLogWatch;
        public static bool IsCameraWatch;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.AntiAdminer);
            CanCheckCamera = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "CanCheckCamera", true, Options.CustomRoleSpawnChances[CustomRoles.AntiAdminer]);
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
            Count--;
            if (Count > 0) return;
            Count = 3;

            if (CustomRoles.AntiAdminer.GetCount() < 1) return;

            bool Admin = false, Camera = false, DoorLog = false, Vital = false;
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.IsAlive() || pc.inVent) continue;
                try
                {
                    Vector2 PlayerPos = pc.GetTruePosition();
                    switch (PlayerControl.GameOptions.MapId)
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
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    FixedUpdatePatch.Postfix(pc);
            }
        }
    }
}