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
        public const string PluginVersion = "1.3";
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
        public static ConfigEntry<string> Sidekick {get; private set;}
        public static ConfigEntry<string> SidekickInfo {get; private set;}
        public static ConfigEntry<string> Vampire {get; private set;}
        public static ConfigEntry<string> VampireInfo {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, string> langTexts = new Dictionary<lang, string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang) {
            var isSuccess = langTexts.TryGetValue(lang, out var text    );
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        //Other Configs
        public static ConfigEntry<bool> TeruteruColor {get; private set;}
        public static CustomWinner currentWinner;
        public static bool IsHideAndSeek;
        public static bool NoGameEnd;
        public static bool OptionControllerIsEnable;
        //色がTeruteruモードとJesterモードがある
        public static Color JesterColor() {
            if(TeruteruColor.Value)
                return new Color(0.823f,0.411f,0.117f);
            else 
                return new Color(0.925f,0.384f,0.647f);
        }
        public static Color VampireColor = new Color(0.65f,0.34f,0.65f);
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => currentImpostor == ImpostorRoles.Vampire;
        public static float BeforeFixCooldown = -1f;
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
        public static bool isSidekick(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Shapeshifter && currentShapeshifter == ShapeshifterRoles.Sidekick)
                return true;
            return false;
        }
        public static bool isVampire(PlayerControl target) {
            if(target.Data.Role.Role == RoleTypes.Impostor && currentImpostor == ImpostorRoles.Vampire)
                return true;
            return false;
        }
        public static void ToggleRole(ScientistRole role) {
            currentScientist = role == currentScientist ? ScientistRole.Default : role;
        }
        public static void ToggleRole(EngineerRole role) {
            currentEngineer = role == currentEngineer ? EngineerRole.Default : role;
        }
        public static void ToggleRole(ShapeshifterRoles role) {
            currentShapeshifter = role == currentShapeshifter ? ShapeshifterRoles.Default : role;
        }
        public static void ToggleRole(ImpostorRoles role) {
            currentImpostor = role == currentImpostor ? ImpostorRoles.Default : role;
        }
        //Enabled Role
        public static ScientistRole currentScientist;
        public static EngineerRole currentEngineer;
        public static ImpostorRoles currentImpostor;
        public static ShapeshifterRoles currentShapeshifter;
        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC() {
            if(!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write((byte)currentScientist);
            writer.Write((byte)currentEngineer);
            writer.Write((byte)currentImpostor);
            writer.Write((byte)currentShapeshifter);
            writer.Write(IsHideAndSeek);
            writer.Write(NoGameEnd);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound) {
            if(AmongUsClient.Instance.AmHost)
                RPCProcedure.PlaySound(PlayerID,sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
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
            Sidekick = Config.Bind("Lang", "SidekickName", "Sidekick");
            SidekickInfo = Config.Bind("Lang", "SidekickInfo", "You are Sidekick");
            Vampire = Config.Bind("Lang", "VampireName", "Vampire");
            VampireInfo = Config.Bind("Lang", "VampireInfo", "Kill all crewmates with your bites");

            currentWinner = CustomWinner.Default;
            IsHideAndSeek = false;
            NoGameEnd = false;
            CustomWinTrigger = false;
            OptionControllerIsEnable = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();

            currentScientist = ScientistRole.Default;
            currentEngineer = EngineerRole.Default;
            currentImpostor = ImpostorRoles.Default;
            currentShapeshifter = ShapeshifterRoles.Default;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);

            langTexts = new Dictionary<lang, string>(){
                {lang.Jester, Jester.Value},
                {lang.Madmate, Madmate.Value},
                {lang.roleEnabled, RoleEnabled.Value},
                {lang.roleDisabled, RoleDisabled.Value},
                {lang.commandError, CommandError.Value},
                {lang.InvalidArgs, InvalidArgs.Value},
                {lang.roleListStart, roleListStart.Value},
                {lang.ON, ON.Value},
                {lang.OFF, OFF.Value},
                {lang.JesterInfo, JesterInfo.Value},
                {lang.MadmateInfo, MadmateInfo.Value},
                {lang.Bait, Bait.Value},
                {lang.BaitInfo, BaitInfo.Value},
                {lang.Terrorist, Terrorist.Value},
                {lang.TerroristInfo, TerroristInfo.Value},
                {lang.Sidekick, Sidekick.Value},
                {lang.SidekickInfo, SidekickInfo.Value},
                {lang.Vampire, Vampire.Value},
                {lang.VampireInfo, VampireInfo.Value}
            };

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
        TerroristInfo,
        Sidekick,
        SidekickInfo,
        Vampire,
        VampireInfo
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
    public enum ImpostorRoles {
        Default = 0,
        Vampire
    }
    public enum ShapeshifterRoles {
        Default = 0,
        Sidekick
    }
}
