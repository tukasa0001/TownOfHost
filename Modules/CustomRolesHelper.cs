using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool IsImpostor(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;
            return
                role is CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.EvilWatcher or
                CustomRoles.EvilTracker;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Madmate;
            return
                role is
                CustomRoles.SKMadmate or
                CustomRoles.MSchrodingerCat;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Neutral;
            return
                role is CustomRoles.Jester or
                CustomRoles.SchrodingerCat or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Egoist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Jackal or
                CustomRoles.JSchrodingerCat or
                CustomRoles.HASTroll or
                CustomRoles.HASFox;
        }
        public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (!role.IsImpostorTeam() && !role.IsNeutral());
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

            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType;

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
        public static int GetChance(this CustomRoles role)
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
                };
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
                CustomRoles.EvilTracker => EvilTracker.CanCreateMadmate,
                CustomRoles.Egoist => Egoist.CanCreateMadmate,
                _ => false,
            };
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.BaseRoleType.Invoke();
            return role switch
            {
                CustomRoles.Jackal => RoleTypes.Impostor,

                CustomRoles.Scientist => RoleTypes.Scientist,

                CustomRoles.Engineer or
                CustomRoles.Terrorist => RoleTypes.Engineer,

                CustomRoles.GuardianAngel or
                CustomRoles.GM => RoleTypes.GuardianAngel,

                CustomRoles.Shapeshifter or
                CustomRoles.FireWorks or
                CustomRoles.Sniper or
                CustomRoles.Egoist => RoleTypes.Shapeshifter,

                CustomRoles.EvilTracker => EvilTracker.RoleTypes,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };
        }
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