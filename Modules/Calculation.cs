using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
namespace TownOfHost {
    class Calculation {
        //ランダムに重複しないindexのリストを返す
        //rangeBegin < rangeEnd , ランダムに取得する個数 count
        public static int[] GetRandomlySelectedMultiple(int rangeBegin, int rangeEnd, int count)
        {
            var rand = new System.Random();
            int[] randList = new int[count];
            List<int> intList = new List<int>();
            for(int i = 0; i < rangeEnd - rangeBegin; i++) {
                intList.Add(i);
            }

            int n = count;
            while (n > 0)
            {
                int k = rand.Next(0, rangeEnd - rangeBegin + n - count);
                n--;
                int intListValue = intList[k];
                randList[n] = intListValue;
                intList.RemoveAt(k);
            }

            return randList;
        }
    }
} 