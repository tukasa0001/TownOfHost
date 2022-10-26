using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TownOfHost
{
    public static class DebugModeManager
    {
        // これが有効の時、通常のゲームに支障のないデバッグ機能(詳細ログ・ゲーム外でのデバッグ表示など)が有効化される。
        // また、ゲーム内オプションでデバッグモードを有効化することができる。
        public static bool AmDebugger { get; private set; } =
#if DEBUG
    true;
#else
    false;
#endif
        // これが有効の時、通常のゲームを破壊する可能性のある強力なデバッグ機能(テレポートなど)が有効化される。
        public static bool IsDebugMode => AmDebugger && EnableDebugMode != null && EnableDebugMode.GetBool();

        public static CustomOption EnableDebugMode;

        public static void SetupCustomOption()
        {
            EnableDebugMode = CustomOption.Create(2, TabGroup.MainSettings, Color.green, "EnableDebugMode", false, null, true, !AmDebugger);
        }
    }
}