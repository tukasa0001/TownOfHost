using System.Text.RegularExpressions;

namespace TownOfHost.Extensions;

public static class StringExtension
{
    public static string Repeat(this string str, int count)
    {
        string s = "";
        for (int i = 0; i < count; i++) s += str;
        return s;
    }

    public static string RemoveColorTags(this string str) => Regex.Replace(str, "<[^size>]*?>", "");
}