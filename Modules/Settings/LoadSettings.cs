using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AmongUs.GameOptions;
using TownOfHost.Modules.Extensions;

namespace TownOfHost.Modules.Settings;

public static class LoadSettings
{
    public static List<BoolOptionNames> BoolSettingNames = new()
    {
        BoolOptionNames.VisualTasks,
        BoolOptionNames.GhostsDoTasks,
        BoolOptionNames.ConfirmImpostor,
        BoolOptionNames.AnonymousVotes,
        BoolOptionNames.IsDefaults,
        BoolOptionNames.UseFlashlight,
        BoolOptionNames.SeekerFinalVents,
        BoolOptionNames.SeekerFinalMap,
        BoolOptionNames.SeekerPings,
        BoolOptionNames.ShowCrewmateNames,
        BoolOptionNames.ShapeshifterLeaveSkin,
        BoolOptionNames.ImpostorsCanSeeProtect,
    };

    public static List<Int32OptionNames> IntSettingNames = new()
    {
        Int32OptionNames.NumImpostors,
        Int32OptionNames.KillDistance,
        Int32OptionNames.NumEmergencyMeetings,
        Int32OptionNames.EmergencyCooldown,
        Int32OptionNames.DiscussionTime,
        Int32OptionNames.VotingTime,
        Int32OptionNames.MaxImpostors,
        Int32OptionNames.MinPlayers,
        Int32OptionNames.MaxPlayers,
        Int32OptionNames.NumCommonTasks,
        Int32OptionNames.NumShortTasks,
        Int32OptionNames.NumLongTasks,
        Int32OptionNames.TaskBarMode,
        Int32OptionNames.CrewmatesRemainingForVitals,
        Int32OptionNames.CrewmateVentUses,
        Int32OptionNames.ImpostorPlayerID,
    };

    public static List<FloatOptionNames> FloatSettingNames = new()
    {
        FloatOptionNames.KillCooldown,
        FloatOptionNames.PlayerSpeedMod,
        FloatOptionNames.ImpostorLightMod,
        FloatOptionNames.CrewLightMod,
        FloatOptionNames.CrewmateTimeInVent,
        FloatOptionNames.FinalEscapeTime,
        FloatOptionNames.EscapeTime,
        FloatOptionNames.SeekerFinalSpeed,
        FloatOptionNames.MaxPingTime,
        FloatOptionNames.CrewmateFlashlightSize,
        FloatOptionNames.ImpostorFlashlightSize,
        FloatOptionNames.ShapeshifterCooldown,
        FloatOptionNames.ShapeshifterDuration,
        FloatOptionNames.ProtectionDurationSeconds,
        FloatOptionNames.GuardianAngelCooldown,
        FloatOptionNames.ScientistCooldown,
        FloatOptionNames.ScientistBatteryCharge,
        FloatOptionNames.EngineerCooldown,
        FloatOptionNames.EngineerInVentMaxTime,
    };

    public static List<ByteOptionNames> ByteSettingNames = new()
    {
        ByteOptionNames.MapId,
    };

    public static IGameOptions CurrentOptions => GameOptionsManager.Instance.currentGameOptions;

    public static void Load()
    {
        if (!File.Exists(@$"./{SaveSettings.SETTINGS_FOLDER}/GameSettings.xml"))
        {
            Logger.Warn("設定ファイルが存在しません。", "LoadSettings");
            return;
        }

        XmlDocument xml = new();
        xml.Load(@$"./{SaveSettings.SETTINGS_FOLDER}/GameSettings.xml");
        XmlElement settings = xml.DocumentElement;

        for (int i = 0; settings.ChildNodes.Count > i; i++)
        {
            XmlElement type = (XmlElement)settings.ChildNodes[i].ChildNodes[0];
            switch (type.FirstChild.Value)
            {
                case "Boolean":
                    XmlElement boolValue = (XmlElement)settings.ChildNodes[i].ChildNodes[1];
                    BoolSettingNames[i].Set(GetBool(boolValue.FirstChild.Value), CurrentOptions);
                    Logger.Info($"Boolean設定番号{i}を読み込みました。", "LoadSettings");
                    break;
                case "Int32":
                    XmlElement intValue = (XmlElement)settings.ChildNodes[i].ChildNodes[1];
                    IntSettingNames[i - 12].Set(GetInt(intValue.FirstChild.Value), CurrentOptions);
                    Logger.Info($"Int32設定番号{i}を読み込みました。", "LoadSettings");
                    break;
                case "Float":
                    XmlElement floatValue = (XmlElement)settings.ChildNodes[i].ChildNodes[1];
                    FloatSettingNames[i - 28].Set(GetFloat(floatValue.FirstChild.Value), CurrentOptions);
                    Logger.Info($"Float設定番号{i}を読み込みました。", "LoadSettings");
                    break;
                case "Byte":
                    XmlElement byteValue = (XmlElement)settings.ChildNodes[i].ChildNodes[1];
                    ByteSettingNames[i - 47].Set(GetByte(byteValue.FirstChild.Value), CurrentOptions);
                    Logger.Info($"Byte設定番号{i}を読み込みました。", "LoadSettings");
                    break;
                default:
                    Logger.Warn($"設定番号{i}の値の種類が不適切です。", "LoadSettings");
                    break;
            }
        }
        GameManager.Instance.LogicOptions.SyncOptions();

        Logger.Info("設定が正常に読み込まれました。", "LoadSettings");
        Logger.SendInGame(string.Format(Translator.GetString("SettingsLoaded")), false);
    }

    public static bool GetBool(this string value)
    {
        return bool.Parse(value);
    }
    public static int GetInt(this string value)
    {
        return int.Parse(value);
    }
    public static float GetFloat(this string value)
    {
        return float.Parse(value);
    }
    public static byte GetByte(this string value)
    {
        return byte.Parse(value);
    }
}