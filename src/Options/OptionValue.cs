using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TownOfHost.Extensions;
using UnityEngine;

namespace TownOfHost.ReduxOptions;

public class OptionValue
{
    public readonly string Text;
    public readonly string Header;
    public readonly string Entry;
    public object Value;
    private string prefix;
    private string suffix;
    private Color? color;


    public OptionValue(object value, string cfgHeader, string cfgEntry, string text = null, string prefix = "", string suffix = "", Color? color = null)
    {
        this.Text = text;
        this.Value = value;
        this.Header = cfgHeader.RemoveHtmlTags().RemoveAll(new Il2CppStructArray<char>(new char[] { '\n', '\t', '\\', '"', '\'', '[', ']', '<', '>', '#', '/' })).TrimEnd();
        //cfgEntry.RemoveHtmlTags().RemoveAll(new Il2CppStructArray<char>(new char[] { '\n', '\t', '\\', '"', '\'', '[', ']', '<', '>', '#', '/' })).DebugLog("Cfg Entry: ");
        this.Entry = cfgEntry.RemoveHtmlTags().RemoveAll(new Il2CppStructArray<char>(new char[] { '\n', '\t', '\\', '"', '\'', '[', ']', '<', '>', '#', '/' })).TrimEnd();

        this.prefix = prefix;
        this.suffix = suffix;
        this.color = color;
    }

    public override string ToString()
    {
        string str = $"{this.prefix}{this.Text ?? this.Value}{this.suffix}";
        return color == null ? str : color.Value.Colorize(str);
    }

    public static Builder ToBuilder(object value = null, string cfgHeader = null, string cfgEntry = null, string prefix = "", string suffix = "", Color? color = null)
    {
        return new Builder(value, cfgHeader, cfgEntry, prefix, suffix, color);
    }

    public class Builder
    {
        private string text;
        private object value;
        private string cfgHeader;
        private string cfgEntry;
        private string prefix;
        private string suffix;
        private Color? color;

        public Builder(object value = null, string cfgHeader = null, string cfgEntry = null,
            string prefix = "", string suffix = "", Color? color = null)
        {
            this.text = null;
            this.value = value;
            this.cfgHeader = cfgHeader;
            this.cfgEntry = cfgEntry;
            this.prefix = prefix;
            this.suffix = suffix;
            this.color = color;
        }

        public Builder Text(string text)
        {
            this.text = text;
            return this;
        }

        public Builder Value(object value)
        {
            this.value = value;
            return this;
        }

        public Builder CfgHeader(string cfgHeader)
        {
            this.cfgHeader = cfgHeader;
            return this;
        }

        public Builder CfgEntry(string cfgEntry)
        {
            this.cfgEntry = cfgEntry;
            return this;
        }

        public Builder Prefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public Builder Suffix(string suffix)
        {
            this.suffix = suffix;
            return this;
        }

        public Builder Color(Color color)
        {
            this.color = color;
            return this;
        }

        public OptionValue Build()
        {
            return new OptionValue(value, cfgHeader, cfgEntry, text,  prefix, suffix, color);
        }

    }
}