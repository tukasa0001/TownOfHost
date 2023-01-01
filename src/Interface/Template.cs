using System.Linq;
using TMPro;
using UnityEngine;

namespace TownOfHost.Interface;

public static class Template
{
    private static StringOption stringOption;
    private static SpriteRenderer spriteRenderer;
    private static TextMeshPro textMeshPro;

    static Template()
    {
        stringOption = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
        spriteRenderer = stringOption.transform.Find("Background").GetComponent<SpriteRenderer>();
        textMeshPro = stringOption.TitleText;
    }

    public static StringOption GetStringOption() => stringOption;
    public static SpriteRenderer GetSpriteRenderer(Transform parent) => parent == null ? spriteRenderer : Object.Instantiate(spriteRenderer, parent);
    public static TextMeshPro GetTextMeshPro(Transform parent) => Object.Instantiate(textMeshPro, parent);
}