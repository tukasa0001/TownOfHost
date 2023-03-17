using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Hazel;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.RPC;
using VentLib.Logging;

namespace TOHTOR.Managers;

public static class AntiBlackout
{
    ///<summary>
    ///追放処理を上書きするかどうか
    ///</summary>
    public static bool OverrideExiledPlayer => StaticOptions.NoGameEnd || GameStates.CountAliveRealImpostors() >= GameStates.CountAliveRealCrew();
    public static GameData.PlayerInfo? ExiledPlayer;
    public static GameData.PlayerInfo? FakeExiled;

    public static bool IsCached { get; private set; }
    private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
    private static Dictionary<byte, (string playerName, Il2CppSystem.Collections.Generic.Dictionary<PlayerOutfitType, GameData.PlayerOutfit> outfits)> cosmeticsCache = new();

    public static void SaveCosmetics()
    {
        GameData.Instance.AllPlayers.ToArray().Where(i => i != null).Do(i => cosmeticsCache[i.PlayerId] = (i.PlayerName, i.Outfits));
    }

    public static void LoadCosmetics([CallerMemberName] string callerMethodName = "")
    {
        VentLogger.Trace($"Load Cosmetics Called From: {callerMethodName}");
        GameData.PlayerInfo[] playerInfos = GameData.Instance.AllPlayers.ToArray();
        foreach (byte playerId in cosmeticsCache.Keys)
        {
            GameData.PlayerInfo? playerInfo = playerInfos.FirstOrDefault(p => p.PlayerId == playerId);
            if (playerInfo == null) continue;
            playerInfo.PlayerName = cosmeticsCache[playerId].playerName;
            playerInfo.Outfits = cosmeticsCache[playerId].outfits;
        }
    }

    public static GameData.PlayerInfo? CreateFakePlayer(GameData.PlayerInfo? realPlayer)
    {
        //return null;
        if (realPlayer == null) return null;
        GameData.PlayerInfo? deadPlayer = GameData.Instance.AllPlayers.ToArray().Where(p => p.Disconnected || p.IsDead).FirstOrDefault(AntiBlackoutLogic.IsFakeable);
        if (deadPlayer == null) return null;
        VentLogger.Info($"Created Fake Player Using: {deadPlayer.Object.GetRawName()} => {realPlayer.Object.GetRawName()}");

        GameData.PlayerOutfit outfit = realPlayer.Outfits[PlayerOutfitType.Default].Clone();
        outfit.PlayerName = deadPlayer.PlayerName = "Modified " + realPlayer.Object.GetRawName();

        deadPlayer.Outfits[PlayerOutfitType.Default] = outfit;
        FakeExiled = deadPlayer;
        return deadPlayer;
    }



    public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        VentLogger.Old($"SetIsDead is called from {callerMethodName}", "AntiBlackout");
        if (IsCached)
        {
            VentLogger.Old("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。", "AntiBlackout.Error");
            return;
        }
        isDeadCache.Clear();
        GameData.Instance.AllPlayers.ToArray().Where(i => i != null).Do(i => isDeadCache[i.PlayerId] = (i.IsDead, i.Disconnected));
        IsCached = true;

        if (doSend) SendPatchedData();
    }
    public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        VentLogger.Old($"RestoreIsDead is called from {callerMethodName}", "AntiBlackout");
        GameData.Instance.AllPlayers.ToArray().Where(i => i != null).Do(info =>
        {
            if (!isDeadCache.TryGetValue(info.PlayerId, out var val)) return;
            info.IsDead = val.isDead;
            info.Disconnected = val.Disconnected;
        });
        isDeadCache.Clear();
        IsCached = false;
        if (doSend) RestoreGameData();
    }

    private static void RestoreGameData([CallerMemberName] string callerMethodName = "")
    {
        VentLogger.Old($"RestoreGameData is called from {callerMethodName}", "AntiBlackout");
        SendGameData();
    }

    private static void SendPatchedData()
    {
        HostRpc.RpcDebug("Game Data BEFORE Patch");
        AntiBlackoutLogic.PatchedDataLogic();
        HostRpc.RpcDebug("Game Data AFTER Patch");
    }

    public static void SendGameData(int clientId = -1)
    {
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        // 書き込み {}は読みやすさのためです。
        writer.StartMessage((byte)(clientId == -1 ? 5 : 6)); //0x05 GameData
        {
            writer.Write(AmongUsClient.Instance.GameId);
            if (clientId != -1) writer.WritePacked(clientId);
            writer.StartMessage(1); //0x01 Data
            {
                writer.WritePacked(GameData.Instance.NetId);
                GameData.Instance.Serialize(writer, true);
            }
            writer.EndMessage();
        }
        writer.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    public static void OnDisconnect(GameData.PlayerInfo player)
    {
        // 実行条件: クライアントがホストである, IsDeadが上書きされている, playerが切断済み
        if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
        isDeadCache[player.PlayerId] = (true, true);
        player.IsDead = player.Disconnected = false;
    }

    public static void Reset()
    {
        VentLogger.Old("==Reset==", "AntiBlackout");
        if (isDeadCache == null) isDeadCache = new();
        isDeadCache.Clear();
        IsCached = false;
    }
}