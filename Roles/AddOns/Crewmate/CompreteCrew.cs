using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Crewmate
{
    public static class CompreteCrew
    {
        private static readonly int Id = 72000;
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.CompreteCrew);
            Options.SetUpAddOnOptions(Id + 10, CustomRoles.CompreteCrew, TabGroup.Addons);
        }
        [GameModuleInitializer]
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

        public static bool CanBeCompreteCrew(PlayerControl pc)
           => pc.IsAlive()
           && !IsThisRole(pc.PlayerId)
           && pc.GetPlayerTaskState().IsTaskFinished
           && pc.Is(CustomRoleTypes.Crewmate);

        public static void OnCompleteTask(PlayerControl pc)
        {
            if (!CustomRoles.CompreteCrew.IsEnable() || playerIdList.Count >= CustomRoles.CompreteCrew.GetCount()) return;
            if (!CanBeCompreteCrew(pc)) return;

            pc.RpcSetCustomRole(CustomRoles.CompreteCrew);
            if (AmongUsClient.Instance.AmHost)
            {
                if (Options.AddOnBuffAssign[CustomRoles.CompreteCrew].GetBool() || Options.AddOnDebuffAssign[CustomRoles.CompreteCrew].GetBool())
                {
                    foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                    {
                        if (Options.AddOnRoleOptions.TryGetValue((CustomRoles.CompreteCrew, Addon), out var option) && option.GetBool())
                        {
                            pc.RpcSetCustomRole(Addon);
                            CustomRoleManager.subRoleAdd(pc.PlayerId, Addon);
                        }
                    }
                }
                Add(pc.PlayerId);
                pc.SyncSettings();
                Utils.NotifyRoles();
            }
        }

    }
}