using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    class AddTasksFromListPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            if (!Options.DisableTasks.GetBool()) return;
            List<NormalPlayerTask> disabledTasks = new();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard.GetBool()) disabledTasks.Add(task);//カードタスク
                if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan.GetBool()) disabledTasks.Add(task);//スキャンタスク
                if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe.GetBool()) disabledTasks.Add(task);//金庫タスク
                if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData.GetBool()) disabledTasks.Add(task);//アップロードタスク
                if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor.GetBool()) disabledTasks.Add(task);//リアクターの3x3タスク
                if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker.GetBool()) disabledTasks.Add(task);//レバータスク
            }
            foreach (var task in disabledTasks)
            {
                Logger.Msg("削除: " + task.TaskType.ToString(), "AddTask");
                unusedTasks.Remove(task);
            }
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.RpcSetTasks))]
    class RpcSetTasksPatch
    {
        //タスクを割り当ててRPCを送る処理が行われる直前にタスクを上書きするPatch
        //バニラのタスク割り当て処理自体には干渉しない
        public static void Prefix(GameData __instance,
        [HarmonyArgument(0)] byte playerId,
        [HarmonyArgument(1)] ref UnhollowerBaseLib.Il2CppStructArray<byte> taskTypeIds)
        {
            //null対策
            if (Main.RealOptionsData == null)
            {
                Logger.Warn("警告:RealOptionsDataがnullです。", "RpcSetTasksPatch");
                return;
            }

            CustomRoles? RoleNullable = Utils.GetPlayerById(playerId)?.GetCustomRole();
            if (RoleNullable == null) return;
            CustomRoles role = RoleNullable.Value;

            if (!Options.OverrideTasksData.AllData.ContainsKey(role)) return;
            var data = Options.OverrideTasksData.AllData[role];

            bool doOverride = data.doOverride.GetBool(); // タスク数を上書きするかどうか
                                                         // falseの時、タスクの内容が変更される前にReturnされる。

            bool hasCommonTasks = data.assignCommonTasks.GetBool(); // コモンタスク(通常タスク)を割り当てるかどうか
                                                                    // 割り当てる場合でも再割り当てはされず、他のクルーと同じコモンタスクが割り当てられる。

            int NumLongTasks = (int)data.numLongTasks.GetFloat(); // 割り当てるロングタスクの数
            int NumShortTasks = (int)data.numShortTasks.GetFloat(); // 割り当てるショートタスクの数
                                                                    // ロングとショートは常時再割り当てが行われる。

            if (!doOverride) return;
            //割り当て可能なタスクのIDが入ったリスト
            //本来のRpcSetTasksの第二引数のクローン
            Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
            foreach (var num in taskTypeIds)
                TasksList.Add(num);

            //参考:ShipStatus.Begin
            //不要な割り当て済みのタスクを削除する処理
            //コモンタスクを割り当てる設定ならコモンタスク以外を削除
            //コモンタスクを割り当てない設定ならリストを空にする
            if (hasCommonTasks) TasksList.RemoveRange(Main.RealOptionsData.NumCommonTasks, TasksList.Count - Main.RealOptionsData.NumCommonTasks);
            else TasksList.Clear();

            //割り当て済みのタスクが入れられるHashSet
            //同じタスクが複数割り当てられるのを防ぐ
            Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
            int start2 = 0;
            int start3 = 0;

            //割り当て可能なロングタスクのリスト
            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
            foreach (var task in ShipStatus.Instance.LongTasks)
                LongTasks.Add(task);
            Shuffle<NormalPlayerTask>(LongTasks);

            //割り当て可能なショートタスクのリスト
            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
            foreach (var task in ShipStatus.Instance.NormalTasks)
                ShortTasks.Add(task);
            Shuffle<NormalPlayerTask>(ShortTasks);

            //実際にAmong Us側で使われているタスクを割り当てる関数を使う。
            ShipStatus.Instance.AddTasksFromList(
                ref start2,
                NumLongTasks,
                TasksList,
                usedTaskTypes,
                LongTasks
            );
            ShipStatus.Instance.AddTasksFromList(
                ref start3,
                NumShortTasks,
                TasksList,
                usedTaskTypes,
                ShortTasks
            );

            //タスクのリストを配列(Il2CppStructArray)に変換する
            taskTypeIds = new UnhollowerBaseLib.Il2CppStructArray<byte>(TasksList.Count);
            for (int i = 0; i < TasksList.Count; i++)
            {
                taskTypeIds[i] = TasksList[i];
            }

        }
        public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                T obj = list[i];
                int rand = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[rand];
                list[rand] = obj;
            }
        }
    }
}