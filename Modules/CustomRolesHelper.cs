using AmongUs.GameOptions;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool IsNK(this CustomRoles role) // 是否带刀中立
        {
            return
                role is CustomRoles.Egoist or
                CustomRoles.Jackal or
                CustomRoles.OpportunistKiller;
        }

        public static bool IsNNK(this CustomRoles role) // 是否无刀中立
        {
            return
                role is CustomRoles.Arsonist or
                CustomRoles.Opportunist or
                CustomRoles.Mario or
                CustomRoles.God or
                CustomRoles.Jester or
                CustomRoles.Terrorist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Executioner;
        }


        public static bool IsNeutralKilling(this CustomRoles role) //是否邪恶中立（抢夺或单独胜利的中立）
        {
            return
                role is CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.Jackal or
                CustomRoles.God or
                CustomRoles.Mario;
        }
        public static bool IsCK(this CustomRoles role) //是否带刀船员
        {
            return
                role is CustomRoles.ChivalrousExpert or
                CustomRoles.Sheriff;
        }

        public static bool IsImpostor(this CustomRoles role)
        {
            return
                role is CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.Vampire or
                CustomRoles.Witch or
                CustomRoles.Zombie or
                CustomRoles.Warlock or
                CustomRoles.Assassin or
                CustomRoles.Hacker or
                CustomRoles.Miner or
                CustomRoles.Escapee or
                CustomRoles.SerialKiller or
                CustomRoles.Mare or
                CustomRoles.Puppeteer or
                CustomRoles.EvilWatcher or
                CustomRoles.TimeThief or
                CustomRoles.Mafia or
                CustomRoles.Minimalism or
                CustomRoles.FireWorks or
                CustomRoles.Sniper or
                CustomRoles.EvilTracker or
                CustomRoles.EvilGuesser or
                CustomRoles.AntiAdminer or
                CustomRoles.Sans or
                CustomRoles.Bomber or
                CustomRoles.BoobyTrap;
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
                CustomRoles.Mario or
                CustomRoles.SchrodingerCat or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Jackal or
                CustomRoles.JSchrodingerCat or
                CustomRoles.HASTroll or
                CustomRoles.HASFox or
                CustomRoles.OpportunistKiller or
                CustomRoles.God;
        }
        public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral();
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
        public static bool IsKilledSchrodingerCat(this CustomRoles role)
        {
            return role is
                CustomRoles.SchrodingerCat or
                CustomRoles.MSchrodingerCat or
                CustomRoles.CSchrodingerCat or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.JSchrodingerCat;
        }

        public static RoleType GetRoleType(this CustomRoles role)
        {
            RoleType type = RoleType.Crewmate;
            if (role.IsImpostor()) type = RoleType.Impostor;
            if (role.IsNeutral()) type = RoleType.Neutral;
            if (role.IsMadmate()) type = RoleType.Madmate;
            return type;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
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
                var roleOpt = Main.NormalOptions.RoleOptions;
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
        public static bool CanMakeMadmate(this CustomRoles role)
            => role switch
            {
                CustomRoles.Shapeshifter => true,
                CustomRoles.EvilTracker => EvilTracker.CanCreateMadmate.GetBool(),
                CustomRoles.Egoist => Egoist.CanCreateMadmate.GetBool(),
                _ => false,
            };
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}