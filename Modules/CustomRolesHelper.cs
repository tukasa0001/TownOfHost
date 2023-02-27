using AmongUs.GameOptions;

namespace TOHE
{
    static class CustomRolesHelper
    {
        public static CustomRoles GetVNRole(this CustomRoles role) // 对应原版职业
        {
            if (role.IsVanilla()) return role;
            return role switch
            {
                CustomRoles.Sniper => CustomRoles.Shapeshifter,
                CustomRoles.Jester => CustomRoles.Crewmate,
                CustomRoles.Bait => CustomRoles.Crewmate,
                CustomRoles.Mayor => Options.MayorHasPortableButton.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate,
                CustomRoles.Opportunist => CustomRoles.Crewmate,
                CustomRoles.Snitch => CustomRoles.Crewmate,
                CustomRoles.SabotageMaster => CustomRoles.Engineer,
                CustomRoles.Mafia => CustomRoles.Impostor,
                CustomRoles.Terrorist => CustomRoles.Engineer,
                CustomRoles.Executioner => CustomRoles.Crewmate,
                CustomRoles.Vampire => CustomRoles.Impostor,
                CustomRoles.BountyHunter => CustomRoles.Shapeshifter,
                CustomRoles.Witch => CustomRoles.Impostor,
                CustomRoles.ShapeMaster => CustomRoles.Shapeshifter,
                CustomRoles.Warlock => CustomRoles.Shapeshifter,
                CustomRoles.SerialKiller => CustomRoles.Shapeshifter,
                CustomRoles.FireWorks => CustomRoles.Shapeshifter,
                CustomRoles.SpeedBooster => CustomRoles.Crewmate,
                CustomRoles.Trapper => CustomRoles.Crewmate,
                CustomRoles.Dictator => CustomRoles.Crewmate,
                CustomRoles.Mare => CustomRoles.Impostor,
                CustomRoles.Doctor => CustomRoles.Scientist,
                CustomRoles.Puppeteer => CustomRoles.Impostor,
                CustomRoles.TimeThief => CustomRoles.Impostor,
                CustomRoles.EvilTracker => CustomRoles.Shapeshifter,
                CustomRoles.Paranoia => CustomRoles.Engineer,
                CustomRoles.Miner => CustomRoles.Shapeshifter,
                CustomRoles.Psychic => CustomRoles.Crewmate,
                CustomRoles.Needy => CustomRoles.Crewmate,
                CustomRoles.SuperStar => CustomRoles.Crewmate,
                CustomRoles.Hacker => CustomRoles.Impostor,
                CustomRoles.Assassin => CustomRoles.Shapeshifter,
                CustomRoles.Luckey => CustomRoles.Crewmate,
                CustomRoles.CyberStar => CustomRoles.Crewmate,
                CustomRoles.Escapee => CustomRoles.Shapeshifter,
                CustomRoles.NiceGuesser => CustomRoles.Crewmate,
                CustomRoles.EvilGuesser => CustomRoles.Impostor,
                CustomRoles.Detective => CustomRoles.Crewmate,
                CustomRoles.Minimalism => CustomRoles.Impostor,
                CustomRoles.God => CustomRoles.Crewmate,
                CustomRoles.Zombie => CustomRoles.Impostor,
                CustomRoles.Mario => CustomRoles.Engineer,
                CustomRoles.AntiAdminer => CustomRoles.Impostor,
                CustomRoles.Sans => CustomRoles.Impostor,
                CustomRoles.Bomber => CustomRoles.Shapeshifter,
                CustomRoles.BoobyTrap => CustomRoles.Impostor,
                CustomRoles.Scavenger => CustomRoles.Impostor,
                CustomRoles.Transporter => CustomRoles.Crewmate,
                CustomRoles.Veteran => CustomRoles.Engineer,
                CustomRoles.Capitalism => CustomRoles.Impostor,
                CustomRoles.Bodyguard => CustomRoles.Crewmate,
                CustomRoles.Grenadier => CustomRoles.Engineer,
                _ => role.IsImpostor() ? CustomRoles.Impostor : CustomRoles.Crewmate,
            };
        }
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        => GetVNRole(role) switch
        {
            CustomRoles.Impostor => RoleTypes.Impostor,
            CustomRoles.Scientist => RoleTypes.Scientist,
            CustomRoles.Engineer => RoleTypes.Engineer,
            CustomRoles.GuardianAngel => RoleTypes.GuardianAngel,
            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,
            CustomRoles.Crewmate => RoleTypes.Crewmate,
            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };

        public static bool IsDesyncRole(this CustomRoles role) => role.GetDYRole() != RoleTypes.Scientist;
        public static RoleTypes GetDYRole(this CustomRoles role) // 对应原版职业（反职业）
        {
            return role switch
            {
                CustomRoles.Sheriff => RoleTypes.Impostor,
                CustomRoles.Arsonist => RoleTypes.Impostor,
                CustomRoles.Jackal => RoleTypes.Impostor,
                CustomRoles.ChivalrousExpert => RoleTypes.Impostor,
                CustomRoles.Innocent => RoleTypes.Impostor,
                CustomRoles.Pelican => RoleTypes.Impostor,
                CustomRoles.Counterfeiter => RoleTypes.Impostor,
                _ => RoleTypes.Scientist
            };
        }
        public static bool IsAdditionRole(this CustomRoles role)
        {
            return role is
                CustomRoles.Lovers or
                CustomRoles.LastImpostor or
                CustomRoles.Ntr or
                CustomRoles.Madmate or
                CustomRoles.Watcher or
                CustomRoles.Flashman or
                CustomRoles.Lighter or
                CustomRoles.Seer or
                CustomRoles.Brakar or
                CustomRoles.Oblivious or
                CustomRoles.Bewilder or
                CustomRoles.Workhorse or
                CustomRoles.Fool or
                CustomRoles.Avanger or
                CustomRoles.Youtuber or
                CustomRoles.Egoist or
                CustomRoles.Piper;
        }
        public static bool IsNK(this CustomRoles role) // 是否带刀中立
        {
            return role is
                CustomRoles.Jackal or
                CustomRoles.Pelican;
        }
        public static bool IsNNK(this CustomRoles role) => role.IsNeutral() && !role.IsNK(); // 是否无刀中立
        public static bool IsNeutralKilling(this CustomRoles role) //是否邪恶中立（抢夺或单独胜利的中立）
        {
            return role is
                CustomRoles.Terrorist or
                CustomRoles.Arsonist or
                CustomRoles.Jackal or
                CustomRoles.God or
                CustomRoles.Mario or
                CustomRoles.Innocent or
                CustomRoles.Pelican or
                CustomRoles.Egoist;
        }
        public static bool IsCK(this CustomRoles role) // 是否带刀船员
        {
            return role is
                CustomRoles.ChivalrousExpert or
                CustomRoles.Sheriff;
        }
        public static bool IsImpostor(this CustomRoles role) // 是否内鬼
        {
            return role is
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.BountyHunter or
                CustomRoles.Vampire or
                CustomRoles.Witch or
                CustomRoles.ShapeMaster or
                CustomRoles.Zombie or
                CustomRoles.Warlock or
                CustomRoles.Assassin or
                CustomRoles.Hacker or
                CustomRoles.Miner or
                CustomRoles.Escapee or
                CustomRoles.SerialKiller or
                CustomRoles.Mare or
                CustomRoles.Puppeteer or
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
                CustomRoles.Scavenger or
                CustomRoles.BoobyTrap or
                CustomRoles.Capitalism;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role == CustomRoles.Madmate;
        public static bool IsNeutral(this CustomRoles role) // 是否中立
        {
            return role is
                CustomRoles.Jester or
                CustomRoles.Opportunist or
                CustomRoles.Mario or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Arsonist or
                CustomRoles.Jackal or
                CustomRoles.God or
                CustomRoles.Innocent or
                CustomRoles.Pelican;
        }
        public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral();
        public static bool IsVanilla(this CustomRoles role) // 是否原版职业
        {
            return role is
                CustomRoles.Crewmate or
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
            return type;
        }
        public static bool RoleExist(this CustomRoles role, bool countDead = false)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(role) && (pc.IsAlive() || countDead)) return true;
            }
            return false;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                if (Options.DisableVanillaRoles.GetBool()) return 0;
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
        public static int GetMode(this CustomRoles role) => Options.GetRoleSpawnMode(role);
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

        public static bool HasSubRole(this PlayerControl pc) => Main.PlayerStates[pc.PlayerId].SubRoles.Count > 0;
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral
    }
}