using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;


namespace TownOfHost
{
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

            foreach (RoleTypes role in new RoleTypes[] { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.GuardianAngel, RoleTypes.Shapeshifter })
            {
                AllValues.Add(new RoleRateBackupValue(role, option.RoleOptions.GetNumPerGame(role), option.RoleOptions.GetChancePerGame(role)));
            }
        }
    }
}