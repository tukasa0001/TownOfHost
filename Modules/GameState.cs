using System.Collections.Generic;

namespace TownOfHost
{
    public static class PlayerState
    {

        static PlayerState()
        {
            Init();
        }

        public static void Init()
        {
            players = new();
            isDead = new();
            conditions = new();
            isDead = new();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                players.Add(p.PlayerId);
                isDead.Add(p.PlayerId, false);
                conditions.Add(p.PlayerId, Condition.etc);
            }

        }
        public static List<byte> players = new List<byte>();
        public static Dictionary<byte, bool> isDead = new Dictionary<byte, bool>();
        public static Dictionary<byte, Condition> conditions = new Dictionary<byte, Condition>();
        public static void setCondition(byte p, Condition reason) { conditions[p] = reason; }
        public static Condition getCondition(byte p) { return conditions.TryGetValue(p, out var reason) ? reason : Condition.etc; }
        public static bool isSuicide(byte p) { return conditions[p] == Condition.Suicide; }

        public enum Condition
        {
            Dead,
            Exiled,
            Suicide,
            Spelled,
            Bited,
            Bombed,
            Misfire,
            Disconnected,
            etc = -1
        }
    }
    public class TaskState
    {
        public int AllTasksCount;
        public int CompletedTasksCount;
        public bool hasTasks;
        public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
        public bool doExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
        public bool isTaskFinished => RemainingTasksCount <= 0 && hasTasks;
        public TaskState(int all, int completed)
        {
            this.AllTasksCount = all;
            this.CompletedTasksCount = completed;
            this.hasTasks = true;
        }
        public TaskState()
        {
            this.AllTasksCount = 0;
            this.CompletedTasksCount = 0;
            this.hasTasks = false;
        }
    }
}
