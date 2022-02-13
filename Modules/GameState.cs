using System.Linq;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;

namespace TownOfHost {
    class PlayerState {
        
        public PlayerState()
        {
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                players.Add(p);
                isDead.Add(p,false);
                deathReasons.Add(p,DeathReason.etc);
            }
        }
        public List<PlayerControl> players = new List<PlayerControl>();
        public Dictionary<PlayerControl,bool> isDead = new Dictionary<PlayerControl, bool>();
        public Dictionary<PlayerControl,DeathReason> deathReasons = new Dictionary<PlayerControl, DeathReason>();
        public void setDeathReason(PlayerControl p, DeathReason reason) { deathReasons[p] = reason; }
        public DeathReason getDeathReason(PlayerControl p) { return deathReasons[p]; }
        public bool isSuicide(PlayerControl p) { return deathReasons[p] == DeathReason.Suicide; }
        
        public enum DeathReason
        {
            Kill,
            Vote,
            Suicide,
            etc = -1
        }
    }
    class TaskState {
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
