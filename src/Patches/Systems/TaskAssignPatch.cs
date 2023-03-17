using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles;
using TOHTOR.Roles.RoleGroups.Vanilla;
using VentLib.Logging;

namespace TOHTOR.Patches.Systems;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
class AddTasksFromListPatch
{
    public static void Prefix(ShipStatus __instance,
        [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
    {
        if (!StaticOptions.DisableTasks) return;
        List<NormalPlayerTask> disabledTasks = new();
        for (var i = 0; i < unusedTasks.Count; i++)
        {
            var task = unusedTasks[i];
            if (task.TaskType == TaskTypes.SwipeCard && StaticOptions.DisableSwipeCard) disabledTasks.Add(task);//カードタスク
            if (task.TaskType == TaskTypes.SubmitScan && StaticOptions.DisableSubmitScan) disabledTasks.Add(task);//スキャンタスク
            if (task.TaskType == TaskTypes.UnlockSafe && StaticOptions.DisableUnlockSafe) disabledTasks.Add(task);//金庫タスク
            if (task.TaskType == TaskTypes.UploadData && StaticOptions.DisableUploadData) disabledTasks.Add(task);//アップロードタスク
            if (task.TaskType == TaskTypes.StartReactor && StaticOptions.DisableStartReactor) disabledTasks.Add(task);//リアクターの3x3タスク
            if (task.TaskType == TaskTypes.ResetBreakers && StaticOptions.DisableResetBreaker) disabledTasks.Add(task);//レバータスク
        }
        foreach (var task in disabledTasks)
        {
            VentLogger.Info("削除: " + task.TaskType.ToString(), "AddTask");
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
        [HarmonyArgument(1)] ref Il2CppStructArray<byte> taskTypeIds)
    {

        CustomRole role = Utils.GetPlayerById(playerId)?.GetCustomRole();
        if (role is not Crewmate { HasOverridenTasks: true } crewmate) return;

        bool hasCommonTasks = crewmate.HasCommonTasks; // コモンタスク(通常タスク)を割り当てるかどうか
                                                                // 割り当てる場合でも再割り当てはされず、他のクルーと同じコモンタスクが割り当てられる。

        //本来のRpcSetTasksの第二引数のクローン
        Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
        foreach (var num in taskTypeIds)
            TasksList.Add(num);

        //参考:ShipStatus.Begin
        //不要な割り当て済みのタスクを削除する処理
        //コモンタスクを割り当てる設定ならコモンタスク以外を削除
        //コモンタスクを割り当てない設定ならリストを空にする
        if (hasCommonTasks) TasksList.RemoveRange(DesyncOptions.OriginalHostOptions.AsNormalOptions()!.NumCommonTasks, TasksList.Count - DesyncOptions.OriginalHostOptions.AsNormalOptions()!.NumCommonTasks);
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
        Shuffle(LongTasks);

        //割り当て可能なショートタスクのリスト
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
        foreach (var task in ShipStatus.Instance.NormalTasks)
            ShortTasks.Add(task);
        Shuffle(ShortTasks);

        //実際にAmong Us側で使われているタスクを割り当てる関数を使う。
        ShipStatus.Instance.AddTasksFromList(
            ref start2,
            crewmate.LongTasks,
            TasksList,
            usedTaskTypes,
            LongTasks
        );
        ShipStatus.Instance.AddTasksFromList(
            ref start3,
            !hasCommonTasks && crewmate.ShortTasks == 0 && crewmate.LongTasks == 0 ? 1 : crewmate.ShortTasks,
            TasksList,
            usedTaskTypes,
            LongTasks
        );

        //タスクのリストを配列(Il2CppStructArray)に変換する
        taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
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
            list[i] = list[ rand];
            list[rand] = obj;
        }
    }
}