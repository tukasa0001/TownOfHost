using System.Linq;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE
{
    static class CustomRolesHelper
    {
        /// <summary>すべての役職(属性は含まない)</summary>
        public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).ToArray();
        /// <summary>すべての属性</summary>
        public static readonly CustomRoles[] AllAddOns = EnumHelper.GetAllValues<CustomRoles>().Where(role => role > CustomRoles.NotAssigned).ToArray();
        /// <summary>スタンダードモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllStandardRoles = AllRoles.Where(role => role is not (CustomRoles.HASFox or CustomRoles.HASTroll)).ToArray();
        /// <summary>HASモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllHASRoles = { CustomRoles.HASFox, CustomRoles.HASTroll };
        /// <summary>大惨事爆裂大戦モードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllBAKURETSURoles = { CustomRoles.BAKURETSUKI};
        public static readonly CustomRoleTypes[] AllRoleTypes = EnumHelper.GetAllValues<CustomRoleTypes>();

        public static bool IsImpostor(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;

            return false;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Madmate;
            return
                role is
                CustomRoles.SKMadmate or
                CustomRoles.MOjouSama or
                CustomRoles.IUsagi;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Neutral;
            return
                role is
                CustomRoles.JOjouSama or
                CustomRoles.EOjouSama or
                CustomRoles.DOjouSama or
                CustomRoles.OOjouSama or
                CustomRoles.GOjouSama or
                CustomRoles.BAKURETSUKI or
                CustomRoles.HASFox;
        }
        public static bool IsAnimals(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Animals;
            return
                role is
                //CustomRoles.ASchrodingerCat or
                CustomRoles.AOjouSama;
        }
        public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (!role.IsImpostorTeam() && !role.IsNeutral() && !role.IsAnimals() && !role.IsAddOn() && !role.IsLovers());

        public static bool IsLovers(this CustomRoles role)
        {
            return role is CustomRoles.Lovers or
                CustomRoles.PlatonicLover or
                CustomRoles.OtakuPrincess;
        }
        public static bool IsWhiteCrew(this CustomRoles role)
        {
            return
                role is CustomRoles.Rainbow or
                CustomRoles.Express or
                CustomRoles.Metaton or
                CustomRoles.OjouSama;
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
        //public static bool IsReplaceImposter(this CustomRoles role)
        //{
        //    return
        //        role is CustomRoles.Sheriff or CustomRoles.Arsonist or CustomRoles.PlagueDoctor 
        //            or CustomRoles.Hunter or CustomRoles.SillySheriff or CustomRoles.MadSheriff
        //            or CustomRoles.DarkHide or CustomRoles.PlatonicLover or CustomRoles.OtakuPrincess or CustomRoles.Totocalcio
        //            or CustomRoles.Jackal or CustomRoles.Gizoku or CustomRoles.Oniichan
        //            or CustomRoles.Coyote or CustomRoles.Braki or CustomRoles.Leopard or CustomRoles.RedPanda;
        //}

        public static bool IsAddAddOn(this CustomRoles role)
        {
            return role.IsMadmate() || 
                role is CustomRoles.Jackal or CustomRoles.JClient or CustomRoles.RedPanda or CustomRoles.Dolphin;
        }
        public static bool IsAddOn(this CustomRoles role) => role.IsBuffAddOn() || role.IsDebuffAddOn();
        public static bool IsBuffAddOn(this CustomRoles role)
        {
            return
                role is CustomRoles.AddWatch or
                CustomRoles.AddLight or
                CustomRoles.AddSeer or
                CustomRoles.Autopsy or
                CustomRoles.VIP or
                CustomRoles.Revenger or
                CustomRoles.Management or
                CustomRoles.Sending or
                CustomRoles.TieBreaker or
                CustomRoles.Loyalty or
                CustomRoles.PlusVote or
                CustomRoles.Guarding or
                CustomRoles.AddBait or
                CustomRoles.Chu2Byo or
                CustomRoles.Gambler or
                CustomRoles.Refusing;
        }
        public static bool IsDebuffAddOn(this CustomRoles role)
        {
            return
                role is
                CustomRoles.Sunglasses or
                CustomRoles.Clumsy or
                CustomRoles.InfoPoor or
                CustomRoles.NonReport;
        }
        public static bool IsKilledOhouSama(this CustomRoles role)
        {
            return role is
                CustomRoles.MOjouSama or
                CustomRoles.EOjouSama or
                CustomRoles.DOjouSama or
                CustomRoles.JOjouSama or
                CustomRoles.AOjouSama or
                CustomRoles.OOjouSama;
        }
        public static bool IsDirectKillRole(this CustomRoles role)
        {
            return role is
                CustomRoles.Arsonist or
                CustomRoles.PlatonicLover or
                CustomRoles.OtakuPrincess or
                CustomRoles.Totocalcio or
                CustomRoles.MadSheriff;
        }
        public static bool IsNotAssignRoles(this CustomRoles role)
        {
            if (role.IsKilledOhouSama() ||
                role.IsVanilla()) return true;

            return role is
                CustomRoles.Hachiware or
                CustomRoles.Usagi or
                CustomRoles.IUsagi or
                CustomRoles.Tiikawa or
                CustomRoles.SKMadmate or
                CustomRoles.HASFox or
                CustomRoles.HASTroll or
                CustomRoles.BAKURETSUKI or
                CustomRoles.NotAssigned or
                CustomRoles.GM;
        }

        public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
        {
            CustomRoleTypes type = CustomRoleTypes.Crewmate;

            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType;

            if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsAnimals()) type = CustomRoleTypes.Animals;
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
        {
            if (role.GetRoleInfo() is SimpleRoleInfo info)
            {
                return info.CanMakeMadmate;
            }

            return false;
        }
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.BaseRoleType.Invoke();
            return role switch
            {
                CustomRoles.GM => RoleTypes.GuardianAngel,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };
        }
        public static bool PetActivatedAbility(this CustomRoles role)
        {
            return
                role is CustomRoles.Badger or
                CustomRoles.Gizoku or
                CustomRoles.Nyaoha or
                CustomRoles.DogSheriff or
                CustomRoles.Dolphin or
                CustomRoles.SpiderMad;
        }
    }
    public enum CountTypes
    {
        OutOfGame,
        None,
        Crew,
        Impostor,
        Jackal,
        Animals,
        SB,
    }
}