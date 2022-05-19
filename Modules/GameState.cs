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
        public static List<byte> players = new();
        public static Dictionary<byte, bool> isDead = new();
        public static Dictionary<byte, DeathReason> deathReasons = new();
        public static Dictionary<byte, TaskState> taskState = new();
        public static void SetDeathReason(byte p, DeathReason reason) { deathReasons[p] = reason; }
        public static DeathReason GetDeathReason(byte p) { return deathReasons.TryGetValue(p, out var reason) ? reason : DeathReason.etc; }
        public static void SetDead(byte p)
        {
            isDead[p] = true;
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.SendDeathReason(p, deathReasons[p]);
            }
        }
        public static bool IsSuicide(byte p) { return deathReasons[p] == DeathReason.Suicide; }
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
            LoversSuicide,
            Bite,
            Bombed,
            Misfire,
            Torched,
            Sniped,
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
        public bool DoExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
        public bool IsTaskFinished => RemainingTasksCount <= 0 && hasTasks;
        public TaskState()
        {
            this.AllTasksCount = -1;
            this.CompletedTasksCount = 0;
            this.hasTasks = false;
        }

        public void Init(PlayerControl player)
        {
            Logger.Info($"{player.GetNameWithRole()}: InitTask", "TaskCounts");
            if (player == null || player.Data == null || player.Data.Tasks == null) return;
            if (!Utils.HasTasks(player.Data, false)) return;
            hasTasks = true;
            AllTasksCount = player.Data.Tasks.Count;
            Logger.Info($"{player.GetNameWithRole()}: {CompletedTasksCount}/{AllTasksCount}", "TaskCounts");
        }
        public void Update(PlayerControl player)
        {
            Logger.Info($"{player.GetNameWithRole()}: UpdateTask", "TaskCounts");
            if (!hasTasks) return;
            //初期化出来ていなかったら初期化
            if (AllTasksCount == -1) Init(player);
            //クリアしてたらカウントしない
            if (CompletedTasksCount >= AllTasksCount) return;

            CompletedTasksCount++;

            //調整後のタスク量までしか表示しない
            CompletedTasksCount = Math.Min(AllTasksCount, CompletedTasksCount);
            Logger.Info($"{player.GetNameWithRole()}: {CompletedTasksCount}/{AllTasksCount}", "TaskCounts");

        }
    }
    public static class GameStates
    {
        public static bool InGame = false;
        public static bool IsLobby => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Joined;
        public static bool IsInGame => InGame;
        public static bool IsEnded => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Ended;
        public static bool IsNotJoined => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.NotJoined;
        public static bool IsOnlineGame => AmongUsClient.Instance.GameMode == GameModes.OnlineGame;
        public static bool IsLocalGame => AmongUsClient.Instance.GameMode == GameModes.LocalGame;
        public static bool IsFreePlay => AmongUsClient.Instance.GameMode == GameModes.FreePlay;
        public static bool IsInTask => InGame && !MeetingHud.Instance;
        public static bool IsMeeting => InGame && MeetingHud.Instance;
        public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    }
}