using System.Linq;
using TMPro;
using UnityEngine;

namespace TownOfHost.GUI;

public static class Template
{
    private static StringOption stringOption;
    private static SpriteRenderer spriteRenderer;
    private static TextMeshPro textMeshPro;

    static Template() => Refresh();


    public static StringOption GetStringOption() => stringOption;
    public static SpriteRenderer GetSpriteRenderer(Transform parent) => parent == null ? spriteRenderer : Object.Instantiate(spriteRenderer, parent);
    public static TextMeshPro GetTextMeshPro(Transform parent) => Object.Instantiate(textMeshPro, parent);

    public static void Refresh()
    {
        StringOption tempStringOption = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
        if (tempStringOption == null) return;
        stringOption = tempStringOption;
        spriteRenderer = stringOption.transform.Find("Background").GetComponent<SpriteRenderer>();
        textMeshPro = stringOption.TitleText;
    }
}