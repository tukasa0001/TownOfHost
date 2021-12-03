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
        public const string PluginVersion = "0.1";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        //Lang-Config
        //これらのconfigの値がlangTextsリストに入る
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
        //Lang-arrangement
        private static List<string> langTexts = new List<string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang) {
            return langTexts[(int)lang];
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
            if(target.Data.Role.Role == RoleTypes.Scientist && JesterEnabled)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Engineer && MadmateEnabled)
                return true;
            return false;
        }
        //Enabled Role
        public static bool JesterEnabled;
        public static bool MadmateEnabled;
        public static byte ExiledJesterID;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC() {
            if(!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write(JesterEnabled);
            writer.Write(MadmateEnabled);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public override void Load()
        {
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
            currentWinner = CustomWinner.Default;
            IsHideAndSeek = false;
            JesterEnabled = false;
            MadmateEnabled = false;
            langTexts.Add(Jester.Value);
            langTexts.Add(Madmate.Value);
            langTexts.Add(RoleEnabled.Value);
            langTexts.Add(RoleDisabled.Value);
            langTexts.Add(CommandError.Value);
            langTexts.Add(InvalidArgs.Value);
            langTexts.Add(roleListStart.Value);
            langTexts.Add(ON.Value);
            langTexts.Add(OFF.Value);
            langTexts.Add(JesterInfo.Value);
            langTexts.Add(MadmateInfo.Value);
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
        MadmateInfo
    }
    //WinData
    public enum CustomWinner {
        Draw = 0,
        Default,
        Jester
    }
}
