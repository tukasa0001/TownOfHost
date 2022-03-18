namespace TownOfHost {
    static class CustomRolesHelper {
        public static bool isImpostor(this CustomRoles role) {
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
        public static bool isMadmate(this CustomRoles role) {
            return
                role == CustomRoles.Madmate ||
                role == CustomRoles.SKMadmate ||
                role == CustomRoles.MadGuardian ||
                role == CustomRoles.MadSnitch;
        }
        public static bool isImpostorTeam(this CustomRoles role) => role.isImpostor() || role.isMadmate();
        public static bool isNeutral(this CustomRoles role) {
            return
                role == CustomRoles.Jester ||
                role == CustomRoles.Opportunist ||
                role == CustomRoles.Terrorist ||
                role == CustomRoles.Troll ||
                role == CustomRoles.Fox;
        }
        public static bool isVanilla(this CustomRoles role) {
            return
                role == CustomRoles.Crewmate ||
                role == CustomRoles.Engineer ||
                role == CustomRoles.Scientist ||
                role == CustomRoles.GuardianAngel ||
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter;
        }
        public static bool CanUseKillButton(this PlayerControl pc) {
            bool canUse =
                pc.getCustomRole().isImpostor() ||
                pc.isSheriff();

            if(pc.isMafia()) {
                if(Utils.NumOfAliveImpostors() > 1) canUse = false;
            }
            return canUse;
        }
        public static bool isLastImpostor(this PlayerControl pc) { //キルクールを変更するインポスター役職は省く
            if(pc.getCustomRole().isImpostor() &&
                !pc.Data.IsDead &&
                Options.EnableLastImpostor &&
                !pc.isVampire() &&
                !pc.isBountyHunter() &&
                !pc.isSerialKiller() &&
                Utils.NumOfAliveImpostors() == 1)
                return true;
            return false;
        }
        public static IntroTypes getIntroType(this CustomRoles role) {
            IntroTypes type = IntroTypes.Crewmate;
            if(role.isImpostor()) type = IntroTypes.Impostor;
            if(role.isNeutral()) type = IntroTypes.Neutral;
            if(role.isMadmate()) type = IntroTypes.Madmate;
            return type;
        }
        public static void setCount(this CustomRoles role, int num) => Options.setRoleCount(role, num);
        public static int getCount(this CustomRoles role) => Options.getRoleCount(role);
        public static bool isEnable(this CustomRoles role) => Options.getRoleCount(role) > 0;
    }
    public enum IntroTypes {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}
