using HarmonyLib;
using System.Collections.Generic;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    class AddTasksFromListPatch
    {
        public static void Prefix(ShipStatus __instance,
        [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            List<NormalPlayerTask> disabledTasks = new List<NormalPlayerTask>();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task.TaskType == TaskTypes.SwipeCard && main.DisableSwipeCard) disabledTasks.Add(task);//カードタスク
                if (task.TaskType == TaskTypes.SubmitScan && main.DisableSubmitScan) disabledTasks.Add(task);//スキャンタスク
                if (task.TaskType == TaskTypes.UnlockSafe && main.DisableUnlockSafe) disabledTasks.Add(task);//金庫タスク
                if (task.TaskType == TaskTypes.UploadData && main.DisableUploadData) disabledTasks.Add(task);//アップロードタスク
                if (task.TaskType == TaskTypes.StartReactor && main.DisableStartReactor) disabledTasks.Add(task);//リアクターの3x3タスク
                if (task.TaskType == TaskTypes.ResetBreakers && main.DisableResetBreaker) disabledTasks.Add(task);//レバータスク
            }
            foreach (var task in disabledTasks)
            {
                Logger.msg("削除: " + task.TaskType.ToString());
                unusedTasks.Remove(task);
            }
        }
    }
}
