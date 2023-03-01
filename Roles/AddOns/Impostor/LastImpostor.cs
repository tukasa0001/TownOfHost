using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Impostor
{
    public static class LastImpostor
    {
        private static readonly int Id = 80000;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, 1);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor])
                .SetValueFormat(OptionFormat.Seconds);
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
            && pc.Is(CustomRoleTypes.Impostor)
            && pc.GetCustomRole()
            is not CustomRoles.Vampire
                and not CustomRoles.BountyHunter
                and not CustomRoles.SerialKiller;
        public static void SetSubRole()
        {
            //ラストインポスターがすでにいれば処理不要
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1)
                return;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                    Add(pc.PlayerId);
                    SetKillCooldown();
                    pc.SyncSettings();
                    Utils.NotifyRoles();
                    break;
                }
            }
        }
    }
}