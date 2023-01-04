using System;
using System.Collections.Generic;
using System.Linq;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static Dictionary<byte, CustomRole> PlayersCustomRolesRedux = new();
    public static Dictionary<byte, CustomRole> LastRoundCustomRoles = new();

    public static Dictionary<byte, List<Subrole>> PlayerSubroles = new();

    public static List<byte> RoleBlockedPlayers = new();
    public static StaticRoles Static = new();
    public static CustomRole Default = Static.Crewmate;

    public static readonly List<CustomRole> Roles = Static.GetType()
        .GetFields()
        .Select(f => (CustomRole)f.GetValue(Static))
        .ToList();


    public static void AddRole(CustomRole staticRole)
    {
        Roles.Add(staticRole);
    }
    public static int GetRoleId(CustomRole role) => role == null ? 0 : GetRoleId(role.GetType());
    public static CustomRole GetRoleFromType(Type roleType) => GetRoleFromId(GetRoleId(roleType));

    public static int GetRoleId(Type roleType)
    {
        for (int i = 0; i < Roles.Count; i++)
            if (roleType == Roles[i].GetType())
                return i;
        return -1;
    }

    public static CustomRole GetRoleFromId(int id)
    {
        if (id == -1) id = 0;
        return Roles[id] ?? Default;
    }

    public static void AddPlayerSubrole(byte playerId, Subrole subrole)
    {
        if (!PlayerSubroles.ContainsKey(playerId)) PlayerSubroles[playerId] = new List<Subrole>();
        PlayerSubroles[playerId].Add(subrole);
    }

    public class StaticRoles
    {
        public GM GM = new GM();
        public Impostor Impostor = new Impostor();
        public Morphling Morphling = new Morphling();
        public Madmate Madmate = new Madmate();
        public Miner Miner = new Miner();
        public Mafia Mafia = new Mafia();
        public Sniper Sniper = new Sniper();
        public BountyHunter BountyHunter = new BountyHunter();
        public Janitor Janitor = new Janitor();
        public Disperser Disperser = new Disperser();
        public Consort Consort = new Consort();
        public FireWorks FireWorks = new FireWorks();
        public Puppeteer Puppeteer = new Puppeteer();
        public Vampire Vampire = new Vampire();
        public SerialKiller SerialKiller = new SerialKiller();
        public Mare Mare = new Mare();
        public Camouflager Camouflager = new Camouflager();
        public Grenadier Grenadier = new Grenadier();
        public TimeThief TimeThief = new TimeThief();
        public Warlock Warlock = new Warlock();
        public Witch Witch = new Witch();
        public Coven Coven = new Coven();
        public BloodKnight BloodKnight = new BloodKnight();

        public LastImpostor LastImpostor = new LastImpostor();
        public Ninja Ninja = new Ninja();

        public Crewmate Crewmate = new Crewmate();
        public Engineer Engineer = new Engineer();
        public Scientist Scientist = new Scientist();

        public Baiter Baiter = new Baiter();
        public Observer Observer = new Observer();
        public Sheriff Sheriff = new Sheriff();
        public Transporter Transporter = new Transporter();
        public Veteran Veteran = new Veteran();
        public Investigator Investigator = new Investigator();
        public Mystic Mystic = new Mystic();
        public Jester Jester = new Jester();
        public CrewPostor CrewPostor = new CrewPostor();
        public Opportunist Opportunist = new Opportunist();


        public Glitch Glitch = new Glitch();
        public Jackal Jackal = new Jackal();
        public Arsonist Arsonist = new Arsonist();
        public Terrorist Terrorist = new Terrorist();
        public Executioner Executioner = new Executioner();



        public GuardianAngel GuardianAngel = new GuardianAngel();

        public Sidekick Sidekick = new Sidekick();
        public SchrodingerCat SchrodingerCat = new SchrodingerCat();
        public Demolitionist Demolitionist = new Demolitionist();
        public Vampiress Vampiress = new Vampiress();


        public Amnesiac Amnesiac = new Amnesiac();
        public Survivor Survivor = new Survivor();
        public Snitch Snitch = new Snitch();
        public Speedrunner Speedrunner = new Speedrunner();
        public Trapper Trapper = new Trapper();
        public Phantom Phantom = new Phantom();

        public Egoist Egoist = new Egoist();

        public Lovers Lovers = new Lovers();
        public Debugger Debugger = new Debugger();


    }
}