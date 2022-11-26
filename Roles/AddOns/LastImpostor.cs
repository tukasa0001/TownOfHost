using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class LastImpostor
    {
        private static readonly int Id = 80000;
        public static List<byte> playerIdList = new();
        public static OptionItem KillCooldown;
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, 1);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.Addons, Color.white, "KillCooldown", 15, 0, 180, 1, Options.CustomRoleSpawnChances[CustomRoles.LastImpostor], format: OptionFormat.Seconds);
        }
        public static void Init() => playerIdList = new();
        public static void Add(byte id) => playerIdList.Add(id);
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static bool CanBeLastImpostor(byte playerId)
        { //キルクールを変更するインポスター役職は省く
            var state = Main.PlayerStates[playerId];
            return state.MainRole.IsImpostor()
                && !state.IsDead
                && state.MainRole
                is not CustomRoles.Vampire
                    or CustomRoles.BountyHunter
                    or CustomRoles.SerialKiller;
        }
        public static void SetSubRole()
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1)
                return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (CanBeLastImpostor(pc.PlayerId) && !pc.Is(CustomRoles.LastImpostor))
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