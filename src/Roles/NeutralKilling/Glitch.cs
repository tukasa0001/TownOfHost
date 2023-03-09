using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Victory;
using UnityEngine;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

public class Glitch: Morphling
{
    public GlitchMode Mode = GlitchMode.Killing;
    private GlitchMode previousMode = GlitchMode.Killing;
    private HnSImpostorScreamSfx ImpostorScreamSfx;
    public bool AllowHuntMode = false;

    protected override void Setup(PlayerControl player)
    {
        $"Setting up {RoleName} with player: {player.GetRawName()}".DebugLog();
        GameObject gameObject = GameObject.FindObjectsOfType<GameObject>().ToArray().ToList()
            .FirstOrDefault(obj => obj.GetComponent<HnSImpostorScreamSfx>() != null);
        if (gameObject != null)
        {
            ImpostorScreamSfx = gameObject.GetComponent<HnSImpostorScreamSfx>();
        }

        Game.GetWinDelegate().AddSubscriber(GlitchWinCondition);


        base.Setup(player);
    }

    private void GlitchWinCondition(WinDelegate winDelegate)
    {
        // idk some dumb code about why glitch should specifically win

        winDelegate.SetWinners(new List<PlayerControl> { MyPlayer });
        winDelegate.ForceGameWin();
    }






    /*[RoleAction(RoleActionType.RoundStart)]
    public void EnterKillModeOnRoundStart() => EnterKillMode();


    [RoleAction(RoleActionType.OnPet)]
    public void SwapGlitchModes()
    {
        "Swapping Glitch Mode".DebugLog();
        this.MyPlayer.name.DebugLog("My player: s");
        this.MyPlayer.MyPhysics.SetBodyType(PlayerBodyTypes.Normal);
        switch (Mode)
        {
            case GlitchMode.Killing:
                EnterHackMode();
                break;
            case GlitchMode.Hacking:
                EnterKillMode();
                break;
            case GlitchMode.Hunting when previousMode is GlitchMode.Hacking:
                EnterHackMode();
                break;
            case GlitchMode.Hunting:
                EnterKillMode();
                break;
        }
    }*/

    /*[RoleAction(RoleActionType.Shapeshift)]
    public void ShapeshiftIntoHunter()
    {
        MyPlayer.Shapeshift(MyPlayer, true);
        MyPlayer.RpcShapeshift(MyPlayer, true);
        ImpostorScreamSfx.LocalImpostorScream();
        EnterHuntMode();
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    public void ShapeshiftOutHunter()
    {
        "Unshapeshifted".DebugLog();
        SwapGlitchModes();
    }*/

    [RoleAction(RoleActionType.AttemptKill)]
    public void OnTargetPlayer(PlayerControl target)
    {
        "trying to kill".DebugLog();
        if (this.CheckInteractions(target.GetCustomRole(), target, Mode) is InteractionResult.Halt) return;
        var _ =Mode switch
        {
            GlitchMode.Killing or GlitchMode.Hunting => AttemptKillPlayer(target),
            GlitchMode.Hacking => AttemptHackPlayer(target),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /*private void EnterKillMode()
    {
        Main.IsHackMode = false;
        previousMode = Mode;
        RpcV2.Immediate((byte)MyPlayer.NetId, (byte)RpcCalls.SetHatStr).Write("hat_pkHW01_Horns").Send(MyPlayer.GetClientId());
        Mode = GlitchMode.Killing;
    }

    private void EnterHackMode()
    {
        Main.IsHackMode = true;
        previousMode = Mode;
        RpcV2.Immediate((byte)MyPlayer.NetId, (byte)RpcCalls.SetHatStr).Write("hat_pk04_Vagabond").Send(MyPlayer.GetClientId());
        Mode = GlitchMode.Hacking;
    }

    private void EnterHuntMode()
    {
        Main.IsHackMode = false;
        previousMode = Mode;
        new GameObject("Scream").AddComponent<HnSImpostorScreamSfx>().LocalImpostorScream();
        this.MyPlayer.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
        Mode = GlitchMode.Hunting;
    }*/

    private bool AttemptKillPlayer(PlayerControl target)
    {
        MyPlayer.RpcMurderPlayer(target);
        return true;
    }

    private bool AttemptHackPlayer(PlayerControl target)
    {
        throw new NotImplementedException("Implement hack");
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).Color(Color.green);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleName("The Glitch")
            .Factions(Faction.Solo)
            .VanillaRole(RoleTypes.Shapeshifter)
            .SpecialType(SpecialType.NeutralKilling)
            .RoleColor(Color.green);

    public enum GlitchMode
    {
        Killing,
        Hacking,
        Hunting,
    }
}