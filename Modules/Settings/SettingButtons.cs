using UnityEngine;
using UnityEngine.Events;

namespace TownOfHost.Modules.Settings;

public static class SettingButtons
{
    // Load
    public static GameObject LoadButton;
    public static SpriteRenderer LoadRenderer;
    public static PassiveButton PassiveLoadButton;
    public static BoxCollider2D LoadCollider2D;
    public static AspectPosition LoadAspect;
    public static ButtonRolloverHandler LoadButtonRolloverHandler;

    // Save
    public static GameObject SaveButton;
    public static SpriteRenderer SaveRenderer;
    public static PassiveButton PassiveSaveButton;
    public static BoxCollider2D SaveCollider2D;
    public static AspectPosition SaveAspect;
    public static ButtonRolloverHandler SaveButtonRolloverHandler;

    public static void SetButtons()
    {
        LoadButton = new()
        {
            layer = 5,
            active = false,
            name = "LoadButton",
        };
        LoadButton.transform.parent = HudManager.Instance.transform;
        LoadRenderer = LoadButton.AddComponent<SpriteRenderer>();
        LoadRenderer.sprite = Utils.LoadSprite("TownOfHost.Resources.Load.png", 300f);
        PassiveLoadButton = LoadButton.AddComponent<PassiveButton>();
        PassiveLoadButton.OnClick = new();
        PassiveLoadButton.OnClick.AddListener((UnityAction)delegate
        {
            LoadSettings.Load();
        });
        PassiveLoadButton.OnMouseOver = new();
        PassiveLoadButton.OnMouseOver.AddListener((UnityAction)delegate
        {
            LoadRenderer.color = Palette.AcceptedGreen;
        });
        PassiveLoadButton.OnMouseOut = new();
        PassiveLoadButton.OnMouseOut.AddListener((UnityAction)delegate
        {
            LoadRenderer.color = Palette.White;
        });
        LoadCollider2D = LoadButton.AddComponent<BoxCollider2D>();
        LoadAspect = LoadButton.AddComponent<AspectPosition>();
        LoadAspect.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        LoadAspect.DistanceFromEdge = new(0.25f, 0.75f, -400);
        LoadAspect.updateAlways = true;
        LoadButtonRolloverHandler = LoadButton.AddComponent<ButtonRolloverHandler>();
        LoadButtonRolloverHandler.HoverSound = HudManager.Instance.Chat.ChatButton.GetComponent<ButtonRolloverHandler>().HoverSound;

        SaveButton = new()
        {
            layer = 5,
            active = false,
            name = "SaveButton",
        };
        SaveButton.transform.parent = HudManager.Instance.transform;
        SaveRenderer = SaveButton.AddComponent<SpriteRenderer>();
        SaveRenderer.sprite = Utils.LoadSprite("TownOfHost.Resources.Save.png", 300f);
        PassiveSaveButton = SaveButton.AddComponent<PassiveButton>();
        PassiveSaveButton.OnClick = new();
        PassiveSaveButton.OnClick.AddListener((UnityAction)delegate
        {
            SaveSettings.Save();
        });
        PassiveSaveButton.OnMouseOver = new();
        PassiveSaveButton.OnMouseOver.AddListener((UnityAction)delegate
        {
            SaveRenderer.color = Palette.AcceptedGreen;
        });
        PassiveSaveButton.OnMouseOut = new();
        PassiveSaveButton.OnMouseOut.AddListener((UnityAction)delegate
        {
            SaveRenderer.color = Palette.White;
        });
        SaveCollider2D = SaveButton.AddComponent<BoxCollider2D>();
        SaveAspect = SaveButton.AddComponent<AspectPosition>();
        SaveAspect.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        SaveAspect.DistanceFromEdge = new(0.25f, 0.3f, -400);
        SaveAspect.updateAlways = true;
        SaveButtonRolloverHandler = SaveButton.AddComponent<ButtonRolloverHandler>();
        SaveButtonRolloverHandler.HoverSound = HudManager.Instance.Chat.ChatButton.GetComponent<ButtonRolloverHandler>().HoverSound;
    }

    public static void ButtonsVisible(bool active)
    {
        LoadButton.SetActive(active);
        SaveButton.SetActive(active);
    }
}