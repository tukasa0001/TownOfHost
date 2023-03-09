using System;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TOHTOR.Roles;
using UnityEngine;
using VentLib.Logging;

namespace TOHTOR.Patches.Systems;

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
            rolesFolder.FolderName = TOHPlugin.ModName;
            CustomRolesFolder = rolesFolder;
            __instance.Root.SubFolders.Add(rolesFolder);
        }
    }
    public static void Postfix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        VentLogger.Old("Opened " + taskFolder.FolderName, "TaskFolder");
        float xCursor = 0f;
        float yCursor = 0f;
        float maxHeight = 0f;
        if (CustomRolesFolder != null && CustomRolesFolder.FolderName == taskFolder.FolderName)
        {
            var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.FirstOrDefault(role => role.Role == RoleTypes.Crewmate);
            foreach (CustomRole role in CustomRoleManager.AllRoles)
            {
                TaskAddButton button = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
                button.Text.text = role.RoleName;
                __instance.AddFileAsChild(CustomRolesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
                RoleBehaviour roleBehaviour = (RoleBehaviour)typeof(RoleBehaviour).GetConstructor(Array.Empty<Type>())!.Invoke(null);
                roleBehaviour.Role = (RoleTypes)CustomRoleManager.GetRoleId(role) + 1000;
                button.Role = roleBehaviour;

                Color IconColor = Color.white;
                var roleColor = role.RoleColor;
                var RoleType = role.GetRoleType();

                button.FileImage.color = roleColor;
                button.RolloverHandler.OutColor = roleColor;
                button.RolloverHandler.OverColor = new Color(roleColor.r * 0.5f, roleColor.g * 0.5f, roleColor.b * 0.5f);
            }
        }
    }
}

/*[HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.Update))]
class TaskAddButtonUpdatePatch
{
    public static bool Prefix(TaskAddButton __instance)
    {
        try
        {
            if ((int)__instance.Role.Role >= 1000)
            {
                var PlayerCustomRole = PlayerControl.LocalPlayer.GetCustomRole();
                CustomRole FileCustomRole = CustomRoleManager.GetRoleFromId((int)((CustomRoles)__instance.Role.Role - 1000));
                ((Renderer)__instance.Overlay).enabled = PlayerCustomRole == FileCustomRole;
            }
        }
        catch { }
        return true;
    }
}*/
/*[HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.AddTask))]
class AddTaskButtonPatch
{
    public static bool Prefix(TaskAddButton __instance)
    {
        try
        {
            if ((int)__instance.Role.Role >= 1000)
            {
                CustomRole FileCustomRole = CustomRoleManager.GetRoleFromId((int)((CustomRoles)__instance.Role.Role - 1000));
                PlayerControl.LocalPlayer.RpcSetCustomRole(FileCustomRole);
                PlayerControl.LocalPlayer.RpcSetRole(FileCustomRole.DesyncRole ?? FileCustomRole.VirtualRole);
                return false;
            }
        }
        catch { }
        return true;
    }
}*/