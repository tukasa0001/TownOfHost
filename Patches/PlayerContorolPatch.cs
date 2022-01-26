using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost)
                return;
            Logger.SendToFile("MurderPlayer発生: " + __instance.name + "=>" + target.name);
            //When Bait is killed
            if (target.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRoles.Bait && AmongUsClient.Instance.AmHost
            && __instance.PlayerId != target.PlayerId)
            {
                Logger.SendToFile(target.name + "はBaitだった");
                Thread.Sleep(150); //Fix This
                __instance.CmdReportDeadBody(target.Data);
            }
            else
            //Terrorist
            if (main.isTerrorist(target))
            {
                Logger.SendToFile(target.name + "はTerroristだった");
                main.CheckTerroristWin(target.Data);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.SendToFile("CheckMurder発生: " + __instance.name + "=>" + target.name);
            if(main.IsHideAndSeek && main.HideAndSeekKillDelayTimer > 0) {
                Logger.info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。");
                return false;
            }
            if (main.isSidekick(__instance))
            {
                var ImpostorCount = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Impostor &&
                         !pc.Data.IsDead) ImpostorCount++;
                }
                Logger.SendToFile("ImpostorCount: " + ImpostorCount);
                if (ImpostorCount > 0)
                {
                    Logger.SendToFile(__instance.name + "はSidekickだったので、キルはキャンセルされました。");
                    return false;
                }
                else
                    Logger.SendToFile(__instance.name + "はSidekickですが、他のインポスターがいないのでキルが許可されました。");
            }
            //###############
            //#####DEBUG#####
            //###############
            if(main.AmDebugger.Value) {
                Logger.SendInGame("GuardAndKillAsync");
                __instance.RpcGuardAndKill(target);
                return false;
            }
            if(main.isMadGuardian(target)) {
                var isTaskFinished = true;
                foreach(var task in target.Data.Tasks) {
                    if(!task.Complete) {
                        isTaskFinished = false;
                        break;
                    }
                }
                if(isTaskFinished) {
                    __instance.RpcProtectPlayer(target, 0);
                    __instance.RpcMurderPlayer(target);
                    if(main.MadGuardianCanSeeBarrier) {
                        //MadGuardian視点用
                        target.RpcProtectPlayer(target, 0);
                        target.RpcMurderPlayer(target);
                    }
                    return false;
                }
            }
            if (main.isVampire(__instance) && !main.isBait(target))
            { //キルキャンセル&自爆処理
                __instance.RpcProtectPlayer(target, 0);
                __instance.RpcMurderPlayer(target);
                main.BitPlayers.Add(target.PlayerId, (__instance.PlayerId, 0f));
                return false;
            }

            __instance.RpcMurderPlayer(target);
            if (main.isFixedCooldown)
            {
                __instance.RpcProtectPlayer(target, 0);
                __instance.RpcMurderPlayer(target);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (main.IsHideAndSeek) return false;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (main.SyncButtonMode && target == null)
            {
                Logger.SendToFile("最大:" + main.SyncedButtonCount + ", 現在:" + main.UsedButtonCount, LogLevel.Message);
                if (main.SyncedButtonCount <= main.UsedButtonCount)
                {
                    Logger.SendToFile("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", LogLevel.Message);
                    return false;
                }
                else main.UsedButtonCount++;
                if (main.SyncedButtonCount == main.UsedButtonCount)
                {
                    Logger.SendToFile("使用可能ボタン回数が最大数に達したため、ボタンクールダウンが1時間に設定されました。");
                    PlayerControl.GameOptions.EmergencyCooldown = 3600;
                    PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
                }
            }

            foreach (var bp in main.BitPlayers)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (bp.Key == pc.PlayerId && !pc.Data.IsDead)
                    {
                        pc.RpcMurderPlayer(pc);
                        main.PlaySoundRPC(bp.Value.Item1, Sounds.KillSound);
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "を自爆させました。");
                    }
                    else
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "はすでに死んでいました。");
                }
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();

            if(__instance.Data.IsDead) return true;
            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            if(main.SyncButtonMode && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead) {
                //SyncButtonMode中にホストが死んでいる場合
                ChangeLocalNameAndRevert(
                    "緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。",
                    1000
                );
            }

            return true;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time) {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetName(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetName(revertName);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if(main.AmDebugger.Value && Input.GetKey(KeyCode.J)) {
                __instance.RpcProtectPlayer(__instance, 0);
                __instance.RpcMurderPlayer(__instance);
            }
            if(main.AmDebugger.Value && Input.GetKey(KeyCode.K)) {
                __instance.RpcGuardAndKill();
            }
            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                //Vampireの処理
                if (main.BitPlayers.ContainsKey(__instance.PlayerId))
                {
                    //__instance：キルされる予定のプレイヤー
                    //main.BitPlayers[__instance.PlayerId].Item1：キルしたプレイヤーのID
                    //main.BitPlayers[__instance.PlayerId].Item2：キルするまでの秒数
                    if (main.BitPlayers[__instance.PlayerId].Item2 >= main.VampireKillDelay)
                    {
                        byte vampireID = main.BitPlayers[__instance.PlayerId].Item1;
                        if (!__instance.Data.IsDead)
                        {
                            __instance.RpcMurderPlayer(__instance);
                            main.PlaySoundRPC(vampireID, Sounds.KillSound);
                            Logger.SendToFile("Vampireに噛まれている" + __instance.name + "を自爆させました。");
                        }
                        else
                            Logger.SendToFile("Vampireに噛まれている" + __instance.name + "はすでに死んでいました。");
                        main.BitPlayers.Remove(__instance.PlayerId);
                    }
                    else
                    {
                        main.BitPlayers[__instance.PlayerId] =
                        (main.BitPlayers[__instance.PlayerId].Item1, main.BitPlayers[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                    }
                }
                if(__instance.AmOwner) main.ApplySuffix();
            }
            if(main.IsHideAndSeek && main.IgnoreVent) {
                if(__instance.inVent) __instance.MyPhysics.RpcBootFromVent(0);
            }
            //各クライアントが全員分実行
            //役職テキストの表示
            var RoleTextTransform = __instance.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null)
            {
                var RoleTextData = main.GetRoleText(__instance.Data.Role.Role);
                if(main.IsHideAndSeek) {
                    var hasRole = main.HideAndSeekRoleList.TryGetValue(__instance.PlayerId, out var role);
                    if(hasRole) RoleTextData = main.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                }
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if (__instance.AmOwner) RoleText.enabled = true;
                else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleText.enabled = true;
                else RoleText.enabled = false;
                if (!AmongUsClient.Instance.IsGameStarted &&
                AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                    RoleText.enabled = false;
                if (!__instance.AmOwner && main.VisibleTasksCount && main.hasTasks(__instance.Data, false))
                    RoleText.text += " <color=#e6b422>(" + main.getTaskText(__instance.Data.Tasks) + ")</color>";
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.nameText);
            roleText.transform.SetParent(__instance.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.175f, 0f);
            roleText.fontSize = 0.55f;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = "RoleTextMeeting";
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enabled = false;
            }
            if (main.SyncButtonMode)
            {
                if(AmongUsClient.Instance.AmHost) PlayerControl.LocalPlayer.RpcSetName("test");
                main.SendToAll("緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。");
                Logger.SendToFile("緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。", LogLevel.Message);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                var RoleTextMeetingTransform = pva.NameText.transform.Find("RoleTextMeeting");
                var RoleTextMeeting = RoleTextMeetingTransform.GetComponent<TMPro.TextMeshPro>();
                if (RoleTextMeeting != null)
                {
                    var pc = PlayerControl.AllPlayerControls.ToArray()
                        .Where(pc => pc.PlayerId == pva.TargetPlayerId)
                        .FirstOrDefault();
                    if (pc == null) return;

                    var RoleTextData = main.GetRoleText(pc.Data.Role.Role);
                    RoleTextMeeting.text = RoleTextData.Item1;
                    if (main.VisibleTasksCount && main.hasTasks(pc.Data, false)) RoleTextMeeting.text += " <color=#e6b422>(" + main.getTaskText(pc.Data.Tasks) + ")</color>";
                    RoleTextMeeting.color = RoleTextData.Item2;
                    if (pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) RoleTextMeeting.enabled = true;
                    else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleTextMeeting.enabled = true;
                    else RoleTextMeeting.enabled = false;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl),nameof(PlayerControl.SetColor))]
    class SetColorPatch {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor) {
            //色変更バグ対策
            if(!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if(AmongUsClient.Instance.IsGameStarted && main.IsHideAndSeek) {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance);
            }
            return true;
        }
    }
}
