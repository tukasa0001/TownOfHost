using System;

namespace TownOfHost
{
    public abstract class ValueRule<T>
    {
        public T MinValue { get; protected set; }
        public T MaxValue { get; protected set; }
        public T Step { get; protected set; }

        public ValueRule(T minValue, T maxValue, T step)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Step = step;
        }
        public ValueRule((T, T, T) tuple)
        : this(tuple.Item1, tuple.Item2, tuple.Item3)
        { }

        /// <summary>
        /// 引数に指定された値が範囲外の場合、折り返した値を返します。
        /// </summary>
        // 例: (2, 12, 2)の時、0から順番に値を入力した場合:
        //     0, 1, 2, 3, 4, 0, 1, 2...
        public abstract int RepeatIndex(int value);
        public abstract T GetValueByIndex(int index);
        public abstract int GetNearestIndex(T num);
    }


}