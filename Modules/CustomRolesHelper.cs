namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool IsImpostor(this CustomRoles role)
        {
            return
                role is CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.Vampire or
                CustomRoles.Witch or
                CustomRoles.ShapeMaster or
                CustomRoles.Warlock or
                CustomRoles.SerialKiller or
                CustomRoles.Mare or
                CustomRoles.Puppeteer or
                CustomRoles.EvilWatcher or
                CustomRoles.TimeThief or
                CustomRoles.Mafia or
                CustomRoles.FireWorks or
                CustomRoles.Sniper;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            return
                role is CustomRoles.Madmate or
                CustomRoles.SKMadmate or
                CustomRoles.MadGuardian or
                CustomRoles.MadSnitch or
                CustomRoles.MSchrodingerCat;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            return
                role is CustomRoles.Jester or
                CustomRoles.Opportunist or
                CustomRoles.SchrodingerCat or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.HASTroll or
                CustomRoles.HASFox;
        }
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role is CustomRoles.Crewmate or
                CustomRoles.Engineer or
                CustomRoles.Scientist or
                CustomRoles.GuardianAngel or
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter;
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
        public static float GetChance(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                    _ => 0
                } / 100f;
            }
            else
            {
                return Options.GetRoleChance(role);
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