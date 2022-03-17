using System.Collections.Generic;

namespace TownOfHost {
    public static class PlayerState {
        
        static PlayerState()
        {
            Init();
        }

        public static void Init()
        {
            players = new();
            isDead = new();
            deathReasons = new();
            isDead = new();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                players.Add(p.PlayerId);
                isDead.Add(p.PlayerId,false);
                deathReasons.Add(p.PlayerId,DeathReason.etc);
            }

        }
        public static List<byte> players = new List<byte>();
        public static Dictionary<byte,bool> isDead = new Dictionary<byte, bool>();
        public static Dictionary<byte,DeathReason> deathReasons = new Dictionary<byte, DeathReason>();
        public static void setDeathReason(byte p, DeathReason reason) { deathReasons[p] = reason; }
        public static DeathReason getDeathReason(byte p) { return deathReasons.TryGetValue(p,out var reason) ? reason : DeathReason.etc; }
        public static bool isSuicide(byte p) { return deathReasons[p] == DeathReason.Suicide; }
        
        public enum DeathReason
        {
            Kill,
            Vote,
            Suicide,
            Spell,
            Bite,
            Misfire,
            Disconnected,
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
