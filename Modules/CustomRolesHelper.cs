namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool isImpostor(this CustomRoles role)
        {
            return
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter ||
                role == CustomRoles.BountyHunter ||
                role == CustomRoles.Vampire ||
                role == CustomRoles.Witch ||
                role == CustomRoles.ShapeMaster ||
                role == CustomRoles.Warlock ||
                role == CustomRoles.SerialKiller ||
                role == CustomRoles.Mafia;
        }
        public static bool isMadmate(this CustomRoles role)
        {
            return
                role == CustomRoles.Madmate ||
                role == CustomRoles.SKMadmate ||
                role == CustomRoles.MadGuardian ||
                role == CustomRoles.MadSnitch ||
                role == CustomRoles.MSchrodingerCat;
        }
        public static bool isImpostorTeam(this CustomRoles role) => role.isImpostor() || role.isMadmate();
        public static bool isNeutral(this CustomRoles role)
        {
            return
                role == CustomRoles.Jester ||
                role == CustomRoles.Opportunist ||
                role == CustomRoles.SchrodingerCat ||
                role == CustomRoles.Terrorist ||
                role == CustomRoles.Arsonist ||
                role == CustomRoles.Egoist ||
                role == CustomRoles.EgoSchrodingerCat ||
                role == CustomRoles.Troll ||
                role == CustomRoles.Fox;
        }
        public static bool isVanilla(this CustomRoles role)
        {
            return
                role == CustomRoles.Crewmate ||
                role == CustomRoles.Engineer ||
                role == CustomRoles.Scientist ||
                role == CustomRoles.GuardianAngel ||
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter;
        }
        public static bool CanUseKillButton(this CustomRoles role)
        {
            bool canUse =
                role.isImpostor() ||
                role == CustomRoles.Sheriff ||
                role == CustomRoles.Arsonist;

            if (role == CustomRoles.Mafia)
            {
                int AliveImpostorCount = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    CustomRoles pc_role = pc.getCustomRole();
                    if (pc_role.isImpostor() && !pc.Data.IsDead && pc_role != CustomRoles.Mafia) AliveImpostorCount++;
                }
                if (AliveImpostorCount > 0) canUse = false;
            }
            return canUse;
        }
        public static RoleType getRoleType(this CustomRoles role)
        {
            RoleType type = RoleType.Crewmate;
            if (role.isImpostor()) type = RoleType.Impostor;
            if (role.isNeutral()) type = RoleType.Neutral;
            if (role.isMadmate()) type = RoleType.Madmate;
            return type;
        }
        public static void setCount(this CustomRoles role, int num) => Options.setRoleCount(role, num);
        public static int getCount(this CustomRoles role) => Options.getRoleCount(role);
        public static bool isEnable(this CustomRoles role) => Options.getRoleCount(role) > 0;
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}
