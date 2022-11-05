using System;

namespace TownOfHost
{
    public class Xorshift : IRandom
    {
        // 参考元
        public const string REFERENCE = "https://ja.wikipedia.org/wiki/Xorshift";

        private uint num;

        public Xorshift(uint seed)
        {
            num = seed;
        }

        public uint Next()
        {
            num ^= num << 13;
            num ^= num >> 17;
            num ^= num << 5;

            return num;
        }
        public int Next(int minValue, int maxValue)
        {
            return (int)(minValue + (Next() % (maxValue - minValue)));
        }
        public int Next(int maxValue) => Next(0, maxValue);
    }
}