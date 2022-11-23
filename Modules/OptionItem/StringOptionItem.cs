using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using TownOfHost;

namespace TownOfHost
{
    public class StringOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public string[] Selections;

        // コンストラクタ
        public StringOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, string[] selections)
        : base(id, name, defaultValue, tab, isSingleValue)
        {
            Rule = (0, selections.Length - 1, 1);
            Selections = selections;
        }
        public static StringOptionItem Create(
            int id, string name, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue
        )
        {
            return new StringOptionItem(
                id, name, defaultIndex, tab, isSingleValue, selections
            );
        }

    }
}