using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AmongUs.GameOptions;

namespace TownOfHost.Modules;

public class BoolSettings
{
    public string Name { get; set; }
    public bool BoolValue { get; set; }
}

public class IntSettings
{
    public string Name { get; set; }
    public int IntValue { get; set; }
}

public class FloatSettings
{
    public string Name { get; set; }
    public float FloatValue { get; set; }
}

public class ByteSettings
{
    public string Name { get; set; }
    public byte ByteValue { get; set; }
}

public static class SaveSettings
{
    public const string SETTINGS_FOLDER = "Settings";

    public static void Generate()
    {
        // リストたち
        List<BoolSettings> BoolSettingsList = new()
        {
            new(){ Name = "VisualTasks", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.VisualTasks) },
            new(){ Name = "GhostsDoTasks", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.GhostsDoTasks) },
            new(){ Name = "ConfirmImpostor", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.ConfirmImpostor) },
            new(){ Name = "AnonymousVotes", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.AnonymousVotes) },
            new(){ Name = "IsDefaults", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.IsDefaults) },
            new(){ Name = "UseFlashlight", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.UseFlashlight) },
            new(){ Name = "SeekerFinalVents", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.SeekerFinalVents) },
            new(){ Name = "SeekerFinalMap", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.SeekerFinalMap) },
            new(){ Name = "SeekerPings", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.SeekerPings) },
            new(){ Name = "ShowCrewmateNames", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.ShowCrewmateNames) },
            new(){ Name = "ShapeshifterLeaveSkin", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.ShapeshifterLeaveSkin) },
            new(){ Name = "ImpostorsCanSeeProtect", BoolValue = GameOptionsManager.Instance.currentGameOptions.GetBool(BoolOptionNames.ImpostorsCanSeeProtect) },
        };

        List<IntSettings> IntSettingsList = new()
        {
            new(){ Name = "NumImpostors", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.NumImpostors) },
            new(){ Name = "KillDistance", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.KillDistance) },
            new(){ Name = "NumEmergencyMeetings", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.NumEmergencyMeetings) },
            new(){ Name = "EmergencyCooldown", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.EmergencyCooldown) },
            new(){ Name = "DiscussionTime", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.DiscussionTime) },
            new(){ Name = "VotingTime", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.VotingTime) },
            new(){ Name = "MaxImpostors", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.MaxImpostors) },
            new(){ Name = "MinPlayers", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.MinPlayers) },
            new(){ Name = "MaxPlayers", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.MaxPlayers) },
            new(){ Name = "NumCommonTasks", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.NumCommonTasks) },
            new(){ Name = "NumShortTasks", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.NumShortTasks) },
            new(){ Name = "NumLongTasks", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.NumLongTasks) },
            new(){ Name = "TaskBarMode", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.TaskBarMode) },
            new(){ Name = "CrewmatesRemainingForVitals", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.CrewmatesRemainingForVitals) },
            new(){ Name = "CrewmateVentUses", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.CrewmateVentUses) },
            new(){ Name = "ImpostorPlayerID", IntValue = GameOptionsManager.Instance.currentGameOptions.GetInt(Int32OptionNames.ImpostorPlayerID) },
        };

        List<FloatSettings> FloatSettingsList = new()
        {
            new(){ Name = "KillCooldown", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.KillCooldown) },
            new(){ Name = "PlayerSpeedMod", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod) },
            new(){ Name = "ImpostorLightMod", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod) },
            new(){ Name = "CrewLightMod", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.CrewLightMod) },
            new(){ Name = "CrewmateTimeInVent", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.CrewmateTimeInVent) },
            new(){ Name = "FinalEscapeTime", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.FinalEscapeTime) },
            new(){ Name = "EscapeTime", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.EscapeTime) },
            new(){ Name = "SeekerFinalSpeed", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.SeekerFinalSpeed) },
            new(){ Name = "MaxPingTime", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.MaxPingTime) },
            new(){ Name = "CrewmateFlashlightSize", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.CrewmateFlashlightSize) },
            new(){ Name = "ImpostorFlashlightSize", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ImpostorFlashlightSize) },
            new(){ Name = "ShapeshifterCooldown", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ShapeshifterCooldown) },
            new(){ Name = "ShapeshifterDuration", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ShapeshifterDuration) },
            new(){ Name = "ProtectionDurationSeconds", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ProtectionDurationSeconds) },
            new(){ Name = "GuardianAngelCooldown", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.GuardianAngelCooldown) },
            new(){ Name = "ScientistCooldown", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ScientistCooldown) },
            new(){ Name = "ScientistBatteryCharge", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.ScientistBatteryCharge) },
            new(){ Name = "EngineerCooldown", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.EngineerCooldown) },
            new(){ Name = "EngineerInVentMaxTime", FloatValue = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.EngineerInVentMaxTime) },
        };

        List<ByteSettings> ByteSettingsList = new()
        {
            new(){ Name = "MapId", ByteValue = GameOptionsManager.Instance.currentGameOptions.GetByte(ByteOptionNames.MapId) },
        };

        // ルート要素
        XmlDocument xmlDocument = new();
        XmlDeclaration declaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
        XmlElement element = xmlDocument.CreateElement("settings");
        xmlDocument.AppendChild(declaration);
        xmlDocument.AppendChild(element);

        // Bool
        foreach (var boolSetting in BoolSettingsList)
        {
            // ルート要素の今年て属性付きの設定を追加
            XmlElement settingElement = xmlDocument.CreateElement("setting");
            settingElement.SetAttribute("name", boolSetting.Name);
            element.AppendChild(settingElement);

            // 設定の値の種類
            XmlElement ValueType = xmlDocument.CreateElement("type");
            settingElement.AppendChild(ValueType);

            XmlNode valueType = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            valueType.Value = "Boolean";
            ValueType.AppendChild(valueType);

            // 設定の値
            XmlElement Value = xmlDocument.CreateElement("value");
            settingElement.AppendChild(Value);

            XmlNode value = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            value.Value = boolSetting.BoolValue.ToString().ToLower();
            Value.AppendChild(value);
        }

        // Int
        foreach (var intSetting in IntSettingsList)
        {
            // ルート要素の今年て属性付きの設定を追加
            XmlElement settingElement = xmlDocument.CreateElement("setting");
            settingElement.SetAttribute("name", intSetting.Name);
            element.AppendChild(settingElement);

            // 設定の値の種類
            XmlElement ValueType = xmlDocument.CreateElement("type");
            settingElement.AppendChild(ValueType);

            XmlNode valueType = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            valueType.Value = "Int32";
            ValueType.AppendChild(valueType);

            // 設定の値
            XmlElement Value = xmlDocument.CreateElement("value");
            settingElement.AppendChild(Value);

            XmlNode value = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            value.Value = intSetting.IntValue.ToString();
            Value.AppendChild(value);
        }

        // Float
        foreach (var floatSetting in FloatSettingsList)
        {
            // ルート要素の今年て属性付きの設定を追加
            XmlElement settingElement = xmlDocument.CreateElement("setting");
            settingElement.SetAttribute("name", floatSetting.Name);
            element.AppendChild(settingElement);

            // 設定の値の種類
            XmlElement ValueType = xmlDocument.CreateElement("type");
            settingElement.AppendChild(ValueType);

            XmlNode valueType = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            valueType.Value = "Float";
            ValueType.AppendChild(valueType);

            // 設定の値
            XmlElement Value = xmlDocument.CreateElement("value");
            settingElement.AppendChild(Value);

            XmlNode value = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            value.Value = floatSetting.FloatValue.ToString();
            Value.AppendChild(value);
        }

        // Byte
        foreach (var byteSetting in ByteSettingsList)
        {
            // ルート要素の今年て属性付きの設定を追加
            XmlElement settingElement = xmlDocument.CreateElement("setting");
            settingElement.SetAttribute("name", byteSetting.Name);
            element.AppendChild(settingElement);

            // 設定の値の種類
            XmlElement ValueType = xmlDocument.CreateElement("type");
            settingElement.AppendChild(ValueType);

            XmlNode valueType = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            valueType.Value = "Byte";
            ValueType.AppendChild(valueType);

            // 設定の値
            XmlElement Value = xmlDocument.CreateElement("value");
            settingElement.AppendChild(Value);

            XmlNode value = xmlDocument.CreateNode(XmlNodeType.Text, "", "");
            value.Value = byteSetting.ByteValue.ToString();
            Value.AppendChild(value);
        }

        xmlDocument.Save(@$"./{SETTINGS_FOLDER}/GameSettings.xml");
    }

    public static void Save()
    {
        // 設定フォルダが存在しない時生成する
        if (!Directory.Exists(SETTINGS_FOLDER)) Directory.CreateDirectory(SETTINGS_FOLDER);

        Generate();
        Logger.Info("保存に成功しました。", "SaveSettings");
        Logger.SendInGame(string.Format(Translator.GetString("SettingsSaved")), false);
    }
}