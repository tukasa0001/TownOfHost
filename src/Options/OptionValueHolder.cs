using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using TownOfHost.Extensions;
using UnityEngine;

namespace TownOfHost.ReduxOptions;

public class OptionValueHolder
{
    private ConfigEntryBase configEntry;
    private List<OptionValue> values;
    public int Index;
    public int Count => this.values.Count;
    public Action<object> LateBinding { set => _lateBinding = value; }
    private Action<object> _lateBinding;


    /// <summary>
    /// THIS METHOD MAKES THE ASSUMPTION THAT ALL VALUES HAVE THE SAME HEADER & ENTRY
    /// </summary>
    /// <param name="values">List of similar option values</param>
    public OptionValueHolder(List<OptionValue> values)
    {
        if (values.Count > 0)
            BindFirstValue(values[0]);
        this.values = values;
        this.Index = this.values.FindIndex(v => fuzzyEquals(configEntry?.BoxedValue, v?.Value));
        this.Index = this.Index == -1 ? -1 : this.Index;
        this.UpdateBinding();
    }

    public object GetValue(int index = -1) => this.values?[index > -1 ? index : this.Index == -1 ? this.Index = 0 : this.Index].Value;
    public T GetValue<T>(int index = -1) => (T) this.values[index > -1 ? index : this.Index == -1 ? this.Index = 0 : this.Index].Value;

    public object Increment() =>
        UpdateBoxedValue((this.Index + 1 < this.values.Count ? this.values[++this.Index] : this.values[this.Index = 0]).Value);

    public object Decrement() =>
        UpdateBoxedValue((this.Index - 1 >= 0 ? this.values[--this.Index] : this.values[this.Index = this.values.Count - 1]).Value);

    public string GetAsString(int index = -1)
    {
        if (this.values == null || this.values.Count == 0) return "N/A";
        this.Index = this.Index == -1 ? 0 : this.Index;
        object value = this.values[index > -1 && index < this.values.Count ? index : this.Index];
        return value is not (float or double) ? value.ToString() : Math.Round(Convert.ToDecimal(value), 2).ToString();
    }


    public object UpdateBoxedValue(object value)
    {
        this._lateBinding?.Invoke(value);
        if (this.configEntry == null) return value;
        this.configEntry.BoxedValue = this.configEntry.BoxedValue switch
        {
            bool => Convert.ToBoolean(value),
            string => Convert.ToString(value),
            byte => Convert.ToByte(value),
            sbyte => Convert.ToSByte(value),
            short => Convert.ToInt16(value),
            ushort => Convert.ToUInt16(value),
            int => Convert.ToInt32(value),
            uint => Convert.ToUInt32(value),
            long => Convert.ToInt64(value),
            ulong => Convert.ToUInt64(value),
            float => Convert.ToSingle(value),
            double => Convert.ToDouble(value),
            decimal => Convert.ToDecimal(value),
            Enum => value,
            _ => value
        };
        return value;
    }

    public void UpdateBinding() => this._lateBinding?.Invoke(this.GetValue());

    private void BindFirstValue(OptionValue value)
    {

        if (Main.OptionManager.GetPreset().ContainsKey(new ConfigDefinition(value.Header, value.Entry))) return;
        this.configEntry = value.Value switch
        {
            bool b => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, b),
            string => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, value.ToString()),
            byte by => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, by),
            sbyte sby => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, sby),
            short i16 => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToSingle(i16)),
            ushort u16 => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToSingle(u16)),
            int i => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToSingle(i)),
            uint u32 => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToSingle(u32)),
            long l => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToDouble(l)),
            ulong ul => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, Convert.ToDouble(ul)),
            float f => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, f),
            double db => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, db),
            decimal dec => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, dec),
            Enum en => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, en),
            _ => Main.OptionManager.GetPreset().Bind(value.Header, value.Entry, value)
        };

    }

    private static bool fuzzyEquals(object reference, object other)
    {
        if (reference == null || other == null) return false;
        return reference switch
        {
            OptionValue v => fuzzyEquals(v.Value, other),
            bool b => b == Convert.ToBoolean(other),
            string s => s == Convert.ToString(other),
            byte b => b == Convert.ToByte(other),
            sbyte sb => sb == Convert.ToSByte(other),
            short s => s == Convert.ToInt16(other),
            ushort us => us == Convert.ToUInt16(other),
            int i => i == Convert.ToInt32(other),
            uint u => u == Convert.ToUInt32(other),
            long l => l == Convert.ToInt64(other),
            ulong ul => ul == Convert.ToUInt64(other),
            float f => Math.Abs(f - Convert.ToSingle(other)) < 0.05,
            double d => Math.Abs(d - Convert.ToDouble(other)) < 0.05,
            decimal dec => dec == Convert.ToDecimal(other),
            Enum => reference == other,
            _ => reference.Equals(other)
        };
    }
}