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
                //CustomRoles.ShapeMaster or
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
                CustomRoles.LastImpostor;
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
                CustomRoles.Thief or
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
        public static RoleTypes GetVanillaRole(this CustomRoles role)
        {
            // 第三陣営を除き，Shapeshifter, Engineer, Scientistの置き換え役職のみ書けばOK
            if (role.IsImpostor())
            {
                return role switch
                {
                    // Shapeshifter置き換えのインポスター役職たち
                    CustomRoles.Shapeshifter |
                    CustomRoles.BountyHunter |
                    CustomRoles.FireWorks |
                    CustomRoles.SerialKiller |
                    CustomRoles.Sniper |
                    CustomRoles.Warlock |
                    CustomRoles.EvilTracker
                        => RoleTypes.Shapeshifter,

                    _ => RoleTypes.Impostor
                };
            }
            if (role.IsNeutral())
            {
                return role switch
                {
                    // Shapeshifter/DesyncShapeshifter置き換えの第三陣営役職たち
                    CustomRoles.Thief |
                    CustomRoles.Egoist
                        => RoleTypes.Shapeshifter,

                    // Impostor/DesyncImpostor置き換えの第三陣営役職たち
                    CustomRoles.Arsonist |
                    CustomRoles.Jackal
                        => RoleTypes.Impostor,

                    _ => RoleTypes.Crewmate
                };
            }
            if (role.IsCrewmate())
            {
                return role switch
                {
                    // Engineer置き換えのクルー役職たち
                    CustomRoles.Engineer
                        => RoleTypes.Engineer,

                    // Scientist置き換えのクルー役職たち
                    CustomRoles.Scientist |
                    CustomRoles.Doctor
                        => RoleTypes.Scientist,

                    // 設定によって置き換え先が変わるクルー役職たち
                    CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,

                    // 守護天使
                    CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,

                    _ => RoleTypes.Crewmate
                };
            }
            if (role.IsMadmate())
            {
                return role switch
                {
                    // マッドメイトたち
                    CustomRoles.Madmate => RoleTypes.Engineer,
                    CustomRoles.MadSnitch => Options.MadSnitchCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,

                    _ => RoleTypes.Crewmate
                };
            }
            Logger.Warn($"{role}にGetVanillaRoleを正常に実行できませんでした", "CustomRolesHelper");
            return RoleTypes.Crewmate;
        }
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}