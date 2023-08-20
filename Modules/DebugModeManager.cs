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

        public static OptionItem EnableDebugMode;

        public static void Auth(HashAuth auth, string input)
        {
            // AmDebugger = デバッグビルドである || デバッグキー認証が通った
            AmDebugger = AmDebugger || auth.CheckString(input);
        }
        public static void SetupCustomOption()
        {
            EnableDebugMode = BooleanOptionItem.Create(2, "EnableDebugMode", false, TabGroup.MainSettings, true)
                .SetColor(Color.green)
                .SetHidden(!AmDebugger);
        }
    }
}