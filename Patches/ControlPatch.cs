using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
    class ControllerManagerUpdatePatch
    {
        static readonly System.Random random = new();
        static PlayerControl bot;
        static readonly (int, int)[] resolutions = { (480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900) };
        static int resolutionIndex = 0;
        public static void Postfix(ControllerManager __instance)
        {
            //カスタム設定切り替え
            if (Input.GetKeyDown(KeyCode.Tab) && GameStates.IsLobby)
            {
                OptionShower.Next();
            }
            //解像度変更
            if (Input.GetKeyDown(KeyCode.F11))
            {
                resolutionIndex++;
                if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
                ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
            }
            //ログファイルのダンプ
            if (Input.GetKeyDown(KeyCode.F1) && Input.GetKey(KeyCode.LeftControl))
            {
                Logger.Info("Dump Logs", "KeyCommand");
                Utils.DumpLog();
            }

            //--以下ホスト専用コマンド--//
            if (!AmongUsClient.Instance.AmHost) return;
            //廃村
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.L) && Input.GetKey(KeyCode.LeftShift) && GameStates.IsInGame)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPC.EndGame();
            }
            //ミーティングを強制終了
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.LeftShift) && GameStates.IsMeeting)
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
            //現在の有効な設定を表示
            if (Input.GetKeyDown(KeyCode.N) && Input.GetKey(KeyCode.LeftControl))
            {
                Utils.ShowActiveSettingsHelp();
            }
            //TOHオプションをデフォルトに設定
            if (Input.GetKeyDown(KeyCode.Delete) && Input.GetKey(KeyCode.LeftControl) && GameObject.Find(GameOptionsMenuPatch.TownOfHostObjectName) != null)
            {
                CustomOption.Options.ToArray().Where(x => x.Id > 0).Do(x => x.UpdateSelection(x.DefaultSelection));
            }

            //--以下デバッグモード用コマンド--//
            if (!Main.AmDebugger.Value) return;

            //BOTの作成
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.N))
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
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.RightShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.NoCheckStartMeeting(PlayerControl.LocalPlayer.Data);
            }
            //自分自身を追放
            if (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.LeftShift) && GameStates.IsInGame)
            {
                PlayerControl.LocalPlayer.RpcExile();
            }
            //ログをゲーム内にも出力するかトグル
            if (Input.GetKeyDown(KeyCode.F2) && Input.GetKey(KeyCode.LeftControl))
            {
                Logger.isAlsoInGame = !Logger.isAlsoInGame;
                Logger.SendInGame($"ログのゲーム内出力: {Logger.isAlsoInGame}");
            }
            //RpcResetAbilityCooldownのテスト
            if (Input.GetKey(KeyCode.R))
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) PlayerControl.LocalPlayer.RpcResetAbilityCooldown();
                if (Input.GetKeyDown(KeyCode.Alpha1)) Utils.GetPlayerById(1)?.RpcResetAbilityCooldown();
                if (Input.GetKeyDown(KeyCode.Alpha2)) Utils.GetPlayerById(2)?.RpcResetAbilityCooldown();
                if (Input.GetKeyDown(KeyCode.Alpha3)) Utils.GetPlayerById(3)?.RpcResetAbilityCooldown();
                if (Input.GetKeyDown(KeyCode.Alpha4)) Utils.GetPlayerById(4)?.RpcResetAbilityCooldown();
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
            if (player.GetButtonDown(8) && // 8:キルボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            (PlayerControl.LocalPlayer.Is(CustomRoles.Sheriff) || PlayerControl.LocalPlayer.Is(CustomRoles.Arsonist)) && PlayerControl.LocalPlayer.Data.Role.Role != RoleTypes.GuardianAngel)
            {
                DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
            }
            if (player.GetButtonDown(50) && // 50:インポスターのベントボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.Is(CustomRoles.Arsonist) && PlayerControl.LocalPlayer.Data.Role.Role != RoleTypes.GuardianAngel)
            {
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick();
            }
        }
    }
}