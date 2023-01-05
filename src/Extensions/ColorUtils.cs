using System;
using UnityEngine;

namespace TownOfHost.Extensions;

public static class ColorUtils
{
    public static Color ToColor(this string str)
    {
        if (!ColorUtility.TryParseHtmlString(str, out Color color))
            throw new ArgumentException($"Cannot parse {color} to HTML color");
        return color;
    }

    public static string ColorString(Color c, string s)
    {
        return $"<color={c.ToHex()}>{s}</color>";
    }

    public static string ToHex(this Color c)
    {
        return $"#{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}{ToByte(c.a):X2}";
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }

    public static string Colorize(this Color color, string str) => ColorString(color, str);
}