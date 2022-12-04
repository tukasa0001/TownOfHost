using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Hazel;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer => IsRequired && (IsSingleImpostor || Diff_CrewImp == 1);
        ///<summary>
        ///インポスターが一人しか存在しない設定かどうか
        ///</summary>
        public static bool IsSingleImpostor => Main.RealOptionsData != null ? Main.RealOptionsData.NumImpostors == 1 : PlayerControl.GameOptions.NumImpostors == 1;
        ///<summary>
        ///AntiBlackout内の処理が必要であるかどうか
        ///</summary>
        public static bool IsRequired => Options.NoGameEnd.GetBool() || Jackal.IsEnable;
        ///<summary>
        ///インポスター以外の人数とインポスターの人数の差
        ///</summary>
        public static int Diff_CrewImp
        {
            get
            {
                int numImpostors = 0;
                int numCrewmates = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.IsImpostor) numImpostors++;
                    else numCrewmates++;
                }
                return numCrewmates - numImpostors;
            }
        }
        public static bool IsCached { get; private set; } = false;
        private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"SetIsDead is called from {callerMethodName}", "AntiBlackout");
            if (IsCached)
            {
                Logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。", "AntiBlackout.Error");
                return;
            }
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
                info.IsDead = false;
                info.Disconnected = false;
            }
            IsCached = true;
            if (doSend) SendGameData();
        }
        public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"RestoreIsDead is called from {callerMethodName}", "AntiBlackout");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                {
                    info.IsDead = val.isDead;
                    info.Disconnected = val.Disconnected;
                }
            }
            isDeadCache.Clear();
            IsCached = false;
            if (doSend) SendGameData();
        }

        public static void SendGameData([CallerMemberName] string callerMethodName = "")
        {
            Logger.Info($"SendGameData is called from {callerMethodName}", "AntiBlackout");
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
            // 書き込み {}は読みやすさのためです。
            writer.StartMessage(5); //0x05 GameData
            {
                writer.Write(AmongUsClient.Instance.GameId);
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
            SendGameData();
        }

        ///<summary>
        ///一時的にIsDeadを本来のものに戻した状態でコードを実行します
        ///<param name="action">実行内容</param>
        ///</summary>
        public static void TempRestore(Action action)
        {
            Logger.Info("==Temp Restore==", "AntiBlackout");
            //IsDeadが上書きされた状態でTempRestoreが実行されたかどうか
            bool before_IsCached = IsCached;
            try
            {
                if (before_IsCached) RestoreIsDead(doSend: false);
                action();
            }
            catch (Exception ex)
            {
                Logger.Warn("AntiBlackout.TempRestore内で例外が発生しました", "AntiBlackout");
                Logger.Error(ex.ToString(), "AntiBlackout.TempRestore");
            }
            finally
            {
                if (before_IsCached) SetIsDead(doSend: false);
                Logger.Info("==/Temp Restore==", "AntiBlackout");
            }
        }

        public static void Reset()
        {
            Logger.Info("==Reset==", "AntiBlackout");
            if (isDeadCache == null) isDeadCache = new();
            isDeadCache.Clear();
            IsCached = false;
        }
    }
}