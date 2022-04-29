using System;
using HarmonyLib;
using UnityEngine;
using UnhollowerBaseLib;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerPatch
    {
        public static bool ShowDebugText = false;
        public static int LastCallNotifyRolesPerSecond = 0;
        public static int NowCallNotifyRolesCount = 0;
        public static int LastSetNameDesyncCount = 0;
        public static int LastFPS = 0;
        public static int NowFrameCount = 0;
        public static float FrameRateTimer = 0.0f;
        public static TMPro.TextMeshPro LowerInfoText;
        public static void Postfix(HudManager __instance)
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;
            var TaskTextPrefix = "";
            var FakeTasksText = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FakeTasks, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            //壁抜け
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started ||
                    AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                {
                    player.Collider.offset = new Vector2(0f, 127f);
                }
            }
            //壁抜け解除
            if (player.Collider.offset.y == 127f)
            {
                if (!Input.GetKey(KeyCode.LeftControl) || AmongUsClient.Instance.IsGameStarted)
                {
                    player.Collider.offset = new Vector2(0f, -0.3636f);
                }
            }

            __instance.GameSettings.text = OptionShower.getText();
            __instance.GameSettings.fontSizeMin =
            __instance.GameSettings.fontSizeMax = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || main.ForceJapanese.Value) ? 1.05f : 1.2f;
            //ゲーム中でなければ以下は実行されない
            if (!AmongUsClient.Instance.IsGameStarted) return;
            //バウンティハンターのターゲットテキスト
            if (LowerInfoText == null)
            {
                LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.buttonLabelText);
                LowerInfoText.transform.parent = __instance.transform;
                LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                LowerInfoText.alignment = TMPro.TextAlignmentOptions.Center;
                LowerInfoText.overflowMode = TMPro.TextOverflowModes.Overflow;
                LowerInfoText.enableWordWrapping = false;
                LowerInfoText.color = Palette.EnabledColor;
                LowerInfoText.fontSizeMin = 2.0f;
                LowerInfoText.fontSizeMax = 2.0f;
            }

            if (player.isBountyHunter())
            {
                //バウンティハンター用処理
                var target = player.getBountyTarget();
                LowerInfoText.text = target == null ? "null" : getString("BountyCurrentTarget") + ":" + player.getBountyTarget().name;
                LowerInfoText.enabled = target != null || main.AmDebugger.Value;
            }
            else if (player.isWitch())
            {
                //魔女用処理
                var ModeLang = player.GetKillOrSpell() ? "WitchModeSpell" : "WitchModeKill";
                LowerInfoText.text = getString("WitchCurrentMode") + ":" + getString(ModeLang);
                LowerInfoText.enabled = true;
            }
            else
            {
                //バウンティハンターじゃない
                LowerInfoText.enabled = false;
            }
            if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
            {
                LowerInfoText.enabled = false;
            }

            if (!player.getCustomRole().isVanilla())
            {
                TaskTextPrefix = $"<color={player.getRoleColorCode()}>{player.getRoleName()}\r\n";
                if (player.isMafia())
                {
                    if (!player.CanUseKillButton())
                        TaskTextPrefix += $"{getString("BeforeMafiaInfo")}";
                    else
                        TaskTextPrefix += $"{getString("AfterMafiaInfo")}";
                }
                else
                    TaskTextPrefix += $"{getString(player.getCustomRole() + "Info")}";
                TaskTextPrefix += "</color>\r\n";
            }
            switch (player.getCustomRole())
            {
                case CustomRoles.Madmate:
                case CustomRoles.SKMadmate:
                case CustomRoles.Jester:
                    TaskTextPrefix += FakeTasksText;
                    break;
                case CustomRoles.Mafia:
                    if (!player.CanUseKillButton())
                        __instance.KillButton.SetDisabled();
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                        player.Data.Role.CanUseKillButton = true;
                    break;
            }

            if (!__instance.TaskText.text.Contains(TaskTextPrefix)) __instance.TaskText.text = TaskTextPrefix + "\r\n" + __instance.TaskText.text;

            if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                Action<MapBehaviour> tmpAction = (MapBehaviour m) => { m.ShowSabotageMap(); };
                __instance.ShowMap(tmpAction);
                if (player.AmOwner)
                {
                    player.MyPhysics.inputHandler.enabled = true;
                    ConsoleJoystick.SetMode_Task();
                }
            }
            if (Input.GetKeyDown(KeyCode.F3)) ShowDebugText = !ShowDebugText;
            if (ShowDebugText)
            {
                string text = "==Debug State==\r\n";
                text += "Frame Per Second: " + LastFPS + "\r\n";
                text += "Call Notify Roles Per Second: " + LastCallNotifyRolesPerSecond + "\r\n";
                text += "Last Set Name Desync Count: " + LastSetNameDesyncCount;
                __instance.TaskText.text = text;
            }
            if (FrameRateTimer >= 1.0f)
            {
                FrameRateTimer = 0.0f;
                LastFPS = NowFrameCount;
                LastCallNotifyRolesPerSecond = NowCallNotifyRolesCount;
                NowFrameCount = 0;
                NowCallNotifyRolesCount = 0;
            }
            NowFrameCount++;
            FrameRateTimer += Time.deltaTime;

            if (AmongUsClient.Instance.GameMode == GameModes.OnlineGame) RepairSender.enabled = false;
            if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.GameMode != GameModes.OnlineGame)
            {
                RepairSender.enabled = !RepairSender.enabled;
                RepairSender.Reset();
            }
            if (RepairSender.enabled && AmongUsClient.Instance.GameMode != GameModes.OnlineGame)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
                if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
                if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
                if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
                if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
                if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
                if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
                if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
                if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
                __instance.TaskText.text = RepairSender.getText();
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
    class ToggleHighlightPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team)
        {
            var player = PlayerControl.LocalPlayer;
            if ((player.getCustomRole() == CustomRoles.Sheriff || player.getCustomRole() == CustomRoles.Arsonist) && !player.Data.IsDead)
            {
                ((Renderer)__instance.MyRend).material.SetColor("_OutlineColor", Utils.getRoleColor(player.getCustomRole()));
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
    class FindClosestTargetPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] ref bool protecting)
        {
            var player = PlayerControl.LocalPlayer;
            if ((player.getCustomRole() == CustomRoles.Sheriff || player.getCustomRole() == CustomRoles.Arsonist) &&
                __instance.Data.Role.Role != RoleTypes.GuardianAngel)
            {
                protecting = true;
            }
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
    class SetHudActivePatch
    {
        public static void Postfix(HudManager __instance, [HarmonyArgument(0)] bool isActive)
        {
            var player = PlayerControl.LocalPlayer;
            switch (player.getCustomRole())
            {
                case CustomRoles.Sheriff:
                    if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                        __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
                    __instance.SabotageButton.ToggleVisible(false);
                    __instance.ImpostorVentButton.ToggleVisible(false);
                    __instance.AbilityButton.ToggleVisible(false);
                    break;
                case CustomRoles.Arsonist:
                    __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
                    __instance.SabotageButton.ToggleVisible(false);
                    __instance.ImpostorVentButton.ToggleVisible(false);
                    __instance.AbilityButton.ToggleVisible(false);
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    class ShowNormalMapPatch
    {
        public static void Prefix(ref RoleTeamTypes __state)
        {
            var player = PlayerControl.LocalPlayer;
            if (player.isSheriff() || player.isArsonist())
            {
                __state = player.Data.Role.TeamType;
                player.Data.Role.TeamType = RoleTeamTypes.Crewmate;
            }
        }

        public static void Postfix(ref RoleTeamTypes __state)
        {
            var player = PlayerControl.LocalPlayer;
            if (player.isSheriff() || player.isArsonist())
            {
                player.Data.Role.TeamType = __state;
            }
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
    class CoShowIntroPatch {
        public static void Prefix(HudManager __instance) {
            Logger.info("--------名前表示--------");
            foreach(var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info($"{pc.PlayerId}:{pc.name}:{pc.nameText.text}");
                main.RealNames[pc.PlayerId] = pc.name;
                pc.nameText.text = pc.name; 
            }
            Logger.info("------役職割り当て------");
            foreach(var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info($"{pc.name}({pc.PlayerId}):{pc.getRoleName()}");
            }
            Logger.info("----------環境----------");
            foreach(var pc in PlayerControl.AllPlayerControls)
            {
                var text = pc.PlayerId == PlayerControl.LocalPlayer.PlayerId ? "[*]" : "";
                text += $"{pc.PlayerId}:{pc.name}:{(pc.getClient().PlatformData.Platform).ToString().Replace("Standalone","")}";
                if(main.playerVersion.TryGetValue(pc.PlayerId,out PlayerVersion pv))
                {
                    text += $":Mod({pv.version}:";
                    text += $"{pv.tag})";
                }else text += ":Vanilla";
                Logger.info(text);
            }
            Logger.info("--------基本設定--------");
            Logger.info(PlayerControl.GameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10));
            Logger.info("---------その他---------");
            Logger.info($"プレイヤー数: {PlayerControl.AllPlayerControls.Count}人");

            if (!AmongUsClient.Instance.AmHost)
            {
                //クライアントの役職初期設定はここで行う
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    PlayerState.InitTask(pc);
                }
                Utils.CountAliveImpostors();
            }
        }
    }
    class RepairSender
    {
        public static bool enabled = false;
        public static bool TypingAmount = false;

        public static int SystemType;
        public static int amount;

        public static void Input(int num)
        {
            if (!TypingAmount)
            {
                //SystemType入力中
                SystemType = SystemType * 10;
                SystemType += num;
            }
            else
            {
                //Amount入力中
                amount = amount * 10;
                amount += num;
            }
        }
        public static void InputEnter()
        {
            if (!TypingAmount)
            {
                //SystemType入力中
                TypingAmount = true;
            }
            else
            {
                //Amount入力中
                send();
            }
        }
        public static void send()
        {
            ShipStatus.Instance.RpcRepairSystem((SystemTypes)SystemType, amount);
            Reset();
        }
        public static void Reset()
        {
            TypingAmount = false;
            SystemType = 0;
            amount = 0;
        }
        public static string getText()
        {
            return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
        }
    }
}
