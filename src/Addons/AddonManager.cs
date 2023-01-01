using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TownOfHost.Factions;
using TownOfHost.Roles;

namespace TownOfHost.Addons;

public class AddonManager
{

    public static void ImportAddons()
    {
        DirectoryInfo addonDirectory = new("./addons/");
        if (!addonDirectory.Exists)
            addonDirectory.Create();
        addonDirectory.EnumerateFiles().Do(LoadAddon);
    }

    private static void LoadAddon(FileInfo file)
    {
        try
        {
            Assembly assembly = Assembly.LoadFile(file.FullName);
            Type tohType = assembly.GetTypes().FirstOrDefault(t => t.IsAssignableTo(typeof(TOHAddon)));
            if (tohType == null)
                throw new ConstraintException("TownOfHost Addons requires ONE class file that extends TOHAddon");
            TOHAddon addon = (TOHAddon)tohType.GetConstructor(new Type[] { })!.Invoke(null);
            MethodInfo initialize = tohType.GetMethod("Initialize");
            initialize!.Invoke(addon, null);

            addon.factions.Do(f => FactionConstraintValidator.ValidateAndAdd(f, file.Name));
            addon.customRoles.Do(r =>
            {
                CustomRole role = (CustomRole)r.GetConstructor(new Type[] { })!.Invoke(null);
                CustomRoleManager.AddRole(role);
            });
        }
        catch (Exception e)
        {
            throw new AddonException($"Exception encountered while loading addon: {file.Name}", e);
        }
    }
}