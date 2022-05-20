namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool IsImpostor(this CustomRoles role)
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
                role == CustomRoles.Mare ||
                role == CustomRoles.Puppeteer ||
                role == CustomRoles.EvilWatcher ||
                role == CustomRoles.TimeThief ||
                role == CustomRoles.Mafia ||
                role == CustomRoles.FireWorks ||
                role == CustomRoles.Sniper ||
                role == CustomRoles.SlaveDriver;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            return
                role == CustomRoles.Madmate ||
                role == CustomRoles.SKMadmate ||
                role == CustomRoles.MadGuardian ||
                role == CustomRoles.MadSnitch ||
                role == CustomRoles.MSchrodingerCat;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            return
                role == CustomRoles.Jester ||
                role == CustomRoles.Opportunist ||
                role == CustomRoles.SchrodingerCat ||
                role == CustomRoles.Terrorist ||
                role == CustomRoles.Executioner ||
                role == CustomRoles.Arsonist ||
                role == CustomRoles.Egoist ||
                role == CustomRoles.EgoSchrodingerCat ||
                role == CustomRoles.HASTroll ||
                role == CustomRoles.HASFox;
        }
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role == CustomRoles.Crewmate ||
                role == CustomRoles.Engineer ||
                role == CustomRoles.Scientist ||
                role == CustomRoles.GuardianAngel ||
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter;
        }

        public static RoleType GetRoleType(this CustomRoles role)
        {
            RoleType type = RoleType.Crewmate;
            if (role.IsImpostor()) type = RoleType.Impostor;
            if (role.IsNeutral()) type = RoleType.Neutral;
            if (role.IsMadmate()) type = RoleType.Madmate;
            return type;
        }
        public static void SetCount(this CustomRoles role, int num) => Options.SetRoleCount(role, num);
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.GetRoleCount(role);
            }
        }
        public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}