using System;
using System.Collections.Generic;

namespace TOHE;

public interface IRandom
{
    /// <summary>0以上maxValue未満の乱数を生成します。</summary>
    public int Next(int maxValue);
    /// <summary>minValue以上maxValue未満の乱数を生成します。</summary>
    public int Next(int minValue, int maxValue);

    // == static ==
    // IRandomを実装するクラスのリスト
    public static Dictionary<int, Type> randomTypes = new()
    {
        { 0, typeof(NetRandomWrapper) }, //Default
        { 1, typeof(NetRandomWrapper) },
        { 2, typeof(HashRandomWrapper) },
        { 3, typeof(Xorshift) },
        { 4, typeof(MersenneTwister) },
    };

    public static IRandom Instance { get; private set; }
    public static void SetInstance(IRandom instance)
    {
        if (instance != null)
            Instance = instance;
    }

    public static void SetInstanceById(int id)
    {
        if (randomTypes.TryGetValue(id, out var type))
        {
            // 現在のインスタンスがnull または 現在のインスタンスの型が指定typeと一致しない
            if (Instance == null || Instance.GetType() != type)
            {
                Instance = Activator.CreateInstance(type) as IRandom ?? Instance;
            }
        }
        else Logger.Warn($"無効なID: {id}", "IRandom.SetInstanceById");
    }
}