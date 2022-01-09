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
            if (!target.Data.IsDead)
                return;
            //When Bait is killed
            if (target.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Bait && AmongUsClient.Instance.AmHost
            && __instance.PlayerId != target.PlayerId)
            {
                Thread.Sleep(150);
                __instance.CmdReportDeadBody(target.Data);
            }
            else
            //Terrorist
            if (main.isTerrorist(target))
            {
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
            if (main.isSidekick(__instance))
            {
                var ImpostorCount = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Impostor &&
                         !pc.Data.IsDead) ImpostorCount++;
                }
                if (ImpostorCount > 0) return false;
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
        public static bool Prefix(PlayerControl __instance)
        {
            if (main.IsHideAndSeek) return false;
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var bp in main.BitPlayers)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (bp.Key == pc.PlayerId && !pc.Data.IsDead)
                        {
                            pc.RpcMurderPlayer(pc);
                            main.PlaySoundRPC(bp.Value.Item1, Sounds.KillSound);
                        }
                    }
                }
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (main.BitPlayers.ContainsKey(__instance.PlayerId))
                {
                    //__instance：キルされる予定のプレイヤー
                    //main.BitPlayers[__instance.PlayerId].Item1：キルしたプレイヤーのID
                    //main.BitPlayers[__instance.PlayerId].Item2：キルするまでの秒数
                    if (main.BitPlayers[__instance.PlayerId].Item2 >= 10)
                    {
                        byte vampireID = main.BitPlayers[__instance.PlayerId].Item1;
                        if (!__instance.Data.IsDead)
                        {
                            __instance.RpcMurderPlayer(__instance);
                            main.PlaySoundRPC(vampireID, Sounds.KillSound);
                        }
                        main.BitPlayers.Remove(__instance.PlayerId);
                    }
                    else
                    {
                        main.BitPlayers[__instance.PlayerId] =
                        (main.BitPlayers[__instance.PlayerId].Item1, main.BitPlayers[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                    }
                }
            }
            var RoleTextTransform = __instance.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if(RoleText != null) {
                var RoleTextData = main.GetRoleText(__instance.Data.Role.Role);
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if(__instance.AmOwner) RoleText.enabled = true;
                else if(main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleText.enabled = true;
                else RoleText.enabled = false;
                if(!AmongUsClient.Instance.IsGameStarted &&
                AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                RoleText.enabled = false;
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch {
        public static void Postfix(PlayerControl __instance) {
            var roleText = UnityEngine.Object.Instantiate(__instance.nameText);
            roleText.transform.SetParent(__instance.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f,0.175f,0f);
            roleText.fontSize = 0.55f;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch {
        public static void Postfix(MeetingHud __instance) {
            foreach(var pva in __instance.playerStates) {
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f,-0.18f,0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = "RoleTextMeeting";
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enabled = false;
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch {
        public static void Postfix(MeetingHud __instance) {
            foreach(var pva in __instance.playerStates) {
                var RoleTextMeetingTransform = pva.NameText.transform.Find("RoleTextMeeting");
                var RoleTextMeeting = RoleTextMeetingTransform.GetComponent<TMPro.TextMeshPro>();
                if(RoleTextMeeting != null) {
                    var pc = PlayerControl.AllPlayerControls.ToArray()
                        .Where(pc => pc.PlayerId == pva.TargetPlayerId)
                        .FirstOrDefault();
                    if(pc == null) return;
                    
                    var RoleTextData = main.GetRoleText(pc.Data.Role.Role);
                    RoleTextMeeting.text = RoleTextData.Item1;
                    if(main.VisibleTasksCount) RoleTextMeeting.text += " <color=#e6b422>(" + main.getTaskText(pc.myTasks) + ")</color>";
                    RoleTextMeeting.color = RoleTextData.Item2;
                    if(pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) RoleTextMeeting.enabled = true;
                    if(main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleTextMeeting.enabled = true;
                    else RoleTextMeeting.enabled = false;
                }
            }
        } 
    }
}