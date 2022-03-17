using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    class OnDisconnectedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            main.VisibleTasksCount = false;
            main.OptionControllerIsEnable = false;
            if(!IntroTypes.Impostor.isLastImpostor())
                main.AliveImpostorCount = Utils.NumOfAliveImpostors(main.AliveImpostorCount);
        }
    }
}
