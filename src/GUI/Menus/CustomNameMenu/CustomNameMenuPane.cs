using UnityEngine;

namespace TownOfHost.GUI.Menus.CustomNameMenu;

public class CustomNameMenuPane
{
    public PoolablePlayer PreviewArea;
    //public UiElement BackButton;
    public GameObject GameObject = new();

    private const float ViewScale = 0.65f;

    public CustomNameMenuPane(Transform? parent = null)
    {
        parent ??= GameObject.transform;
        PreviewArea = Object.Instantiate(HudManager.Instance.IntroPrefab.PlayerPrefab, parent);

        PreviewArea.cosmetics.enabled = true;

        PreviewArea.transform.localScale = new Vector3(ViewScale, ViewScale, ViewScale);
        PreviewArea.SetName("<size=4.5>Evan\r\nBeastWindowMaker(4/4)</size>");
        PreviewArea.SetNamePosition(new Vector3(0, 2f));
        PreviewArea.ToggleName(true);
        PreviewArea.ToggleHat(true);
        PreviewArea.TogglePet(true);
        PreviewArea.enabled = true;

        PreviewArea.UpdateFromDataManager(PlayerMaterial.MaskType.None);
        PreviewArea.SetBodyColor(PreviewArea.cosmetics.bodyMatProperties.ColorId);
    }


    public void SetName(string name)
    {
        PreviewArea.SetName(name);
    }
}