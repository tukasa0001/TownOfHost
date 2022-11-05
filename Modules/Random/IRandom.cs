using System;

namespace TownOfHost
{
    public interface IRandom
    {
        /// <summary>0以上maxValue未満の乱数を生成します。</summary>
        public int Next(int maxValue);
        /// <summary>minValue以上maxValue未満の乱数を生成します。</summary>
        public int Next(int minValue, int maxValue);

        // == static ==
        public static IRandom Instance { get; private set; }
        public static void SetInstance(IRandom instance)
        {
            if (instance != null)
                Instance = instance;
        }
    }
}