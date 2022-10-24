using System.Collections.Generic;
using System.Linq;

namespace TownOfHost
{
    public static class FortuneTeller
    {
        private static readonly int Id = 21100;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, PlayerControl> Target = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void VoteForecastTarget(this PlayerControl player, byte targetId)
        {
            if (GameData.Instance.AllPlayers.ToArray().Where(x => x.IsDead).Count() <= 0) //死体無し
            {
                Logger.Info($"VoteForecastTarget NotForecast NoDeadBody player: {player.name}, targetId: {targetId}", "FortuneTeller");
                return;
            }

            player.SetForecastTarget(targetId);
        }
        public static void SetForecastTarget(this PlayerControl player, byte targetId)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null || target.Data.IsDead || target.Data.Disconnected) return;
            //todo:既に占い結果があるときはターゲットにならない

            Target[player.PlayerId] = target;
            Logger.Info($"SetForecastTarget player: {player.name}, target: {target.name}", "FortuneTeller");
        }
        public static bool HasForecastTarget(this PlayerControl player)
        {
            if (!Target.TryGetValue(player.PlayerId, out var target)) return false;
            return target != null;
        }
    }
}