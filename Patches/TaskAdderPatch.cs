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
    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ShowFolder))]
    class ShowFolderPatch {
        private static TaskFolder CustomRolesFolder;
        private static Dictionary<CustomRoles, RoleTypes> RolePairs = new Dictionary<CustomRoles, RoleTypes>(){
            //デフォルトでクルーなので、クルー判定役職は書かなくてOK
            {CustomRoles.Vampire, RoleTypes.Impostor},
            {CustomRoles.Mafia, RoleTypes.Shapeshifter},
            {CustomRoles.BountyHunter, RoleTypes.Impostor},
            {CustomRoles.Madmate, RoleTypes.Engineer},
            {CustomRoles.Terrorist, RoleTypes.Engineer},
        };
        public static void Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder) {
            if(__instance.Root == taskFolder && CustomRolesFolder == null) {
                TaskFolder rolesFolder = UnityEngine.Object.Instantiate<TaskFolder>(
                    __instance.RootFolderPrefab,
                    __instance.transform
                );
                rolesFolder.gameObject.SetActive(false);
                rolesFolder.FolderName = "Town Of Host";
                CustomRolesFolder = rolesFolder;
                __instance.Root.SubFolders.Add(rolesFolder);
            }
        }
        public static void Postfix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder) {
            Logger.info("Opened " + taskFolder.FolderName);
            float xCursor = 0f;
            float yCursor = 0f;
            float maxHeight = 0f;
            if(CustomRolesFolder != null && CustomRolesFolder.FolderName == taskFolder.FolderName) {
                var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.Where(role => role.Role == RoleTypes.Crewmate).FirstOrDefault();
                foreach(var cRoleID in Enum.GetValues(typeof(CustomRoles))) {
                    CustomRoles cRole = (CustomRoles)cRoleID;
                    if(cRole == CustomRoles.Default ||
                    cRole == CustomRoles.Impostor ||
                    cRole == CustomRoles.Scientist ||
                    cRole == CustomRoles.Engineer ||
                    cRole == CustomRoles.GuardianAngel ||
                    cRole == CustomRoles.Shapeshifter
                    ) continue;

                    TaskAddButton button = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
                    button.Text.text = main.getRoleName(cRole);
                    __instance.AddFileAsChild(CustomRolesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
                    var roleBehaviour = new RoleBehaviour();
                    roleBehaviour.Role = ((RoleTypes)cRole + 1000);
                    button.Role = roleBehaviour;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.Update))]
    class TaskAddButtonUpdatePatch {
        public static void Prefix(TaskAddButton __instance) {
            ((Renderer) __instance.Overlay).enabled = Input.GetKey(KeyCode.Space);
            if(__instance.Role != null && (ushort)__instance.Role.Role >= 1000) {
            }
        }
    }
}