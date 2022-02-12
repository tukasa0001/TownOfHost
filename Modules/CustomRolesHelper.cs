using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using InnerNet;

namespace TownOfHost {
    static class CustomRolesHelper {
        public static bool isImpostor(this CustomRoles role) {
            bool isImpostor =
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter ||
                role == CustomRoles.Vampire ||
                role == CustomRoles.BountyHunter ||
                role == CustomRoles.Witch ||
                role == CustomRoles.Solicitor ||
                role == CustomRoles.Bribber ||
                role == CustomRoles.Mafia;
            return isImpostor;
        }
        public static bool isImpostorTeam(this CustomRoles role) {
            bool isImpostor =
                role.isImpostor() ||
                role == CustomRoles.Madmate ||
                role == CustomRoles.MadGuardian ||
                role == CustomRoles.SKMadmate;
            return isImpostor;
        }
        public static bool CanUseKillButton(this CustomRoles role) {
            bool canUse =
                role.isImpostor() ||
                role == CustomRoles.Sheriff;

            if(role == CustomRoles.Mafia) {
                int AliveImpostorCount = 0;
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    CustomRoles pc_role = pc.getCustomRole();
                    if(pc_role.isImpostor() && !pc.Data.IsDead && pc_role != CustomRoles.Mafia) AliveImpostorCount++;
                }
                if(AliveImpostorCount > 0) canUse = false;
            }
            return canUse;
        }
        public static IntroTypes GetIntroType(this CustomRoles role) {
            IntroTypes type = IntroTypes.Crewmate;
            switch(role) {
                case CustomRoles.Impostor:
                case CustomRoles.Shapeshifter:
                case CustomRoles.Vampire:
                case CustomRoles.Mafia:
                case CustomRoles.BountyHunter:
                case CustomRoles.Witch:
                case CustomRoles.Solicitor:
                case CustomRoles.Bribber:
                    type = IntroTypes.Impostor;
                    break;

                case CustomRoles.Jester:
                case CustomRoles.Opportunist:
                case CustomRoles.Terrorist:
                case CustomRoles.Troll:
                case CustomRoles.Fox:
                    type = IntroTypes.Neutral;
                    break;

                case CustomRoles.Madmate:
                case CustomRoles.MadGuardian:
                case CustomRoles.SKMadmate:
                    type = IntroTypes.Madmate;
                    break;
            }
            return type;
        }
    }
    public enum IntroTypes {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}
