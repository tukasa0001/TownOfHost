using System;

namespace TownOfHost
{
    public class NetRandomWrapper : IRandom
    {
        public Random wrapping;

        public NetRandomWrapper(Random instance)
        {
            wrapping = instance;
        }

        public int Next(int minValue, int maxValue) => wrapping.Next(minValue, maxValue);
        public int Next(int maxValue) => wrapping.Next(maxValue);
        public int Next() => wrapping.Next();
    }
}