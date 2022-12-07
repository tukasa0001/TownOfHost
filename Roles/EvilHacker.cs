using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class EvilHacker
    {
        public static readonly int Id = 3100;
        public static List<byte> playerIdList = new();

        public static OptionItem CanSeeDeadPos;
        public static OptionItem CanSeeOtherImp;
        public static OptionItem CanSeeKillFlash;
        public static OptionItem CanSeeMurderScene;

        public static Dictionary<SystemTypes, int> PlayerCount = new();
        public static Dictionary<SystemTypes, int> DeadCount = new();
        public static List<SystemTypes> ImpRooms = new();
        // (キルしたインポスター, 殺害現場の部屋)
        public static List<(PlayerControl, SystemTypes)> KillersAndRooms = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilHacker);
            CanSeeDeadPos = BooleanOptionItem.Create(Id + 10, "CanSeeDeadPos", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilHacker]);
            CanSeeOtherImp = BooleanOptionItem.Create(Id + 11, "CanSeeOtherImp", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilHacker]);
            CanSeeKillFlash = BooleanOptionItem.Create(Id + 12, "CanSeeKillFlash", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilHacker]);
            CanSeeMurderScene = BooleanOptionItem.Create(Id + 13, "CanSeeMurderScene", true, TabGroup.ImpostorRoles, false).SetParent(CanSeeKillFlash);
        }
        public static void Init()
        {
            playerIdList = new();
            PlayerCount = new();
            DeadCount = new();
            ImpRooms = new();
            KillersAndRooms = new();
        }
        public static void Add(byte playerId) => playerIdList.Add(playerId);
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void InitDeadCount()
        {
            if (ShipStatus.Instance == null)
            {
                Logger.Warn("死者カウントの初期化時にShipStatus.Instanceがnullでした", "EvilHacker");
                return;
            }
            foreach (var room in ShipStatus.Instance.AllRooms) DeadCount[room.RoomId] = 0;
        }

        public static void OnStartMeeting()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            // 全生存プレイヤーの位置を取得
            foreach (var room in ShipStatus.Instance.AllRooms) PlayerCount[room.RoomId] = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.IsAlive()) continue;
                var room = pc.GetRoom();
                PlayerCount[room]++;
                if (CanSeeOtherImp.GetBool() && pc.GetCustomRole().IsImpostor() && !ImpRooms.Contains(room))
                    ImpRooms.Add(room);
            }
            PlayerCount.Remove(SystemTypes.Hallway);
            DeadCount.Remove(SystemTypes.Hallway);
            // 送信するメッセージを生成
            string message = "";
            foreach (var kvp in PlayerCount)
            {
                if (ImpRooms.Contains(kvp.Key)) message += '★';
                var roomName = DestroyableSingleton<TranslationController>.Instance.GetString(kvp.Key);
                if (CanSeeDeadPos.GetBool())
                {
                    message = $"{message}{roomName}: {kvp.Value + DeadCount[kvp.Key]}";
                    message += DeadCount[kvp.Key] > 0 ? $"({GetString("Deadbody")}\u00d7{DeadCount[kvp.Key]})\n" : '\n';
                }
                else
                {
                    message = $"{message}{roomName}: {kvp.Value + DeadCount[kvp.Key]}\n";
                }
            }
            // 生存イビルハッカーに送信
            var aliveEvilHackerIds = playerIdList.Where(x => Utils.GetPlayerById(x).IsAlive()).ToArray();
            aliveEvilHackerIds.Do(id => Utils.SendMessage(message, id, Utils.ColorString(Palette.AcceptedGreen, $"{GetString("Message.LastAdminInfo")}")));

            InitDeadCount();
            ImpRooms = new();
        }
        public static void OnMurder(PlayerControl killer, PlayerControl target)
        {
            var room = target.GetRoom();
            DeadCount[room]++;
            if (CanSeeOtherImp.GetBool() && target.GetCustomRole().IsImpostor() && !ImpRooms.Contains(room))
                ImpRooms.Add(room);
            if (CanSeeMurderScene.GetBool() && Utils.IsImpostorKill(killer, target))
            {
                var realKiller = target.GetRealKiller() ?? killer;
                KillersAndRooms.Add((realKiller, room));
                RpcSyncMurderScenes();
                new LateTask(() =>
                {
                    if (!GameStates.IsInGame)
                    {
                        Logger.Info("待機中にゲームが終了したためキャンセル", "EvilHacker");
                        return;
                    }
                    KillersAndRooms.Remove((realKiller, room));
                    RpcSyncMurderScenes();
                }, 10f, "Remove EvilHacker KillersAndRooms");
            }
        }
        public static void RpcSyncMurderScenes()
        {
            // タプルの数，キル者ID1，キル現場1，キル者ID2，キル現場2，......
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncEvilHackerScenes, SendOption.Reliable, -1);
            writer.Write(KillersAndRooms.Count);
            KillersAndRooms.ForEach(tuple =>
            {
                writer.Write(tuple.Item1.PlayerId);
                writer.Write((byte)tuple.Item2);
            });
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            int count = reader.ReadInt32();
            List<(PlayerControl, SystemTypes)> rooms = new(count);
            for (int i = 0; i < count; i++)
            {
                rooms.Add((Utils.GetPlayerById(reader.ReadByte()), (SystemTypes)reader.ReadByte()));
            }
            KillersAndRooms = rooms;
        }
        public static string GetMurderSceneText(PlayerControl seer)
        {
            if (!seer.IsAlive()) return "";
            var roomNames = from tuple in KillersAndRooms
                            where tuple.Item1 != seer  // 自身がキルしたものは除外
                            select DestroyableSingleton<TranslationController>.Instance.GetString(tuple.Item2);
            if (roomNames.Count() < 1) return "";
            return $"{GetString("EvilHackerMurderOccurred")}: {string.Join(", ", roomNames)}";
        }
        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target) =>
            CanSeeKillFlash.GetBool() && Utils.IsImpostorKill(killer, target);
    }
}
