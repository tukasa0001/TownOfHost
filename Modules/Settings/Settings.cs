using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AmongUs.GameOptions;
using TownOfHost.Modules.Extensions;

namespace TownOfHost.Modules.Settings;

public static class SettingsGeneral
{
    public static IGameOptions CurrentOptions => GameOptionsManager.Instance.CurrentGameOptions;

    public const string SETTINGS_FOLDER = @"./TOH_DATA/Settings";
    public const string PRESET_FILE_1 = SETTINGS_FOLDER + @"/Preset1.config";
    public const string PRESET_FILE_2 = SETTINGS_FOLDER + @"/Preset2.config";
    public const string PRESET_FILE_3 = SETTINGS_FOLDER + @"/Preset3.config";

    public static bool GetBoolOption(string key)
    {
        if (key is ("true" or "false") and not null)
        {
            return bool.Parse(key);
        }
        return false;
    }

    public static int GetIntOption(string key)
    {
        if (key is not null)
        {
            return int.Parse(key);
        }
        return 0;
    }

    public static float GetFloatOption(string key)
    {
        if (key is not null)
        {
            return float.Parse(key);
        }
        return 0f;
    }

    public static byte GetByteOption(string key)
    {
        if (key is not null)
        {
            return byte.Parse(key);
        }
        return 0;
    }
}