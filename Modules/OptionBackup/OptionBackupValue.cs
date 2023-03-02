using AmongUs.GameOptions;
using System;


namespace TOHE;

public abstract class OptionBackupValue
{
    public abstract void Restore(IGameOptions option);
}

public abstract class OptionBackupValueBase<NameT, ValueT> : OptionBackupValue
where NameT : Enum
{
    public readonly NameT OptionName;
    public readonly ValueT Value;
    public OptionBackupValueBase(NameT name, ValueT value)
    {
        OptionName = name;
        Value = value;
    }
}

public class ByteOptionBackupValue : OptionBackupValueBase<ByteOptionNames, byte>
{
    public ByteOptionBackupValue(ByteOptionNames name, byte value) : base(name, value) { }
    public override void Restore(IGameOptions option)
    {
        option.SetByte(OptionName, Value);
    }
}
public class BoolOptionBackupValue : OptionBackupValueBase<BoolOptionNames, bool>
{
    public BoolOptionBackupValue(BoolOptionNames name, bool value) : base(name, value) { }
    public override void Restore(IGameOptions option)
    {
        option.SetBool(OptionName, Value);
    }
}
public class FloatOptionBackupValue : OptionBackupValueBase<FloatOptionNames, float>
{
    public FloatOptionBackupValue(FloatOptionNames name, float value) : base(name, value) { }
    public override void Restore(IGameOptions option)
    {
        option.SetFloat(OptionName, Value);
    }
}
public class IntOptionBackupValue : OptionBackupValueBase<Int32OptionNames, int>
{
    public IntOptionBackupValue(Int32OptionNames name, int value) : base(name, value) { }
    public override void Restore(IGameOptions option)
    {
        option.SetInt(OptionName, Value);
    }
}
public class UIntOptionBackupValue : OptionBackupValueBase<UInt32OptionNames, uint>
{
    public UIntOptionBackupValue(UInt32OptionNames name, uint value) : base(name, value) { }
    public override void Restore(IGameOptions option)
    {
        option.SetUInt(OptionName, Value);
    }
}

public class RoleRateBackupValue : OptionBackupValue
{
    public RoleTypes roleType;
    public int maxCount;
    public int chance;

    public RoleRateBackupValue(RoleTypes type, int maxCount, int chance)
    {
        this.roleType = type;
        this.maxCount = maxCount;
        this.chance = chance;
    }
    public override void Restore(IGameOptions option)
    {
        option.RoleOptions.SetRoleRate(roleType, maxCount, chance);
    }
}