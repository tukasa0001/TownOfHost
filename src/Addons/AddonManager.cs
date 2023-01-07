using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Gamemodes;
using TownOfHost.Roles;
using TownOfHost.RPC;
using VentFramework;

namespace TownOfHost.Addons;

public class AddonManager
{
    public static List<TOHAddon> Addons = new();


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
            Logger.Color($"Loading Addon [{addon.AddonName()} {addon.AddonVersion()}]", "AddonManager", ConsoleColor.Magenta);
            Addons.Add(addon);
            MethodInfo initialize = tohType.GetMethod("Initialize");
            initialize!.Invoke(addon, null);

            addon.Factions.Do(f => FactionConstraintValidator.ValidateAndAdd(f, file.Name));
            CustomRoleManager.AllRoles.AddRange(addon.CustomRoles);
            TOHPlugin.GamemodeManager.Gamemodes.AddRange(addon.Gamemodes);
        }
        catch (Exception e)
        {
            throw new AddonException($"Exception encountered while loading addon: {file.Name}", e);
        }
    }

    [ModRPC((uint)ModCalls.VerifyAddons, RpcActors.Everyone, RpcActors.Host)]
    public static void VerifyClientAddons(List<AddonInfo> addons)
    {
        List<AddonInfo> hostInfo = Addons.Select(AddonInfo.From).ToList();

        List<AddonInfo> mismatchInfo = hostInfo.Select(hostAddon =>
        {
            AddonInfo matchingAddon = addons.FirstOrDefault(a => a == hostAddon);
            if (matchingAddon == null)
            {
                hostAddon.Mismatches = Mismatch.ClientMissingAddon;
                return hostAddon;
            }

            matchingAddon.CheckVersion(matchingAddon);
            return matchingAddon;
        }).ToList();

        mismatchInfo.AddRange(addons.Select(clientAddon =>
            {
                AddonInfo matchingAddon = hostInfo.FirstOrDefault(a => a == clientAddon);
                if (matchingAddon == null)
                    clientAddon.Mismatches = Mismatch.HostMissingAddon;
                else
                    clientAddon.CheckVersion(matchingAddon);
                return clientAddon;
            }));

        VerifyClientAddons(mismatchInfo.DistinctBy(addon => addon.Name).Where(addon => addon.Mismatches is not Mismatch.None).ToList());
    }

    [ModRPC((uint)ModCalls.VerifyAddons, RpcActors.None, RpcActors.NonHosts)]
    public static void ReceiveAddonVerification(List<AddonInfo> addons)
    {
        if (addons.Count == 0) return;
        Logger.Error(" Error Validating Addons. All CustomRPCs between the host and this client have been disabled.", "VerifyAddons");
        Logger.Error(" -=-=-=-=-=-=-=-=-=[Errored Addons]=-=-=-=-=-=-=-=-=-", "VerifyAddons");
        foreach (var rejectReason in addons.Select(addonInfo => (addonInfo.Mismatches)
             switch {
                 Mismatch.Version => $" {addonInfo.Name}:{addonInfo.Version} => Local version is not compatible with the host version of the addon",
                 Mismatch.ClientMissingAddon => $" {addonInfo.Name}:{addonInfo.Version} => Client Missing Addon ",
                 Mismatch.HostMissingAddon => $" {addonInfo.Name}:{addonInfo.Version} => Host Missing Addon ",
                 _ => throw new ArgumentOutOfRangeException()
             }))
            Logger.Error(rejectReason, "VerifyAddons");
    }
}