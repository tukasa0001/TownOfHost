/*using System;
using TownOfHost.Extensions;
using UnityEngine;

namespace TownOfHost.Interface;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class NameUI: Attribute, IComparable<NameUI>
{
    public NameUI() { }

    public NameUI(UIPosition position, int order = 0, string value = null, string format = "{0}", string color = null)
    {
        this.Value = value;
        this.format = format;
        if (color != null)
            this.color = color.ToColor();
        this.order = order;
        this.Position = position;
    }

    public UIPosition Position;
    private string Value;
    private Color color = Color.white;
    private bool destroy;
    private Func<string> supplier;

    public void SetValueSupplier(Func<string> supplier) => this.supplier = supplier;
    public void SetFormat(string format) => this.format = format;
    public void SetColor(Color color) => this.color = color;
    public void Destroy() => this.destroy = true;

    public string GetText(Color? color = null, bool colored = true)
    {
        this.Value = supplier?.Invoke() ?? Value;
        return String.Format(format, colored ? Helpers.ColorString(color ?? this.color, Value) : Value);
    }

    public bool ShouldDestroy() => this.destroy;

    public int CompareTo(NameUI other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return order.CompareTo(other.order);
    }
}

public enum UIPosition
{
    Prefix,
    Main,
    Suffix
}*/