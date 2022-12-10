using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;


namespace TownOfHost
{
    public abstract class OptionBackupValue
    {
        public abstract void Restore(IGameOptions option);
    }

    public abstract class OptionBackupValueBase<NameT, ValueT> : OptionBackupValue
    where NameT : Enum
    where ValueT : struct
    {
        public readonly NameT OptionName;
        public readonly ValueT Value;
        public OptionBackupValueBase(NameT name, ValueT value)
        {
            OptionName = name;
            Value = value;
        }
    }
}