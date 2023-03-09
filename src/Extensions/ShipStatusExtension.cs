#nullable enable
namespace TOHTOR.Extensions;

public static class ShipStatusExtension
{
    public static ISystemType? GetSystem(this ShipStatus shipStatus, SystemTypes system)
    {
        return shipStatus.Systems.ContainsKey(system) ? shipStatus.Systems[system] : null;
    }

    public static bool TryGetSystem(this ShipStatus shipStatus, SystemTypes system, out ISystemType? systemType)
    {
        systemType = null;
        if (!shipStatus.Systems.ContainsKey(system)) return false;
        systemType = shipStatus.Systems[system];
        return true;
    }
}