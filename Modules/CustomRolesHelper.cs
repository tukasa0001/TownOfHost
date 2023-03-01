using AmongUs.GameOptions;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

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
                CustomRoles.Sniper or
                CustomRoles.EvilTracker or
                CustomRoles.EvilHacker;
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
                CustomRoles.Jackal or
                CustomRoles.JSchrodingerCat or
                CustomRoles.HASTroll or
                CustomRoles.HASFox;
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

        public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
        {
            CustomRoleTypes type = CustomRoleTypes.Crewmate;
            if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsMadmate()) type = CustomRoleTypes.Madmate;
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
                CustomRoles.EvilTracker => EvilTracker.CanCreateMadmate,
                CustomRoles.Egoist => Egoist.CanCreateMadmate,
                _ => false,
            };
        public static RoleTypes GetRoleTypes(this CustomRoles role)
            => role switch
            {
                CustomRoles.Sheriff or
                CustomRoles.Arsonist or
                CustomRoles.Jackal => RoleTypes.Impostor,

                CustomRoles.Scientist or
                CustomRoles.Doctor => RoleTypes.Scientist,

                CustomRoles.Engineer or
                CustomRoles.Madmate or
                CustomRoles.Terrorist => RoleTypes.Engineer,

                CustomRoles.GuardianAngel or
                CustomRoles.GM => RoleTypes.GuardianAngel,

                CustomRoles.MadSnitch => Options.MadSnitchCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoles.Watcher => Options.IsEvilWatcher ? RoleTypes.Impostor : RoleTypes.Crewmate,
                CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,

                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.SerialKiller or
                CustomRoles.FireWorks or
                CustomRoles.Sniper or
                CustomRoles.ShapeMaster or
                CustomRoles.Warlock or
                CustomRoles.Egoist => RoleTypes.Shapeshifter,

                CustomRoles.EvilTracker => EvilTracker.RoleTypes,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };

        public static CountTypes GetCountTypes(this CustomRoles role)
            => role switch
            {
                CustomRoles.GM => CountTypes.OutOfGame,
                CustomRoles.Egoist => CountTypes.Impostor,
                CustomRoles.Jackal => CountTypes.Jackal,
                CustomRoles.HASFox or
                CustomRoles.HASTroll => CountTypes.None,
                _ => role.IsImpostor() ? CountTypes.Impostor : CountTypes.Crew,
            };
    }
    public enum CustomRoleTypes
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
    public enum CountTypes
    {
        OutOfGame,
        None,
        Crew,
        Impostor,
        Jackal,
    }
}