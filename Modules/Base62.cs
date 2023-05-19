using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TownOfHost.Modules;

/// <summary>
/// 62進数 整数を短縮して扱える
/// </summary>
public static class Base62
{
    private const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// 整数を62進文字列に変換します
    /// </summary>
    /// <param name="number">変換したい数</param>
    /// <returns>変換された文字列</returns>
    public static string ToBase62(long number)
    {
        if (number == 0)
        {
            return "0";
        }

        var isNegative = number < 0;
        if (isNegative)
        {
            number *= -1;
        }

        var stack = new Stack<char>();
        do
        {
            var remainder = (int)(number % 62);
            stack.Push(Chars[remainder]);
            number /= 62;
        }
        while (number > 0);
        var resultBuilder = new StringBuilder(stack.Count);
        if (isNegative)
        {
            resultBuilder.Append('-');
        }
        // 最後に入れたものから順に追加
        foreach (var c in stack)
        {
            resultBuilder.Append(c);
        }
        return resultBuilder.ToString();
    }
    /// <summary>
    /// 62進文字列を<see cref="int"/>に変換します
    /// </summary>
    /// <param name="base62">変換したい62進文字列</param>
    /// <returns>変換された整数</returns>
    /// <exception cref="ArgumentException"/>
    public static int ToInt(string base62)
    {
        var isNegative = base62.StartsWith('-');
        if (isNegative)
        {
            base62 = base62[1..];
        }
        // 下位ビットから処理したいので前後反転
        base62 = new string(base62.Reverse().ToArray());
        var result = 0;
        for (var i = 0; i < base62.Length; i++)
        {
            var c = base62[i];
            var index = Chars.IndexOf(c);
            if (index < 0)
            {
                throw new ArgumentException($"{c}は62進の数字として適切ではありません");
            }
            result += index * (int)Math.Pow(62, i);
        }
        if (isNegative)
        {
            result *= -1;
        }
        return result;
    }
    /// <summary>
    /// 62進文字列を<see cref="byte"/>に変換します
    /// </summary>
    /// <param name="base62">変換したい62進文字列</param>
    /// <returns>変換された整数</returns>
    public static byte ToByte(string base62) => (byte)ToInt(base62);
    /// <summary>
    /// 62進文字列を<see cref="uint"/>に変換します
    /// </summary>
    /// <param name="base62">変換したい62進文字列</param>
    /// <returns>変換された整数</returns>
    public static uint ToUInt(string base62) => (uint)ToInt(base62);
}
