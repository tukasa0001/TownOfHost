using System;
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
            deathReasons = new();
            taskState = new();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                players.Add(p.PlayerId);
                isDead.Add(p.PlayerId, false);
                deathReasons.Add(p.PlayerId, DeathReason.etc);
                taskState.Add(p.PlayerId, new());
            }

        }
        public static List<byte> players = new List<byte>();
        public static Dictionary<byte, bool> isDead = new Dictionary<byte, bool>();
        public static Dictionary<byte, DeathReason> deathReasons = new Dictionary<byte, DeathReason>();
        public static Dictionary<byte, TaskState> taskState = new();
        public static void setDeathReason(byte p, DeathReason reason) { deathReasons[p] = reason; }
        public static DeathReason getDeathReason(byte p) { return deathReasons.TryGetValue(p, out var reason) ? reason : DeathReason.etc; }
        public static void setDead(byte p)
        {
            isDead[p] = true;
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.SendDeathReason(p, deathReasons[p]);
            }
        }
        public static bool isSuicide(byte p) { return deathReasons[p] == DeathReason.Suicide; }
        public static void InitTask(PlayerControl player)
        {
            taskState[player.PlayerId].Init(player);
        }
        public static void UpdateTask(PlayerControl player)
        {
            taskState[player.PlayerId].Update(player);
        }
        public enum DeathReason
        {
            Kill,
            Vote,
            Suicide,
            Spell,
            Bite,
            Bombed,
            Misfire,
            Torched,
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
        public TaskState()
        {
            this.AllTasksCount = -1;
            this.CompletedTasksCount = 0;
            this.hasTasks = false;
        }

        public void Init(PlayerControl player)
        {
            Logger.info($"{player.name}: InitTask", "TaskCounts");
            if (player == null || player.Data == null || player.Data.Tasks == null) return;
            if (!Utils.hasTasks(player.Data, false)) return;
            hasTasks = true;
            AllTasksCount = player.Data.Tasks.Count;

            //役職ごとにタスク量の調整を行う
            var adjustedTasksCount = AllTasksCount;
            switch (player.getCustomRole())
            {
                case CustomRoles.MadSnitch:
                    adjustedTasksCount = Options.MadSnitchTasks.GetInt();
                    break;
                default:
                    break;
            }
            //タスク数が通常タスクより多い場合は再設定が必要
            AllTasksCount = Math.Min(adjustedTasksCount, AllTasksCount);
            Logger.info($"{player.name}: {CompletedTasksCount}/{AllTasksCount}", "TaskCounts");
        }
        public void Update(PlayerControl player)
        {
            Logger.info($"{player.name}: UpdateTask", "TaskCounts");
            if (!hasTasks) return;
            //初期化出来ていなかったら初期化
            if (AllTasksCount == -1) Init(player);
            //クリアしてたらカウントしない
            if (CompletedTasksCount >= AllTasksCount) return;

            CompletedTasksCount++;

            //調整後のタスク量までしか表示しない
            CompletedTasksCount = Math.Min(AllTasksCount, CompletedTasksCount);
            Logger.info($"{player.name}: {CompletedTasksCount}/{AllTasksCount}", "TaskCounts");

        }
    }
    public static class GameStates
    {
        public static bool isLobby => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Joined;
        public static bool isInGame => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Started;
        public static bool isEnded => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Ended;
        public static bool isNotJoined => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.NotJoined;
        public static bool isOnlineGame => AmongUsClient.Instance.GameMode == GameModes.OnlineGame;
        public static bool isLocalGame => AmongUsClient.Instance.GameMode == GameModes.LocalGame;
        public static bool isFreePlay => AmongUsClient.Instance.GameMode == GameModes.FreePlay;
        public static bool isMeeting => MeetingHud.Instance;
        public static bool isCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    }
}
