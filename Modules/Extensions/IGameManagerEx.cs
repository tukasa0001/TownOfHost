using AmongUs.GameOptions;

namespace TownOfHost.Modules.Extensions
{
    public static class IGameManagerEx
    {
        public static void Set(this BoolOptionNames name, bool value, IGameOptions opt) => opt.SetBool(name, value);
        public static void Set(this BoolOptionNames name, bool value, NormalGameOptionsV08 opt) => opt.SetBool(name, value);
        public static void Set(this BoolOptionNames name, bool value, HideNSeekGameOptionsV08 opt) => opt.SetBool(name, value);

        public static void Set(this Int32OptionNames name, int value, IGameOptions opt) => opt.SetInt(name, value);
        public static void Set(this Int32OptionNames name, int value, NormalGameOptionsV08 opt) => opt.SetInt(name, value);
        public static void Set(this Int32OptionNames name, int value, HideNSeekGameOptionsV08 opt) => opt.SetInt(name, value);

        public static void Set(this FloatOptionNames name, float value, IGameOptions opt) => opt.SetFloat(name, value);
        public static void Set(this FloatOptionNames name, float value, NormalGameOptionsV08 opt) => opt.SetFloat(name, value);
        public static void Set(this FloatOptionNames name, float value, HideNSeekGameOptionsV08 opt) => opt.SetFloat(name, value);

        public static void Set(this ByteOptionNames name, byte value, IGameOptions opt) => opt.SetByte(name, value);
        public static void Set(this ByteOptionNames name, byte value, NormalGameOptionsV08 opt) => opt.SetByte(name, value);
        public static void Set(this ByteOptionNames name, byte value, HideNSeekGameOptionsV08 opt) => opt.SetByte(name, value);

        public static void Set(this UInt32OptionNames name, uint value, IGameOptions opt) => opt.SetUInt(name, value);
        public static void Set(this UInt32OptionNames name, uint value, NormalGameOptionsV08 opt) => opt.SetUInt(name, value);
        public static void Set(this UInt32OptionNames name, uint value, HideNSeekGameOptionsV08 opt) => opt.SetUInt(name, value);
    }
}