using System;
using System.Collections.Generic;
namespace TownOfHost
{
    class LateTask
    {
        public string name;
        public float timer;
        public Action action;
        public static List<LateTask> Tasks = new List<LateTask>();
        public bool run(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0)
            {
                action();
                return true;
            }
            return false;
        }
        public LateTask(Action action, float time, string name = "No Name Task")
        {
            this.action = action;
            this.timer = time;
            this.name = name;
            Tasks.Add(this);
            Logger.info("New LateTask \"" + name + "\" is created");
        }
        public static void Update(float deltaTime)
        {
            var TasksToRemove = new List<LateTask>();
            Tasks.ForEach((task) =>
            {
                try
                {
                    if (task.run(deltaTime))
                    {
                        Logger.info($"\"{task.name}\" is finished", "LateTask");
                        TasksToRemove.Add(task);
                    }
                }
                catch (Exception ex)
                {
                    Logger.error($"{ex.GetType().ToString()}: {ex.Message}  in \"{task.name}\"", "LateTask.Error");
                    TasksToRemove.Add(task);
                }
            });
            TasksToRemove.ForEach(task => Tasks.Remove(task));
        }
    }
}
