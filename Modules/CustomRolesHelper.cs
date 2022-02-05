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
                role == CustomRoles.Mafia;
            return isImpostor;
        }
        public static bool isImpostorTeam(this CustomRoles role) {
            bool isImpostor = 
                role.isImpostor() ||
                role == CustomRoles.Madmate ||
                role == CustomRoles.MadGuardian;
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
    }
}