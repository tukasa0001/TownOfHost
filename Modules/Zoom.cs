using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE;

// 来源：https://github.com/tugaru1975/TownOfPlus/TOPmods/Zoom.cs 
// 参考：https://github.com/Yumenopai/TownOfHost_Y
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class Zoom
{
    public static void Postfix()
    {
        //if (PlayerControl.LocalPlayer.Is(RoleType.Impostor) && Options.OperateVisibilityImpostor.GetBool()) return;
        if ((GameStates.IsShip || GameStates.IsLobby) && !GameStates.IsMeeting && GameStates.IsCanMove)
        {
            if (Input.mouseScrollDelta.y > 0)
            {
                if (Camera.main.orthographicSize > 3.0f)
                {
                    SetZoomSize(times: false);
                }

            }
            if (Input.mouseScrollDelta.y < 0)
            {
                if (GameStates.IsDead || GameStates.IsFreePlay || DebugModeManager.AmDebugger || GameStates.IsLobby || 
                    Utils.CanUseDevCommand(PlayerControl.LocalPlayer))
                {
                    if (Camera.main.orthographicSize < 18.0f)
                    {
                        SetZoomSize(times: true);
                    }
                }
            }
            Flag.NewFlag("Zoom");
        }
        else
        {
            Flag.Run(() =>
            {
                SetZoomSize(reset: true);
            }, "Zoom");
        }
    }

    public static GameObject ShadowQuad;

    private static void SetZoomSize(bool times = false, bool reset = false)
    {
        var size = 1.5f;
        if (!times) size = 1 / size;
        if ((ShadowQuad = GameObject.Find("ShadowQuad")) != null) ShadowQuad.SetActive(!times || reset);
        if (reset)
        {
            Camera.main.orthographicSize = 3.0f;
            HudManager.Instance.UICamera.orthographicSize = 3.0f;
            HudManager.Instance.Chat.transform.localScale = Vector3.one;
            if (GameStates.IsMeeting) MeetingHud.Instance.transform.localScale = Vector3.one;
        }
        else
        {
            Camera.main.orthographicSize *= size;
            HudManager.Instance.UICamera.orthographicSize *= size;
        }
        ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height);
    }
}

public static class Flag
{
    private static readonly List<string> OneTimeList = new();
    private static readonly List<string> FirstRunList = new();
    public static void Run(Action action, string type, bool firstrun = false)
    {
        if (OneTimeList.Contains(type) || (firstrun && !FirstRunList.Contains(type)))
        {
            if (!FirstRunList.Contains(type)) FirstRunList.Add(type);
            OneTimeList.Remove(type);
            action();
        }

    }
    public static void NewFlag(string type)
    {
        if (!OneTimeList.Contains(type)) OneTimeList.Add(type);
    }

    public static void DeleteFlag(string type)
    {
        if (OneTimeList.Contains(type)) OneTimeList.Remove(type);
    }
}