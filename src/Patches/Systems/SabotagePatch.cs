using System;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Systems;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RepairSystem))]
public static class SabotagePatch
{
    public static SabotageType? CurrentSabotage;

    public static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        ActionHandle handle = ActionHandle.NoInit();
        ISystemType systemInstance;
        switch (systemType)
        {
            case SystemTypes.Sabotage:
                if (!Game.CurrentGamemode.AllowSabotage()) return false;
                if (player.GetCustomRole() is Impostor impostor && !impostor.CanSabotage()) return false;
                SabotageType sabotage = (SystemTypes)amount switch
                {
                    SystemTypes.Electrical => SabotageType.Lights,
                    SystemTypes.Comms => SabotageType.Communications,
                    SystemTypes.LifeSupp => SabotageType.Oxygen,
                    SystemTypes.Reactor => SabotageType.Reactor,
                    SystemTypes.Laboratory => SabotageType.Reactor,
                    _ => throw new Exception("Invalid Sabotage Type")
                };
                Game.TriggerForAll(RoleActionType.SabotageStarted, ref handle, sabotage, player);
                if (!handle.IsCanceled) CurrentSabotage = sabotage;
                break;
            case SystemTypes.Electrical:
                if (CurrentSabotage != SabotageType.Lights) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                SwitchSystem electrical = systemInstance!.Cast<SwitchSystem>();
                byte currentSwitches = electrical.ActualSwitches;
                if (amount.HasBit(128))
                    currentSwitches ^= (byte) (amount & 31U);
                else
                    currentSwitches ^= (byte) (1U << amount);
                if (currentSwitches != electrical.ExpectedSwitches) break;
                Logger.Msg($"Electrical Sabotage Fixed by {player.GetRawName()}", "SabotageFix");
                Game.TriggerForAll(RoleActionType.SabotageFixed, ref handle, SabotageType.Lights, player);
                CurrentSabotage = null;
                break;
            case SystemTypes.Comms:
                if (CurrentSabotage != SabotageType.Communications) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                if (systemInstance!.GetType() == typeof(HudOverrideSystemType) && amount == 0)
                {
                    Game.TriggerForAll(RoleActionType.SabotageFixed, ref handle, SabotageType.Communications, player);
                    CurrentSabotage = null;
                } else if (systemInstance.GetType() == typeof(HqHudSystemType)) // Mira has a special communications which requires two people
                {
                    HqHudSystemType miraComms = systemInstance.Cast<HqHudSystemType>(); // Get mira comm instance
                    byte commsNum = (byte) (amount & 15U); // Convert to 0 or 1 for respective console
                    if (miraComms.CompletedConsoles.Contains(commsNum)) break; // Negative check if console has already been fixed (refreshes periodically)

                    // Send partial fix action
                    Game.TriggerForAll(RoleActionType.SabotagePartialFix, ref handle, SabotageType.Communications, player, commsNum);
                    // If there's more than 1 already fixed then comms is fixed totally
                    if (miraComms.NumComplete == 0) break;
                    Game.TriggerForAll(RoleActionType.SabotageFixed, ref handle, SabotageType.Communications, player);
                    CurrentSabotage = null;
                }
                if (CurrentSabotage == null)
                    Logger.Msg($"Communications Sabotage Fixed by {player.GetRawName()}", "SabotageFix");
                break;
            case SystemTypes.LifeSupp:
                if (CurrentSabotage != SabotageType.Oxygen) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                LifeSuppSystemType oxygen = systemInstance!.Cast<LifeSuppSystemType>();
                int o2Num = amount & 3;
                if (oxygen.CompletedConsoles.Contains(o2Num)) break;
                Game.TriggerForAll(RoleActionType.SabotagePartialFix, ref handle, SabotageType.Oxygen, o2Num);
                if (oxygen.UserCount == 0) break;
                Game.TriggerForAll(RoleActionType.SabotageFixed, ref handle, SabotageType.Oxygen);
                CurrentSabotage = null;
                Logger.Msg($"Oxygen Sabotage Fixed by {player.GetRawName()}", "SabotageFix");
                break;
            case SystemTypes.Laboratory:
            case SystemTypes.Reactor:
                if (CurrentSabotage != SabotageType.Reactor) break;
                if (!__instance.TryGetSystem(systemType, out systemInstance)) break;
                ReactorSystemType reactor = systemInstance!.Cast<ReactorSystemType>();
                int reactNum = amount & 3;
                if (reactor.UserConsolePairs.ToList().Any(p => p.Item2 == reactNum)) break;
                Game.TriggerForAll(RoleActionType.SabotagePartialFix, ref handle, SabotageType.Reactor, reactNum);
                if (reactor.UserCount == 0) break;
                Game.TriggerForAll(RoleActionType.SabotageFixed, ref handle, SabotageType.Reactor);
                CurrentSabotage = null;
                Logger.Msg($"Reactor Sabotage Fixed by {player.GetRawName()}", "SabotageFix");
                break;
            default:
                return true;
        }

        return !handle.IsCanceled;
    }
}