using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TownOfHost
{
    public static class Egoist
    {
        static readonly int Id = 50600;
        static List<byte> playerIdList = new();
        static Color RoleColor = Utils.GetRoleColor(CustomRoles.Egoist);
        static string RoleColorCode = Utils.GetRoleColorCode(CustomRoles.Egoist);

        static OptionItem OptionKillCooldown;
        static OptionItem OptionCanCreateMadmate;

        private static float KillCooldown;
        public static bool CanCreateMadmate;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Egoist);
            OptionKillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Egoist])
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanCreateMadmate = BooleanOptionItem.Create(Id + 11, "CanCreateMadmate", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Egoist]);
        }
        public static void Init()
        {
            IsEnable = false;
            playerIdList = new();
            KillCooldown = OptionKillCooldown.GetFloat();
            CanCreateMadmate = OptionCanCreateMadmate.GetBool();
        }
        public static void Add(byte ego)
        {
            IsEnable = true;
            playerIdList.Add(ego);
            TeamEgoist.Add(ego);
            foreach (var impostor in Main.AllPlayerControls.Where(pc => pc.Is(RoleType.Impostor)))
            {
                NameColorManager.Instance.RpcAdd(impostor.PlayerId, ego, RoleColorCode);
            }
        }
        public static bool IsEnable = false;
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown;
        public static void OverrideCustomWinner()
        {
            foreach (var id in playerIdList)
                if (TeamEgoist.CompleteWinCondition(id))
                    CustomWinnerHolder.WinnerTeam = CustomWinner.Egoist;
        }
    }
}