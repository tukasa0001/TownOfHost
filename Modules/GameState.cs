using System.Collections.Generic;

namespace TownOfHost {
    public static class PlayerState {
        
        static PlayerState()
        {
            Init();
        }

        public static void Init()
        {
            names = new();
            customRoles = new();
            deathReasons = new();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                names.Add(p.PlayerId, p.name);
                deathReasons.Add(p.PlayerId,DeathReason.Living);
            }
        }

        public static Dictionary<byte, string> names;
        public static Dictionary<byte, CustomRoles> customRoles;
        public static Dictionary<byte,DeathReason> deathReasons;

        public static void setDeathReason(byte p, DeathReason reason) { deathReasons[p] = reason; }
        public static DeathReason getDeathReason(byte p) { return deathReasons[p]; }
        public static bool isSuicide(byte p) { return deathReasons[p] == DeathReason.Suicide; }
        public static bool isDead(byte p) { return deathReasons[p] != DeathReason.Living; }

        public enum DeathReason
        {
            Living = 0,
            Kill,
            Vote,
            Suicide,
            Spell,
            Bite,
            etc = -1
        }
    }
    public class TaskState {
        public int AllTasksCount;
        public int CompletedTasksCount;
        public bool hasTasks;
        public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
        public bool doExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
        public bool isTaskFinished => RemainingTasksCount <= 0 && hasTasks;
        public TaskState(int all, int completed) {
            this.AllTasksCount = all;
            this.CompletedTasksCount = completed;
            this.hasTasks = true;
        }
        public TaskState() {
            this.AllTasksCount = 0;
            this.CompletedTasksCount = 0;
            this.hasTasks = false;
        }
    }
}
