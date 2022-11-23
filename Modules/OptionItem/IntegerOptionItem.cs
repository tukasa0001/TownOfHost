using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using TownOfHost;

namespace TownOfHost
{
    public class IntegerOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;

        // コンストラクタ
        public IntegerOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, IntegerValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue)
        {
            Rule = rule;
        }
        public static IntegerOptionItem Create(
            int id, string name, IntegerValueRule rule, int defaultValue, TabGroup tab, bool isSingleValue
        )
        {
            return new IntegerOptionItem(
                id, name, defaultValue, tab, isSingleValue, rule
            );
        }

    }
}