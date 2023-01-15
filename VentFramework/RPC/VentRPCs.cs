#nullable enable
using System;
using System.Linq;
using System.Reflection;
using VentLib.Logging;

namespace VentLib;

public static class VentRPCs
{
    [ModRPC((uint)VentRPC.SetControlFlag, RpcActors.Host, RpcActors.NonHosts)]
    public static void SetControlFlag(string assemblyName, int controlFlag)
    {
        VentLogger.Debug($"SetControlFlag(assemblyName={assemblyName}, controlFlag={controlFlag})", "VentFramework");
        Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().FullName == assemblyName);
        if (assembly == null) return;
        VentFramework.SetControlFlag(assembly, (VentControlFlag)controlFlag);
        VentLogger.Debug($"Control Flag Set For: {assembly.GetName().Name} | Flag: {(VentControlFlag)controlFlag})");
    }
}

public enum VentRPC: uint
{
    SetControlFlag = 1017
}