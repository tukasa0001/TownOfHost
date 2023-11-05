using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHost.Templates;

public class SimpleButton
{
    /// <summary>新しいボタンを作成する</summary>
    /// <param name="parent">親オブジェクト</param>
    /// <param name="name">オブジェクト名</param>
    /// <param name="normalColor">通常時の背景色</param>
    /// <param name="hoverColor">マウスホバー時の背景色</param>
    /// <param name="action">クリック時に発火するアクション</param>
    /// <param name="label">ボタンのラベル</param>
    /// <param name="scale">ボタンの大きさ</param>
    /// <param name="isActive">初期状態でアクティブにするかどうか(デフォルトtrue)</param>
    public SimpleButton(
        Transform parent,
        string name,
        Vector3 localPosition,
        Color32 normalColor,
        Color32 hoverColor,
        Action action,
        string label,
        bool isActive = true)
    {
        if (baseButton == null)
        {
            throw new InvalidOperationException("baseButtonが未設定");
        }

        Button = Object.Instantiate(baseButton, parent);
        Label = Button.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
        NormalSprite = Button.inactiveSprites.GetComponent<SpriteRenderer>();
        HoverSprite = Button.activeSprites.GetComponent<SpriteRenderer>();
        buttonCollider = Button.GetComponent<BoxCollider2D>();

        // ラベルをセンタリング
        var container = Label.transform.parent;
        Object.Destroy(Label.GetComponent<AspectPosition>());
        container.SetLocalX(0f);
        Label.transform.SetLocalX(0f);
        Label.horizontalAlignment = HorizontalAlignmentOptions.Center;

        Button.name = name;
        Button.transform.localPosition = localPosition;
        NormalSprite.color = normalColor;
        HoverSprite.color = hoverColor;
        Button.OnClick.AddListener(action);
        Label.text = label;
        Button.gameObject.SetActive(isActive);
    }
    public PassiveButton Button { get; }
    public TextMeshPro Label { get; }
    public SpriteRenderer NormalSprite { get; }
    public SpriteRenderer HoverSprite { get; }
    private readonly BoxCollider2D buttonCollider;
    private Vector2 _scale;
    public Vector2 Scale
    {
        get => _scale;
        set => _scale = NormalSprite.size = HoverSprite.size = buttonCollider.size = value;
    }
    private float _fontSize;
    public float FontSize
    {
        get => _fontSize;
        set => _fontSize = Label.fontSize = Label.fontSizeMin = Label.fontSizeMax = value;
    }

    private static PassiveButton baseButton;
    public static void SetBase(PassiveButton passiveButton)
    {
        if (baseButton != null || passiveButton == null)
        {
            return;
        }
        // 複製
        baseButton = Object.Instantiate(passiveButton);
        var label = baseButton.transform.Find("FontPlacer/Text_TMP").GetComponent<TextMeshPro>();
        baseButton.gameObject.SetActive(false);
        // シーン切替時に破棄されないようにする
        Object.DontDestroyOnLoad(baseButton);
        baseButton.name = "TOH_SimpleButtonBase";
        // 不要なコンポーネントを無効化
        Object.Destroy(baseButton.GetComponent<AspectPosition>());
        label.DestroyTranslator();
        label.fontSize = label.fontSizeMax = label.fontSizeMin = 3.5f;
        label.enableWordWrapping = false;
        label.text = "TOH SIMPLE BUTTON BASE";
        // 当たり判定がズレてるのを直す
        var buttonCollider = baseButton.GetComponent<BoxCollider2D>();
        buttonCollider.offset = new(0f, 0f);
        baseButton.OnClick = new();
    }
    public static bool IsNullOrDestroyed(SimpleButton button) => button == null || button.Button == null;
}
