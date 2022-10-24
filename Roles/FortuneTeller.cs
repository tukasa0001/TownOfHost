using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TownOfHost
{
    public static class FortuneTeller
    {
        private static readonly int Id = 21100;
        public static List<byte> playerIdList = new();

        public static CustomOption NumOfForecast;
        public static Dictionary<byte, PlayerControl> Target = new();
        public static Dictionary<byte, Dictionary<byte, PlayerControl>> TargetResult = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
            NumOfForecast = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "FortuneTellerNumOfForecast", 2f, 1f, 99f, 1f, Options.CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
            TargetResult = new();
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
            if (player.HasForecastResult(target.PlayerId)) return;  //既に占い結果があるときはターゲットにならない

            Target[player.PlayerId] = target;
            Logger.Info($"SetForecastTarget player: {player.name}, target: {target.name}", "FortuneTeller");
        }
        public static bool HasForecastTarget(this PlayerControl player)
        {
            if (!Target.TryGetValue(player.PlayerId, out var target)) return false;
            return target != null;
        }
        public static void ConfirmForecastResult()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                if (player.Is(CustomRoles.FortuneTeller) &&
                    !player.Data.IsDead && !player.Data.Disconnected && player.HasForecastTarget())
                    player.SetForecastResult();
            }
        }
        public static void SetForecastResult(this PlayerControl player)
        {
            if (!Target.TryGetValue(player.PlayerId, out var target))
            {
                Logger.Info($"SetForecastResult NotSet NotHasForecastTarget player: {player.name}", "FortuneTeller");
                return;
            }
            Target.Remove(player.PlayerId);

            if (target == null || target.Data.IsDead || target.Data.Disconnected)
            {
                Logger.Info($"SetForecastResult NotSet TargetNotValid player: {player.name}, target: {target?.name} dead: {target?.Data.IsDead}, disconnected: {target?.Data.Disconnected}", "FortuneTeller");
                return;
            }

            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget))
            {
                resultTarget = new();
                TargetResult[player.PlayerId] = resultTarget;
            }
            if (resultTarget.Count >= NumOfForecast.GetInt())
            {
                Logger.Info($"SetForecastResult NotSet ForecastCountOver player: {player.name}, target: {target.name} forecastCount: {resultTarget.Count}, canCount: {NumOfForecast.GetInt()}", "FortuneTeller");
                return;
            }

            resultTarget[target.PlayerId] = target;
            Logger.Info($"SetForecastResult SetTarget player: {player.name}, target: {target.name}", "FortuneTeller");
        }
         public static bool HasForecastResult(this PlayerControl player, byte targetId)
        {
            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget)) return false;
            return resultTarget.ContainsKey(targetId);
        }
        public static bool HasForecastResult(this PlayerControl player)
        {
            if (!TargetResult.TryGetValue(player.PlayerId, out var resultTarget)) return false;
            return resultTarget.Count > 0;
        }
        public static string TargetMark(PlayerControl seer, PlayerControl target)
        {
            if (seer == null || target == null) return "";
            if (!seer.Is(CustomRoles.FortuneTeller)) return ""; //占い師以外処理しない
            if (!seer.HasForecastResult(target.PlayerId)) return "";

            return Helpers.ColorString(Utils.GetRoleColor(CustomRoles.FortuneTeller), "★");
        }
    }
}