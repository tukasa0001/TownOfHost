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
    public static void Load()
    {
        Logger.Info("設定の読み込みを開始", "LoadSettings");
        LoadAndSetSettings(1);
        Logger.Info("設定の読み込みが完了", "LoadSettings");
    }

    /// <summary>
    /// ファイルが有るか確認する。
    /// なかった場合はエラーを表示してfalseを返す。
    /// </summary>
    /// <param name="path">ファイルパス</param>
    /// <returns>ファイルの有無</returns>
    public static bool CheckFileExists(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Warn($"プリセットファイル[{path}]が存在しません。", "LoadSettings");
            Logger.SendInGame(Translator.GetString("NoPresetFile"), false);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 設定をロードしてセットする。
    /// 同期も行う。
    /// </summary>
    /// <param name="id">プリセットID</param>
    public static void LoadAndSetSettings(int id)
    {
        string path = string.Empty;
        switch (id)
        {
            case 1:
                path = SettingsGeneral.PRESET_FILE_1;
                break;
            case 2:
                path = SettingsGeneral.PRESET_FILE_2;
                break;
            case 3:
                path = SettingsGeneral.PRESET_FILE_3;
                break;
        }

        if (!CheckFileExists(path)) return;

        // 設定を格納する辞書
        Dictionary<int, string> Settings = new();
        string[] settings = File.ReadAllLines(path);
        // 行数
        int i = 1;
        foreach (var setting in settings)
        {
            Settings.Add(i, setting.Split(":")[1]);
            i++;
        }

        // 辞書から行数で値を取り出す
        foreach (var s in Settings)
        {
            switch (s.Key)
            {
                case 1: BoolOptionNames.VisualTasks.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 2: BoolOptionNames.GhostsDoTasks.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 3: BoolOptionNames.ConfirmImpostor.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 4: BoolOptionNames.AnonymousVotes.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 5: BoolOptionNames.IsDefaults.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 6: BoolOptionNames.UseFlashlight.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 7: BoolOptionNames.SeekerFinalVents.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 8: BoolOptionNames.SeekerFinalMap.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 9: BoolOptionNames.SeekerPings.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 10: BoolOptionNames.ShowCrewmateNames.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 11: BoolOptionNames.ShapeshifterLeaveSkin.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 12: BoolOptionNames.ImpostorsCanSeeProtect.Set(SettingsGeneral.GetBoolOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 13: Int32OptionNames.NumImpostors.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 14: Int32OptionNames.KillDistance.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 15: Int32OptionNames.NumEmergencyMeetings.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 16: Int32OptionNames.EmergencyCooldown.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 17: Int32OptionNames.DiscussionTime.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 18: Int32OptionNames.VotingTime.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 19: Int32OptionNames.MaxImpostors.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 20: Int32OptionNames.MinPlayers.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 21: Int32OptionNames.MaxPlayers.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 22: Int32OptionNames.NumCommonTasks.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 23: Int32OptionNames.NumShortTasks.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 24: Int32OptionNames.NumLongTasks.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 25: Int32OptionNames.TaskBarMode.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 26: Int32OptionNames.CrewmatesRemainingForVitals.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 27: Int32OptionNames.CrewmateVentUses.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 28: Int32OptionNames.ImpostorPlayerID.Set(SettingsGeneral.GetIntOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 29: FloatOptionNames.KillCooldown.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 30: FloatOptionNames.PlayerSpeedMod.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 31: FloatOptionNames.ImpostorLightMod.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 32: FloatOptionNames.CrewLightMod.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 33: FloatOptionNames.CrewmateTimeInVent.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 34: FloatOptionNames.FinalEscapeTime.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 35: FloatOptionNames.EscapeTime.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 36: FloatOptionNames.SeekerFinalSpeed.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 37: FloatOptionNames.MaxPingTime.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 38: FloatOptionNames.CrewmateFlashlightSize.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 39: FloatOptionNames.ImpostorFlashlightSize.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 40: FloatOptionNames.ShapeshifterCooldown.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 41: FloatOptionNames.ShapeshifterDuration.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 42: FloatOptionNames.ProtectionDurationSeconds.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 43: FloatOptionNames.GuardianAngelCooldown.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 44: FloatOptionNames.ScientistCooldown.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 45: FloatOptionNames.ScientistBatteryCharge.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 46: FloatOptionNames.EngineerCooldown.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 47: FloatOptionNames.EngineerInVentMaxTime.Set(SettingsGeneral.GetFloatOption(s.Value), SettingsGeneral.CurrentOptions); break;
                case 48: ByteOptionNames.MapId.Set(SettingsGeneral.GetByteOption(s.Value), SettingsGeneral.CurrentOptions); break;
                default: break;
            }
        }

        // 同期
        GameManager.Instance.LogicOptions.SyncOptions();

        Logger.SendInGame(Translator.GetString("SettingsLoaded"), false);
    }
}