using UnityEngine;

namespace TownOfHost;

public static class ColorHelper
{
    /// <summary>蛍光マーカーのような色合いの透過色に変換する</summary>
    /// <param name="bright">最大明度にするかどうか．黒っぽい色を黒っぽいままにしたい場合はfalse</param>
    public static Color ToMarkingColor(this Color color, bool bright = true)
    {
        Color.RGBToHSV(color, out var h, out _, out var v);
        var markingColor = Color.HSVToRGB(h, MarkerSat, bright ? MarkerVal : v).SetAlpha(MarkerAlpha);
        return markingColor;
    }
    /// <summary>白背景での可読性を保てる色に変換する</summary>
    public static Color ToReadableColor(this Color color)
    {
        Color.RGBToHSV(color, out var h, out var s, out var v);
        // 適切な彩度でない場合は彩度を変更
        if (s < ReadableSat)
        {
            s = ReadableSat;
        }
        // 適切な明度でない場合は明度を変更
        if (v > ReadableVal)
        {
            v = ReadableVal;
        }
        return Color.HSVToRGB(h, s, v);
    }

    /// <summary>マーカー色のS値 = 彩度</summary>
    private const float MarkerSat = 1f;
    /// <summary>マーカー色のV値 = 明度</summary>
    private const float MarkerVal = 1f;
    /// <summary>マーカー色のアルファ = 不透明度</summary>
    private const float MarkerAlpha = 0.2f;
    /// <summary>白背景テキスト色の最大S = 彩度</summary>
    private const float ReadableSat = 0.8f;
    /// <summary>白背景テキスト色の最大V = 明度</summary>
    private const float ReadableVal = 0.8f;
}
