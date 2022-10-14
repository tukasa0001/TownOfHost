using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public class ModUpdater
    {
        private static readonly string URL = "https://api.github.com/repos/tukasa0001/TownOfHost";
        public static bool hasUpdate = false;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static Version latestVersion = null;
        public static string latestTitle = null;
        public static string downloadUrl = null;
        public static GenericPopup InfoPopup;
    }
}