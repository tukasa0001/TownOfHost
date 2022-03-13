using System.Collections.Generic;

namespace TownOfHost {
    public class PlayerState {
        //クラス内にリストなどを持つのではなく、リストにこのクラス変数を入れるように変更しました。
        public PlayerState(PlayerControl player)
        {
            this.player = player;
            name = player.name;
            roles = new();
            isDead = false;
            deathReason = DeathReason.etc;
        }
        public PlayerControl player;
        public byte playerId => player != null ? player.PlayerId : byte.MaxValue;
        public string name;
        public List<CustomRoles> roles;
        public bool hasRole()
        {
            return roles.Count != 0;
        }
        public void setRole(CustomRoles role,bool inGame=false)
        {
            if(!hasRole() || getLastRoles()!=role)
            {
                //ゲーム前なら差し替え
                if (!inGame) roles.Clear();
                roles.Add(role);
            }
        }
        public CustomRoles getLastRoles()
        {
            if (roles.Count == 0)
            {
                return CustomRoles.Default;
            }
            else
            {
                return roles[roles.Count - 1];
            }
        }
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
    static class ExtPlayState
    {
        static public bool TryGetValue(this Dictionary<byte, PlayerState> playerStates, byte playerId, out CustomRoles role)
        {
            if (!playerStates.ContainsKey(playerId))
            {
                role = CustomRoles.Default;
                return false;
            }
            var ps = playerStates[playerId];
            role = ps.getLastRoles();
            return ps.hasRole();
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
