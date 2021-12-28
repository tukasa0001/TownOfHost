using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using Hazel;

namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class main : BasePlugin
    {
        //Sorry for some Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "1.2";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        //Lang-Config
        //これらのconfigの値がlangTextsリストに入る
        public static ConfigEntry<string> Japanese {get; private set;}
        public static ConfigEntry<string> Jester {get; private set;}
        public static ConfigEntry<string> Madmate {get; private set;}
        public static ConfigEntry<string> RoleEnabled {get; private set;}
        public static ConfigEntry<string> RoleDisabled {get; private set;}
        public static ConfigEntry<string> CommandError {get; private set;}
        public static ConfigEntry<string> InvalidArgs {get; private set;}
        public static ConfigEntry<string> roleListStart {get; private set;}
        public static ConfigEntry<string> ON {get; private set;}
        public static ConfigEntry<string> OFF {get; private set;}
        public static ConfigEntry<string> JesterInfo {get; private set;}
        public static ConfigEntry<string> MadmateInfo {get; private set;}
        public static ConfigEntry<string> Bait {get; private set;}
        public static ConfigEntry<string> BaitInfo {get; private set;}
        public static ConfigEntry<string> Terrorist {get; private set;}
        public static ConfigEntry<string> TerroristInfo {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, ConfigEntry<string>> langTexts = new Dictionary<lang, ConfigEntry<string>>(){
            {lang.Jester, Jester},
            {lang.Madmate, Madmate},
            {lang.roleEnabled, RoleEnabled},
            {lang.roleDisabled, RoleDisabled},
            {lang.commandError, CommandError},
            {lang.InvalidArgs, InvalidArgs},
            {lang.roleListStart, roleListStart},
            {lang.ON, ON},
            {lang.OFF, OFF},
            {lang.JesterInfo, JesterInfo},
            {lang.MadmateInfo, MadmateInfo},
            {lang.Bait, Bait},
            {lang.BaitInfo, BaitInfo},
            {lang.Terrorist, Terrorist},
            {lang.TerroristInfo, TerroristInfo}
        };
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang) {
            var isSuccess = langTexts.TryGetValue(lang, out var entry);
            return isSuccess ? entry.Value : "<Not Found:" + lang.ToString() + ">";
        }
        //Other Configs
        public static ConfigEntry<bool> TeruteruColor {get; private set;}
        public static CustomWinner currentWinner;
        public static bool IsHideAndSeek;
        //色がTeruteruモードとJesterモードがある
        public static Color JesterColor() {
            if(TeruteruColor.Value)
                return new Color(0.823f,0.411f,0.117f);
            else 
                return new Color(0.925f,0.384f,0.647f);
        }
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isJester(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRole.Jester)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRole.Madmate)
                return true;
            return false;
        }

        public static bool isBait(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRole.Bait)
                return true;
            return false;
        }
        public static bool isTerrorist(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRole.Terrorist)
                return true;
            return false;
        }
        //Enabled Role
        public static ScientistRole currentScientist;
        public static EngineerRole currentEngineer;
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC() {
            if(!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write((byte)currentScientist);
            writer.Write((byte)currentEngineer);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist) {
            if(!AmongUsClient.Instance.AmHost) return;
            var isAllConpleted = true;
            foreach(var task in Terrorist.Tasks) {
                if(!task.Complete) isAllConpleted = false;
            }
            if(isAllConpleted) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TerroristWin, Hazel.SendOption.Reliable, -1);
                writer.Write(Terrorist.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.TerroristWin(Terrorist.PlayerId);
            }
        }
        public override void Load()
        {
            Japanese = Config.Bind("Info", "Japanese", "日本語");
            Jester = Config.Bind("Lang", "JesterName", "Jester");
            Madmate = Config.Bind("Lang", "MadmateName", "Madmate");
            RoleEnabled = Config.Bind("Lang", "RoleEnabled", "%1$ is Enabled.");
            RoleDisabled = Config.Bind("Lang", "RoleDisabled", "%1$ is Disabled.");
            CommandError = Config.Bind("Lang", "CommandError", "Error:%1$");
            InvalidArgs = Config.Bind("Lang", "InvalidArgs", "Invaild Arguments");
            roleListStart = Config.Bind("Lang", "roleListStart", "Role List");
            ON = Config.Bind("Lang", "ON", "ON");
            OFF = Config.Bind("Lang", "OFF", "OFF");
            JesterInfo = Config.Bind("Lang", "JesterInfo", "Get Voted Out To Win");
            MadmateInfo = Config.Bind("Lang", "MadmateInfo", "Help Impostors To Win");
            Bait = Config.Bind("Lang", "BaitName", "Bait");
            BaitInfo = Config.Bind("Lang", "BaitInfo", "Bait Your Enemies");
            Terrorist = Config.Bind("Lang", "TerroristName", "Terrorist");
            TerroristInfo = Config.Bind("Lang", "TerroristInfo", "Finish your tasks, then die");
            currentWinner = CustomWinner.Default;
            IsHideAndSeek = false;
            CustomWinTrigger = false;
            currentScientist = ScientistRole.Default;
            currentEngineer = EngineerRole.Default;
            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            Harmony.PatchAll();
        }
    }
    //Lang-enum
    public enum lang {
        Jester = 0,
        Madmate,
        roleEnabled,
        roleDisabled,
        commandError,
        InvalidArgs,
        roleListStart,
        ON,
        OFF,
        JesterInfo,
        MadmateInfo,
        Bait,
        BaitInfo,
        Terrorist,
        TerroristInfo
    }
    //WinData
    public enum CustomWinner {
        Draw = 0,
        Default,
        Jester,
        Terrorist
    }
    public enum ScientistRole {
        Default = 0,
        Jester,
        Bait
    }
    public enum EngineerRole {
        Default = 0,
        Madmate,
        Terrorist
    }
}
