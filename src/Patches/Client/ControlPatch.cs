using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles;
using TOHTOR.Victory.Conditions;
using UnityEngine;
using VentLib.Localization;
using VentLib.Logging;

namespace TOHTOR.Patches.Client;

[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
class ControllerManagerUpdatePatch
{
    static readonly (int, int)[] resolutions = { (480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900), (1920, 1080) };
    static int resolutionIndex = 0;
    public static bool showPing = true;
    public static void Postfix(ControllerManager __instance)
    {
        //カスタム設定切り替え
        if (GameStates.IsLobby)
        {
            if (GetKeysDown(KeyCode.RightControl, KeyCode.Tab))
            {
                OptionShower.GetOptionShower().Previous();
            }

            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                OptionShower.GetOptionShower().Next();
                //OptionShower.Next();
            }


            for (var i = 0; i < 9; i++)
            {
                /*if (ORGetKeysDown(KeyCode.Alpha1 + i, KeyCode.Keypad1 + i) && OptionShower.pages.Count >= i + 1)
                    OptionShower.currentPage = i;*/
            }
        }
        //解像度変更
        if (Input.GetKeyDown(KeyCode.F11))
        {
            resolutionIndex++;
            if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
            ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
        }
        if (GetKeysDown(KeyCode.LeftShift | KeyCode.RightShift, KeyCode.F12))
        {
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            // probably a better way to do this ngl
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.MapButton.gameObject.SetActive(!hudManager.MapButton.gameObject.active);
            hudManager.SettingsButton.gameObject.SetActive(!hudManager.SettingsButton.gameObject.active);
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.TaskPanel.gameObject.SetActive(!hudManager.TaskPanel.gameObject.active);
            //   if (Game.State is not GameState.Freeplay) return; // WHERE TF IS MY FREEPLAY ONE
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.TaskStuff.gameObject.SetActive(!hudManager.TaskStuff.gameObject.active);
            hudManager.UseButton.gameObject.SetActive(!hudManager.UseButton.gameObject.active);
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.ReportButton.gameObject.SetActive(!hudManager.ReportButton.gameObject.active);
            if (PlayerControl.LocalPlayer.Data.RoleType is not RoleTypes.Impostor or RoleTypes.Crewmate)
                hudManager.AbilityButton.gameObject.SetActive(!hudManager.AbilityButton.gameObject.active);
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                hudManager.ImpostorVentButton.gameObject.SetActive(!hudManager.ImpostorVentButton.gameObject.active);
                hudManager.SabotageButton.gameObject.SetActive(!hudManager.SabotageButton.gameObject.active);
                hudManager.KillButton.gameObject.SetActive(!hudManager.KillButton.gameObject.active);
            }
            showPing = !showPing;
        }
        else if (Input.GetKeyDown(KeyCode.F12))
        {
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            // probably a better way to do this ngl
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.MapButton.gameObject.SetActive(!hudManager.MapButton.gameObject.active);
            hudManager.SettingsButton.gameObject.SetActive(!hudManager.SettingsButton.gameObject.active);
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.TaskPanel.gameObject.SetActive(!hudManager.TaskPanel.gameObject.active);
            //   if (Game.State is not GameState.Freeplay) return; // WHERE TF IS MY FREEPLAY ONE
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.TaskStuff.gameObject.SetActive(!hudManager.TaskStuff.gameObject.active);
            hudManager.UseButton.gameObject.SetActive(!hudManager.UseButton.gameObject.active);
            if (Game.State is GameState.None or GameState.InLobby) return;
            hudManager.ReportButton.gameObject.SetActive(!hudManager.ReportButton.gameObject.active);
            if (PlayerControl.LocalPlayer.Data.RoleType is not RoleTypes.Impostor or RoleTypes.Crewmate)
                hudManager.AbilityButton.gameObject.SetActive(!hudManager.AbilityButton.gameObject.active);
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                hudManager.ImpostorVentButton.gameObject.SetActive(!hudManager.ImpostorVentButton.gameObject.active);
                hudManager.SabotageButton.gameObject.SetActive(!hudManager.SabotageButton.gameObject.active);
                hudManager.KillButton.gameObject.SetActive(!hudManager.KillButton.gameObject.active);
            }
        }
        //カスタム翻訳のリロード
        if (GetKeysDown(KeyCode.F5, KeyCode.T))
        {
            VentLogger.Old("Reload Custom Translation File", "KeyCommand");
            Localizer.Initialize();
            VentLogger.SendInGame("Reloaded Custom Translation File");
        }
        //ログファイルのダンプ
        if (GetKeysDown(KeyCode.F1, KeyCode.LeftControl))
        {
            VentLogger.Old("Dump Logs", "KeyCommand");
            Utils.DumpLog();
        }
        //実行ファイルのフォルダを開く
        /*if (GetKeysDown(KeyCode.F10))
        {
            System.Diagnostics.Process.Start(System.Environment.CurrentDirectory);
        }*/

        //--以下ホスト専用コマンド--//
        if (!AmongUsClient.Instance.AmHost) return;
        //廃村
        if (GetKeysDown(KeyCode.Return, KeyCode.L, KeyCode.LeftShift) && GameStates.IsInGame)
        {
            ManualWin manualWin = new(new List<PlayerControl>(), WinReason.HostForceEnd);
            manualWin.Activate();
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
            VentLogger.Old("CountDownTimer set to 0", "KeyCommand");
            GameStartManager.Instance.countDownTimer = 0;
        }
        //カウントダウンキャンセル
        if (Input.GetKeyDown(KeyCode.C) && GameStates.IsCountDown)
        {
            VentLogger.Old("Reset CountDownTimer", "KeyCommand");
            GameStartManager.Instance.ResetStartState();
        }
        //現在の有効な設定の説明を表示
        if (GetKeysDown(KeyCode.N, KeyCode.LeftShift, KeyCode.LeftControl))
        {
            Utils.ShowActiveSettingsHelp();
        }
        //現在の有効な設定を表示
        if (GetKeysDown(KeyCode.N, KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
        {
            /*Utils.ShowActiveSettings();*/
        }
        //TOHオプションをデフォルトに設定
        if (GetKeysDown(KeyCode.Delete, KeyCode.LeftControl))
        {
            /*TOHPlugin.OptionManager.AllHolders.Do(h => h.valueHolder.Default());*/
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
            TOHPlugin.VisibleTasksCount = !TOHPlugin.VisibleTasksCount;
            DestroyableSingleton<HudManager>.Instance.Notifier.AddItem("VisibleTaskCountが" + TOHPlugin.VisibleTasksCount.ToString() + "に変更されました。");
        }
        //エアシップのトイレのドアを全て開ける
        if (Input.GetKeyDown(KeyCode.P))
        {
            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 79);
            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 80);
            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 81);
            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 82);
        }
        //現在の座標を取得
        if (Input.GetKeyDown(KeyCode.I))
            VentLogger.Old(PlayerControl.LocalPlayer.GetTruePosition().ToString(), "GetLocalPlayerPos");
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
        if (keys.Any(Input.GetKeyDown) && keys.All(Input.GetKey))
        {
            VentLogger.Old($"KeyDown:{keys.Where(Input.GetKeyDown).First()} in [{string.Join(",", keys)}]", "GetKeysDown");
            return true;
        }
        return false;
    }
    static bool ORGetKeysDown(params KeyCode[] keys) => keys.Any(Input.GetKeyDown);
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
            (PlayerControl.LocalPlayer.GetCustomRole() is Sheriff or Arsonist or Jackal) && PlayerControl.LocalPlayer.Data.Role.Role != RoleTypes.GuardianAngel)
        {
            DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
        }
        if (player.GetButtonDown(50) && !PlayerControl.LocalPlayer.GetCustomRole().CanVent())
        {
            DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick();
        }
    }
}