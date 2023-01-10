using System;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.ReduxOptions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHost.Options;

public class GameOptionTab: IComparable<GameOptionTab>
{
    public string Name;
    public Sprite Sprite;
    public Transform Transform;
    public GameObject GameObject;
    public TabOrder Order { get; }
    private List<OptionHolder> options = new();

    private string assetPath;

    public List<OptionHolder> GetHolders() => this.options;

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
        this.GameObject.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = this.Sprite = Utils.LoadSprite(assetPath, 100);
        return this;
    }

    public void Register()
    {
        TOHPlugin.OptionManager.AddTab(this);
    }

    public void AddHolder(OptionHolder holder)
    {
        if (this.options.All(h => h.Name != holder.Name))
            this.options.Add(holder);
    }

    public int CompareTo(GameOptionTab other)
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
}

// We use an enum instead of an index so that addons can't steal the positions of the main mod's tabs
public enum TabOrder
{
    First,
    None,
    Last
}