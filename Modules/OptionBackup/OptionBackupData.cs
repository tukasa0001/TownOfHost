using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TOHE;

public class OptionBackupData
{
    public List<OptionBackupValue> AllValues;
    public OptionBackupData(IGameOptions option)
    {
        AllValues = new(32);

        foreach (ByteOptionNames name in Enum.GetValues(typeof(ByteOptionNames)))
        {
            if (option.TryGetByte(name, out var value))
                AllValues.Add(new ByteOptionBackupValue(name, value));
        }
        foreach (BoolOptionNames name in Enum.GetValues(typeof(BoolOptionNames)))
        {
            if (option.TryGetBool(name, out var value))
                AllValues.Add(new BoolOptionBackupValue(name, value));
        }
        foreach (FloatOptionNames name in Enum.GetValues(typeof(FloatOptionNames)))
        {
            if (option.TryGetFloat(name, out var value))
                AllValues.Add(new FloatOptionBackupValue(name, value));
        }
        foreach (Int32OptionNames name in Enum.GetValues(typeof(Int32OptionNames)))
        {
            if (option.TryGetInt(name, out var value))
                AllValues.Add(new IntOptionBackupValue(name, value));
        }
        // [バニラ側バグ] GetIntで部屋の人数のみ取得できないため、別で取得する
        AllValues.Add(new IntOptionBackupValue(Int32OptionNames.MaxPlayers, option.MaxPlayers));
        // TryGetUIntが実装されていないため、別で取得する
        AllValues.Add(new UIntOptionBackupValue(UInt32OptionNames.Keywords, (uint)option.Keywords));

        foreach (RoleTypes role in new RoleTypes[] { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.GuardianAngel, RoleTypes.Shapeshifter })
        {
            AllValues.Add(new RoleRateBackupValue(role, option.RoleOptions.GetNumPerGame(role), option.RoleOptions.GetChancePerGame(role)));
        }
    }

    public IGameOptions Restore(IGameOptions option)
    {
        foreach (var value in AllValues)
        {
            value.Restore(option);
        }
        return option;
    }

    public byte GetByte(ByteOptionNames name) => Get<ByteOptionNames, byte>(name);
    public bool GetBool(BoolOptionNames name) => Get<BoolOptionNames, bool>(name);
    public float GetFloat(FloatOptionNames name) => Get<FloatOptionNames, float>(name);
    public int GetInt(Int32OptionNames name) => Get<Int32OptionNames, int>(name);
    public uint GetUInt(UInt32OptionNames name) => Get<UInt32OptionNames, uint>(name);
    public TValue Get<TKey, TValue>(TKey name)
    where TKey : Enum
    {
        var value = AllValues
            .OfType<OptionBackupValueBase<TKey, TValue>>()
            .Where(val => val.OptionName.Equals(name)).
            FirstOrDefault();

        return value == null ? default : value.Value;
    }
}