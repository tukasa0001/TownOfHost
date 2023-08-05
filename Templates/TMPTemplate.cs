using TMPro;
using UnityEngine;

namespace TownOfHost.Templates;

public sealed class TMPTemplate
{
    private static TextMeshPro baseTMP;
    public static void SetBase(TextMeshPro tmp)
    {
        if (baseTMP != null) return;

        baseTMP = Object.Instantiate(tmp);
        Object.Destroy(baseTMP.GetComponent<AspectPosition>());
        Object.DontDestroyOnLoad(baseTMP);
        baseTMP.gameObject.SetActive(false);
        baseTMP.gameObject.name = "TMPTemplateBase";
    }
    public static TextMeshPro Create(
        string name,
        string text = null,
        Color? color = null,
        float? fontSize = null,
        TextAlignmentOptions? alignment = null,
        bool setActive = false,
        Transform parent = null
        )
    {
        var replicatedObject = parent == null
            ? Object.Instantiate(baseTMP)
            : Object.Instantiate(baseTMP, parent);
        replicatedObject.text = text ?? "";
        replicatedObject.color = color ?? Color.white;
        replicatedObject.fontSize =
        replicatedObject.fontSizeMax =
        replicatedObject.fontSizeMin = fontSize ?? baseTMP.fontSize;
        replicatedObject.alignment = alignment ?? TextAlignmentOptions.Center;

        replicatedObject.gameObject.SetActive(setActive);
        replicatedObject.gameObject.name = name;

        return replicatedObject;
    }
}