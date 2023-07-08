using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Modules;

public static class AdminProvider
{
    // ref: MapCountOverlay.Update
    /// <summary>
    /// 実行された時点でのアドミン情報を取得する
    /// </summary>
    /// <returns>Key: 部屋のSystemType, Value: <see cref="AdminEntry"/>で，Key順にソートされた辞書</returns>
    public static SortedDictionary<SystemTypes, AdminEntry> CalculateAdmin()
    {
        SortedDictionary<SystemTypes, AdminEntry> allAdmins = new();
        // 既にカウントされた人のPlayerIdを格納する
        // これに追加しようとしたときにfalseが返ってきたらカウントしないようにすることで，各プレイヤーが1回しかカウントされないようになっている
        HashSet<int> countedPlayers = new(15);
        // 検出された当たり判定の格納用に使い回す配列 変換時の負荷を回避するためIl2CppReferenceArrayで扱う
        Il2CppReferenceArray<Collider2D> colliders = new(45);
        // ref: MapCountOverlay.Awake
        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = Constants.LivingPlayersOnlyMask,
            useTriggers = true,
        };

        // 各部屋の人数カウント処理
        foreach (var room in ShipStatus.Instance.AllRooms)
        {
            var roomId = room.RoomId;
            // 通路か当たり判定がないなら何もしない
            if (roomId == SystemTypes.Hallway || room.roomArea == null)
            {
                continue;
            }
            // 検出された当たり判定の数 検出された当たり判定はここでcollidersに格納される
            var numColliders = room.roomArea.OverlapCollider(filter, colliders);
            // 実際にアドミンで表示される，死体も含めた全部の数
            var totalPlayers = 0;
            // 死体の数
            var numDeadBodies = 0;
            // インポスターの数
            var numImpostors = 0;

            // 検出された各当たり判定への処理
            for (var i = 0; i < numColliders; i++)
            {
                var collider = colliders[i];
                // おにくの場合
                if (collider.CompareTag("DeadBody"))
                {
                    var deadBody = collider.GetComponent<DeadBody>();
                    if (deadBody != null && countedPlayers.Add(deadBody.ParentId))
                    {
                        totalPlayers++;
                        numDeadBodies++;
                        // インポスターの死体だった場合
                        if (Utils.GetPlayerById(deadBody.ParentId)?.Is(CustomRoleTypes.Impostor) == true)
                        {
                            numImpostors++;
                        }
                    }
                }
                // 生きてる場合
                else if (!collider.isTrigger)
                {
                    var playerControl = collider.GetComponent<PlayerControl>();
                    if (playerControl.IsAlive() && countedPlayers.Add(playerControl.PlayerId))
                    {
                        totalPlayers++;
                        // インポスターだった場合
                        if (playerControl.Is(CustomRoleTypes.Impostor))
                        {
                            numImpostors++;
                        }
                    }
                }
            }

            allAdmins[roomId] = new()
            {
                Room = roomId,
                TotalPlayers = totalPlayers,
                NumDeadBodies = numDeadBodies,
                NumImpostors = numImpostors,
            };
        }
        return allAdmins;
    }

    public readonly record struct AdminEntry
    {
        /// <summary>対象の部屋</summary>
        public SystemTypes Room { get; init; }
        /// <summary>部屋の中にいるプレイヤーの合計
        public int TotalPlayers { get; init; }
        /// <summary>部屋の中にある死体の数</summary>
        public int NumDeadBodies { get; init; }
        /// <summary>部屋の中にインポスターがいるかどうか</summary>
        public int NumImpostors { get; init; }
    }
}
