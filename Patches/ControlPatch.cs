using System.Linq;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Modules;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
    class ControllerManagerUpdatePatch
    {
        static readonly (int, int)[] resolutions = { (480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900), (1920, 1080) };
        static int resolutionIndex = 0;
        public static void Postfix(ControllerManager __instance)
        {
            if (GameStates.IsLobby)
            {
                //カスタム設定切り替え
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    OptionShower.Next();
                }
                for (var i = 0; i < 9; i++)
                {
                    if (ORGetKeysDown(KeyCode.Alpha1 + i, KeyCode.Keypad1 + i) && OptionShower.pages.Count >= i + 1)
                        OptionShower.currentPage = i;
                }
                // 現在の設定を文字列形式のデータに変換してコピー
                if (GetKeysDown(KeyCode.O, KeyCode.LeftAlt))
                {
                    OptionSerializer.SaveToClipboard();
                }
                // 現在の設定を文字列形式のデータに変換してファイルに出力
                if (GetKeysDown(KeyCode.L, KeyCode.LeftAlt))
                {
                    OptionSerializer.SaveToFile();
                }
                // クリップボードから文字列形式の設定データを読み込む
                if (GetKeysDown(KeyCode.P, KeyCode.LeftAlt))
                {
                    OptionSerializer.LoadFromClipboard();
                }
            }
            //解像度変更
            if (Input.GetKeyDown(KeyCode.F11))
            {
                resolutionIndex++;
                if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
                ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
            }
            //カスタム翻訳のリロード
            if (GetKeysDown(KeyCode.F5, KeyCode.T))
            {
                Logger.Info("Reload Custom Translation File", "KeyCommand");
                Translator.LoadLangs();
                Logger.SendInGame("Reloaded Custom Translation File");
            }
            if (GetKeysDown(KeyCode.F5, KeyCode.X))
            {
                Logger.Info("Export Custom Translation File", "KeyCommand");
                Translator.ExportCustomTranslation();
                Logger.SendInGame("Exported Custom Translation File");
            }
            //ログファイルのダンプ
            if (GetKeysDown(KeyCode.F1, KeyCode.LeftControl))
            {
                Logger.Info("Dump Logs", "KeyCommand");
                Utils.DumpLog();
            }
            //現在の設定をテキストとしてコピー
            if (GetKeysDown(KeyCode.LeftAlt, KeyCode.C) && !Input.GetKey(KeyCode.LeftShift) && !GameStates.IsNotJoined)
            {
                Utils.CopyCurrentSettings();
            }
            //実行ファイルのフォルダを開く
            if (GetKeysDown(KeyCode.F10))
            {
                Utils.OpenDirectory(System.Environment.CurrentDirectory);
            }

            //--以下ホスト専用コマンド--//
            if (!AmongUsClient.Instance.AmHost) return;
            //廃村
            if (GetKeysDown(KeyCode.Return, KeyCode.L, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
            }
            //ミーティングを強制終了
            if (GetKeysDown(KeyCode.Return, KeyCode.M, KeyCode.LeftShift) && GameStates.IsMeeting)
            {
                MeetingHud.Instance.RpcClose();
            }
            //即スタート
            if (Input.GetKeyDown(KeyCode.LeftShift) && GameStates.IsCountDown)
            {
                Logger.Info("CountDownTimer set to 0", "KeyCommand");
                GameStartManager.Instance.countDownTimer = 0;
            }
            //カウントダウンキャンセル
            if (Input.GetKeyDown(KeyCode.C) && GameStates.IsCountDown)
            {
                Logger.Info("Reset CountDownTimer", "KeyCommand");
                GameStartManager.Instance.ResetStartState();
            }
            //現在の有効な設定の説明を表示
            if (GetKeysDown(KeyCode.N, KeyCode.LeftShift, KeyCode.LeftControl))
            {
                Main.isChatCommand = true;
                Utils.ShowActiveSettingsHelp();
            }
            //現在の有効な設定を表示
            if (GetKeysDown(KeyCode.N, KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
            {
                Main.isChatCommand = true;
                Utils.ShowActiveSettings();
            }
            //TOHオプションをデフォルトに設定
            if (GetKeysDown(KeyCode.Delete, KeyCode.LeftControl))
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
            }

            //--以下デバッグモード用コマンド--//
            if (!DebugModeManager.IsDebugMode) return;

            //設定の同期
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RPC.SyncCustomSettingsRPC();
            }
            //投票をクリア
            if (Input.GetKeyDown(KeyCode.V) && GameStates.IsMeeting && !GameStates.IsOnlineGame)
            {
                MeetingHud.Instance.RpcClearVote(AmongUsClient.Instance.ClientId);
            }
            //自分自身の死体をレポート
            if (GetKeysDown(KeyCode.Return, KeyCode.M, KeyCode.RightShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.NoCheckStartMeeting(PlayerControl.LocalPlayer.Data);
            }
            //自分自身を追放
            if (GetKeysDown(KeyCode.Return, KeyCode.E, KeyCode.LeftShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.RpcExile();
            }
            //ログをゲーム内にも出力するかトグル
            if (GetKeysDown(KeyCode.F2, KeyCode.LeftControl))
            {
                Logger.isAlsoInGame = !Logger.isAlsoInGame;
                Logger.SendInGame($"ログのゲーム内出力: {Logger.isAlsoInGame}");
            }

            //--以下フリープレイ用コマンド--//
            if (!GameStates.IsFreePlay) return;
            //キルクールを0秒に設定
            if (Input.GetKeyDown(KeyCode.X))
            {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            //自身のタスクをすべて完了
            if (Input.GetKeyDown(KeyCode.O))
            {
                foreach (var task in PlayerControl.LocalPlayer.myTasks)
                    PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            }
            //イントロテスト
            if (Input.GetKeyDown(KeyCode.G))
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            }
            //タスクカウントの表示切替
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                Main.VisibleTasksCount = !Main.VisibleTasksCount;
                DestroyableSingleton<HudManager>.Instance.Notifier.AddItem("VisibleTaskCountが" + Main.VisibleTasksCount.ToString() + "に変更されました。");
            }
            //エアシップのトイレのドアを全て開ける
            if (Input.GetKeyDown(KeyCode.P))
            {
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
            }
            //現在の座標を取得
            if (Input.GetKeyDown(KeyCode.I))
                Logger.Info(PlayerControl.LocalPlayer.GetTruePosition().ToString(), "GetLocalPlayerPos");
            //マスゲーム用コード
            /*if (Input.GetKeyDown(KeyCode.C))
            {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) pc.MyPhysics.RpcEnterVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                Vector2 pos = PlayerControl.LocalPlayer.NetTransform.transform.position;
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) {
                        pc.NetTransform.RpcSnapTo(pos);
                        pos.x += 0.5f;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) pc.MyPhysics.RpcExitVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                VentilationSystem.Update(VentilationSystem.Operation.StartCleaning, 0);
            }*/
            //マスゲーム用コード終わり
        }
        static bool GetKeysDown(params KeyCode[] keys)
        {
            if (keys.Any(k => Input.GetKeyDown(k)) && keys.All(k => Input.GetKey(k)))
            {
                Logger.Info($"KeyDown:{keys.Where(k => Input.GetKeyDown(k)).First()} in [{string.Join(",", keys)}]", "GetKeysDown");
                return true;
            }
            return false;
        }
        static bool ORGetKeysDown(params KeyCode[] keys) => keys.Any(k => Input.GetKeyDown(k));
    }

    [HarmonyPatch(typeof(ConsoleJoystick), nameof(ConsoleJoystick.HandleHUD))]
    class ConsoleJoystickHandleHUDPatch
    {
        public static void Postfix()
        {
            HandleHUDPatch.Postfix(ConsoleJoystick.player);
        }
    }
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.HandleHud))]
    class KeyboardJoystickHandleHUDPatch
    {
        public static void Postfix()
        {
            HandleHUDPatch.Postfix(KeyboardJoystick.player);
        }
    }
    class HandleHUDPatch
    {
        public static void Postfix(Rewired.Player player)
        {
            if (player.GetButtonDown(8) && // 8:キルボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.CanUseKillButton())
            {
                DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
            }
            if (player.GetButtonDown(50) && // 50:インポスターのベントボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.CanUseImpostorVentButton())
            {
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick();
            }
        }
    }
}