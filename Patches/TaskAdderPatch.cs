using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ShowFolder))]
    class ShowFolderPatch
    {
        private static TaskFolder CustomRolesFolder;
        public static void Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
        {
            if (__instance.Root == taskFolder && CustomRolesFolder == null)
            {
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
        public static void Postfix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
        {
            Logger.info("Opened " + taskFolder.FolderName);
            float xCursor = 0f;
            float yCursor = 0f;
            float maxHeight = 0f;
            if (CustomRolesFolder != null && CustomRolesFolder.FolderName == taskFolder.FolderName)
            {
                var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.Where(role => role.Role == RoleTypes.Crewmate).FirstOrDefault();
                foreach (var cRoleID in Enum.GetValues(typeof(CustomRoles)))
                {
                    CustomRoles cRole = (CustomRoles)cRoleID;
                    /*if(cRole == CustomRoles.Crewmate ||
                    cRole == CustomRoles.Impostor ||
                    cRole == CustomRoles.Scientist ||
                    cRole == CustomRoles.Engineer ||
                    cRole == CustomRoles.GuardianAngel ||
                    cRole == CustomRoles.Shapeshifter
                    ) continue;*/

                    TaskAddButton button = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
                    button.Text.text = Utils.getRoleName(cRole);
                    __instance.AddFileAsChild(CustomRolesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
                    var roleBehaviour = new RoleBehaviour();
                    roleBehaviour.Role = ((RoleTypes)cRole + 1000);
                    button.Role = roleBehaviour;

                    Color IconColor = Color.white;
                    var roleColor = Utils.getRoleColor(cRole);
                    var IntroType = cRole.getIntroType();

                    button.FileImage.color = roleColor;
                    button.RolloverHandler.OutColor = roleColor;
                    button.RolloverHandler.OverColor = new Color(roleColor.r * 0.5f, roleColor.g * 0.5f, roleColor.b * 0.5f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.Update))]
    class TaskAddButtonUpdatePatch
    {
        public static bool Prefix(TaskAddButton __instance)
        {
            try
            {
                if ((int)__instance.Role.Role >= 1000)
                {
                    var PlayerCustomRole = PlayerControl.LocalPlayer.getCustomRole();
                    CustomRoles FileCustomRole = (CustomRoles)__instance.Role.Role - 1000;
                    ((Renderer)__instance.Overlay).enabled = PlayerCustomRole == FileCustomRole;
                }
            }
            catch { }
            return true;
        }
    }
    [HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.AddTask))]
    class AddTaskButtonPatch
    {
        private static Dictionary<CustomRoles, RoleTypes> RolePairs = new Dictionary<CustomRoles, RoleTypes>(){
            //デフォルトでクルーなので、クルー判定役職は書かなくてOK
            {CustomRoles.Engineer, RoleTypes.Engineer},
            {CustomRoles.Scientist, RoleTypes.Scientist},
            {CustomRoles.Shapeshifter, RoleTypes.Shapeshifter},
            {CustomRoles.Impostor, RoleTypes.Impostor},
            {CustomRoles.GuardianAngel, RoleTypes.GuardianAngel},
            {CustomRoles.Mafia, RoleTypes.Shapeshifter},
            {CustomRoles.BountyHunter, RoleTypes.Shapeshifter},
            {CustomRoles.Witch, RoleTypes.Impostor},
            {CustomRoles.Warlock, RoleTypes.Shapeshifter},
            {CustomRoles.SerialKiller, RoleTypes.Shapeshifter},
            {CustomRoles.Vampire, RoleTypes.Impostor},
            {CustomRoles.ShapeMaster, RoleTypes.Shapeshifter},
            {CustomRoles.Madmate, RoleTypes.Engineer},
            {CustomRoles.Terrorist, RoleTypes.Engineer},
        };
        public static bool Prefix(TaskAddButton __instance)
        {
            try
            {
                if ((int)__instance.Role.Role >= 1000)
                {
                    CustomRoles FileCustomRole = (CustomRoles)__instance.Role.Role - 1000;
                    PlayerControl.LocalPlayer.RpcSetCustomRole(FileCustomRole);
                    RoleTypes oRole;
                    if (!RolePairs.TryGetValue(FileCustomRole, out oRole)) PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    else PlayerControl.LocalPlayer.RpcSetRole(oRole);
                    return false;
                }
            }
            catch { }
            return true;
        }
    }
}