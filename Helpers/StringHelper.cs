using UnityEngine;

namespace TownOfHost;

public static class StringHelper
{
    /// <summary>蛍光マーカーのような装飾をする</summary>
    /// <param name="self">文字列</param>
    /// <param name="color">元の色 自動で半透明の蛍光色に変換される</param>
    /// <returns>マーキング済文字列</returns>
    public static string Mark(this string self, Color color)
    {
        var markingColor = color.ToMarkingColor();
        var markingColorCode = ColorUtility.ToHtmlStringRGBA(markingColor);
        return $"<mark=#{markingColorCode}>{self}</mark>";
    }
}
