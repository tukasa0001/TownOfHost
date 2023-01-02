using System;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using static TownOfHost.Translator;
using TownOfHost.Roles;

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
                if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                    && player.CanMove)
                {
                    player.Collider.offset = new Vector2(0f, 127f);
                }
            }
            //壁抜け解除
            if (player.Collider.offset.y == 127f)
            {
                if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
                {
                    player.Collider.offset = new Vector2(0f, -0.3636f);
                }
            }
            __instance.GameSettings.text = OptionShower.GetText();
            __instance.GameSettings.fontSizeMin =
            __instance.GameSettings.fontSizeMax = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || Main.ForceJapanese.Value) ? 1.05f : 1.2f;
            //ゲーム中でなければ以下は実行されない
            if (!AmongUsClient.Instance.IsGameStarted) return;

            Utils.CountAliveImpostors();

            if (SetHudActivePatch.IsActive)
            {//MOD入り用のボタン下テキスト変更
                switch (player.GetCustomRole())
                {
                    /*case Sniper:
                        __instance.AbilityButton.OverrideText(SniperOLD.OverrideShapeText(player.PlayerId));
                        break;*/
                    case FireWorks:
                        __instance.AbilityButton.OverrideText($"{GetString("FireWorksExplosionButtonText")}");
                        break;
                    /*case SerialKiller:
                        // ? What ?
                        SerialKillerOLD.GetAbilityButtonText(__instance, player);
                        break;*/
                    case Warlock:
                        if (!Main.CheckShapeshift[player.PlayerId] && !Main.isCurseAndKill[player.PlayerId])
                        {
                            __instance.KillButton.OverrideText($"{GetString("WarlockCurseButtonText")}");
                        }
                        else
                        {
                            __instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
                        }
                        break;
                    /*case Witch:
                        WitchOLD.GetAbilityButtonText(__instance);
                        break;*/
                    case Vampire:
                        __instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
                        break;
                    case Arsonist:
                        __instance.KillButton.OverrideText($"{GetString("ArsonistDouseButtonText")}");
                        break;
                    case Puppeteer:
                        __instance.KillButton.OverrideText($"{GetString("PuppeteerOperateButtonText")}");
                        break;
                    /*case BountyHunter:
                        BountyHunterOLD.GetAbilityButtonText(__instance);
                        break;
                    case EvilTracker:
                        EvilTrackerOLD.GetAbilityButtonText(__instance, player.PlayerId);
                        break;*/
                }

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


                LowerInfoText.enabled = false;
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }
                switch (player.GetCustomRole())
                {
                    case Madmate:
                    case SKMadmate:
                    case Jester:
                        TaskTextPrefix += FakeTasksText;
                        break;
                    case Sheriff:
                    case Arsonist:
                    case Jackal:
                        player.CanUseImpostorVent();
                        if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                            player.Data.Role.CanUseKillButton = true;
                        break;
                }
            }


            if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                __instance.ToggleMapVisible(new MapOptions()
                {
                    Mode = MapOptions.Modes.Sabotage,
                    AllowMovementWhileMapOpen = true
                });
                if (player.AmOwner)
                {
                    player.MyPhysics.inputHandler.enabled = true;
                    ConsoleJoystick.SetMode_Task();
                }
            }

            if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
            if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                RepairSender.enabled = !RepairSender.enabled;
                RepairSender.Reset();
            }
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
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
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
    class ToggleHighlightPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team)
        {
            var player = PlayerControl.LocalPlayer;
            if (!GameStates.IsInTask) return;

            if ((player.GetCustomRole() is Sheriff ||
                player.GetCustomRole() is Arsonist ||
                player.GetCustomRole() is Jackal)
            && !player.Data.IsDead)
            {
                ((Renderer)__instance.cosmetics.currentBodySprite.BodySprite).material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
            }
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
    class SetVentOutlinePatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
        {
            var player = PlayerControl.LocalPlayer;
            Color color = PlayerControl.LocalPlayer.GetRoleColor();
            ((Renderer)__instance.myRend).material.SetColor("_OutlineColor", color);
            ((Renderer)__instance.myRend).material.SetColor("_AddColor", mainTarget ? color : Color.clear);
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
    class SetHudActivePatch
    {
        public static bool IsActive = false;
        public static void Postfix(HudManager __instance, [HarmonyArgument(0)] bool isActive)
        {
            IsActive = isActive;
            if (!isActive) return;

            var player = PlayerControl.LocalPlayer;
            switch (player.GetCustomRole())
            {
                case Sheriff:
                case Arsonist:
                    if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                        __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
                    __instance.SabotageButton.ToggleVisible(false);
                    __instance.ImpostorVentButton.ToggleVisible(false);
                    __instance.AbilityButton.ToggleVisible(false);
                    break;
                case Jackal:
                    if (player.GetCustomRole() is not Impostor impostor) break;

                    if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                        __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
                    __instance.SabotageButton.ToggleVisible(isActive && impostor.CanSabotage());
                    __instance.ImpostorVentButton.ToggleVisible(isActive && impostor.CanVent());
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
            if (player.Is(Sheriff.Ref<Sheriff>()) || player.Is(Arsonist.Ref<Arsonist>()))
            {
                __state = player.Data.Role.TeamType;
                player.Data.Role.TeamType = RoleTeamTypes.Crewmate;
            }
        }

        public static void Postfix(ref RoleTeamTypes __state)
        {
            var player = PlayerControl.LocalPlayer;
            if (player.Is(Sheriff.Ref<Sheriff>()) || player.Is(Arsonist.Ref<Arsonist>()))
            {
                player.Data.Role.TeamType = __state;
            }
        }
    }
    [HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
    class TaskPanelBehaviourPatch
    {
        // タスク表示の文章が更新・適用された後に実行される
        public static void Postfix(TaskPanelBehaviour __instance)
        {
            PlayerControl player = PlayerControl.LocalPlayer;

            // 役職説明表示
            if (!player.GetCustomRole().IsVanilla())
            {
                var RoleWithInfo = $"{player.GetDisplayRoleName()}:\r\n";
                RoleWithInfo += player.GetRoleInfo();
                __instance.taskText.text = Utils.ColorString(player.GetRoleColor(), RoleWithInfo) + "\n" + __instance.taskText.text;
            }

            // RepairSenderの表示
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                __instance.taskText.text = RepairSender.GetText();
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