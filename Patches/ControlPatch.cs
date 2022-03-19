using HarmonyLib;
using UnityEngine;
using Hazel;
using InnerNet;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
    class DebugManager
    {
        static System.Random random = new System.Random();
        static PlayerControl bot;
        public static void Postfix(ControllerManager __instance)
        {

            //##ホスト専用コマンド##
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.L) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPC.EndGame();
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) && GameStartManager._instance && AmongUsClient.Instance.AmHost)
            {
                Logger.info("CountDownTimer set to 0");
                GameStartManager.Instance.countDownTimer = 0;
            }
            if (Input.GetKeyDown(KeyCode.C) && GameStartManager._instance && AmongUsClient.Instance.AmHost)
            {
                Logger.info("Reset CountDownTimer");
                GameStartManager.Instance.ResetStartState();
            }
            if (Input.GetKeyDown(KeyCode.N) && Input.GetKeyDown(KeyCode.LeftControl) && AmongUsClient.Instance.AmHost)
            {
                Utils.ShowActiveRoles();
            }
            //====================
            //##テスト用キーコマンド##
            // | キー | 条件 | 動作 |
            // | ---- | ---- | ---- |
            // | X | フリープレイ中 | キルクール0 |
            // | Y | ホスト | カスタム設定同期 |
            // | O | フリープレイ中 | 全タスク完了 |
            // | G | フリープレイ中 | 開始画面表示 |
            // | = | フリープレイ中 | VisibleTaskCountを切り替え |
            // | P | フリープレイ中 | トイレのドアを一気に開ける |
            // | U | オンライン以外 | 自分の投票をClearする |
            // | N | ホストデバッガー | プレイヤーを生成 |
            //====================


            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.N) && AmongUsClient.Instance.AmHost && main.AmDebugger.Value)
            {
                //これいつか革命を起こしてくれるコードなので絶対に消さないでください
                if (bot == null)
                {
                    bot = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                    bot.PlayerId = 15;
                    GameData.Instance.AddPlayer(bot);
                    AmongUsClient.Instance.Spawn(bot, -2, SpawnFlags.None);
                    bot.transform.position = PlayerControl.LocalPlayer.transform.position;
                    bot.NetTransform.enabled = true;
                    GameData.Instance.RpcSetTasks(bot.PlayerId, new byte[0]);
                }

                bot.RpcSetColor((byte)PlayerControl.LocalPlayer.CurrentOutfit.ColorId);
                bot.RpcSetName(PlayerControl.LocalPlayer.name);
                bot.RpcSetPet(PlayerControl.LocalPlayer.CurrentOutfit.PetId);
                bot.RpcSetSkin(PlayerControl.LocalPlayer.CurrentOutfit.SkinId);
                bot.RpcSetNamePlate(PlayerControl.LocalPlayer.CurrentOutfit.NamePlateId);

                new LateTask(() => bot.NetTransform.RpcSnapTo(new Vector2(0, 15)), 0.2f, "Bot TP Task");
                new LateTask(() => { foreach (var pc in PlayerControl.AllPlayerControls) pc.RpcMurderPlayer(bot); }, 0.4f, "Bot Kill Task");
                new LateTask(() => bot.Despawn(), 0.6f, "Bot Despawn Task");
            }
            if (Input.GetKeyDown(KeyCode.X) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
            }
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost)
            {
                MeetingHud.Instance.RpcClose();
            }
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.RightShift) && AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.ReportDeadBody(PlayerControl.LocalPlayer.Data);
            }
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcExile();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (AmongUsClient.Instance.GameMode != GameModes.OnlineGame && main.AmDebugger.Value)
                {
                    MeetingHud.Instance.RpcClearVote(AmongUsClient.Instance.ClientId);
                }
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        foreach (var task in pc.myTasks)
                        {
                            pc.RpcCompleteTask(task.Id);
                        }
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.G) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                var list = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                list.Add(PlayerControl.LocalPlayer);
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro(list));
            }
            if (Input.GetKeyDown(KeyCode.Equals) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                main.VisibleTasksCount = !main.VisibleTasksCount;
                DestroyableSingleton<HudManager>.Instance.Notifier.AddItem("VisibleTaskCountが" + main.VisibleTasksCount.ToString() + "に変更されました。");
            }
            if (Input.GetKeyDown(KeyCode.P) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 82);
            }
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

            //##カスタム設定コマンド##
            if (Input.GetKeyDown(KeyCode.Tab) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined)
            {
                //Logger.SendInGame("tabキーが押されました");
                main.OptionControllerIsEnable = !main.OptionControllerIsEnable;
                CustomOptionController.currentPage = CustomOptionController.basePage;
                CustomOptionController.currentCursor = 0;
            }
            if (main.OptionControllerIsEnable)
            {
                main.TextCursorTimer += Time.deltaTime;
                if (main.TextCursorTimer > 0.5f)
                {
                    main.TextCursorTimer = 0f;
                    main.TextCursorVisible = !main.TextCursorVisible;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)) CustomOptionController.Up();
                if (Input.GetKeyDown(KeyCode.DownArrow)) CustomOptionController.Down();
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    CustomOptionController.Enter();
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    CustomOptionController.Return();
                }
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) CustomOptionController.Input(0);
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) CustomOptionController.Input(1);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) CustomOptionController.Input(2);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) CustomOptionController.Input(3);
                if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) CustomOptionController.Input(4);
                if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) CustomOptionController.Input(5);
                if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) CustomOptionController.Input(6);
                if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) CustomOptionController.Input(7);
                if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) CustomOptionController.Input(8);
                if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) CustomOptionController.Input(9);
            }
        }
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
            if (player.GetButtonDown(8) &&
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.isSheriff())
            {
                DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
            }
        }
    }
}
