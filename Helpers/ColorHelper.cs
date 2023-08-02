using UnityEngine;

namespace TownOfHost;

public static class ColorHelper
{
    /// <summary>蛍光マーカーのような色合いの透過色に変換する</summary>
    public static Color ToMarkingColor(this Color color)
    {
        Color.RGBToHSV(color, out var h, out _, out _);
        var markingColor = Color.HSVToRGB(h, MarkerSat, MarkerVal).SetAlpha(MarkerAlpha);
        return markingColor;
    }

    /// <summary>マーカー色のS値 = 彩度</summary>
    private const float MarkerSat = 1f;
    /// <summary>マーカー色のV値 = 明度</summary>
    private const float MarkerVal = 1f;
    /// <summary>マーカー色のアルファ = 不透明度</summary>
    private const float MarkerAlpha = 0.2f;
}
