using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class LastImpostor
    {
        private static readonly int Id = 80000;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, 1);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.Addons, Color.white, "KillCooldown", 15, 0, 180, 1, Options.CustomRoleSpawnChances[CustomRoles.LastImpostor], format: OptionFormat.Seconds);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown()
        {
            if (currentId == byte.MaxValue) return;
            Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
        }
        public static bool CanBeLastImpostor(PlayerControl pc)
            => pc.IsAlive()
            && !pc.Is(CustomRoles.LastImpostor)
            && pc.Is(RoleType.Impostor)
            && pc.GetCustomRole()
            is not CustomRoles.Vampire
                or CustomRoles.BountyHunter
                or CustomRoles.SerialKiller;
        public static void SetSubRole()
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1)
                return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                    Add(pc.PlayerId);
                    Utils.NotifyRoles();
                    Utils.CustomSyncAllSettings();
                    break;
                }
            }
        }
    }
}