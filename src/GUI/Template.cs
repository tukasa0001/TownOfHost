using System.Linq;
using TMPro;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace TOHTOR.GUI;

public static class Template
{
    private static StringOption stringOption;
    private static SpriteRenderer spriteRenderer;
    private static TextMeshPro textMeshPro;
    private static bool _initialized;

    static Template() => Initialize();


    public static StringOption GetStringOption() => stringOption;
    public static SpriteRenderer GetSpriteRenderer(Transform parent) => parent == null ? spriteRenderer : Object.Instantiate(spriteRenderer, parent);
    public static TextMeshPro GetTextMeshPro(Transform parent) => Object.Instantiate(textMeshPro, parent);

    public static void Initialize()
    {
        if (_initialized) return;
        StringOption tempStringOption = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
        tempStringOption.DontUnload();
        tempStringOption.DontDestroyOnLoad();
        stringOption = tempStringOption;
        spriteRenderer = stringOption.transform.Find("Background").GetComponent<SpriteRenderer>();
        textMeshPro = stringOption.TitleText;
        _initialized = true;
    }
}