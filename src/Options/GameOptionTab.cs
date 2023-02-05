using System;
using System.Collections.Generic;
using UnityEngine;
using VentLib.Options.Interfaces;
using VentLib.Options;
using Object = UnityEngine.Object;

namespace TownOfHost.Options;

public class GameOptionTab: AbstractOptionTab, IComparable<GameOptionTab>
{
    public string Name;
    public Transform Transform = null!;
    public GameObject GameObject = null!;
    public GameObject ParentMenu;
    public GameOptionsMenu Menu;
    public TabOrder Order { get; }
    internal List<Option> Options = new();

    private string assetPath;

    public GameOptionTab(string name, string assetPath, TabOrder order = TabOrder.None)
    {
        this.Name = name;
        this.assetPath = assetPath;
        this.Order = order;
    }

    public GameOptionTab Instantiate(GameObject originalTab, Transform parent)
    {
        this.GameObject = Object.Instantiate(originalTab, parent);
        this.Transform = this.GameObject.transform;
        this.GameObject.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite(assetPath, 100);
        return this;
    }

    public void SetParentMenu(GameObject parentMenu)
    {
        ParentMenu = parentMenu;
        Menu = ParentMenu.transform.FindChild("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();

    }

    public void Register()
    {
        TOHPlugin.OptionManager.AddTab(this);
    }

    public int CompareTo(GameOptionTab? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Order.CompareTo(other.Order);
    }

    public void SetActive(bool active)
    {
        if (GameObject == null) return;
        try
        {
            GameObject.SetActive(active);
        } catch { /* ignored */ }
    }

    public override void Load()
    {
        base.Load();
        SetActive(true);
    }

    public override void Unload()
    {
        base.Unload();
        SetActive(false);
    }

    public override Transform GetTransform() => Menu.transform;

    public override void AddOption(Option option) => Options.Add(option);

    public override void RemoveOption(Option option) => Options.Remove(option);

    public override List<Option> GetOptions() => Options;

    public override void SetOptions(List<Option> options) => Options = options;
}

// We use an enum instead of an index so that addons can't steal the positions of the main mod's tabs
public enum TabOrder
{
    First,
    None,
    Last
}