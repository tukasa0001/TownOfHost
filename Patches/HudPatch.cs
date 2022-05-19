using System;
using HarmonyLib;
using UnhollowerBaseLib;
using UnityEngine;
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

            __instance.GameSettings.text = OptionShower.GetText();
            __instance.GameSettings.fontSizeMin =
            __instance.GameSettings.fontSizeMax = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || Main.ForceJapanese.Value) ? 1.05f : 1.2f;
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

            if (player.Is(CustomRoles.BountyHunter))
            {
                //バウンティハンター用処理
                var target = player.GetBountyTarget();
                LowerInfoText.text = target == null ? "null" : GetString("BountyCurrentTarget") + ":" + player.GetBountyTarget().name;
                LowerInfoText.enabled = target != null || Main.AmDebugger.Value;
            }
            else if (player.Is(CustomRoles.Witch))
            {
                //魔女用処理
                var ModeLang = player.GetKillOrSpell() ? "WitchModeSpell" : "WitchModeKill";
                LowerInfoText.text = GetString("WitchCurrentMode") + ":" + GetString(ModeLang);
                LowerInfoText.enabled = true;
            }
            else if (player.Is(CustomRoles.FireWorks))
            {
                var stateText = FireWorks.GetStateText(player);
                LowerInfoText.text = stateText;
                LowerInfoText.enabled = true;
            }
            else
            {
                LowerInfoText.enabled = false;
            }
            if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
            {
                LowerInfoText.enabled = false;
            }

            if (!player.GetCustomRole().IsVanilla())
            {
                TaskTextPrefix = $"<color={player.GetRoleColorCode()}>{player.GetRoleName()}\r\n";
                if (player.Is(CustomRoles.Mafia))
                {
                    if (!player.CanUseKillButton())
                        TaskTextPrefix += $"{GetString("BeforeMafiaInfo")}";
                    else
                        TaskTextPrefix += $"{GetString("AfterMafiaInfo")}";
                }
                else
                    TaskTextPrefix += $"{GetString(player.GetCustomRole() + "Info")}";
                TaskTextPrefix += "</color>\r\n";
            }
            switch (player.GetCustomRole())
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
                    player.CanUseImpostorVent();
                    goto DesyncImpostor;

                DesyncImpostor:
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
                __instance.TaskText.text = RepairSender.GetText();
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
    class ToggleHighlightPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var player = PlayerControl.LocalPlayer;
            if ((player.GetCustomRole() == CustomRoles.Sheriff || player.GetCustomRole() == CustomRoles.Arsonist) && !player.Data.IsDead)
            {
                __instance.MyRend.material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
    class FindClosestTargetPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] ref bool protecting)
        {
            var player = PlayerControl.LocalPlayer;
            if ((player.GetCustomRole() == CustomRoles.Sheriff || player.GetCustomRole() == CustomRoles.Arsonist) &&
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
            switch (player.GetCustomRole())
            {
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    if (player.Data.Role.Role != RoleTypes.GuardianAngel)
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
            if (player.Is(CustomRoles.Sheriff) || player.Is(CustomRoles.Arsonist))
            {
                __state = player.Data.Role.TeamType;
                player.Data.Role.TeamType = RoleTeamTypes.Crewmate;
            }
        }

        public static void Postfix(ref RoleTeamTypes __state)
        {
            var player = PlayerControl.LocalPlayer;
            if (player.Is(CustomRoles.Sheriff) || player.Is(CustomRoles.Arsonist))
            {
                player.Data.Role.TeamType = __state;
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
                SystemType *= 10;
                SystemType += num;
            }
            else
            {
                //Amount入力中
                amount *= 10;
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
                Send();
            }
        }
        public static void Send()
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
        public static string GetText()
        {
            return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
        }
    }
}