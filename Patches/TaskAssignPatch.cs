using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

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
                if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard) disabledTasks.Add(task);//カードタスク
                if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan) disabledTasks.Add(task);//スキャンタスク
                if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe) disabledTasks.Add(task);//金庫タスク
                if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData) disabledTasks.Add(task);//アップロードタスク
                if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor) disabledTasks.Add(task);//リアクターの3x3タスク
                if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker) disabledTasks.Add(task);//レバータスク
            }
            foreach (var task in disabledTasks)
            {
                Logger.msg("削除: " + task.TaskType.ToString());
                unusedTasks.Remove(task);
            }
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.RpcSetTasks))]
    class RpcSetTasksPatch {
        public static void Prefix(GameData __instance,
        [HarmonyArgument(0)] byte playerId,
        [HarmonyArgument(1)] ref UnhollowerBaseLib.Il2CppStructArray<byte> taskTypeIds) {
            //null対策
            if(main.RealOptionsData == null) {
                Logger.warn("警告:RealOptionsDataがnullです。(RppcSetTasksPatch.Prefix)");
                return;
            }

            CustomRoles? RoleNullable = Utils.getPlayerById(playerId)?.getCustomRole();
            CustomRoles role = CustomRoles.Crewmate;
            if(RoleNullable == null) return;
            else role = RoleNullable.Value;
            
            bool doOverride = false;

            bool hasCommonTasks = true;
            int NumLongTasks = main.RealOptionsData.NumLongTasks;
            int NumShortTasks = main.RealOptionsData.NumShortTasks;
            
            Options.MadGuardianTasksData.CheckAndSet(role, ref doOverride, ref hasCommonTasks, ref NumLongTasks, ref NumShortTasks);
            Options.TerroristTasksData.CheckAndSet(role, ref doOverride, ref hasCommonTasks, ref NumLongTasks, ref NumShortTasks);
            Options.SnitchTasksData.CheckAndSet(role, ref doOverride, ref hasCommonTasks, ref NumLongTasks, ref NumShortTasks);
            Options.MadSnitchTasksData.CheckAndSet(role, ref doOverride, ref hasCommonTasks, ref NumLongTasks, ref NumShortTasks);

            if(!doOverride) return;
            Il2CppSystem.Collections.Generic.List<byte> TasksList = new Il2CppSystem.Collections.Generic.List<byte>();
            foreach(var num in taskTypeIds)
                TasksList.Add(num);
            
            //参考:ShipStatus.Begin
            if(hasCommonTasks) TasksList.RemoveRange(main.RealOptionsData.NumCommonTasks, TasksList.Count - main.RealOptionsData.NumCommonTasks);
            else TasksList.Clear();

            Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();
            int start2 = 0;
            int start3 = 0;

            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach(var task in ShipStatus.Instance.LongTasks)
                LongTasks.Add(task);
            Shuffle<NormalPlayerTask>(LongTasks);

            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach(var task in ShipStatus.Instance.NormalTasks)
                ShortTasks.Add(task);
            Shuffle<NormalPlayerTask>(ShortTasks);

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

            taskTypeIds = new UnhollowerBaseLib.Il2CppStructArray<byte>(TasksList.Count);
            for(int i = 0; i < TasksList.Count; i++) {
                taskTypeIds[i] = TasksList[i];
            }

        }
        public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list) {
            for(int i = 0; i < list.Count - 1; i++) {
                T obj = list[i];
                int rand = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[rand];
                list[rand] = obj;
            }
        }
    }
}
