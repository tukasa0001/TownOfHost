using UnityEngine;
using UnityEngine.Events;

namespace TownOfHost.Modules;

public static class SettingButtons
{
    // Load
    public static GameObject LoadButton;
    public static SpriteRenderer loadRenderer;
    public static PassiveButton passiveLoadButton;
    public static BoxCollider2D loadCollider2D;
    public static AspectPosition loadAspect;
    public static ButtonRolloverHandler loadButtonRolloverHandler;

    // Save
    public static GameObject SaveButton;
    public static SpriteRenderer saveRenderer;
    public static PassiveButton passiveSaveButton;
    public static BoxCollider2D saveCollider2D;
    public static AspectPosition saveAspect;
    public static ButtonRolloverHandler saveButtonRolloverHandler;

    public static void SetButtons()
    {
        LoadButton = new()
        {
            layer = 5,
            active = true,
            name = "LoadButton",
        };
        LoadButton.transform.parent = HudManager.Instance.transform;
        loadRenderer = LoadButton.AddComponent<SpriteRenderer>();
        loadRenderer.sprite = Utils.LoadSprite("TownOfHost.Resources.Load.png", 300f);
        passiveLoadButton = LoadButton.AddComponent<PassiveButton>();
        passiveLoadButton.OnClick = new();
        passiveLoadButton.OnClick.AddListener((System.Action)delegate
        {
            LoadSettings.Load();
        });
        passiveLoadButton.OnMouseOver = new();
        passiveLoadButton.OnMouseOver.AddListener((UnityAction)delegate
        {
            loadRenderer.color = Palette.AcceptedGreen;
        });
        passiveLoadButton.OnMouseOut = new();
        passiveLoadButton.OnMouseOut.AddListener((UnityAction)delegate
        {
            loadRenderer.color = Palette.White;
        });
        loadCollider2D = LoadButton.AddComponent<BoxCollider2D>();
        loadAspect = LoadButton.AddComponent<AspectPosition>();
        loadAspect.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        loadAspect.DistanceFromEdge = new(0.25f, 0.75f, -400);
        loadAspect.updateAlways = true;
        loadButtonRolloverHandler = LoadButton.AddComponent<ButtonRolloverHandler>();
        loadButtonRolloverHandler.HoverSound = HudManager.Instance.Chat.ChatButton.GetComponent<ButtonRolloverHandler>().HoverSound;

        SaveButton = new()
        {
            layer = 5,
            active = true,
            name = "SaveButton",
        };
        SaveButton.transform.parent = HudManager.Instance.transform;
        saveRenderer = SaveButton.AddComponent<SpriteRenderer>();
        saveRenderer.sprite = Utils.LoadSprite("TownOfHost.Resources.Save.png", 300f);
        passiveSaveButton = SaveButton.AddComponent<PassiveButton>();
        passiveSaveButton.OnClick = new();
        passiveSaveButton.OnClick.AddListener((UnityAction)delegate
        {
            SaveSettings.Save();
        });
        passiveSaveButton.OnMouseOver = new();
        passiveSaveButton.OnMouseOver.AddListener((UnityAction)delegate
        {
            saveRenderer.color = Palette.AcceptedGreen;
        });
        passiveSaveButton.OnMouseOut = new();
        passiveSaveButton.OnMouseOut.AddListener((UnityAction)delegate
        {
            saveRenderer.color = Palette.White;
        });
        saveCollider2D = SaveButton.AddComponent<BoxCollider2D>();
        saveAspect = SaveButton.AddComponent<AspectPosition>();
        saveAspect.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        saveAspect.DistanceFromEdge = new(0.25f, 0.3f, -400);
        saveAspect.updateAlways = true;
        saveButtonRolloverHandler = SaveButton.AddComponent<ButtonRolloverHandler>();
        saveButtonRolloverHandler.HoverSound = HudManager.Instance.Chat.ChatButton.GetComponent<ButtonRolloverHandler>().HoverSound;
    }

    public static void ButtonsVisible(bool active)
    {
        LoadButton.SetActive(active);
        SaveButton.SetActive(active);
    }
}