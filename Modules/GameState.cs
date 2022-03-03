using System.Collections.Generic;

namespace TownOfHost {
    public class PlayerState {
        //クラス内にリストなどを持つのではなく、リストにこのクラス変数を入れるように変更しました。
        public PlayerState(PlayerControl player)
        {
            this.player = player;
        }
        public PlayerControl player;
        public bool isDead;
        public DeathReason deathReason;
        public bool isSuicide() { return deathReason == DeathReason.Suicide; }
        
        public enum DeathReason
        {
            Kill,
            Vote,
            Suicide,
            Spell,
            etc = -1
        }
    }
    public class TaskState {
        public int AllTasksCount;
        public int CompletedTasksCount;
        public bool hasTasks;
        public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
        public bool doExpose => RemainingTasksCount <= main.SnitchExposeTaskLeft && hasTasks;
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
