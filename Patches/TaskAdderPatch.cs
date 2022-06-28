using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

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
            Logger.Info("Opened " + taskFolder.FolderName, "TaskFolder");
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
                    button.Text.text = Utils.GetRoleName(cRole);
                    __instance.AddFileAsChild(CustomRolesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
                    var roleBehaviour = new RoleBehaviour
                    {
                        Role = (RoleTypes)cRole + 1000
                    };
                    button.Role = roleBehaviour;

                    Color IconColor = Color.white;
                    var roleColor = Utils.GetRoleColor(cRole);
                    var RoleType = cRole.GetRoleType();

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
                    var PlayerCustomRole = PlayerControl.LocalPlayer.GetCustomRole();
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
        private static readonly Dictionary<CustomRoles, RoleTypes> RolePairs = new()
        {
            //デフォルトでクルーなので、クルー判定役職は書かなくてOK
            { CustomRoles.Engineer, RoleTypes.Engineer },
            { CustomRoles.Scientist, RoleTypes.Scientist },
            { CustomRoles.Shapeshifter, RoleTypes.Shapeshifter },
            { CustomRoles.Impostor, RoleTypes.Impostor },
            { CustomRoles.GuardianAngel, RoleTypes.GuardianAngel },
            { CustomRoles.Mafia, RoleTypes.Impostor },
            { CustomRoles.BountyHunter, RoleTypes.Shapeshifter },
            { CustomRoles.Witch, RoleTypes.Impostor },
            { CustomRoles.Warlock, RoleTypes.Shapeshifter },
            { CustomRoles.SerialKiller, RoleTypes.Shapeshifter },
            { CustomRoles.Vampire, RoleTypes.Impostor },
            { CustomRoles.ShapeMaster, RoleTypes.Shapeshifter },
            { CustomRoles.Madmate, RoleTypes.Engineer },
            { CustomRoles.Terrorist, RoleTypes.Engineer },
            { CustomRoles.EvilWatcher, RoleTypes.Impostor },
            { CustomRoles.Mare, RoleTypes.Impostor },
            { CustomRoles.Doctor, RoleTypes.Scientist },
            { CustomRoles.TimeThief, RoleTypes.Impostor },
        };
        public static bool Prefix(TaskAddButton __instance)
        {
            try
            {
                if ((int)__instance.Role.Role >= 1000)
                {
                    CustomRoles FileCustomRole = (CustomRoles)__instance.Role.Role - 1000;
                    PlayerControl.LocalPlayer.RpcSetCustomRole(FileCustomRole);
                    if (!RolePairs.TryGetValue(FileCustomRole, out RoleTypes oRole)) PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    else PlayerControl.LocalPlayer.RpcSetRole(oRole);
                    return false;
                }
            }
            catch { }
            return true;
        }
    }
}