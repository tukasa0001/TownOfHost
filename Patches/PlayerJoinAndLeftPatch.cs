using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using InnerNet;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();

            NameColorManager.Begin();
            Options.Load();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            //            main.RealNames.Remove(data.Character.PlayerId);
            if (GameStates.IsInGame)
            {
                if (data.Character.Is(CustomRoles.TimeThief))
                    data.Character.ResetThiefVotingTime();
                if (data.Character.Is(CustomRoles.Lovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.LoversPlayers.ToArray())
                    {
                        Main.isLoversDead = true;
                        Main.LoversPlayers.Remove(lovers);
                        Main.AllPlayerCustomSubRoles[lovers.PlayerId] = CustomRoles.NoSubRoleAssigned;
                    }
                if (data.Character.Is(CustomRoles.Executioner) && Main.ExecutionerTarget.ContainsKey(data.Character.PlayerId))
                {
                    data.Character.RpcSetCustomRole(Options.CRoleExecutionerChangeRoles[Options.ExecutionerChangeRolesAfterTargetKilled.GetSelection()]);
                    Main.ExecutionerTarget.Remove(data.Character.PlayerId);
                    RPC.RemoveExecutionerKey(data.Character.PlayerId);
                }
                if (PlayerState.GetDeathReason(data.Character.PlayerId) == PlayerState.DeathReason.etc) //死因が設定されていなかったら
                {
                    PlayerState.SetDeathReason(data.Character.PlayerId, PlayerState.DeathReason.Disconnected);
                    PlayerState.SetDead(data.Character.PlayerId);
                }
            }
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason})", "Session");
        }
    }
}