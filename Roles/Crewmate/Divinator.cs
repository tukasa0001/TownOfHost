using System.Collections.Generic;

namespace TOHE.Roles.Crewmate;

public static class Divinator
{
    private static readonly int Id = 8022560;
    private static List<byte> playerIdList = new();
    private static Dictionary<byte, int> CheckLimit = new();
    public static OptionItem CheckLimitOpt;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Divinator);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "DivinatorSkillLimit", new(1, 990, 1), 5, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Divinator])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        CheckLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CheckLimit.TryAdd(playerId, CheckLimitOpt.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void CheckPlayer(PlayerControl player, PlayerControl target)
    {
        if ((CheckLimit.TryGetValue(player.PlayerId, out var x) ? x : 0) < 1) return;
        CheckLimit[player.PlayerId]--;
        string text = player.GetCustomRole() switch
        {
            CustomRoles.TimeThief or
            CustomRoles.AntiAdminer or
            CustomRoles.SuperStar or
            CustomRoles.Mayor or
            CustomRoles.Snitch or
            CustomRoles.Counterfeiter or
            CustomRoles.God
            => "HideMsg",

            CustomRoles.Miner or
            CustomRoles.Scavenger or
            CustomRoles.Bait or
            CustomRoles.Luckey or
            CustomRoles.Needy or
            CustomRoles.SabotageMaster or
            CustomRoles.Jackal or
            CustomRoles.Mario
            => "Honest",

            CustomRoles.SerialKiller or
            CustomRoles.BountyHunter or
            CustomRoles.Minimalism or
            CustomRoles.Sans or
            CustomRoles.SpeedBooster or
            CustomRoles.Sheriff or
            CustomRoles.Arsonist or
            CustomRoles.Innocent
            => "Impulse",

            CustomRoles.Vampire or
            CustomRoles.Assassin or
            CustomRoles.Escapee or
            CustomRoles.Sniper or
            CustomRoles.SwordsMan or
            CustomRoles.Bodyguard or
            CustomRoles.Opportunist or
            CustomRoles.Pelican
            => "Weirdo",

            CustomRoles.EvilGuesser or
            CustomRoles.Bomber or
            CustomRoles.Capitalism or
            CustomRoles.NiceGuesser or
            CustomRoles.Trapper or
            CustomRoles.Grenadier or
            CustomRoles.Terrorist
            => "Blockbuster",

            CustomRoles.Warlock or
            CustomRoles.Hacker or
            CustomRoles.Mafia or
            CustomRoles.Doctor or
            CustomRoles.Transporter or
            CustomRoles.Veteran
            => "Strong",

            CustomRoles.Witch or
            CustomRoles.Puppeteer or
            CustomRoles.ShapeMaster or
            CustomRoles.Paranoia or
            CustomRoles.Psychic or
            CustomRoles.Executioner
            => "Incomprehensible",

            CustomRoles.FireWorks or
            CustomRoles.EvilTracker or
            CustomRoles.Gangster or
            CustomRoles.Dictator or
            CustomRoles.CyberStar
            => "Enthusiasm",

            CustomRoles.BoobyTrap or
            CustomRoles.Zombie or
            CustomRoles.Mare or
            CustomRoles.Detective or
            CustomRoles.TimeManager or
            CustomRoles.Jester
            => "Disturbed",

            _ => "None",
        };
        
        string msg = string.Format(Translator.GetString("DivinatorCheck." + text), target.GetRealName() + "\n");
        Utils.SendMessage(Translator.GetString("Message.DivinatorCheck") + msg + "\n\n" + string.Format(Translator.GetString("Message.DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, "DivinatorCheckMsgTitle");
    }
}